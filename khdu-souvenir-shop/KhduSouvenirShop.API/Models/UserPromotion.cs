using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("UserPromotions")]
    public class UserPromotion
    {
        [Key]
        public int UserPromotionId { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [ForeignKey("Promotion")]
        public int PromotionId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public int UsedCount { get; set; } = 0;

        // Навігаційні властивості
        public virtual User? User { get; set; }
        public virtual Promotion? Promotion { get; set; }
    }
}
