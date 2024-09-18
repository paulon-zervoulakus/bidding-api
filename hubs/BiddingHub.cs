using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

namespace SignalR.Hubs 
{    
    [Authorize]
    public class Bidding : Hub
    {
        private readonly IConfiguration _configuration;
        private static ConcurrentDictionary<string, string> _connections = new();
        public Bidding(IConfiguration configuration){
            _configuration = configuration;
        }
        public override async Task OnConnectedAsync()
        {
            
            var userEmail = Context.User?.Identity?.Name;
            if (userEmail != null)
            {
                Console.WriteLine("BiddingHub : OnConnectedAsync");
                _connections[Context.ConnectionId] = userEmail;
                var activeEmailList = _connections.Values.Distinct().ToList();
                await Clients.All.SendAsync("UpdateOnlineUsersList", activeEmailList);
            }

            await base.OnConnectedAsync();
        }
           

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var disconnectedId = Context.ConnectionId;
            var disconnectedEmail = _connections[disconnectedId];
            if (_connections.TryRemove(Context.ConnectionId, out _)) {
                // update online users
                Console.WriteLine("BiddingHub : OnDisconnectedAsync : " + disconnectedEmail);

                var activeEmailList = _connections.Values.Distinct().ToList();
                await Clients.All.SendAsync("UpdateOnlineUsersList", activeEmailList);
            }
            await base.OnDisconnectedAsync(exception);
        }

        private bool ValidateSignalRToken(string accessToken)
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
                    var userEmail = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                    if (userEmail != null && _connections.Values.Contains(userEmail))
                    {
                        // Token is valid and user is registered
                        Console.WriteLine("BiddingHub : ValidateSignalRToken :" + userEmail);
                        return true;
                    }
                    else
                    {
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

        // Token here is from UI hub parameter upon request
        public Task SendMessage(string message, string token)
        {
            if(ValidateSignalRToken(token)) {
                var userEmail = _connections[Context.ConnectionId];
                // Use the userEmail to identify the sender
                return Clients.All.SendAsync("ReceiveMessage", userEmail, message);
            }
            else {
                // send denial message and deactivate the user who send the message with invalide token
                return Clients.All.SendAsync("ReceiveMessage", "System", "Invalid token");
            }
        }
    }
}