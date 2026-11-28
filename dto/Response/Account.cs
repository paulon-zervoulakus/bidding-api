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

// Add to DTO.Response.Account or create ValidateTokenResponseDTO
public class ValidateTokenResponseDTO
{
    public string Status { get; set; } = "token validated";
    public string Message { get; set; } = "Access token revalidated and renew.";
    public AccountProfileDTO AccountProfile { get; set; } = new AccountProfileDTO();
}

public class AccountProfileDTO
{
    public int Id { get; set; }
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public int Role { get; set; }
    public int Gender { get; set; }
    public DateTime LastLoggedIn { get; set; }
}