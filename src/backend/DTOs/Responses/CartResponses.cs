namespace KhduSouvenirShop.API.DTOs.Responses
{
    /// <summary>
    /// Вихідний DTO з вмістом кошика.
    /// </summary>
    public class CartResponse
    {
        public int CartId { get; set; }
        public List<CartItemResponse> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// Вихідний DTO з позицією кошика.
    /// Поля ProductPrice та Subtotal обчислюються бізнес-логікою (з урахуванням акцій).
    /// </summary>
    public class CartItemResponse
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductNameEn { get; set; }
        public decimal ProductPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public string? ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal Weight { get; set; }
        public decimal Subtotal { get; set; }
    }
}
