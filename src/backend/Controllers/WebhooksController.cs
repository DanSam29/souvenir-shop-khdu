using KhduSouvenirShop.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhooksController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(IPaymentService paymentService, ILogger<WebhooksController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost("stripe")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];

            if (string.IsNullOrEmpty(stripeSignature))
            {
                _logger.LogWarning("Missing Stripe-Signature header");
                return BadRequest();
            }

            var result = await _paymentService.HandleWebhookAsync(json, stripeSignature!, null);

            if (!result)
            {
                _logger.LogError("Failed to handle Stripe webhook");
                return BadRequest();
            }

            return Ok();
        }
    }
}
