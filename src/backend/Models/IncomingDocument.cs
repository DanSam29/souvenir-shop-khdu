using KhduSouvenirShop.API.Models.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("incomingdocuments")]
    public class IncomingDocument : BaseEntity
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

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Навігаційні властивості
        public virtual Product? Product { get; set; }
        public virtual Company? Company { get; set; }
        
        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }
    }
}
