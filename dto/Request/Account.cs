using biddingServer.Models;

namespace DTO.Request.Account
{
    public class BasicInfoDTO
    {
        public int id { get; set; } = 0;
        public string userName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string fullName { get; set; } = string.Empty;
        public DateTime dateOfBirth { get; set; }
        public RoleEnumerated role { get; set; }
        public GenderEnumerated gender { get; set; }
    }
}