namespace KhduSouvenirShop.API.DTOs.Responses
{
    /// <summary>
    /// Вихідний DTO з детальною інформацією про замовлення.
    /// </summary>
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
        public List<OrderHistoryResponse> History { get; set; } = new();
        public ShippingResponse? Shipping { get; set; }
        public PaymentResponse? Payment { get; set; }
    }

    /// <summary>
    /// Вихідний DTO зі стислою інформацією про замовлення (для списків).
    /// </summary>
    public class OrderSummaryResponse
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Вихідний DTO з позицією замовлення.
    /// </summary>
    public class OrderItemResponse
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductNameEn { get; set; }
        public int Quantity { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
    }

    /// <summary>
    /// Вихідний DTO з історією зміни статусу замовлення.
    /// </summary>
    public class OrderHistoryResponse
    {
        public int HistoryId { get; set; }
        public int OrderId { get; set; }
        public string? OldStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public int? ChangedByUserId { get; set; }
        public string? Comment { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Вихідний DTO з інформацією про платіж.
    /// </summary>
    public class PaymentResponse
    {
        public int PaymentId { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Вихідний DTO з інформацією про доставку.
    /// </summary>
    public class ShippingResponse
    {
        public int ShippingId { get; set; }
        public string City { get; set; } = string.Empty;
        public string WarehouseNumber { get; set; } = string.Empty;
        public string? CityRef { get; set; }
        public string? WarehouseRef { get; set; }
        public string? WarehouseAddress { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string DeliveryMethod { get; set; } = string.Empty;
        public decimal DeliveryCost { get; set; }
        public int? EstimatedDays { get; set; }
    }
}
