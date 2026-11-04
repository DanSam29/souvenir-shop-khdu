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
}
