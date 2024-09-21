using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using biddingServer.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
namespace biddingServer.Models
{
    public class ProductImagesModels
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("ProductModel")]
        public int ProductId { get; set; }
        public required ProductModel Product { get; set; }
        public Boolean IsPrimary { get; set; } = false;
        [Required] // public uri path
        public required string OriginalSize { get; set; }
        public string? SquareSize { get; set; }
        public string? ThubmnailSize { get; set; }
        public string? IconSize { get; set; }

    }
}