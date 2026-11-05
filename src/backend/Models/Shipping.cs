using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    // Таблиця shipping має подвійне призначення:
    // 1) доставка конкретного замовлення (заповнено OrderId, UserId = null);
    // 2) збережена адреса користувача для повторного використання (заповнено UserId, OrderId = null).
    [Table("shipping")]
    public class Shipping
    {
        [Key]
        public int ShippingId { get; set; }
        // Зв'язок із замовленням; null для збереженої адреси користувача
        public int? OrderId { get; set; }
        // Зв'язок із користувачем; null для доставки конкретного замовлення
        public int? UserId { get; set; }
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
        [Required]
        [MaxLength(100)]
        public string RecipientName { get; set; } = string.Empty;
        [Required]
        [MaxLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? TrackingNumber { get; set; }
        // Адреса за замовчуванням серед збережених адрес користувача
        public bool IsDefault { get; set; }
        // Навігаційні властивості
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
