using KhduSouvenirShop.API.Models.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    // Черга повторної верифікації студентів, коли University API недоступний
    [Table("verificationqueue")]
    public class VerificationQueue : BaseEntity
    {
        [Key]
        public int QueueId { get; set; }
        [ForeignKey("User")]
        public int UserId { get; set; }
        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public DateTime NextAttemptAt { get; set; }
        [MaxLength(500)]
        public string? LastError { get; set; }
        // Status: Pending (очікує), Processing (виконується), Completed (успішно), Failed (вичерпано спроби)
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";
        // Навігаційні властивості
        public virtual User? User { get; set; }
    }
}
