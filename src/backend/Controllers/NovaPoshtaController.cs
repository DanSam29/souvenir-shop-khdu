using KhduSouvenirShop.API.DTOs.Requests;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KhduSouvenirShop.API.Controllers
{
    // Публічний контролер Nova Poshta: доступний без авторизації (використовується на кроці оформлення замовлення).
    [Route("api/np")]
    [ApiController]
    [EnableRateLimiting("NovaPoshtaPolicy")]
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
        public async Task<ActionResult> GetCities([FromQuery] string? search)
        {
            try
            {
                var cities = await _novaPoshtaService.GetCitiesAsync(search);
                return Ok(ApiResponse<object>.SuccessResult(cities));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cities from NP");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося отримати список міст", "IntegrationError"));
            }
        }
        [HttpGet("warehouses")]
        public async Task<ActionResult> GetWarehouses([FromQuery] string cityRef, [FromQuery] string? search)
        {
            try
            {
                var warehouses = await _novaPoshtaService.GetWarehousesAsync(cityRef, search);
                return Ok(ApiResponse<object>.SuccessResult(warehouses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching warehouses from NP for city {CityRef}", cityRef);
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося отримати список відділень", "IntegrationError"));
            }
        }
        [HttpPost("calculate")]
        public async Task<ActionResult> Calculate([FromBody] CalculateDeliveryRequest request)
        {
            try
            {
                var cost = await _novaPoshtaService.CalculateDeliveryCostAsync(request.CityRef, request.WarehouseRef, request.Weight, request.TotalAmount);
                var estimatedDays = _novaPoshtaService.EstimateDeliveryDays(request.Weight);
                return Ok(ApiResponse<object>.SuccessResult(new { cost, estimatedDays }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating delivery cost from NP");
                return StatusCode(500, ApiResponse<object>.FailureResult("Не вдалося розрахувати вартість доставки", "IntegrationError"));
            }
        }
    }
}
