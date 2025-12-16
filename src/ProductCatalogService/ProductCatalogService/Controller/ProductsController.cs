using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Data;
using ProductCatalogService.Models;
using Microsoft.Extensions.Caching.Distributed;
// using Microsoft.Extensions.Caching.StackExchangeRedis; // Not strictly needed here, abstraction is enough

namespace ProductCatalogService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductDbContext _context;
        private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache _cache;

        public ProductsController(ProductDbContext context, Microsoft.Extensions.Caching.Distributed.IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice)
        {
            // Create a unique cache key based on all parameters
            string cacheKey = $"products_{search}_{category}_{minPrice}_{maxPrice}";

            // 1. Check Cache
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                // Cache Hit!
                return System.Text.Json.JsonSerializer.Deserialize<List<Product>>(cachedData)!;
            }

            // 2. Cache Miss - Query Database
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()) 
                                      || p.Description.ToLower().Contains(search.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            var products = await query.ToListAsync();

            // 3. Save to Cache (Expiration 60s)
            var cacheOptions = new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
            };
            var serializedData = System.Text.Json.JsonSerializer.Serialize(products);
            await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);

            return products;
        }

        // GET: api/Products/1
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            // Simple endpoint to get a single product by ID
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // For simplicity, we skip POST/PUT/DELETE for MVP, but they would go here.

        // Example for internal check (needed by Cart Service later):
        [HttpGet("{id}/price")]
        public async Task<ActionResult<decimal>> GetProductPrice(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return product.Price;
        }

        // POST: api/Products/1/reduce-stock
        [HttpPost("{id}/reduce-stock")]
        public async Task<IActionResult> ReduceStock(int id, [FromBody] Dictionary<string, int> request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Product not found.");
            }

            if (!request.TryGetValue("quantity", out int quantityToReduce) || quantityToReduce <= 0)
            {
                return BadRequest("Invalid quantity.");
            }

            if (product.StockQuantity < quantityToReduce)
            {
                return BadRequest($"Insufficient stock. Available: {product.StockQuantity}");
            }

            product.StockQuantity -= quantityToReduce;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Stock updated successfully." });
        }
    }
}