using System.ComponentModel.DataAnnotations;

namespace KhduSouvenirShop.API.DTOs.Requests
{
    /// <summary>
    /// Вхідний DTO для створення прибуткової накладної (склад).
    /// </summary>
    public class CreateIncomingDocumentRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PurchasePrice { get; set; }

        [Required]
        public int CompanyId { get; set; }

        public DateTime? DocumentDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Вхідний DTO для створення видаткової накладної (склад).
    /// </summary>
    public class CreateOutgoingDocumentRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(200)]
        public string Reason { get; set; } = "Other";

        public int? CompanyId { get; set; }

        public DateTime? DocumentDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
