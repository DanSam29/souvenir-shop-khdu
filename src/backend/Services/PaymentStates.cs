namespace KhduSouvenirShop.API.Services
{
    // Статуси платежу (у БД зберігаються як рядки)
    public enum PaymentStatus
    {
        Pending,
        RequiresAction,
        Completed,
        Failed,
        Cancelled,
        Refunded
    }
    // Способи оплати
    public enum PaymentMethod
    {
        Card,
        CashOnDelivery
    }
    // Довідкові значення та перевірки для платежів
    public static class PaymentStates
    {
        public static bool IsKnownMethod(string? value)
            => Enum.TryParse<PaymentMethod>(value, ignoreCase: false, out _);
        // Успішно оплачений платіж
        public static bool IsPaid(string? status)
            => status == PaymentStatus.Completed.ToString();
        // Оплата при отриманні
        public static bool IsCashOnDelivery(string? method)
            => method == PaymentMethod.CashOnDelivery.ToString();
        // Сума платежу має дорівнювати підсумковій сумі замовлення.
        // Order.TotalAmount уже включає вартість доставки (ShippingCost).
        public static bool IsAmountConsistent(decimal paymentAmount, decimal orderTotalAmount)
            => paymentAmount == orderTotalAmount;
    }
}
