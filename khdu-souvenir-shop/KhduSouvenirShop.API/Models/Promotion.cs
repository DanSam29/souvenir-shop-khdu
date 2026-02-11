using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("Promotions")]
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // Type: PERCENTAGE, FIXED_AMOUNT, SPECIAL_PRICE
        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "PERCENTAGE"; 

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Value { get; set; }

        // TargetType: PRODUCT, CATEGORY, CART, SHIPPING
        [Required]
        [MaxLength(20)]
        public string TargetType { get; set; } = "PRODUCT";

        public int? TargetId { get; set; }

        // AudienceType: ALL, STUDENTS, STAFF, ALUMNI, CUSTOM
        [MaxLength(20)]
        public string AudienceType { get; set; } = "ALL";

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(50)]
        public string? PromoCode { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MinOrderAmount { get; set; }

        public int? MinQuantity { get; set; }

        public int Priority { get; set; } = 0;

        public int? UsageLimit { get; set; }
        public int CurrentUsage { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [ForeignKey("CreatedBy")]
        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційна властивість
        public virtual User? CreatedByUser { get; set; }
    }
}
