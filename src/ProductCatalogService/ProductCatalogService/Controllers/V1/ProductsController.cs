using Microsoft.AspNetCore.Mvc;
using MediatR;
using ProductCatalogService.Application.Queries.GetProducts;
using ProductCatalogService.Application.Queries.GetProductById;
using ProductCatalogService.Application.Commands.ReduceStock;
using Asp.Versioning;

namespace ProductCatalogService.Controllers.V1;

/// <summary>
/// Product Catalog API V1 - Original implementation
/// </summary>
[ApiController]
[ApiVersion("1.0")]
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
    /// Get all products with optional filtering
    /// </summary>
    /// <param name="search">Search term for product name or description</param>
    /// <param name="category">Filter by category</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of products</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        CancellationToken cancellationToken)
    {
        var query = new GetProductsQuery(search, category, minPrice, maxPrice);
        var products = await _mediator.Send(query, cancellationToken);
        return Ok(products);
    }

    /// <summary>
    /// Get a single product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(int id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var product = await _mediator.Send(query, cancellationToken);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        return Ok(product);
    }

    /// <summary>
    /// Get product price by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product price</returns>
    [HttpGet("{id}/price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductPrice(int id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var product = await _mediator.Send(query, cancellationToken);

        if (product == null)
        {
            return NotFound(new { Message = $"Product with ID {id} not found" });
        }

        return Ok(product.Price);
    }

    /// <summary>
    /// Reduce product stock (used by Order Service)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Stock reduction request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/reduce-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReduceStock(
        int id,
        [FromBody] ReduceStockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReduceStockCommand(id, request.Quantity);
        var success = await _mediator.Send(command, cancellationToken);

        if (!success)
        {
            return BadRequest(new { Message = "Failed to reduce stock. Check product availability and quantity." });
        }

        return Ok(new { Message = "Stock updated successfully." });
    }
}

/// <summary>
/// Request model for reducing stock
/// </summary>
public record ReduceStockRequest(int Quantity);
