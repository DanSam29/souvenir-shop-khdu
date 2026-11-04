namespace KhduSouvenirShop.API.DTOs.Responses
{
    /// <summary>
    /// Вихідний DTO з інформацією про акцію/промокод.
    /// </summary>
    public class PromotionResponse
    {
        public int PromotionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public int? TargetId { get; set; }
        public string AudienceType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PromoCode { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public int? MinQuantity { get; set; }
        public int Priority { get; set; }
        public int? UsageLimit { get; set; }
        public int CurrentUsage { get; set; }
        public bool IsActive { get; set; }
    }
}
