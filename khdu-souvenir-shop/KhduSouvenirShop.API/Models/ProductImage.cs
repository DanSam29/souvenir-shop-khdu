using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KhduSouvenirShop.API.Models
{
    [Table("ProductImages")]
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageURL { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        [Column("Order")]
        public int DisplayOrder { get; set; } = 0;

        // Навігаційні властивості
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}