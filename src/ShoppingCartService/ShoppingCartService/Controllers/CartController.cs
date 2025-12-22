using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingCartService.Data;
using ShoppingCartService.DTOs;
using ShoppingCartService.Models;
using AutoMapper;

namespace ShoppingCartService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class CartController : ControllerBase
    {
        private readonly CartDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IMapper _mapper;

        public CartController(CartDbContext context, IHttpClientFactory clientFactory, IMapper mapper)
        {
            _context = context;
            _clientFactory = clientFactory;
            _mapper = mapper;
        }

        // Simplification: We use a static dummy User ID for MVP since we haven't integrated auth yet.
        private const string DummyUserId = "chuong";

        // GET: api/Cart/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<CartResponseDto>> GetCart(string userId)
        {
            var cart = await _context
                .Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                // Return an empty cart instead of 404 to provide a better frontend experience
                return Ok(new CartResponseDto 
                { 
                    UserId = userId, 
                    Items = new List<CartItemDto>() 
                });
            }

            // Use AutoMapper to map Cart entity to CartResponseDto
            var cartDto = _mapper.Map<CartResponseDto>(cart);

            // NOTE: In a real scenario, you'd call ProductCatalogService again here to get the current names/images.

            return Ok(cartDto);
        }

        // POST: api/Cart/add
        [HttpPost("add")]
        public async Task<IActionResult> AddItem([FromBody] AddItemRequestDto request)
        {
            // 1. Synchronous Call to ProductCatalogService to get price and validate existence
            var productClient = _clientFactory.CreateClient("ProductClient");
            var response = await productClient.GetAsync($"/api/products/{request.ProductId}/price");

            if (!response.IsSuccessStatusCode)
            {
                // If the product service returns 404 or any other error
                return StatusCode((int)response.StatusCode, "Product validation failed.");
            }

            // Deserialize the price (we rely on the Product Service returning a decimal directly)
            var priceString = await response.Content.ReadAsStringAsync();
            var price = decimal.Parse(priceString.Trim('"')); // Handle JSON quoted string result

            // 2. Find or Create Cart
            var cart = await _context
                .Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == request.UserId);

            if (cart == null)
            {
                cart = new Cart { UserId = request.UserId };
                _context.Carts.Add(cart);
            }

            // 3. Update or Add Item
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                existingItem.PriceAtAddition = price; // Update price just in case
            }
            else
            {
                cart.Items.Add(
                    new CartItem
                    {
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        PriceAtAddition = price,
                        CartId = request.UserId,
                    }
                );
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Item added to cart successfully." });
        }

        // DELETE: api/Cart/{userId}
        [HttpDelete("{userId}")]
        public async Task<IActionResult> ClearCart(string userId)
        {
            var cart = await _context
                .Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                return NotFound("Cart not found.");
            }

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cart cleared successfully." });
        }
    }
}
