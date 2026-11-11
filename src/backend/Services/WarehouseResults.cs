namespace KhduSouvenirShop.API.Services
{
    /// <summary>
    /// Агрегований залишок товару (для адмін-панелі складу та експорту).
    /// totalIncoming/totalOutgoing рахуються з документів, currentStock береться з Product.Stock.
    /// </summary>
    public class StockSummaryItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalIncoming { get; set; }
        public int TotalOutgoing { get; set; }
        public int CurrentStock { get; set; }
        public bool LowStock { get; set; }
    }
}
