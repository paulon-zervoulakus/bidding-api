using biddingServer.Models;

namespace biddingServer.dto.Hubs
{
    public class ConnectedUsersDTO
    {
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string AvatarSrc { get; set; } = "";
    }
    public class RoomUsersDTO: ConnectedUsersDTO
    {
        public int? UserId { get; set; }
        public int? RoomId { get; set; }
        public string? GroupName { get; set; } = null;
    }
    public class RoomIndex
    {
        public int RoomId { get; set; }
        public string GroupName { get; set; } = "";
    }
    public class RoomIndexList: List<RoomIndex>;

    public class RoomMessage
    {
        public string User { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class AuctionRoomProductBid
    {
        public int RoomProductId { get; set; }
        public decimal Amount { get; set; } = 0;
    }
    public class AuctionRoomBidResponseDTO: AuctionRoomProductBid
    {
        public int RoomId { get; set; }
        public string Bidder { get; set; } = "";
        public bool Extended { get; set; }
        public DateTime? EndBidTime { get; set; }

    }
    [Flags]
    public enum HubRoles
    {
        None = 0,
        Guest = (int)RoleEnumerated.Guest, // 1
        Member = (int)RoleEnumerated.Member, // 2
        Administrator = (int)RoleEnumerated.Administrator, // 4
        Seller = (int)RoleEnumerated.Seller // 8
    }
}