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
        Task<decimal> CalculateDeliveryCostAsync(string cityRef, string? warehouseRef, decimal weight, decimal totalAmount);
        Task<NpTrackingStatus> GetTrackingStatusAsync(string trackingNumber);
    }
    public class NovaPoshtaService : INovaPoshtaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NovaPoshtaService> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.novaposhta.ua/v2.0/json/";
        private static readonly TimeSpan CitiesCacheDuration = TimeSpan.FromHours(24);
        private static readonly TimeSpan WarehousesCacheDuration = TimeSpan.FromHours(6);
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
            try
            {
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
                    .SetAbsoluteExpiration(CitiesCacheDuration);
                _cache.Set(cacheKey, result, options);
                return result;
            }
            catch (Exception ex)
            {
                // Graceful fallback: якщо API недоступний, повертаємо порожній список,
                // щоб фронтенд дозволив ручний ввід міста.
                _logger.LogWarning(ex, "Nova Poshta getCities недоступний для пошуку '{Search}'; повертаємо порожній список", findString);
                return Enumerable.Empty<NpCity>();
            }
        }
        public async Task<IEnumerable<NpWarehouse>> GetWarehousesAsync(string cityRef, string? findString = null)
        {
            var cacheKey = $"NP_Warehouses_{cityRef}_{findString ?? "ALL"}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<NpWarehouse>? cachedWarehouses))
            {
                return cachedWarehouses!;
            }
            try
            {
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
                    .SetAbsoluteExpiration(WarehousesCacheDuration);
                _cache.Set(cacheKey, result, options);
                return result;
            }
            catch (Exception ex)
            {
                // Graceful fallback: якщо API недоступний, повертаємо порожній список,
                // щоб фронтенд дозволив ручний ввід відділення.
                _logger.LogWarning(ex, "Nova Poshta getWarehouses недоступний для міста {CityRef}; повертаємо порожній список", cityRef);
                return Enumerable.Empty<NpWarehouse>();
            }
        }
        public async Task<decimal> CalculateDeliveryCostAsync(string cityRef, string? warehouseRef, decimal weight, decimal totalAmount)
        {
            // warehouseRef передається для повноти контракту; API getDocumentPrice розраховує
            // вартість за містом-одержувачем, тому у запиті використовується CityRecipient.
            var request = new
            {
                apiKey = _apiKey,
                modelName = "InternetDocument",
                calledMethod = "getDocumentPrice",
                methodProperties = new
                {
                    CitySender = "db045811-310a-11de-b0fb-001517170a1a", // Херсон
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
                _logger.LogWarning(ex, "Не вдалося розрахувати вартість доставки через NP API (cityRef={CityRef}, warehouseRef={WarehouseRef}), використовуємо fallback-формулу", cityRef, warehouseRef);
            }
            // Fallback: базова вартість + вартість за вагу
            decimal baseCost = 70;
            decimal weightCost = weight * 10;
            return Math.Round(baseCost + weightCost, 2);
        }
        public async Task<NpTrackingStatus> GetTrackingStatusAsync(string trackingNumber)
        {
            var result = new NpTrackingStatus { TrackingNumber = trackingNumber };
            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                result.Available = false;
                return result;
            }
            try
            {
                var request = new
                {
                    apiKey = _apiKey,
                    modelName = "TrackingDocument",
                    calledMethod = "getStatusDocuments",
                    methodProperties = new
                    {
                        Documents = new[]
                        {
                            new { DocumentNumber = trackingNumber, Phone = string.Empty }
                        }
                    }
                };
                var jsonResponse = await SendRawRequestAsync(request);
                if (jsonResponse.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    var document = data[0];
                    result.Status = GetString(document, "Status");
                    result.StatusCode = GetString(document, "StatusCode");
                    result.WarehouseRecipient = GetString(document, "WarehouseRecipient");
                    result.RecipientFullName = GetString(document, "RecipientFullName");
                    var scheduledDate = GetString(document, "ScheduledDeliveryDate");
                    if (DateTime.TryParse(scheduledDate, out var parsedDate))
                    {
                        result.ScheduledDeliveryDate = parsedDate;
                    }
                    result.Available = true;
                }
            }
            catch (Exception ex)
            {
                // Graceful fallback: трекінг недоступний, але сторінка замовлення має працювати.
                _logger.LogWarning(ex, "Nova Poshta tracking недоступний для ТТН {TrackingNumber}", trackingNumber);
                result.Available = false;
            }
            return result;
        }
        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null
                ? value.GetString() ?? string.Empty
                : string.Empty;
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
    public class NpTrackingStatus
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public string WarehouseRecipient { get; set; } = string.Empty;
        public string RecipientFullName { get; set; } = string.Empty;
        public DateTime? ScheduledDeliveryDate { get; set; }
        public bool Available { get; set; } = true;
    }
}
