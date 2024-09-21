using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace biddingServer.Models
{
    public class ProductTransactionModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("ProductModel")]
        public int ProductId { get; set; }
        public required ProductModel Product { get; set; }
    }
}