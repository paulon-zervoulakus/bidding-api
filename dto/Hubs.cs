namespace biddingServer.dto.Hubs
{
    public class ConnectedUsersDTO
    {
        public string? Email { get; set; }
        public string? Fullname { get; set; }
        public string AvatarSrc { get; set; } = "";
    }
}