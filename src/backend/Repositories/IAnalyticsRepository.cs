namespace KhduSouvenirShop.API.Repositories
{
    /// <summary>
    /// Результат агрегації продажів по товару для аналітичних звітів.
    /// </summary>
    public record ProductSalesRecord(int ProductId, string ProductName, int QuantitySold, decimal Revenue);

    public interface IAnalyticsRepository
    {
        Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null);
        Task<int> GetOrdersCountAsync(DateTime? from = null, DateTime? to = null);
        Task<decimal> GetAverageOrderValueAsync(DateTime? from = null, DateTime? to = null);
        Task<IReadOnlyList<ProductSalesRecord>> GetTopSellingProductsAsync(int top, DateTime? from = null, DateTime? to = null);
    }
}
