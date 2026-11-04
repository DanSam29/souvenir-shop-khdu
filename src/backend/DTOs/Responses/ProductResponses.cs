namespace KhduSouvenirShop.API.DTOs.Responses
{
    /// <summary>
    /// Вихідний DTO з повною інформацією про товар.
    /// </summary>
    public class ProductResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public decimal Price { get; set; }
        public decimal Weight { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int Stock { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductImageResponse> Images { get; set; } = new();
    }

    /// <summary>
    /// Вихідний DTO зі стислою інформацією про товар (для списків/каталогу).
    /// </summary>
    public class ProductSummaryResponse
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? PrimaryImageUrl { get; set; }
    }

    /// <summary>
    /// Вихідний DTO із зображенням товару.
    /// </summary>
    public class ProductImageResponse
    {
        public int ImageId { get; set; }
        public string ImageURL { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
    }
}
