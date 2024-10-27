using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using biddingServer.dto.Hubs;

namespace SignalR.Hubs
{
    [Authorize]
    public class Bidding : Hub
    {
        private readonly IConfiguration _configuration;
        private static ConcurrentDictionary<string, ConnectedUsersDTO> _connections = new();
        public Bidding(IConfiguration configuration, ConcurrentDictionary<string, ConnectedUsersDTO> connections)
        {
            _configuration = configuration;
            _connections = connections;
        }
        public override async Task OnConnectedAsync()
        {
            var connectedUser = new ConnectedUsersDTO
            {
                Email = Context.User?.Identity?.Name,
                Fullname = Context.User?.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value,
                AvatarSrc = ""
            };
            if (connectedUser.Email != null)
            {
                // Console.WriteLine("OnConnectedAsync: " + Context.ConnectionId);
                _connections[Context.ConnectionId] = connectedUser;

                // var activeEmailList = _connections.Values.Distinct().ToList();
                var activeUserList = _connections.Values
                    .GroupBy(user => user.Email)
                    .Select(group => group.First()) // Take the first user for each email
                    .ToList();
                await Clients.All.SendAsync("UpdateOnlineUsersList", activeUserList);
            }

            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var disconnectedId = Context.ConnectionId;
            var disconnectedUser = _connections[disconnectedId];

            if (_connections.TryRemove(Context.ConnectionId, out _))
            {
                // update online users
                Console.WriteLine("BiddingHub : OnDisconnectedAsync : " + disconnectedUser.Fullname);

                // var activeEmailList = _connections.Values.Distinct().ToList();

                var activeUserList = _connections.Values
                    .GroupBy(user => user.Email)
                    .Select(group => group.First()) // Take the first user for each email
                    .ToList();

                await Clients.All.SendAsync("UpdateOnlineUsersList", activeUserList);
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

        // Token here is from UI hub parameter upon request
        public Task SendMessage(string message, string token)
        {
            if (ValidateSignalRToken(token))
            {
                var connectedUser = _connections[Context.ConnectionId];
                // Use the userEmail to identify the sender
                return Clients.All.SendAsync("ReceiveMessage", connectedUser.Fullname, message);
            }
            else
            {
                // send denial message and deactivate the user who send the message with invalide token
                return Clients.All.SendAsync("ReceiveMessage", "System", "Invalid token");
            }
        }
        public List<ConnectedUsersDTO> GetOnlineUserList(string token)
        {
            if (ValidateSignalRToken(token))
            {
                var activeUserList = _connections.Values
                    .GroupBy(user => user.Email)
                    .Select(group => group.First()) // Take the first user for each email
                    .ToList();

                // await Clients.All.SendAsync("UpdateOnlineUsersList", activeUserList);
                return activeUserList;
            }
            return [];
        }
    }
}