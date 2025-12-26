using Microsoft.AspNetCore.Mvc;
using MediatR;
using ProductCatalogService.Application.Products.Queries.GetProducts;
using ProductCatalogService.Application.Products.Queries.GetProductById;
using ProductCatalogService.Application.Products.Queries.GetCategories;
using ProductCatalogService.Application.Products.Commands.ReduceStock;
using ProductCatalogService.Application.Common.Models;
using Asp.Versioning;

namespace ProductCatalogService.Controllers.V2;

/// <summary>
/// Product Catalog API V2 - Enhanced with pagination, sorting, and additional metadata
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all products with optional filtering, pagination, and sorting (V2 Enhanced)
    /// </summary>
    /// <param name="search">Search term for product name or description</param>
    /// <param name="category">Filter by category</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="sortBy">Sort field (name, price, category)</param>
    /// <param name="sortOrder">Sort order (asc, desc)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of products with metadata</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            return BadRequest(new { Message = "Page number must be greater than 0" });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { Message = "Page size must be between 1 and 100" });
        }

        // Get products with filters
        var query = new GetProductsQuery(search, category, minPrice, maxPrice);
        var result = await _mediator.Send(query, cancellationToken);
        var allProducts = result.Products;

        // Apply sorting
        var sortedProducts = sortBy.ToLower() switch
        {
            "price" => sortOrder.ToLower() == "desc" 
                ? allProducts.OrderByDescending(p => p.Price).ToList()
                : allProducts.OrderBy(p => p.Price).ToList(),
            "category" => sortOrder.ToLower() == "desc"
                ? allProducts.OrderByDescending(p => p.Category).ToList()
                : allProducts.OrderBy(p => p.Category).ToList(),
            _ => sortOrder.ToLower() == "desc"
                ? allProducts.OrderByDescending(p => p.Name).ToList()
                : allProducts.OrderBy(p => p.Name).ToList()
        };

        // Apply pagination
        var totalCount = sortedProducts.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var paginatedProducts = sortedProducts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Return enhanced response with metadata
        var response = new
        {
            Data = paginatedProducts,
            Pagination = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = page > 1,
                HasNext = page < totalPages
            },
            Filters = new
            {
                Search = search,
                Category = category,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                SortOrder = sortOrder
            },
            Meta = new
            {
                Version = "2.0",
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a single product by ID with enhanced metadata (V2)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="includeMetadata">Include additional metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details with metadata</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(
        int id, 
        [FromQuery] bool includeMetadata = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductByIdQuery(id);
        var product = await _mediator.Send(query, cancellationToken);

        if (product == null)
        {
            return NotFound(new 
            { 
                Message = $"Product with ID {id} not found",
                RequestId = HttpContext.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            });
        }

        if (!includeMetadata)
        {
            return Ok(product);
        }

        // Enhanced response with metadata
        var response = new
        {
            Data = product,
            Meta = new
            {
                Version = "2.0",
                Timestamp = DateTime.UtcNow,
                RequestId = HttpContext.TraceIdentifier,
                InStock = product.StockQuantity > 0,
                StockStatus = product.StockQuantity switch
                {
                    0 => "Out of Stock",
                    < 10 => "Low Stock",
                    _ => "In Stock"
                }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get product price by ID with currency support (V2)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="currency">Currency code (USD, EUR, GBP)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product price in specified currency</returns>
    [HttpGet("{id}/price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductPrice(
        int id, 
        [FromQuery] string currency = "USD",
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductByIdQuery(id);
        var product = await _mediator.Send(query, cancellationToken);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        // Simple currency conversion (in production, use real exchange rates)
        var exchangeRates = new Dictionary<string, decimal>
        {
            { "USD", 1.0m },
            { "EUR", 0.85m },
            { "GBP", 0.73m }
        };

        var rate = exchangeRates.GetValueOrDefault(currency.ToUpper(), 1.0m);
        var convertedPrice = product.Price * rate;

        return Ok(new
        {
            ProductId = id,
            Price = convertedPrice,
            Currency = currency.ToUpper(),
            OriginalPrice = product.Price,
            OriginalCurrency = "USD",
            ExchangeRate = rate
        });
    }

    /// <summary>
    /// Reduce product stock with validation and response details (V2)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Stock reduction request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed success status</returns>
    [HttpPost("{id}/reduce-stock")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReduceStock(
        int id,
        [FromBody] ReduceStockRequest request,
        CancellationToken cancellationToken)
    {
        // Get current product state
        var productQuery = new GetProductByIdQuery(id);
        var product = await _mediator.Send(productQuery, cancellationToken);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        var previousStock = product.StockQuantity;

        // Execute stock reduction
        var command = new ReduceStockCommand(id, request.Quantity);
        var success = await _mediator.Send(command, cancellationToken);

        if (!success)
        {
            return BadRequest(new 
            { 
                Message = "Failed to reduce stock. Insufficient quantity available.",
                ProductId = id,
                RequestedQuantity = request.Quantity,
                AvailableStock = previousStock
            });
        }

        // Return detailed response
        return Ok(new 
        { 
            Message = "Stock updated successfully.",
            ProductId = id,
            PreviousStock = previousStock,
            ReducedBy = request.Quantity,
            NewStock = previousStock - request.Quantity,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get product categories with statistics (V2 only)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available categories with statistics</returns>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var query = new GetCategoriesQuery();
        var categories = await _mediator.Send(query, cancellationToken);

        return Ok(new
        {
            Categories = categories,
            TotalCategories = categories.Count,
            Timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Request model for reducing stock
/// </summary>
public record ReduceStockRequest(int Quantity);
