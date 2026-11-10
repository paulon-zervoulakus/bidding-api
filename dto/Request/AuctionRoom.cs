using System.ComponentModel.DataAnnotations;

namespace DTO.Request.AuctionRoom
{
    public class CreateAuctionRoomRequestDTO
    {
        public int? Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string GroupName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string? Description { get; set; }
        public int Status { get; set; } = 0;
        public List<AuctionRoomProductRequestDTO> RoomProducts { get; set; } = new();
    }

    public class JoinAuctionRoomRequestDTO
    {
        [Required]
        public int AuctionRoomId { get; set; }
    }

    public class LeaveAuctionRoomRequestDTO
    {
        [Required]
        public int AuctionRoomId { get; set; }
    }

    public class AuctionRoomProductRequestDTO
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal StartingBidPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal lastBidPrice { get; set; } = 0;
        [Required]
        [Range(0, double.MaxValue)]
        public decimal MinimumBidIncrement { get; set; }

        [Required]
        public DateTime StartingBidTime { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime EndBidTime { get; set; } = DateTime.UtcNow.AddDays(1);

    }
}
