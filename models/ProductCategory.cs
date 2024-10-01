using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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
        [JsonIgnore]
        public virtual ProductCategoryModel? ParentCategory { get; set; }
        public virtual ICollection<ProductCategoryModel>? SubCategories { get; set; }
    }
}