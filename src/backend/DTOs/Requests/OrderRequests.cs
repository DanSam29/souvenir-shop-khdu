using System.ComponentModel.DataAnnotations;

namespace KhduSouvenirShop.API.DTOs.Requests
{
    /// <summary>
    /// Вхідний DTO для оформлення замовлення.
    /// </summary>
    public class CheckoutRequest
    {
        [Required]
        [MaxLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string WarehouseNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? CityRef { get; set; }

        [MaxLength(100)]
        public string? WarehouseRef { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(50)]
        public string? PromoCode { get; set; }
    }

    /// <summary>
    /// Вхідний DTO для попереднього розрахунку вартості замовлення.
    /// </summary>
    public class CalculateOrderRequest
    {
        [MaxLength(50)]
        public string? PromoCode { get; set; }

        [MaxLength(100)]
        public string? CityRef { get; set; }
    }

    /// <summary>
    /// Вхідний DTO для зміни статусу замовлення (адміністративна дія).
    /// </summary>
    public class UpdateOrderStatusRequest
    {
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }
    }
}
