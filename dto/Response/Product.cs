using System.ComponentModel.DataAnnotations;
using biddingServer.Models;
namespace DTO.Response.Product
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? SKU { get; set; }
        public bool IsSerializable { get; set; }
        public ProductConditionEnum ProductCondition { get; set; }
        public Seller Seller { get; set; }
        public ProductCategory ProductCategory { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Seller
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }

    public class ProductCategory
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
    }

}