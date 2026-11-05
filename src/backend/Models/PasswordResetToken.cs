using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    /// <summary>
    /// Одноразовий токен для скидання паролю користувача.
    /// </summary>
    [Table("passwordresettokens")]
    public class PasswordResetToken
    {
        [Key]
        public int PasswordResetTokenId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(128)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UsedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        /// <summary>Токен чинний, якщо ще не використаний і не прострочений.</summary>
        [NotMapped]
        public bool IsValid => UsedAt == null && DateTime.UtcNow < ExpiresAt;
    }
}
