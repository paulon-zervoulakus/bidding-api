using biddingServer.Models;
using DTO.Response.Account;
using DTO.Response.Product;
using Humanizer;
using ImageMagick;

namespace DTO.Response.AuctionRoom
{
    public class AuctionRoomResponseDTO
    {
        public int Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int HostId { get; set; }
        public string HostUserName { get; set; } = string.Empty;
        public RoomStatusEnum Status { get; set; }
        public RoomVisibilityEnum Visibility { get; set; } = RoomVisibilityEnum.Private;
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
        public List<AuctionRoomMemberResponseDTO>? Members { get; set; }
        public List<AuctionRoomProductResponseDTO>? RoomProducts { get; set; }
    }

    public class AuctionRoomMemberResponseDTO
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    public class AuctionRoomProductResponseDTO
    {
        public int? Id { get; set; }
        public int ProductId { get; set; }
        public decimal LastBidPrice { get; set; } = 0;
        public decimal StartingBidPrice { get; set; } = 0;
        public decimal MinimumBidIncrement { get; set; } = 0;
        public DateTime StartingBidTime { get; set; }
        public DateTime EndBidTime { get; set; }
        public ProductResponseDTO? ProductDetails { get; set; }
        public AccountModel? CurrentHighBidder { get; set; }
        public int? NumberOfBids { get; set; }
        public DateTime? NotifiedAt { get; set; }
        public bool EndBidTimeExtended { get; set; }
        public List<AuctionRoomProductTransactionResponseDTO>? BidHistory { get; set; }
    }
    public class AuctionRoomProductTransactionResponseDTO
    {
        public int? Id { get; set; }
        public DateTime BidTimestamp { get; set; }
        public decimal Bid { get; set; } = 0;
        public int AuctionRoomProductId { get; set; }
        public BasicInfoDTO? CurrentBidder { get; set; } = new BasicInfoDTO();

    }
}
