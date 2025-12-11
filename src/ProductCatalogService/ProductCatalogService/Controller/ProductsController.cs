using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogService.Data;
using ProductCatalogService.Models;

namespace ProductCatalogService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductDbContext _context;

        public ProductsController(ProductDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            // Simple endpoint to list all products
            return await _context.Products.ToListAsync();
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