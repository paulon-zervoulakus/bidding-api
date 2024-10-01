using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace biddingServer.Models
{
    public class ProductBiddingModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("ProductModel")]
        public int ProductId { get; set; }
        public required ProductModel Product { get; set; }
        [Required]
        public DateTime BidStartTime { get; set; }
        [Required]
        public DateTime BidEndTime { get; set; }
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal InitialPrice { get; set; } = 0;
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal BidIncrement { get; set; } = 1;
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal CurrentPrice { get; set; } = 0;
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? BuyNowPrice { get; set; }
        [ForeignKey("AccountModel")]
        public int? CurrentHighBidderID { get; set; }
        public AccountModel? CurrentHighBidder { get; set; }  // Optional reference to the highest bidder
        public int NumberOfBids { get; set; } = 0;
        [Required]
        public required BidStatusEnum BidStatus { get; set; } = BidStatusEnum.NotStarted;

        // public bool AutoBidEnabled { get; set; }

    }
    public enum BidStatusEnum
    {
        NotStarted = 1,
        OnGoing = 2,
        Completed = 3,
        Cancelled = 4
    }

}