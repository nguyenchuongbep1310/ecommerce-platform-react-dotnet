using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.Services;

namespace ProductCatalogService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IElasticsearchService elasticsearchService,
        ILogger<SearchController> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    /// <summary>
    /// Search products with full-text search and filters
    /// </summary>
    /// <param name="q">Search query (searches in name and description)</param>
    /// <param name="category">Filter by category</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <param name="inStock">Filter for in-stock products only</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <returns>Search results with pagination</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResultDto>> Search(
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool? inStock,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var (products, totalCount) = await _elasticsearchService.SearchProductsAsync(
                q, category, minPrice, maxPrice, inStock, page, pageSize, cancellationToken);

            var result = new SearchResultDto
            {
                Products = products,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                Query = q,
                Filters = new SearchFiltersDto
                {
                    Category = category,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    InStockOnly = inStock
                }
            };

            _logger.LogInformation(
                "Search completed: query='{Query}', category='{Category}', results={Count}/{Total}",
                q, category, products.Count, totalCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during product search");
            return StatusCode(500, new { error = "An error occurred during search" });
        }
    }

    /// <summary>
    /// Get autocomplete suggestions for product names
    /// </summary>
    /// <param name="prefix">Search prefix</param>
    /// <param name="limit">Maximum number of suggestions (default: 10)</param>
    /// <returns>List of product name suggestions</returns>
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetSuggestions(
        [FromQuery] string prefix,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return Ok(new List<string>());
            }

            if (limit < 1) limit = 10;
            if (limit > 50) limit = 50;

            var suggestions = await _elasticsearchService.GetSuggestionsAsync(
                prefix, limit, cancellationToken);

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return StatusCode(500, new { error = "An error occurred getting suggestions" });
        }
    }

    /// <summary>
    /// Get category statistics
    /// </summary>
    /// <returns>Dictionary of categories with product counts</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(Dictionary<string, long>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, long>>> GetCategoryStats(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _elasticsearchService.GetCategoryAggregationsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category statistics");
            return StatusCode(500, new { error = "An error occurred getting category statistics" });
        }
    }
}

// DTOs
public class SearchResultDto
{
    public List<Models.ProductDocument> Products { get; set; } = new();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string? Query { get; set; }
    public SearchFiltersDto Filters { get; set; } = new();
}

public class SearchFiltersDto
{
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStockOnly { get; set; }
}
