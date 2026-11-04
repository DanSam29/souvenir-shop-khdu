using System.ComponentModel.DataAnnotations;

namespace KhduSouvenirShop.API.DTOs.Requests
{
    /// <summary>
    /// Вхідний DTO для створення товару.
    /// </summary>
    public class CreateProductRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? NameEn { get; set; }

        public string? Description { get; set; }

        public string? DescriptionEn { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Weight { get; set; } = 0.5m;

        [Required]
        public int CategoryId { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; } = 0;
    }

    /// <summary>
    /// Вхідний DTO для оновлення товару.
    /// </summary>
    public class UpdateProductRequest : CreateProductRequest { }
}
