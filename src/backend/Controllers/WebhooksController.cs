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
                // Повертаємо 200, щоб Stripe не робив повторних спроб
                return Ok();
            }
            try
            {
                var result = await _paymentService.HandleWebhookAsync(json, stripeSignature!, null);
                if (!result)
                {
                    _logger.LogError("Stripe webhook processed with errors");
                }
            }
            catch (Exception ex)
            {
                // Помилки логуються, але відповідь завжди 200 OK, щоб Stripe не робив ретраїв
                _logger.LogError(ex, "Unexpected error while handling Stripe webhook");
            }
            return Ok();
        }
    }
}
