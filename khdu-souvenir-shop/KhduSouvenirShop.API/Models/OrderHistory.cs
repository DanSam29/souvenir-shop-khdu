using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("OrderHistories")]
    public class OrderHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [ForeignKey("Order")]
        public int OrderId { get; set; }

        [MaxLength(50)]
        public string? OldStatus { get; set; }

        [Required]
        [MaxLength(50)]
        public string NewStatus { get; set; } = string.Empty;

        [ForeignKey("ChangedByUser")]
        public int? ChangedByUserId { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Навігаційні властивості
        public virtual Order? Order { get; set; }
        public virtual User? ChangedByUser { get; set; }
    }
}
