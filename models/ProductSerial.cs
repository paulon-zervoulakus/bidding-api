using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace biddingServer.Models
{
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