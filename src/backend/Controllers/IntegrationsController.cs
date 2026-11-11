using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Manager")]
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
            var stripeEnabled = _configuration.GetValue<bool>("Features:StripeEnabled");
            var stripeKey = !string.IsNullOrEmpty(_configuration["Stripe:SecretKey"]);
            var novaPoshtaEnabled = _configuration.GetValue<bool>("Features:NovaPoshtaEnabled");
            var novaPoshtaKey = !string.IsNullOrEmpty(_configuration["NovaPoshta:ApiKey"]);
            var universityEnabled = _configuration.GetValue<bool>("Features:UniversityEnabled");
            var domainsCount = _configuration.GetSection("University:AllowedDomains").Get<string[]>()?.Length ?? 0;
            var smtpEnabled = !string.IsNullOrEmpty(_configuration["Smtp:Server"]);
            var smtpConfigured = !string.IsNullOrEmpty(_configuration["Smtp:Username"])
                                 && !string.IsNullOrEmpty(_configuration["Smtp:Password"]);

            var status = new
            {
                stripe = new
                {
                    // status: ok (увімкнено і налаштовано), degraded (увімкнено без ключа), disabled (вимкнено)
                    status = ResolveStatus(stripeEnabled, stripeKey),
                    enabled = stripeEnabled,
                    keyConfigured = stripeKey
                },
                novaPoshta = new
                {
                    status = ResolveStatus(novaPoshtaEnabled, novaPoshtaKey),
                    enabled = novaPoshtaEnabled,
                    keyConfigured = novaPoshtaKey
                },
                university = new
                {
                    // Університет не потребує ключа; degraded — якщо не задано жодного домену
                    status = !universityEnabled ? "disabled" : (domainsCount == 0 ? "degraded" : "ok"),
                    enabled = universityEnabled,
                    domainsCount
                },
                smtp = new
                {
                    status = !smtpEnabled ? "disabled" : (smtpConfigured ? "ok" : "degraded"),
                    enabled = smtpEnabled,
                    configured = smtpConfigured
                }
            };
            return Ok(ApiResponse<object>.SuccessResult(status));
        }

        // Обчислення стану інтеграції: disabled — вимкнено, degraded — увімкнено без налаштувань, ok — готово
        private static string ResolveStatus(bool enabled, bool configured) =>
            !enabled ? "disabled" : (configured ? "ok" : "degraded");
    }
}