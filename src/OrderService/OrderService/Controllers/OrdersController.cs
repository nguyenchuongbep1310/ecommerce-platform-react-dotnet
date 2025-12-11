using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.Models;
using System.Text.Json;
using System.Net.Http.Json; 

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private const string DummyUserId = "chuong"; 

        public OrdersController(OrderDbContext context, IHttpClientFactory clientFactory)
        {
            _context = context;
            _clientFactory = clientFactory;
        }

        // POST: api/Orders/place
        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder()
        {
            var userId = DummyUserId;

            // --- 1. Get Cart Data from ShoppingCartService ---
            var cartClient = _clientFactory.CreateClient("CartClient");
            var cartResponse = await cartClient.GetAsync($"/api/cart/cart/{userId}");

            if (!cartResponse.IsSuccessStatusCode)
            {
                return BadRequest("Could not retrieve cart data or cart is empty.");
            }
            
            // Deserialize cart response (using JsonElement for flexible parsing)
            var cartData = await cartResponse.Content.ReadFromJsonAsync<JsonElement>(); 
            var cartItems = cartData.GetProperty("items").EnumerateArray().ToList();
            decimal total = cartData.GetProperty("totalPrice").GetDecimal();
            
            if (!cartItems.Any() || total <= 0)
            {
                return BadRequest("Cart is empty or total is zero.");
            }

            // --- 2. Call PaymentService (Mock) ---
            var paymentClient = _clientFactory.CreateClient("PaymentClient");
            
            // NOTE: The Payment Service container and endpoint are added on Day 7.
            var paymentResponse = await paymentClient.PostAsJsonAsync("/api/payment/process", 
                new { Amount = total, UserId = userId }); 
            
            if (!paymentResponse.IsSuccessStatusCode)
            {
                // Payment failure means we stop immediately
                return StatusCode(500, $"Payment failed with status code: {paymentResponse.StatusCode}");
            }
            
            // --- 3. Finalize Order and Update Inventory (Local Transaction) ---
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order { UserId = userId, TotalAmount = total, Status = "Processing" };
                var productClient = _clientFactory.CreateClient("ProductClient");
                
                // Process each item
                foreach (var cartItem in cartItems)
                {
                    int productId = cartItem.GetProperty("productId").GetInt32();
                    int quantity = cartItem.GetProperty("quantity").GetInt32();
                    decimal unitPrice = cartItem.GetProperty("priceAtAddition").GetDecimal();
                    
                    order.Items.Add(new OrderItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = unitPrice
                    });
                    
                    // Call Product Service to reduce stock
                    var stockResponse = await productClient.PostAsJsonAsync($"/api/products/{productId}/reduce-stock", 
                        new { Quantity = quantity });
                        
                    if (!stockResponse.IsSuccessStatusCode)
                    {
                         // If stock fails, rollback the entire order creation and inventory update
                         throw new Exception($"Stock update failed for Product ID: {productId}. Details: {await stockResponse.Content.ReadAsStringAsync()}");
                    }
                }
                
                // Finalize order record
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // NOTE: Clearing the cart and publishing the event will happen *after* the transaction commits.
                
                return Ok(new { Message = "Order placed successfully!", OrderId = order.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the exception (using ILogger injected via DI)
                return StatusCode(500, $"An error occurred while placing the order: {ex.Message}");
            }
        }
        
        // GET: api/Orders/{id} (For order history/tracking)
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && o.UserId == DummyUserId);
            if (order == null)
            {
                return NotFound();
            }
            return order;
        }
    }
}