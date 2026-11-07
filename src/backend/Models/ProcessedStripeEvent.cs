using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    // Журнал оброблених подій Stripe — забезпечує ідемпотентність webhook
    [Table("processed_stripe_events")]
    public class ProcessedStripeEvent
    {
        [Key]
        [MaxLength(255)]
        public string EventId { get; set; } = null!;
        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = null!;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
