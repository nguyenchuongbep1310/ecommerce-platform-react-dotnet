using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Controllers
{
    public record PaymentRequest(decimal Amount, string UserId); 

    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ILogger<PaymentController> logger)
        {
            _logger = logger;
        }

        // POST: api/Payment/process
        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            _logger.LogInformation("Processing payment for User: {UserId}, Amount: {Amount}", request.UserId, request.Amount);
            await Task.Delay(50); 

            // Simulate success for all non-negative amounts
            if (request.Amount <= 0) 
            {
                 _logger.LogWarning("Payment failed due to invalid amount.");
                 return BadRequest(new { Status = "Failed", TransactionId = "N/A" });
            }

            // Mock success response
            return Ok(new 
            { 
                Status = "Success", 
                TransactionId = Guid.NewGuid().ToString() 
            });
        }
    }
}