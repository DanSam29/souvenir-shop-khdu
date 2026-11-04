using System.ComponentModel.DataAnnotations;

namespace KhduSouvenirShop.API.DTOs.Requests
{
    /// <summary>
    /// Вхідний DTO для створення компанії-постачальника.
    /// </summary>
    public class CreateCompanyRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContactPerson { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Вхідний DTO для оновлення компанії-постачальника.
    /// </summary>
    public class UpdateCompanyRequest : CreateCompanyRequest { }
}
