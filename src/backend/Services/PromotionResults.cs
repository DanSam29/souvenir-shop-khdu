using KhduSouvenirShop.API.Models;

namespace KhduSouvenirShop.API.Services
{
    /// <summary>
    /// Результат валідації промокоду.
    /// </summary>
    public class PromoCodeValidationResult
    {
        public bool Valid { get; set; }
        public string? Error { get; set; }
        public Promotion? Promotion { get; set; }
    }

    /// <summary>
    /// Розрахунок ціни однієї позиції кошика з урахуванням знижок.
    /// </summary>
    public class CartItemPrice
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        // Ціна за одиницю до знижок
        public decimal OriginalPrice { get; set; }
        public int? AppliedPromotionId { get; set; }
        // Сумарна знижка на позицію (з урахуванням кількості)
        public decimal DiscountAmount { get; set; }
        // Сума позиції після знижок
        public decimal FinalPrice { get; set; }
    }

    /// <summary>
    /// Підсумок розрахунку всього кошика.
    /// </summary>
    public class CartPricingResult
    {
        public List<CartItemPrice> Items { get; set; } = new();
        // Сума без знижок
        public decimal Subtotal { get; set; }
        // Загальна знижка
        public decimal TotalDiscount { get; set; }
        // Фінальна сума до сплати
        public decimal Total { get; set; }
        public int? AppliedPromoCodeId { get; set; }
        public string? PromoCodeError { get; set; }
    }

    /// <summary>
    /// Статистика використання акції.
    /// </summary>
    public class PromotionStatistics
    {
        public int PromotionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentUsage { get; set; }
        public int? UsageLimit { get; set; }
        // Кількість користувачів, яким призначено акцію персонально/за статусом
        public int AssignedUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
