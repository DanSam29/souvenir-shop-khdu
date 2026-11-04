using System.ComponentModel.DataAnnotations;

namespace KhduSouvenirShop.API.DTOs.Requests
{
    /// <summary>
    /// Вхідний DTO для створення категорії.
    /// </summary>
    public class CreateCategoryRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? NameEn { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? DescriptionEn { get; set; }

        public int? ParentCategoryId { get; set; }

        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// Вхідний DTO для оновлення категорії.
    /// </summary>
    public class UpdateCategoryRequest : CreateCategoryRequest { }
}
