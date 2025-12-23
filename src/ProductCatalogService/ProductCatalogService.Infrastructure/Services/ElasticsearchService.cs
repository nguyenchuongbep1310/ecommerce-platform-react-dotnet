using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using ProductCatalogService.Application.Common.Interfaces;
using ProductCatalogService.Domain.Entities;

namespace ProductCatalogService.Infrastructure.Services;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private const string IndexName = "products";

    public ElasticsearchService(ElasticsearchClient client, ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task InitializeIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if index exists
            var existsResponse = await _client.Indices.ExistsAsync(IndexName, cancellationToken);

            if (existsResponse.Exists)
            {
                _logger.LogInformation("Elasticsearch index '{IndexName}' already exists", IndexName);
                return;
            }

            // Create index with mappings
            var createResponse = await _client.Indices.CreateAsync(IndexName, c => c
                .Mappings(m => m
                    .Properties<ProductDocument>(p => p
                        .IntegerNumber(n => n.Id)
                        .Text(t => t.Name!)
                        .Text(t => t.Description!)
                        .DoubleNumber(n => n.Price)
                        .IntegerNumber(n => n.StockQuantity)
                        .Keyword(k => k.Category!)
                        .Boolean(b => b.InStock)
                        .Date(d => d.IndexedAt)
                    )
                ), cancellationToken);

            if (createResponse.IsValidResponse)
            {
                _logger.LogInformation("Successfully created Elasticsearch index '{IndexName}'", IndexName);
            }
            else
            {
                _logger.LogError("Failed to create Elasticsearch index: {Error}", createResponse.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Elasticsearch index");
            throw;
        }
    }

    public async Task IndexProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = ProductDocument.FromProduct(product);
            var response = await _client.IndexAsync(document, idx => idx.Index(IndexName), cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Failed to index product {ProductId}: {Error}", 
                    product.Id, response.DebugInformation);
            }
            else
            {
                _logger.LogDebug("Successfully indexed product {ProductId}", product.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing product {ProductId}", product.Id);
        }
    }

    public async Task BulkIndexProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = products.Select(ProductDocument.FromProduct).ToList();
            
            if (!documents.Any())
            {
                _logger.LogInformation("No products to index");
                return;
            }

            var bulkResponse = await _client.BulkAsync(b => b
                .Index(IndexName)
                .IndexMany(documents), cancellationToken);

            if (bulkResponse.IsValidResponse)
            {
                _logger.LogInformation("Successfully bulk indexed {Count} products", documents.Count);
            }
            else
            {
                _logger.LogWarning("Bulk index had errors: {Error}", bulkResponse.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing products");
        }
    }

    public async Task<(List<ProductDocument> Products, long TotalCount)> SearchProductsAsync(
        string? searchTerm,
        string? category,
        decimal? minPrice,
        decimal? maxPrice,
        bool? inStockOnly,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mustQueries = new List<Query>();

            // Full-text search on name and description
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                mustQueries.Add(new MultiMatchQuery
                {
                    Query = searchTerm,
                    Fields = new[] { "name^2", "description" }, // Boost name field
                    Fuzziness = new Fuzziness("AUTO"),
                    Operator = Operator.Or
                });
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                mustQueries.Add(new TermQuery("category"!) { Value = category });
            }

            // Price range filter
            if (minPrice.HasValue || maxPrice.HasValue)
            {
                var rangeQuery = new NumberRangeQuery("price"!);
                if (minPrice.HasValue) rangeQuery.Gte = (double)minPrice.Value;
                if (maxPrice.HasValue) rangeQuery.Lte = (double)maxPrice.Value;
                mustQueries.Add(rangeQuery);
            }

            // In stock filter
            if (inStockOnly == true)
            {
                mustQueries.Add(new TermQuery("inStock"!) { Value = true });
            }

            var searchRequest = new SearchRequest(IndexName)
            {
                Query = mustQueries.Any()
                    ? new BoolQuery { Must = mustQueries }
                    : new MatchAllQuery(),
                From = (page - 1) * pageSize,
                Size = pageSize,
                Sort = new List<SortOptions>
                {
                    SortOptions.Score(new ScoreSort { Order = SortOrder.Desc }),
                    SortOptions.Field("name.keyword"!, new FieldSort { Order = SortOrder.Asc })
                }
            };

            var response = await _client.SearchAsync<ProductDocument>(searchRequest, cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Elasticsearch search failed: {Error}", response.DebugInformation);
                return (new List<ProductDocument>(), 0);
            }

            var products = response.Documents.ToList();
            var totalCount = response.Total;

            _logger.LogInformation("Search returned {Count} products out of {Total}", 
                products.Count, totalCount);

            return (products, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return (new List<ProductDocument>(), 0);
        }
    }

    public async Task<List<string>> GetSuggestionsAsync(string prefix, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return new List<string>();
            }

            var searchRequest = new SearchRequest(IndexName)
            {
                Query = new PrefixQuery("name.keyword"!) { Value = prefix! },
                Size = limit
            };

            var response = await _client.SearchAsync<ProductDocument>(searchRequest, cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Failed to get suggestions: {Error}", response.DebugInformation);
                return new List<string>();
            }

            return response.Documents.Select(d => d.Name).Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return new List<string>();
        }
    }

    public async Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DeleteAsync<ProductDocument>(IndexName, productId.ToString(), cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogWarning("Failed to delete product {ProductId}: {Error}", 
                    productId, response.DebugInformation);
            }
            else
            {
                _logger.LogInformation("Successfully deleted product {ProductId} from index", productId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", productId);
        }
    }

    public async Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        // For simplicity, we'll re-index the product
        await IndexProductAsync(product, cancellationToken);
    }

    public async Task<Dictionary<string, long>> GetCategoryAggregationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = new SearchRequest(IndexName)
            {
                Size = 0, // We only want aggregations
                Aggregations = new Dictionary<string, Aggregation>
                {
                    ["categories"] = new TermsAggregation
                    {
                        Field = "category",
                        Size = 100
                    }
                }
            };

            var response = await _client.SearchAsync<ProductDocument>(searchRequest, cancellationToken);

            if (!response.IsValidResponse || response.Aggregations == null)
            {
                _logger.LogWarning("Failed to get category aggregations: {Error}", response.DebugInformation);
                return new Dictionary<string, long>();
            }

            var categoriesAgg = response.Aggregations.GetStringTerms("categories");
            if (categoriesAgg == null)
            {
                return new Dictionary<string, long>();
            }

            return categoriesAgg.Buckets.ToDictionary(
                b => b.Key.ToString() ?? "Unknown",
                b => b.DocCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category aggregations");
            return new Dictionary<string, long>();
        }
    }
}
