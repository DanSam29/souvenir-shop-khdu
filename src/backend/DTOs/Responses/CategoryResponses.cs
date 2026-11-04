namespace KhduSouvenirShop.API.DTOs.Responses
{
    /// <summary>
    /// Вихідний DTO з інформацією про категорію.
    /// </summary>
    public class CategoryResponse
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public string? DescriptionEn { get; set; }
        public int? ParentCategoryId { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Вихідний DTO категорії з вкладеними підкатегоріями (дерево категорій).
    /// </summary>
    public class CategoryTreeResponse : CategoryResponse
    {
        public List<CategoryTreeResponse> SubCategories { get; set; } = new();
    }
}
