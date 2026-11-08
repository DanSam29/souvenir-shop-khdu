namespace KhduSouvenirShop.API.DTOs.Responses
{
    /// <summary>
    /// Вихідний DTO з повною інформацією про користувача.
    /// </summary>
    public class UserResponse
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public string StudentStatus { get; set; } = string.Empty;
        public decimal? GPA { get; set; }
        public DateTime? StudentVerifiedAt { get; set; }
        public DateTime? StudentExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Вихідний DTO зі стислою інформацією про користувача.
    /// </summary>
    public class UserSummaryResponse
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// Вихідний DTO зі збереженою адресою доставки користувача.
    /// </summary>
    public class AddressResponse
    {
        public int ShippingId { get; set; }
        public string City { get; set; } = string.Empty;
        public string WarehouseNumber { get; set; } = string.Empty;
        public string? WarehouseAddress { get; set; }
        public string? CityRef { get; set; }
        public string? WarehouseRef { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// Вихідний DTO з інформацією про використану/призначену користувачу акцію.
    /// </summary>
    public class UserPromotionResponse
    {
        public int UserPromotionId { get; set; }
        public int PromotionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string AudienceType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime AssignedAt { get; set; }
        public int UsedCount { get; set; }
    }
}
