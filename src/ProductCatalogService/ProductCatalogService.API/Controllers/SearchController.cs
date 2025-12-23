using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.Application.Products.Queries.SearchProducts;
using ProductCatalogService.Application.Products.Queries.GetSuggestions;
using ProductCatalogService.Application.Products.Queries.GetCategoryStats;

namespace ProductCatalogService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IMediator mediator,
        ILogger<SearchController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Search products with full-text search and filters
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchResponseDto>> Search(
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
            var query = new SearchProductsQuery(q, category, minPrice, maxPrice, inStock, page, pageSize);
            var result = await _mediator.Send(query, cancellationToken);

            _logger.LogInformation(
                "Search completed: query='{Query}', category='{Category}', results={Count}/{Total}",
                q, category, result.Products.Count, result.TotalCount);

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
    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetSuggestions(
        [FromQuery] string prefix,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetSuggestionsQuery(prefix, limit);
            var suggestions = await _mediator.Send(query, cancellationToken);

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
    [HttpGet("categories")]
    [ProducesResponseType(typeof(Dictionary<string, long>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Dictionary<string, long>>> GetCategoryStats(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetCategoryStatsQuery();
            var stats = await _mediator.Send(query, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category statistics");
            return StatusCode(500, new { error = "An error occurred getting category statistics" });
        }
    }
}

