using System.ComponentModel.DataAnnotations;

namespace biddingServer.Models
{
    public class ProductCategoryModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(255)]
        public required string CategoryName { get; set; }
        public int? ParentCategoryId { get; set; } // Points to other category id as sub category
        public virtual ProductCategoryModel? ParentCategory { get; set; }
        public virtual ICollection<ProductCategoryModel>? SubCategories { get; set; }
    }
}