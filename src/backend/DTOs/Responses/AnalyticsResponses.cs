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
    /// Вихідний DTO з продажами за день.
    /// </summary>
    public class DailySalesResponse
    {
        public string Date { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }
}
