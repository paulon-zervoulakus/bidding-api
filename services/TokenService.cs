using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using biddingServer.Models;

namespace biddingServer.services.Tokens
{
    public class AuthService
    {
        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];

            using var generator = RandomNumberGenerator.Create();

            generator.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }

        public static string GenerateJwtToken(IConfiguration configuration, AccountModel accountProfile, DateTime expirationDate)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("Secret not configured"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, accountProfile.Email),
                    new Claim(ClaimTypes.NameIdentifier, accountProfile.Id.ToString()),
                    new Claim("UserName", accountProfile.UserName),
                    new Claim("Email", accountProfile.Email),
                    new Claim("FullName", accountProfile.FullName),
                    // new Claim("Role", accountProfile.Role.ToString()),
                    new Claim(ClaimTypes.Role, accountProfile.Role.ToString()),
                    new Claim("Gender", accountProfile.Gender.ToString()),
                    new Claim("LastLoggedIn", accountProfile.LastLoggedIn.ToString()),
                    new Claim("RefreshToken", accountProfile.RefreshToken ?? ""),

                ]),
                Expires = expirationDate,
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static string GenerateSignalRToken(IConfiguration configuration, AccountModel accountProfile, DateTime expirationDate)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("Secret not configured"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, accountProfile.UserName),
                    // new Claim("Role", accountProfile.Role.ToString())
                    new Claim(ClaimTypes.Role, accountProfile.Role.ToString())
                ]),
                Expires = expirationDate,
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}