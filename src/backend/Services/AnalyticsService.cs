using KhduSouvenirShop.API.DTOs.Responses;
using KhduSouvenirShop.API.Repositories;

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
        private readonly IAnalyticsRepository _analyticsRepository;

        public AnalyticsService(IAnalyticsRepository analyticsRepository)
            => _analyticsRepository = analyticsRepository;

        public async Task<SalesMetricsResponse> GetSalesMetricsAsync(DateTime? from = null, DateTime? to = null)
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
        }

        public async Task<TimeSeriesResponse> GetTimeSeriesAsync(DateTime? from = null, DateTime? to = null)
        {
            var byDay = await _analyticsRepository.GetSalesByDayAsync(from, to);
            var byMonth = await _analyticsRepository.GetSalesByMonthAsync(from, to);

            return new TimeSeriesResponse
            {
                SalesByDay = byDay.Select(r => new DailySalesResponse { Date = r.Period, Amount = r.Amount, Count = r.Count }).ToList(),
                SalesByMonth = byMonth.Select(r => new DailySalesResponse { Date = r.Period, Amount = r.Amount, Count = r.Count }).ToList()
            };
        }

        public async Task<List<TopSellingProductResponse>> GetTopProductsAsync(int top = 10, DateTime? from = null, DateTime? to = null)
        {
            var rows = await _analyticsRepository.GetTopSellingProductsAsync(top, from, to);
            return rows.Select(r => new TopSellingProductResponse
            {
                ProductId = r.ProductId,
                Name = r.ProductName,
                Quantity = r.QuantitySold,
                Revenue = r.Revenue
            }).ToList();
        }

        public async Task<List<TopCategoryResponse>> GetTopCategoriesAsync(int top = 10, DateTime? from = null, DateTime? to = null)
        {
            var rows = await _analyticsRepository.GetTopCategoriesAsync(top, from, to);
            return rows.Select(r => new TopCategoryResponse
            {
                CategoryId = r.CategoryId,
                Name = r.CategoryName,
                Revenue = r.Revenue
            }).ToList();
        }

        public async Task<PaymentStatsResponse> GetPaymentStatsAsync(DateTime? from = null, DateTime? to = null)
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
        }

        public async Task<List<PromotionStatsResponse>> GetPromotionStatsAsync(DateTime? from = null, DateTime? to = null)
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
        }

        public async Task<FinancialResponse> GetFinancialAsync(DateTime? from = null, DateTime? to = null)
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
        }
    }
}
