using KhduSouvenirShop.API.Models.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("outgoingdocuments")]
    public class OutgoingDocument : BaseEntity
    {
        [Key]
        public int DocumentId { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [ForeignKey("Order")]
        public int? OrderId { get; set; }

        [ForeignKey("Company")]
        public int? CompanyId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Reason { get; set; } = string.Empty; // Order, Damaged, Lost, Return, Inventory

        [Column(TypeName = "decimal(10,2)")]
        public decimal? OriginalPrice { get; set; }

        [ForeignKey("AppliedPromotion")]
        public int? AppliedPromotionId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? DiscountAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? FinalPrice { get; set; }

        [Column(TypeName = "date")]
        public DateTime DocumentDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Навігаційні властивості
        public virtual Product? Product { get; set; }
        public virtual Order? Order { get; set; }
        public virtual Company? Company { get; set; }
        public virtual Promotion? AppliedPromotion { get; set; }
        
        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }
    }
}
