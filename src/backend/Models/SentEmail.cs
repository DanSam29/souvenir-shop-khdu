using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    /// <summary>
    /// Журнал відправлених email-повідомлень для захисту від дублювань (ідемпотентність).
    /// Унікальна пара (TemplateType, ReferenceId) гарантує одне відправлення на подію.
    /// </summary>
    [Table("sentemails")]
    public class SentEmail
    {
        [Key]
        public int SentEmailId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Recipient { get; set; } = string.Empty;

        [Required]
        [MaxLength(512)]
        public string Subject { get; set; } = string.Empty;

        // Тип шаблону (напр. OrderConfirmation, OrderPaymentSuccess)
        [Required]
        [MaxLength(64)]
        public string TemplateType { get; set; } = string.Empty;

        // Ідентифікатор події (напр. номер замовлення або email користувача)
        [Required]
        [MaxLength(128)]
        public string ReferenceId { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Статус відправлення: Sent (надіслано) або Failed (помилка)
        [Required]
        [MaxLength(16)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string? ErrorMessage { get; set; }
    }
}
