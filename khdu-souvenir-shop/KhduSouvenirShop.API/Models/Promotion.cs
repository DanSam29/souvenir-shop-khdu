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
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "Percent"; // Percent, Fixed

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Value { get; set; } // Percent: 0..100, Fixed: amount

        public bool Active { get; set; } = true;

        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }

        public int? MaxUsage { get; set; }
        public int TimesUsed { get; set; } = 0;
    }
}
