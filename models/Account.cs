namespace biddingServer.Models
{
    public class AccountModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; } = DateTime.MinValue;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public DateTime LastLoggedIn { get; set; } = DateTime.MinValue;
        public bool IsLoggedIn { get; set; } = false;
        public RoleEnumerated Role { get; set; } = RoleEnumerated.Guest;
        public GenderEnumerated Gender { get; set; } = GenderEnumerated.Hidden;
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }

    }
    public enum GenderEnumerated
    {
        Hidden = 1,
        Male = 2,
        Female = 3
    }
    public enum RoleEnumerated
    {
        Guest = 1,
        Member = 2,
        Administrator = 3
    }
}