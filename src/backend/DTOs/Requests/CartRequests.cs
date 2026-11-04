using System.ComponentModel.DataAnnotations;

namespace KhduSouvenirShop.API.DTOs.Requests
{
    /// <summary>
    /// Вхідний DTO для додавання товару до кошика.
    /// </summary>
    public class AddToCartRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Вхідний DTO для оновлення кількості товару в кошику.
    /// </summary>
    public class UpdateCartItemRequest
    {
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
