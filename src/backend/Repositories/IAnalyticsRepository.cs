namespace KhduSouvenirShop.API.Repositories
{
    /// <summary>
    /// Результат агрегації продажів по товару для аналітичних звітів.
    /// </summary>
    public record ProductSalesRecord(int ProductId, string ProductName, int QuantitySold, decimal Revenue);

    /// <summary>
    /// Кількість замовлень за конкретним статусом.
    /// </summary>
    public record StatusCountRecord(string Status, int Count);

    /// <summary>
    /// Точка часового ряду продажів (Period — "yyyy-MM-dd" для днів, "yyyy-MM" для місяців).
    /// </summary>
    public record TimeSeriesRecord(string Period, decimal Amount, int Count);

    /// <summary>
    /// Виручка за категорією товарів.
    /// </summary>
    public record CategorySalesRecord(int CategoryId, string CategoryName, decimal Revenue);

    /// <summary>
    /// Кількість оплат за способом оплати.
    /// </summary>
    public record PaymentMethodCountRecord(string Method, int Count);

    /// <summary>
    /// Ефективність акції: кількість замовлень, сума знижки та виручка.
    /// </summary>
    public record PromotionUsageRecord(int PromotionId, string Name, int Uses, decimal TotalDiscountAmount, decimal Revenue);

    public interface IAnalyticsRepository
    {
        // База продажів — оплачені замовлення (Paid/Shipped/Delivered); виручка = TotalAmount - ShippingCost.
        Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null);
        Task<int> GetOrdersCountAsync(DateTime? from = null, DateTime? to = null);
        Task<decimal> GetAverageOrderValueAsync(DateTime? from = null, DateTime? to = null);
        Task<IReadOnlyList<ProductSalesRecord>> GetTopSellingProductsAsync(int top, DateTime? from = null, DateTime? to = null);
        Task<IReadOnlyList<StatusCountRecord>> GetOrdersCountByStatusAsync(DateTime? from = null, DateTime? to = null);
        Task<IReadOnlyList<TimeSeriesRecord>> GetSalesByDayAsync(DateTime? from = null, DateTime? to = null);
        Task<IReadOnlyList<TimeSeriesRecord>> GetSalesByMonthAsync(DateTime? from = null, DateTime? to = null);
        Task<IReadOnlyList<CategorySalesRecord>> GetTopCategoriesAsync(int top, DateTime? from = null, DateTime? to = null);
        Task<IReadOnlyList<PaymentMethodCountRecord>> GetPaymentMethodStatsAsync(DateTime? from = null, DateTime? to = null);
        Task<IReadOnlyList<PromotionUsageRecord>> GetPromotionStatsAsync(DateTime? from = null, DateTime? to = null);
        Task<decimal> GetTotalPurchaseCostAsync(DateTime? from = null, DateTime? to = null);
    }
}
