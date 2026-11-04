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
}
