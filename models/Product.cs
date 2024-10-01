using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace biddingServer.Models
{
    public class ProductModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;
        [StringLength(1024)]
        public string? Description { get; set; }
        [Column(TypeName = "decimal(10, 2)")]
        [Required]
        public decimal Price { get; set; } = 0;
        [Required]
        public int Quantity { get; set; } = 1;
        [StringLength(32)]
        public string? SKU { get; set; }
        [ForeignKey("AccountModel")]
        [Required]
        public int SellerID { get; set; }
        public AccountModel? Seller { get; set; }
        public Boolean IsSerializable { get; set; } = false; // If true, ProductSerial count should match the quantity field
        public ProductConditionEnum ProductCondition { get; set; } = ProductConditionEnum.New; // enum
        [Required]
        [ForeignKey("ProductCategoryModel")]
        public int ProductCategoryID { get; set; }
        public ProductCategoryModel? ProductCategory { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        [Required]
        public DateTime UpdatedAt { get; set; }

    }
    public enum ProductConditionEnum
    {
        New = 0,                // Brand new, unused
        OpenBox = 1,             // Opened but not used
        Refurbished = 2,         // Restored to like-new condition
        UsedLikeNew = 3,         // Minimal signs of wear
        UsedGood = 4,            // Visible wear, good working condition
        UsedAcceptable = 5,      // Significant wear, still functional
        ForPartsOrNotWorking = 6 // Broken or defective
    }

}