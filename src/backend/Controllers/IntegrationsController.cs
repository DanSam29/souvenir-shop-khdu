using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager")]
    public class IntegrationsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public IntegrationsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("status")]
        public ActionResult GetStatus()
        {
            var status = new
            {
                stripe = new
                {
                    enabled = _configuration.GetValue<bool>("Features:StripeEnabled"),
                    keyConfigured = !string.IsNullOrEmpty(_configuration["Stripe:SecretKey"])
                },
                novaPoshta = new
                {
                    enabled = _configuration.GetValue<bool>("Features:NovaPoshtaEnabled"),
                    keyConfigured = !string.IsNullOrEmpty(_configuration["NovaPoshta:ApiKey"])
                },
                university = new
                {
                    enabled = true, // Завжди увімкнено через імітацію
                    domainsCount = _configuration.GetSection("University:AllowedDomains").Get<string[]>()?.Length ?? 0
                }
            };

            return Ok(ApiResponse<object>.SuccessResult(status));
        }
    }
}