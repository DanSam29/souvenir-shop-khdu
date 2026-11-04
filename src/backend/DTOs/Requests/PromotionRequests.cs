using System.ComponentModel.DataAnnotations;

namespace KhduSouvenirShop.API.DTOs.Requests
{
    /// <summary>
    /// Вхідний DTO для створення акції/промокоду.
    /// </summary>
    public class CreatePromotionRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? NameEn { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? DescriptionEn { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "PERCENTAGE";

        public decimal Value { get; set; }

        [Required]
        [MaxLength(20)]
        public string TargetType { get; set; } = "PRODUCT";

        public int? TargetId { get; set; }

        [MaxLength(20)]
        public string AudienceType { get; set; } = "ALL";

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(50)]
        public string? PromoCode { get; set; }

        public decimal? MinOrderAmount { get; set; }

        public int? MinQuantity { get; set; }

        public int Priority { get; set; } = 0;

        public int? UsageLimit { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Вхідний DTO для оновлення акції/промокоду.
    /// </summary>
    public class UpdatePromotionRequest : CreatePromotionRequest { }
}
