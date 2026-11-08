namespace KhduSouvenirShop.API.DTOs.Admin
{
    /// <summary>
    /// Вихідний DTO рядка списку користувачів для адміністративної панелі.
    /// </summary>
    public class AdminUserListItemResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string StudentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    /// <summary>
    /// Вихідний DTO з детальною інформацією про профіль користувача (адмін-панель).
    /// </summary>
    public class AdminUserDetailResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public string StudentStatus { get; set; } = string.Empty;
        public decimal? GPA { get; set; }
        public DateTime? StudentVerifiedAt { get; set; }
        public DateTime? StudentExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsBlocked { get; set; }
    }

    /// <summary>
    /// Вихідний DTO рядка списку замовлень для адміністративної панелі.
    /// </summary>
    public class AdminOrderListItemResponse
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
    }
}
