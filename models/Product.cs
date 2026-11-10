using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using DTO.Request.Product;

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
        public Boolean IsSerializable { get; set; } = false;
        public ProductConditionEnum ProductCondition { get; set; } = ProductConditionEnum.New;

        [ForeignKey("ProductCategoryModel")]
        public int? ProductCategoryID { get; set; }
        public ProductCategoryModel? ProductCategory { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
        [Required]
        public DateTime UpdatedAt { get; set; }

    }

    public class ProductCategoryModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(255)]
        public required string CategoryName { get; set; }
        public int? ParentCategoryId { get; set; } // Points to other category id as sub category
        [JsonIgnore]
        public virtual ProductCategoryModel? ParentCategory { get; set; }
        public virtual ICollection<ProductCategoryModel>? SubCategories { get; set; }
    }

    public class ProductImagesModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("ProductModel")]
        public int ProductId { get; set; }
        public ProductModel? Product { get; set; }
        public Boolean IsPrimary { get; set; } = false;
        [Required] // public uri path
        public required string OriginalSize { get; set; }
        public string? SquareSize { get; set; }
        public string? ThumbnailSize { get; set; }
        public string? IconSize { get; set; }

    }

    public class ProductSerialModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(255)]
        public required string SerialNumber { get; set; }
        [Required]
        [ForeignKey("ProductModel")]
        public int ProductId { get; set; }
        public required ProductModel Product { get; set; }
    }
}