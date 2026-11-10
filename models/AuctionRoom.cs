using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace biddingServer.Models
{
    public class AuctionRoomModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string GroupName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string? Description { get; set; }

        [Required]
        [ForeignKey("AccountModel")]
        public int HostId { get; set; }
        public AccountModel? Host { get; set; }

        [Required]
        public RoomStatusEnum Status { get; set; } = RoomStatusEnum.Active;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public ICollection<AuctionRoomMemberModel>? Members { get; set; } = new List<AuctionRoomMemberModel>();
        public ICollection<AuctionRoomProductModel>? AuctionRoomProducts { get; set; } = new List<AuctionRoomProductModel>();

    }

    public enum RoomStatusEnum
    {
        InActive = 0,
        Active = 1,
        Closed = 2,
        Cancelled = 3,
        Ended = 4
    }

    public class AuctionRoomMemberModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AuctionRoomId { get; set; }
        public AuctionRoomModel? AuctionRoom { get; set; }

        [Required]
        [ForeignKey("AccountModel")]
        public int UserId { get; set; }
        public AccountModel? User { get; set; }

        [Required]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuctionRoomProductModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("ProductModel")]
        public int ProductId { get; set; }
        public ProductModel? Product { get; set; }
        [Column(TypeName = "decimal(10, 2)")]
        public decimal StartingBidPrice { get; set; } = 0;
        [Column(TypeName = "decimal(10, 2)")]
        public decimal LastBidPrice { get; set; } = 0;
        [Column(TypeName = "decimal(10, 2)")]
        public decimal MinimumBidIncrement { get; set; } = 0;
        [Required]
        public DateTime StartingBidTime { get; set; } = DateTime.UtcNow;
        [Required]
        public DateTime EndBidTime { get; set; } = DateTime.UtcNow;
        [ForeignKey("AccountModel")]
        public int? CurrentHighBidderID { get; set; }
        public AccountModel? CurrentHighBidder { get; set; }  // Optional reference to the highest bidder
        public int? NumberOfBids { get; set; }
        [ForeignKey("AuctionRoomModel")]
        public int? AuctionRoomId { get; set; }
        public AuctionRoomModel? AuctionRoom { get; set; }
    }

    public class AuctionRoomProductTransactionModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("AuctionRoomProductModel")]
        public int AuctionRoomProductId { get; set; }
        public required AuctionRoomProductModel AuctionRoomProduct { get; set; }
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Bid { get; set; } = 0;
        [Required]
        [ForeignKey("AccountModel")]
        public int? CurrentBidderID { get; set; }
        public AccountModel? CurrentBidder { get; set; }
        [Required]
        public DateTime? BidTimestamp { get; set; }

    }
}
