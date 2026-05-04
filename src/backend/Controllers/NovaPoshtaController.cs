using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NovaPoshtaController : ControllerBase
    {
        private readonly INovaPoshtaService _novaPoshtaService;
        private readonly ILogger<NovaPoshtaController> _logger;

        public NovaPoshtaController(INovaPoshtaService novaPoshtaService, ILogger<NovaPoshtaController> logger)
        {
            _novaPoshtaService = novaPoshtaService;
            _logger = logger;
        }

        [HttpGet("cities")]
        public async Task<ActionResult> GetCities([FromQuery] string? q)
        {
            try
            {
                var cities = await _novaPoshtaService.GetCitiesAsync(q);
                return Ok(ApiResponse<object>.SuccessResult(cities));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cities from NP");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося отримати список міст", "IntegrationError"));
            }
        }

        [HttpGet("warehouses")]
        public async Task<ActionResult> GetWarehouses([FromQuery] string cityRef, [FromQuery] string? q)
        {
            try
            {
                var warehouses = await _novaPoshtaService.GetWarehousesAsync(cityRef, q);
                return Ok(ApiResponse<object>.SuccessResult(warehouses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching warehouses from NP for city {CityRef}", cityRef);
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося отримати список відділень", "IntegrationError"));
            }
        }

        [HttpGet("calculate")]
        public async Task<ActionResult> Calculate([FromQuery] string cityRef, [FromQuery] decimal weight, [FromQuery] decimal totalAmount)
        {
            try
            {
                var cost = await _novaPoshtaService.CalculateDeliveryCostAsync(cityRef, weight, totalAmount);
                return Ok(ApiResponse<object>.SuccessResult(new { cost }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating delivery cost from NP");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося розрахувати вартість доставки", "IntegrationError"));
            }
        }
    }
}
