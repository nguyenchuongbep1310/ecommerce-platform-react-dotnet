using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Commands;
using OrderService.Application.Queries;
using MediatR;
using System.Security.Claims;
using System.Text.Json;
using OrderService.DTOs;
using AutoMapper;

namespace OrderService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IMapper _mapper;

        public OrdersController(IMediator mediator, IHttpClientFactory clientFactory, IMapper mapper)
        {
            _mediator = mediator;
            _clientFactory = clientFactory;
            _mapper = mapper;
        }

        // POST: api/Orders/place
        [HttpPost("place")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            // 1. Get Cart Data (Presentation Layer Logic)
            var cartClient = _clientFactory.CreateClient("CartClient");
            
            // Forward the JWT token from the incoming request to the Cart Service
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                cartClient.DefaultRequestHeaders.Add("Authorization", authHeader);
            }

            var cartResponse = await cartClient.GetAsync($"/api/cart/{userId}");

            if (!cartResponse.IsSuccessStatusCode)
            {
                return BadRequest($"Could not retrieve cart data or cart is empty.");
            }

            var cartData = await cartResponse.Content.ReadFromJsonAsync<JsonElement>();
            var cartItemsJson = cartData.GetProperty("items").EnumerateArray();
            decimal total = cartData.GetProperty("totalPrice").GetDecimal();

            if (!cartItemsJson.Any() || total <= 0)
            {
                return BadRequest("Cart is empty.");
            }

            // 2. Map Cart to Command DTO
            var orderItems = cartItemsJson.Select(item => new OrderService.Application.Commands.OrderItemDto(
                item.GetProperty("productId").GetInt32(),
                item.GetProperty("quantity").GetInt32(),
                item.GetProperty("priceAtAddition").GetDecimal()
            )).ToList();

            // 3. Send Command
            try 
            {
                var command = new CreateOrderCommand(userId, orderItems, request.PaymentMethodId);
                var order = await _mediator.Send(command);
                return Ok(new { Message = "Order placed successfully!", OrderId = order.Id });
            }
            catch (Exception ex)
            {
                // In production, log this error
                return StatusCode(500, new { Message = "Order placement failed.", Error = ex.Message });
            }
        }

        // GET: api/Orders/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> GetOrder(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var order = await _mediator.Send(new GetOrderByIdQuery(id));

            if (order == null) return NotFound();
            
            // Basic security check: user can only see their own orders
            // (In a real app, use a proper Policy or Authorization Handler)
            if (userId != null && order.UserId != userId && !User.IsInRole("Admin")) 
            {
                return Forbid();
            }

            return Ok(order);
        }

        // GET: api/Orders/history
        [HttpGet("history")]
        public async Task<IActionResult> GetOrderHistory()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var orders = await _mediator.Send(new GetOrdersByUserIdQuery(userId));
            
            // Use AutoMapper to map Order entities to OrderDto
            var orderDtos = _mapper.Map<List<OrderDto>>(orders);

            return Ok(orderDtos);
        }
    }
}