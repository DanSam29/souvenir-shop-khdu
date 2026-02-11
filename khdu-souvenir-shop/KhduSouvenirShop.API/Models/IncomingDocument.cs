using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("IncomingDocuments")]
    public class IncomingDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PurchasePrice { get; set; }

        [ForeignKey("Company")]
        public int CompanyId { get; set; }

        [Column(TypeName = "date")]
        public DateTime DocumentDate { get; set; }

        [ForeignKey("CreatedByUser")]
        public int CreatedByUserId { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційні властивості
        public virtual Product? Product { get; set; }
        public virtual Company? Company { get; set; }
        public virtual User? CreatedByUser { get; set; }
    }
}
