using biddingServer.Models;

namespace DTO.Response.Account
{
    public class BasicInfoDTO
    {
        public int Id { get; set; } = 0;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public RoleEnumerated Role { get; set; }
        public GenderEnumerated Gender { get; set; }
    }
}