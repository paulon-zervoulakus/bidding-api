using System.ComponentModel.DataAnnotations;

namespace DTO.Account
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress(ErrorMessage ="Please enter a valid email address.")]
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

    public class LoginDto
    {
        [Required]
        [EmailAddress(ErrorMessage ="Please enter a valid email address.")]
        public string Email { get; set; } = ""; // username 
        [Required]
        public string Password { get; set; } = "";
    }
}