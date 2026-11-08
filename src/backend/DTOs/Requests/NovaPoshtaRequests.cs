using System.ComponentModel.DataAnnotations;

namespace KhduSouvenirShop.API.DTOs.Requests
{
    /// <summary>
    /// Вхідний DTO для розрахунку вартості доставки Nova Poshta.
    /// </summary>
    public class CalculateDeliveryRequest
    {
        [Required]
        [MaxLength(100)]
        public string CityRef { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? WarehouseRef { get; set; }

        [Range(0.1, 1000, ErrorMessage = "Вага відправлення має бути в межах від 0.1 до 1000 кг")]
        public decimal Weight { get; set; }

        // Оціночна вартість відправлення (впливає на розрахунок NP); необов'язкова
        [Range(0, 1000000)]
        public decimal TotalAmount { get; set; }
    }
}
