using System.ComponentModel.DataAnnotations;
using biddingServer.Models;
namespace DTO.Request.Product
{
    public class ProductRequestDTO
    {
        public int? Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; } = 0;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        [MaxLength(32)]
        public string? SKU { get; set; }

        public Boolean IsSerializable { get; set; } = false; // If true, ProductSerial count should match the quantity field

        public ProductConditionEnum ProductCondition { get; set; } = ProductConditionEnum.New; // enum

        [Required]
        public int ProductCategoryID { get; set; }


    }
}