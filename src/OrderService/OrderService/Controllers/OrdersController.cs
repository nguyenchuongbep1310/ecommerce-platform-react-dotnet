using Microsoft.AspNetCore.Mvc;
using OrderService.Data;
using OrderService.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Shared.Messages.Events;
using System.Security.Claims;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private const string DummyUserId = "chuong";
        private readonly IPublishEndpoint _publishEndpoint;

        public OrdersController(OrderDbContext context, IHttpClientFactory clientFactory, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _clientFactory = clientFactory;
            _publishEndpoint = publishEndpoint;
        }

        // POST: api/Orders/place
        [HttpPost("place")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> PlaceOrder()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            // --- 1. Get Cart Data from ShoppingCartService ---
            var cartClient = _clientFactory.CreateClient("CartClient");
            var cartResponse = await cartClient.GetAsync($"/api/cart/{userId}");

            if (!cartResponse.IsSuccessStatusCode)
            {
                return BadRequest($"Could not retrieve cart data or cart is empty for user '{userId}'.");
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

                var orderPlacedEvent = new OrderPlacedEvent
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    Items = order.Items.Select(oi => new OrderItemEvent
                    {
                        ProductId = oi.ProductId,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice
                    }).ToList()
                };

                await _publishEndpoint.Publish(orderPlacedEvent);

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

        // GET: api/Orders/history
        [HttpGet("history")]
        public async Task<IActionResult> GetOrderHistory()
        {
            // 1. Get the User ID from the JWT token claims
            // NOTE: This assumes the User ID (or unique identifier) is passed in the JWT.
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            // 2. Retrieve all orders for this user, including their items
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Items)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    Items = o.Items.Select(i => new
                    {
                        i.ProductId,
                        i.Quantity,
                        i.UnitPrice
                    })
                })
                .ToListAsync();

            if (!orders.Any())
            {
                return Ok(new { Message = "No orders found for this user." });
            }

            return Ok(orders);
        }
    }
}