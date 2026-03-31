using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text;
using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Services
{
    public interface INovaPoshtaService
    {
        Task<IEnumerable<NpCity>> GetCitiesAsync(string? findString = null);
        Task<IEnumerable<NpWarehouse>> GetWarehousesAsync(string cityRef, string? findString = null);
        Task<decimal> CalculateDeliveryCostAsync(string cityRef, decimal weight, decimal totalAmount);
    }

    public class NovaPoshtaService : INovaPoshtaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NovaPoshtaService> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.novaposhta.ua/v2.0/json/";

        public NovaPoshtaService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            ILogger<NovaPoshtaService> logger,
            IMemoryCache cache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _cache = cache;
            _apiKey = _configuration["NovaPoshta:ApiKey"] ?? string.Empty;
        }

        public async Task<IEnumerable<NpCity>> GetCitiesAsync(string? findString = null)
        {
            var cacheKey = $"NP_Cities_{findString ?? "ALL"}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<NpCity>? cachedCities))
            {
                return cachedCities!;
            }

            var request = new
            {
                apiKey = _apiKey,
                modelName = "Address",
                calledMethod = "getCities",
                methodProperties = new
                {
                    FindByString = findString,
                    Limit = "50"
                }
            };

            var result = await SendRequestAsync<NpCity>(request);
            
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));
            _cache.Set(cacheKey, result, options);

            return result;
        }

        public async Task<IEnumerable<NpWarehouse>> GetWarehousesAsync(string cityRef, string? findString = null)
        {
            var cacheKey = $"NP_Warehouses_{cityRef}_{findString ?? "ALL"}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<NpWarehouse>? cachedWarehouses))
            {
                return cachedWarehouses!;
            }

            var request = new
            {
                apiKey = _apiKey,
                modelName = "Address",
                calledMethod = "getWarehouses",
                methodProperties = new
                {
                    CityRef = cityRef,
                    FindByString = findString,
                    Limit = "100"
                }
            };

            var result = await SendRequestAsync<NpWarehouse>(request);

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(12));
            _cache.Set(cacheKey, result, options);

            return result;
        }

        public async Task<decimal> CalculateDeliveryCostAsync(string cityRef, decimal weight, decimal totalAmount)
        {
            // Формула розрахунку вартості доставки (спрощена, зазвичай Nova Poshta API надає getDocumentPrice)
            // Узгоджено з планом: "узгодити формулу вартості з вагами Products.weight"
            
            var request = new
            {
                apiKey = _apiKey,
                modelName = "InternetDocument",
                calledMethod = "getDocumentPrice",
                methodProperties = new
                {
                    CitySender = "db045811-310a-11de-b0fb-001517170a1a", // Наприклад, Херсон
                    CityRecipient = cityRef,
                    Weight = weight,
                    ServiceType = "WarehouseWarehouse",
                    Cost = totalAmount,
                    CargoType = "Cargo",
                    SeatsAmount = "1"
                }
            };

            try
            {
                var response = await SendRawRequestAsync(request);
                if (response.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    return data[0].GetProperty("Cost").GetDecimal();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate delivery cost via NP API, using fallback formula");
            }

            // Fallback: базова вартість + за вагу
            decimal baseCost = 70;
            decimal weightCost = weight * 10;
            return Math.Round(baseCost + weightCost, 2);
        }

        private async Task<IEnumerable<T>> SendRequestAsync<T>(object request)
        {
            var jsonResponse = await SendRawRequestAsync(request);
            if (jsonResponse.TryGetProperty("data", out var data))
            {
                return JsonSerializer.Deserialize<IEnumerable<T>>(data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Enumerable.Empty<T>();
            }
            return Enumerable.Empty<T>();
        }

        private async Task<JsonElement> SendRawRequestAsync(object request)
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_apiUrl, content);
            response.EnsureSuccessStatusCode();
            
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonDocument.Parse(responseBody);
            
            if (!jsonDocument.RootElement.TryGetProperty("success", out var success) || !success.GetBoolean())
            {
                var errors = jsonDocument.RootElement.TryGetProperty("errors", out var err) ? err.GetRawText() : "Unknown error";
                _logger.LogError("Nova Poshta API Error: {Errors}", errors);
                throw new Exception($"Nova Poshta API Error: {errors}");
            }

            return jsonDocument.RootElement;
        }
    }

    public class NpCity
    {
        public string Description { get; set; } = string.Empty;
        public string Ref { get; set; } = string.Empty;
        public string AreaDescription { get; set; } = string.Empty;
    }

    public class NpWarehouse
    {
        public string Description { get; set; } = string.Empty;
        public string Ref { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
    }
}