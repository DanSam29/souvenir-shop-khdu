using KhduSouvenirShop.API.DTOs.Responses;
using KhduSouvenirShop.API.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace KhduSouvenirShop.API.Services
{
    /// <summary>
    /// Розрахунок аналітичних метрик на основі агрегатів репозиторію.
    /// </summary>
    public interface IAnalyticsService
    {
        Task<SalesMetricsResponse> GetSalesMetricsAsync(DateTime? from = null, DateTime? to = null);
        Task<TimeSeriesResponse> GetTimeSeriesAsync(DateTime? from = null, DateTime? to = null);
        Task<List<TopSellingProductResponse>> GetTopProductsAsync(int top = 10, DateTime? from = null, DateTime? to = null);
        Task<List<TopCategoryResponse>> GetTopCategoriesAsync(int top = 10, DateTime? from = null, DateTime? to = null);
        Task<PaymentStatsResponse> GetPaymentStatsAsync(DateTime? from = null, DateTime? to = null);
        Task<List<PromotionStatsResponse>> GetPromotionStatsAsync(DateTime? from = null, DateTime? to = null);
        Task<FinancialResponse> GetFinancialAsync(DateTime? from = null, DateTime? to = null);
    }

    public class AnalyticsService : IAnalyticsService
    {
        // Ключ версії аналітичного кешу: інкремент робить усі попередні агрегати неактуальними.
        public const string CacheVersionKey = "Analytics_Cache_Version";

        private readonly IAnalyticsRepository _analyticsRepository;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheTtl;

        public AnalyticsService(IAnalyticsRepository analyticsRepository, IMemoryCache cache, IConfiguration configuration)
        {
            _analyticsRepository = analyticsRepository;
            _cache = cache;
            // TTL кешу агрегатів (за замовчуванням 15 хвилин)
            _cacheTtl = TimeSpan.FromMinutes(configuration.GetValue<int?>("Analytics:CacheTtlMinutes") ?? 15);
        }

        // Інвалідація аналітичного кешу — викликається при створенні/зміні замовлень чи документів.
        public static void InvalidateCache(IMemoryCache cache)
            => cache.Set(CacheVersionKey, cache.Get<int>(CacheVersionKey) + 1);

        // Ключ кешу з урахуванням поточної версії; застарілі версії просто витісняються за TTL.
        private string BuildKey(string name, params object?[] args)
            => $"Analytics_{name}_v{_cache.Get<int>(CacheVersionKey)}_{string.Join("_", args)}";

        private async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        {
            if (_cache.TryGetValue(key, out T? cached) && cached is not null)
                return cached;

            var value = await factory();
            _cache.Set(key, value, _cacheTtl);
            return value;
        }

        public Task<SalesMetricsResponse> GetSalesMetricsAsync(DateTime? from = null, DateTime? to = null)
            => GetOrCreateAsync(BuildKey("sales_metrics", from, to), async () =>
            {
                var totalOrders = await _analyticsRepository.GetOrdersCountAsync(from, to);
                var totalRevenue = await _analyticsRepository.GetTotalRevenueAsync(from, to);
                var averageOrderValue = await _analyticsRepository.GetAverageOrderValueAsync(from, to);
                var statuses = await _analyticsRepository.GetOrdersCountByStatusAsync(from, to);

                return new SalesMetricsResponse
                {
                    TotalOrders = totalOrders,
                    TotalRevenue = decimal.Round(totalRevenue, 2),
                    AverageOrderValue = decimal.Round(averageOrderValue, 2),
                    OrdersCountByStatus = statuses.ToDictionary(s => s.Status, s => s.Count)
                };
            });

        public Task<TimeSeriesResponse> GetTimeSeriesAsync(DateTime? from = null, DateTime? to = null)
            => GetOrCreateAsync(BuildKey("time_series", from, to), async () =>
            {
                var byDay = await _analyticsRepository.GetSalesByDayAsync(from, to);
                var byMonth = await _analyticsRepository.GetSalesByMonthAsync(from, to);

                return new TimeSeriesResponse
                {
                    SalesByDay = byDay.Select(r => new DailySalesResponse { Date = r.Period, Amount = r.Amount, Count = r.Count }).ToList(),
                    SalesByMonth = byMonth.Select(r => new DailySalesResponse { Date = r.Period, Amount = r.Amount, Count = r.Count }).ToList()
                };
            });

        public Task<List<TopSellingProductResponse>> GetTopProductsAsync(int top = 10, DateTime? from = null, DateTime? to = null)
            => GetOrCreateAsync(BuildKey("top_products", top, from, to), async () =>
            {
                var rows = await _analyticsRepository.GetTopSellingProductsAsync(top, from, to);
                return rows.Select(r => new TopSellingProductResponse
                {
                    ProductId = r.ProductId,
                    Name = r.ProductName,
                    Quantity = r.QuantitySold,
                    Revenue = r.Revenue
                }).ToList();
            });

        public Task<List<TopCategoryResponse>> GetTopCategoriesAsync(int top = 10, DateTime? from = null, DateTime? to = null)
            => GetOrCreateAsync(BuildKey("top_categories", top, from, to), async () =>
            {
                var rows = await _analyticsRepository.GetTopCategoriesAsync(top, from, to);
                return rows.Select(r => new TopCategoryResponse
                {
                    CategoryId = r.CategoryId,
                    Name = r.CategoryName,
                    Revenue = r.Revenue
                }).ToList();
            });

        public Task<PaymentStatsResponse> GetPaymentStatsAsync(DateTime? from = null, DateTime? to = null)
            => GetOrCreateAsync(BuildKey("payment_stats", from, to), async () =>
            {
                var rows = await _analyticsRepository.GetPaymentMethodStatsAsync(from, to);
                var card = rows.Where(r => r.Method == PaymentMethod.Card.ToString()).Sum(r => r.Count);
                var cod = rows.Where(r => r.Method == PaymentMethod.CashOnDelivery.ToString()).Sum(r => r.Count);
                var total = card + cod;

                return new PaymentStatsResponse
                {
                    CardCount = card,
                    CodCount = cod,
                    CardPercent = total > 0 ? decimal.Round(card * 100m / total, 2) : 0m
                };
            });

        public Task<List<PromotionStatsResponse>> GetPromotionStatsAsync(DateTime? from = null, DateTime? to = null)
            => GetOrCreateAsync(BuildKey("promotion_stats", from, to), async () =>
            {
                var rows = await _analyticsRepository.GetPromotionStatsAsync(from, to);
                return rows.Select(r => new PromotionStatsResponse
                {
                    PromotionId = r.PromotionId,
                    Name = r.Name,
                    Uses = r.Uses,
                    TotalDiscountAmount = r.TotalDiscountAmount,
                    Revenue = r.Revenue,
                    // ROI = (виручка - надана знижка) / надана знижка
                    ROI = r.TotalDiscountAmount > 0
                        ? decimal.Round((r.Revenue - r.TotalDiscountAmount) / r.TotalDiscountAmount, 2)
                        : 0m
                }).ToList();
            });

        public Task<FinancialResponse> GetFinancialAsync(DateTime? from = null, DateTime? to = null)
            => GetOrCreateAsync(BuildKey("financial", from, to), async () =>
            {
                var totalRevenue = await _analyticsRepository.GetTotalRevenueAsync(from, to);
                var totalPurchaseCost = await _analyticsRepository.GetTotalPurchaseCostAsync(from, to);
                var grossProfit = totalRevenue - totalPurchaseCost;

                return new FinancialResponse
                {
                    TotalRevenue = decimal.Round(totalRevenue, 2),
                    TotalPurchaseCost = decimal.Round(totalPurchaseCost, 2),
                    GrossProfit = decimal.Round(grossProfit, 2),
                    // Маржа — частка прибутку у виручці (0..1)
                    Margin = totalRevenue > 0 ? decimal.Round(grossProfit / totalRevenue, 4) : 0m
                };
            });
    }
}
