namespace KhduSouvenirShop.API.DTOs.Responses
{
    /// <summary>
    /// Вихідний DTO з прибутковою накладною (склад).
    /// </summary>
    public class IncomingDocumentResponse
    {
        public int DocumentId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public DateTime DocumentDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Вихідний DTO з видатковою накладною (склад).
    /// </summary>
    public class OutgoingDocumentResponse
    {
        public int DocumentId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public int? OrderId { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal? FinalPrice { get; set; }
        public DateTime DocumentDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Вихідний DTO зі станом залишків товару на складі.
    /// </summary>
    public class StockItemResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int TotalIncoming { get; set; }
        public int TotalOutgoing { get; set; }
    }
}
