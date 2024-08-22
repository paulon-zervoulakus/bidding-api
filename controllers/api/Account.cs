using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using DTO.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Models;
using TestData;

namespace Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<AccountModel> _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher<AccountModel> passwordHasher, IConfiguration configuration)
        {
            _configuration = configuration; 
            _context = context;
            _passwordHasher = passwordHasher;
        }       
        
        [HttpGet("logout")]
        [Authorize]
        public IActionResult Logout()
        {                    
            var token = Request.Cookies["access_token"];
            if (token == null)
                return BadRequest("Token is required.");

            Response.Cookies.Append("access_token", token, new CookieOptions{
                HttpOnly = true,
                Secure = false, // Only send cookie over HTTPS
                SameSite = SameSiteMode.Lax, // Adjust based on your needs
                // Expires = DateTime.UtcNow.AddMinutes(-1),
                Expires = DateTime.UtcNow.AddMinutes(-2),
                Path = "/"
            });

            return Ok(new { message = "Session ended." });

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Validate user credentials (replace with actual validation)
            if (loginDto.Email == "" || loginDto.Password == "")
                return BadRequest("Invalid Email or Password");

            try {

            
                // Find the user by email
                var user = await _context.Accounts.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
                if (user == null)
                {
                    return Unauthorized("Invalid credentials.");
                }

                // Verify the password
                var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, loginDto.Password);
                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    return Unauthorized("Invalid credentials.");
                }

                user.LastLoggedIn = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                DateTime expirationDate = DateTime.UtcNow.AddMinutes(2);

                var token = GenerateJwtToken(user, expirationDate);
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Only send cookie over HTTPS
                    SameSite = SameSiteMode.Lax, // Adjust based on your needs
                    Expires = expirationDate,
                    Path = "/"
                };

                Response.Cookies.Append("access_token", token, cookieOptions);
                return Ok(new { Token = token, Profile = user });

            }catch(Exception err){
                return BadRequest("Connection error: Database connection closed.");
            }
            
        }

        private string GenerateJwtToken(AccountModel accountProfile, DateTime expirationDate)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim("Id", accountProfile.Id.ToString()),
                    new Claim("UserName", accountProfile.UserName),
                    new Claim("Email", accountProfile.Email),
                    new Claim("FullName", accountProfile.FullName),
                    new Claim("Role", accountProfile.Role.ToString()),
                    new Claim("Gender", accountProfile.Gender.ToString()),
                    new Claim("LastLoggedIn", accountProfile.LastLoggedIn.ToString()),
                ]),
                Expires = expirationDate,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

       
        
        [HttpGet("validate-token")]
        [Authorize] // Ensure that only authenticated requests can access this endpoint
        public IActionResult ValidateToken()
        {
            var token = Request.Cookies["access_token"];
            if (token == null)
                return BadRequest("Token is required.");


            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                // Validate the token
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                
                var jwtToken = (JwtSecurityToken)validatedToken;
                var claims = jwtToken.Claims.ToList();
                
                var Id = int.TryParse(claims.FirstOrDefault(x => x.Type == "Id")?.Value, out var id) ? id : 0;
                var accountProfile = _context.Accounts.FirstOrDefault(x => x.Id == id);
                if (accountProfile == null) return Unauthorized(new { message = "Token is invalid or expired."});

                return Ok(new { message = "Token is valid.", accountProfile });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = "Token is invalid or expired.", error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAccount([FromBody] RegisterDto registerDto){       
            // Server-side validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if email is already in use
            if (await _context.Accounts.AnyAsync(a => a.Email == registerDto.Email))
            {
                return Conflict("Email is already in use.");
            }

            bool isTableEmpty = !_context.Accounts.Any();

            // Hash the password
            var account = new AccountModel
            {
                Email = registerDto.Email,
                UserName = registerDto.Email,
                FullName = registerDto.Email.Split('@')[0],
                DateOfBirth = registerDto.DateOfBirth,
                Password = _passwordHasher.HashPassword(null, registerDto.Password),
                CreatedDate = DateTime.UtcNow,
                Role = isTableEmpty ? RoleEnumerated.Administrator : RoleEnumerated.Guest
            };

            // Save the user to the database
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return Ok("Registration successful.");
        
        }
       
    }

    
}