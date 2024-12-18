using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DTO.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using biddingServer.Models;
using biddingServer.services.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using SignalR.Hubs;
using System.Collections.Concurrent;
using biddingServer.dto.Hubs;
using biddingServer.services.account;

namespace biddingServer.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IHubContext<Bidding> _hubContext;
        private static ConcurrentDictionary<string, ConnectedUsersDTO> _connections = new();

        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<AccountModel> _passwordHasher;
        private readonly int _accessTimeoutMinutes;
        private readonly int _refreshTimeoutMinutes;

        private readonly IAccountService _accountService;
        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher<AccountModel> passwordHasher,
            IConfiguration configuration,
            IHubContext<Bidding> hubContext,
            ConcurrentDictionary<string, ConnectedUsersDTO> connections,
            IAccountService accountService
        )
        {
            _configuration = configuration;
            _context = context;
            _passwordHasher = passwordHasher;
            _accessTimeoutMinutes = Convert.ToInt32(_configuration["Token:AccessTimeoutMinutes"] ?? throw new InvalidOperationException("Token access timeout not configured"));
            _refreshTimeoutMinutes = Convert.ToInt32(_configuration["Token:RefreshTimeoutMinutes"] ?? throw new InvalidOperationException("Token refresh timeout not configured"));
            _hubContext = hubContext;
            _connections = connections;
            _accountService = accountService;
        }

        [HttpGet("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout()
        {

            var emailIdentity = HttpContext.User.Identity?.Name;
            if (emailIdentity is null)
                return Unauthorized();

            var user = await _context.Accounts.FirstOrDefaultAsync(x => x.Email == emailIdentity);
            if (user is null)
                return Unauthorized();

            user.RefreshToken = null;
            await _context.SaveChangesAsync();

            var token = Request.Cookies["access_token"];
            if (token == null)
                return BadRequest("Token is required.");

            Response.Cookies.Append("access_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Only send cookie over HTTPS
                SameSite = SameSiteMode.Lax, // Adjust based on your needs
                // Expires = DateTime.UtcNow.AddMinutes(-1),
                Expires = DateTime.UtcNow.AddMinutes(-2),
                Path = "/"
            });

            Response.Cookies.Append("signalr_token", token, new CookieOptions
            {
                HttpOnly = false,
                Secure = false, // Only send cookie over HTTPS
                SameSite = SameSiteMode.Lax, // Adjust based on your needs
                // Expires = DateTime.UtcNow.AddMinutes(-1),
                Expires = DateTime.UtcNow.AddMinutes(-2),
                Path = "/"
            });

            // var connectionId = _connections.FirstOrDefault(x => x.Value.Email == emailIdentity).Key;

            // if (connectionId != null) _connections.TryRemove(connectionId, out _);

            // var activeUserList = _connections.Values
            //     .GroupBy(user => user.Email)
            //     .Select(group => group.First()) // Take the first user for each email
            //     .ToList();

            // await _hubContext.Clients.All.SendAsync("UpdateOnlineUsersList", activeUserList);

            return Ok(new { message = "Session ended." });

        }

        [HttpPost("login")]
        // [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponseDto))]
        // [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            // Validate user credentials (replace with actual validation)
            if (loginDto.Email == "" || loginDto.Password == "")
                return BadRequest("Invalid Email or Password");

            try
            {


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

                // Refresh Token
                DateTime refreshTokenExpiry = DateTime.UtcNow.AddMinutes(_refreshTimeoutMinutes);
                var refreshToken = AuthService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = refreshTokenExpiry;
                user.LastLoggedIn = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Access Token
                DateTime expirationDate = DateTime.UtcNow.AddMinutes(_accessTimeoutMinutes);
                var token = AuthService.GenerateJwtToken(_configuration, user, expirationDate);
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Only send cookie over HTTPS
                    SameSite = SameSiteMode.Lax, // Adjust based on your needs
                    Expires = expirationDate,
                    Path = "/"
                };
                Response.Cookies.Append("access_token", token, cookieOptions);

                var signalrToken = AuthService.GenerateSignalRToken(_configuration, user, expirationDate);
                var signalrCookieOptions = new CookieOptions
                {
                    Secure = false, // Only send cookie over HTTPS
                    SameSite = SameSiteMode.Lax, // Adjust based on your needs
                    Expires = expirationDate,
                    HttpOnly = false,
                    Path = "/"
                };
                Response.Cookies.Append("signalr_token", signalrToken, signalrCookieOptions);

                // return Ok(new { Token = token, Profile = user });
                return Ok(new LoginResponseDto
                {
                    UserName = user.UserName,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = (int)user.Role,
                    Gender = (int)user.Gender,
                    LastLoggedIn = user.LastLoggedIn
                });

            }
            catch
            {
                return BadRequest("Connection error: Database connection closed.");
            }

        }

        [HttpGet("validate-token")]
        [Authorize] // Ensure that only authenticated requests can access this endpoint
        // [ProducesResponseType(StatusCodes.Status200OK)]
        // [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ValidateToken()
        {
            var token = Request.Cookies["access_token"];
            if (token == null)
                return BadRequest("Invalid Token or expired.");


            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                // Validate the token
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("Secret not configured"))),
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    // ClockSkew = TimeSpan.Zero,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var claims = jwtToken.Claims.ToList();

                var nameIdClaim = claims.FirstOrDefault(x => x.Type == "nameid");
                if (nameIdClaim == null || !int.TryParse(nameIdClaim.Value, out var id))
                {
                    return Unauthorized(new { message = "Invalid token: User ID claim is missing or invalid." });
                }

                var accountProfile = _context.Accounts.FirstOrDefault(x => x.Id == id);
                if (accountProfile == null) return Unauthorized(new { message = "Invalid Token or expired." });

                var refreshToken = claims.FirstOrDefault(x => x.Type == "RefreshToken") ?? null;
                if (refreshToken == null) return Unauthorized(new { message = "Refresh Token expired." });

                if (accountProfile.RefreshTokenExpiry < DateTime.UtcNow) return Unauthorized(new { message = "Refresh token expired.." });

                var uiHost = _configuration["UI_HOST"] ?? "localhost";
                // Renew Access Token
                DateTime expirationDate = DateTime.UtcNow.AddMinutes(_accessTimeoutMinutes);
                var newtoken = AuthService.GenerateJwtToken(_configuration, accountProfile, expirationDate);
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false, // Only send cookie over HTTPS
                    SameSite = SameSiteMode.Lax, // Adjust based on your needs
                    Expires = expirationDate,
                    Path = "/",
                    Domain = $".{uiHost}"
                };
                Response.Cookies.Append("access_token", newtoken, cookieOptions);

                var signalrToken = AuthService.GenerateSignalRToken(_configuration, accountProfile, expirationDate);
                var signalrCookieOptions = new CookieOptions
                {
                    Secure = false, // Only send cookie over HTTPS
                    SameSite = SameSiteMode.Lax, // Adjust based on your needs
                    Expires = expirationDate,
                    HttpOnly = false,
                    Path = "/",
                    Domain = $".{uiHost}"
                };
                Response.Cookies.Append("signalr_token", signalrToken, signalrCookieOptions);

                return Ok(new
                {
                    status = "token validated",
                    message = "Access token revalidated and renew.",
                    accountProfile = new
                    {
                        UserName = accountProfile.UserName,
                        FullName = accountProfile.FullName,
                        Email = accountProfile.Email,
                        Role = (int)accountProfile.Role,
                        Gender = (int)accountProfile.Gender,
                        LastLoggedIn = accountProfile.LastLoggedIn
                    }
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = "Invalid Token or expired.", error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterAccount([FromBody] RegisterDto registerDto)
        {

            // Server-side validation
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Register - Bad Request");
                return BadRequest(ModelState);
            }

            // Check if email is already in use
            if (await _context.Accounts.AnyAsync(a => a.Email == registerDto.Email))
            {
                Console.WriteLine("Register - email already exist");
                return Conflict("Email is already in use.");
            }

            bool isTableEmpty = !_context.Accounts.Any();

            // Hash the password
            var account = new AccountModel
            {
                Email = registerDto.Email,
                UserName = registerDto.Email.Split('@')[0],
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


        [HttpGet("get-profile-basic")]
        [Authorize]
        public async Task<IActionResult> GetProfileBasic()
        {
            var user = await _accountService.GetBasicInfo(HttpContext.User.Identity?.Name ?? "");

            return Ok(user);
        }
    }
}