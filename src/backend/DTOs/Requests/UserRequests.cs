using System.ComponentModel.DataAnnotations;

namespace KhduSouvenirShop.API.DTOs.Requests
{
    /// <summary>
    /// Вхідний DTO для оновлення профілю користувача.
    /// </summary>
    public class UpdateProfileRequest
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Вхідний DTO для зміни паролю користувача.
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Вхідний DTO для зміни ролі користувача (адміністративна дія).
    /// </summary>
    public class UpdateRoleRequest
    {
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// Вхідний DTO для ручного встановлення студентського статусу (адміністративна дія).
    /// </summary>
    public class UpdateStudentStatusRequest
    {
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "REGULAR";

        public decimal? GPA { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Вхідний DTO для скидання паролю користувача адміністратором.
    /// Якщо NewPassword не вказано — буде згенеровано тимчасовий пароль.
    /// </summary>
    public class AdminResetPasswordRequest
    {
        [MaxLength(100)]
        public string? NewPassword { get; set; }
    }

    /// <summary>
    /// Вхідний DTO для створення/редагування збереженої адреси доставки.
    /// </summary>
    public class AddressRequest
    {
        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string WarehouseNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? WarehouseAddress { get; set; }

        [MaxLength(100)]
        public string? CityRef { get; set; }

        [MaxLength(100)]
        public string? WarehouseRef { get; set; }

        [Required]
        [MaxLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string RecipientPhone { get; set; } = string.Empty;

        public bool IsDefault { get; set; }
    }
}
