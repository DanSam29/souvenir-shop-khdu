using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeaturesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FeaturesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("status")]
        public ActionResult GetPublicStatus()
        {
            var status = new
            {
                stripeEnabled = _configuration.GetValue<bool>("Features:StripeEnabled"),
                novaPoshtaEnabled = _configuration.GetValue<bool>("Features:NovaPoshtaEnabled"),
                universityEnabled = _configuration.GetValue<bool>("Features:UniversityEnabled")
            };

            return Ok(ApiResponse<object>.SuccessResult(status));
        }
    }
}
