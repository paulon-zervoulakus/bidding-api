using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using biddingServer.dto.Hubs;
using biddingServer.services.product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;

namespace SignalR.Hubs.Room
{
    [Authorize]
    public class AuctionRoom: Hub
    {
        private readonly IGroupNameEncoder _encoder;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, RoomUsersDTO> _connections = new();
        private readonly IAuctionRoomService _auctionRoomService;
        public AuctionRoom(
            ConcurrentDictionary<string, RoomUsersDTO> connections,
            IAuctionRoomService auctionRoomService,
            IConfiguration configuration,
            IGroupNameEncoder encoder)
        {
            _connections = connections;
            _auctionRoomService = auctionRoomService;
            _configuration = configuration;
            _encoder = encoder;
        }

        public override async Task OnConnectedAsync()
        {
            if (!int.TryParse(
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                out int userId))
            {
                throw new HubException("Invalid user ID.");
            }
            var connectedUser = new RoomUsersDTO
            {
                Email = Context.User?.Identity?.Name,
                Fullname = Context.User?.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value,
                AvatarSrc = "",
                UserId = userId
            };
            _connections[Context.ConnectionId] = connectedUser;

            // await Clients.All.SendAsync("UpdateRoomOnlineUsersList", activeUserList);

            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var disconnectedId = Context.ConnectionId;
            // _auctionLobbyGroup.TryRemove(disconnectedId, out _);

            var disconnectedUser = _connections[disconnectedId];

            _connections.TryRemove(Context.ConnectionId, out _);

            await base.OnDisconnectedAsync(exception);
        }
        // private bool ValidateSignalRToken(string accessToken)
        // {

        //     if (accessToken != null)
        //     {
        //         var tokenHandler = new JwtSecurityTokenHandler();
        //         try
        //         {
        //             // var key = System.Text.Encoding.UTF8.GetBytes("YourSecretKeyHere");
        //             var principal = tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
        //             {
        //                 IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt Signing key not configured"))),
        //                 ValidIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt Issuer not configured"),
        //                 ValidAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt Audience not configured"),
        //                 ValidateIssuerSigningKey = true,
        //                 ValidateIssuer = true,
        //                 ValidateAudience = true,
        //                 ValidateLifetime = true,
        //                 ClockSkew = TimeSpan.Zero
        //             }, out SecurityToken validatedToken);


        //             // If token is valid, proceed with other checking

        //             // check claims name if it is registered in the _connection directory
        //             var userEmail = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        //             if (userEmail != null && _connections.Values.Any(c => c.Email == userEmail))
        //             {
        //                 // Token is valid and user is registered
        //                 Console.WriteLine("BiddingHub : ValidateSignalRToken :" + userEmail);
        //                 return true;
        //             }
        //             else
        //             {
        //                 Console.WriteLine("BiddingHub - ValidateSignalRToken :" + userEmail);
        //                 return false;
        //             }
        //         }
        //         catch
        //         {
        //             Console.WriteLine("If signal R token validation fails, close the connection");
        //             // If token validation fails, close the connection
        //             return false;
        //         }
        //     }
        //     else
        //     {
        //         Console.WriteLine("If signal R token is missing, close the connection");
        //         // If token is missing, close the connection
        //         return false;
        //     }
        // }
        [ValidateSignalRToken]
        public async Task JoinAuctionRoom(RoomIndex room)
        {

            if (!_connections.TryGetValue(
                Context.ConnectionId,
                out var connectedUser))
            {
                throw new HubException("Invalid user ID.");
            }

            // Verify room existence
            var existingRoom = await _auctionRoomService
                .IsUserAuthorized(room.RoomId, connectedUser.UserId ?? 0);

            if (!existingRoom)
                throw new HubException("Auction room does not exist.");
            // Check if the user host this or a member of this room

            var roomGroup = _encoder.EncodeGroupName("room", room.RoomId, room.GroupName);
            _connections.AddOrUpdate(
                    Context.ConnectionId,
                    connectedUser,
                    (key, existing) => {
                        existing.RoomId = room.RoomId;
                        existing.GroupName = room.GroupName;
                        return existing;
                    }
                );
            await Groups.AddToGroupAsync(Context.ConnectionId, roomGroup);

            // await Clients.Group(roomGroup).SendAsync("UserJoinedLobby", Context.ConnectionId, roomGroup, _connections[Context.ConnectionId]);
        }
        [ValidateSignalRToken]
        public async Task LeaveAuctionRoom(RoomIndex room)
        {
            var roomGroup = _encoder.EncodeGroupName("room", room.RoomId, room.GroupName);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomGroup);
            _connections.TryRemove(Context.ConnectionId, out _);

        }
        [ValidateSignalRToken]
        public async Task SendProductBid(int productId, decimal amount)
        {
            // Find which groups this connection belongs to and broadcast
            if (_connections.TryGetValue(Context.ConnectionId, out var connectedUser))
            {
                var (room, error) = await _auctionRoomService.ProductBidPush(productId, connectedUser.RoomId ?? 0, connectedUser.UserId ?? 0, amount);

                var roomGroup = _encoder.EncodeGroupName("room", connectedUser.RoomId ?? 0, connectedUser.GroupName ?? "");
                if( error is null)
                {
                    await Clients.Group(roomGroup).SendAsync("UpdateRoomMessage", connectedUser.Fullname ?? "Unknown", error);
                }
                else
                {
                    await Clients.Group(roomGroup).SendAsync("ReceiveAuctionBid", connectedUser.Fullname ?? "Unknown", room);
                }
            }

        }
    }
}