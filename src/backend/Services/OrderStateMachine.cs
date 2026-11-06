namespace KhduSouvenirShop.API.Services
{
    /// <summary>
    /// Статуси замовлення. Значення відповідають рядковим значенням, що зберігаються у БД (orders.Status).
    /// </summary>
    public enum OrderStatus
    {
        Processing,
        Paid,
        Shipped,
        Delivered,
        Cancelled
    }

    /// <summary>
    /// Єдине джерело істини для життєвого циклу замовлення: визначає дозволені переходи між статусами.
    /// Термінальні статуси (Delivered, Cancelled) не мають подальших переходів.
    /// </summary>
    public static class OrderStateMachine
    {
        // Дозволені переходи: Processing/Paid → Shipped → Delivered; скасування — з будь-якого не термінального статусу
        private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
        {
            [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Paid] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new[] { OrderStatus.Delivered, OrderStatus.Cancelled },
            [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

        /// <summary>
        /// Перетворює рядковий статус у <see cref="OrderStatus"/>. Повертає false для невідомих значень.
        /// </summary>
        public static bool TryParse(string? value, out OrderStatus status)
        {
            return Enum.TryParse(value, ignoreCase: false, out status) && Transitions.ContainsKey(status);
        }

        /// <summary>
        /// Перевіряє, чи рядок є відомим статусом замовлення.
        /// </summary>
        public static bool IsKnownStatus(string? value) => TryParse(value, out _);

        /// <summary>
        /// Перевіряє, чи дозволений перехід між двома статусами.
        /// </summary>
        public static bool CanTransition(OrderStatus from, OrderStatus to)
        {
            return Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
        }

        /// <summary>
        /// Перевіряє, чи дозволений перехід між двома рядковими статусами.
        /// </summary>
        public static bool CanTransition(string? from, string? to)
        {
            return TryParse(from, out var fromStatus)
                && TryParse(to, out var toStatus)
                && CanTransition(fromStatus, toStatus);
        }
    }
}
