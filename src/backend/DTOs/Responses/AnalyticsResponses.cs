namespace KhduSouvenirShop.API.DTOs.Responses
{
    /// <summary>
    /// Вихідний DTO зі зведеною аналітикою продажів.
    /// </summary>
    public class AnalyticsSummaryResponse
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal Profit { get; set; }
        public int OrdersCount { get; set; }
        public decimal AverageCheck { get; set; }
        public List<TopSellingProductResponse> PopularProducts { get; set; } = new();
        public List<DailySalesResponse> SalesByDay { get; set; } = new();
    }

    /// <summary>
    /// Вихідний DTO з товаром-лідером продажів.
    /// </summary>
    public class TopSellingProductResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Вихідний DTO з продажами за день або місяць (Date — "yyyy-MM-dd" чи "yyyy-MM").
    /// </summary>
    public class DailySalesResponse
    {
        public string Date { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// Метрики продажів: кількість, виручка, середній чек і розподіл за статусами.
    /// </summary>
    public class SalesMetricsResponse
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public Dictionary<string, int> OrdersCountByStatus { get; set; } = new();
    }

    /// <summary>
    /// Динаміка продажів у часі — окремо за днями та за місяцями.
    /// </summary>
    public class TimeSeriesResponse
    {
        public List<DailySalesResponse> SalesByDay { get; set; } = new();
        public List<DailySalesResponse> SalesByMonth { get; set; } = new();
    }

    /// <summary>
    /// Вихідний DTO з категорією-лідером продажів.
    /// </summary>
    public class TopCategoryResponse
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Статистика способів оплати: картка, післяплата та частка карткових оплат.
    /// </summary>
    public class PaymentStatsResponse
    {
        public int CardCount { get; set; }
        public int CodCount { get; set; }
        public decimal CardPercent { get; set; }
    }

    /// <summary>
    /// Ефективність акції: кількість використань, сума знижки, виручка та ROI.
    /// </summary>
    public class PromotionStatsResponse
    {
        public int PromotionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Uses { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal Revenue { get; set; }
        public decimal ROI { get; set; }
    }

    /// <summary>
    /// Фінансові показники: виручка, собівартість, валовий прибуток і маржа.
    /// </summary>
    public class FinancialResponse
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalPurchaseCost { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal Margin { get; set; }
    }

    /// <summary>
    /// Звіт про продажі: метрики та часовий ряд із вибраною гранулярністю (день/місяць).
    /// </summary>
    public class SalesReportResponse
    {
        public SalesMetricsResponse Metrics { get; set; } = new();
        public List<DailySalesResponse> Series { get; set; } = new();
    }

    /// <summary>
    /// Короткий зведений звіт для головного дашборду.
    /// </summary>
    public class AnalyticsOverviewResponse
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal Margin { get; set; }
        public PaymentStatsResponse Payments { get; set; } = new();
        public List<DailySalesResponse> SalesByDay { get; set; } = new();
    }
}
