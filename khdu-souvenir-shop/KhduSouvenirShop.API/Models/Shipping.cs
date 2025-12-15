using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("Shipping")]
    public class Shipping
    {
        [Key]
        public int ShippingId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string WarehouseNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DeliveryCost { get; set; }

        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        // Навігаційні властивості
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;
    }
}