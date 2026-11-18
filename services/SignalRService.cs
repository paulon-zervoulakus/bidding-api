using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using biddingServer.dto.Hubs;
using Microsoft.IdentityModel.Tokens;

public interface ISignalRTokenValidator
{
    bool IsValid(string? accessToken);
}

public class SignalRTokenValidate: ISignalRTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, RoomUsersDTO> _connections;

    public SignalRTokenValidate(
        IConfiguration configuration,
        ConcurrentDictionary<string, RoomUsersDTO> connections)
    {
        _configuration = configuration;
        _connections = connections;
    }
    public bool IsValid(string? accessToken)
    {
        if (accessToken != null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                // var key = System.Text.Encoding.UTF8.GetBytes("YourSecretKeyHere");
                var principal = tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt Signing key not configured"))),
                    ValidIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt Issuer not configured"),
                    ValidAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt Audience not configured"),
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);


                // If token is valid, proceed with other checking

                // check claims name if it is registered in the _connection directory
                var userEmail = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                if (userEmail != null && _connections.Values.Any(c => c.Email == userEmail))
                {
                    // Token is valid and user is registered
                    Console.WriteLine("BiddingHub : ValidateSignalRToken :" + userEmail);
                    return true;
                }
                else
                {
                    Console.WriteLine("BiddingHub - ValidateSignalRToken :" + userEmail);
                    return false;
                }
            }
            catch
            {
                Console.WriteLine("If signal R token validation fails, close the connection");
                // If token validation fails, close the connection
                return false;
            }
        }
        else
        {
            Console.WriteLine("If signal R token is missing, close the connection");
            // If token is missing, close the connection
            return false;
        }
    }
}