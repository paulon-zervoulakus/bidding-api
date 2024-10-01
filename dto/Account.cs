using System.ComponentModel.DataAnnotations;
using biddingServer.Models;

namespace DTO.Account
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        [Required]
        [MinLength(5)]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        // [Required]
        // public string UserName { get; set; }

        // public string FullName { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }
    }

    public class LoginRequestDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = ""; // username
        [Required]
        public string Password { get; set; } = "";
    }

    public class LoginResponseDto
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int Role { get; set; } = 0;
        public int Gender { get; set; } = 0;
        public DateTime LastLoggedIn { get; set; }
    }
}