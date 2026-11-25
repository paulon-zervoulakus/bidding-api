using System.Collections.Concurrent;
using System.Security.Claims;
using biddingServer.dto.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalR.Hubs.Lobby;
using biddingServer.Attributes;

namespace SignalR.Hubs.Room
{
    [Authorize]
    public class AuctionRoom: Hub
    {
        private readonly IHubContext<AuctionLobby> _hubContext;
        private readonly IHelperService _helperService;
        private readonly ConcurrentDictionary<string, RoomUsersDTO> _connections = new();
        private readonly IAuctionRoomService _auctionRoomService;
        public AuctionRoom(
            ConcurrentDictionary<string, RoomUsersDTO> connections,
            IAuctionRoomService auctionRoomService,
            IHelperService helperService,
            IHubContext<AuctionLobby> hubContext)
        {
            _connections = connections;
            _auctionRoomService = auctionRoomService;
            _helperService = helperService;
            _hubContext = hubContext;
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

            var roomGroup = _helperService.EncodeGroupName("room", room.RoomId, room.GroupName);
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
            await Clients.Group(roomGroup).SendAsync("ReceiveRoomMessage",
                    connectedUser.Fullname!, "Joined the room.");
            var connList = _connections.Values
                    .Where(c => c.RoomId == room.RoomId)
                    .GroupBy(g => g.Email)
                    .Select(s => s.First())
                    .ToList();

            await Clients.Group(roomGroup)
                .SendAsync("UpdateRoomOnlineUsersList", connList);
        }
        [ValidateSignalRToken]
        public async Task LeaveAuctionRoom(RoomIndex room)
        {
            var roomGroup = _helperService.EncodeGroupName("room", room.RoomId, room.GroupName);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomGroup);
            if(_connections.TryRemove(Context.ConnectionId, out var removeConn))
            {

                await Clients.Group(roomGroup).SendAsync("ReceiveRoomMessage",
                    removeConn.Fullname!, "Left the room.");

                await Clients.Group(roomGroup)
                    .SendAsync("UpdateRoomOnlineUsersList",
                    _connections.Values
                    .Where(c => c.RoomId == room.RoomId)
                    .GroupBy(g => g.Email)
                    .Select(s => s.First())
                    .ToList());
            }


        }

        [ValidateSignalRToken(Roles = HubRoles.Administrator | HubRoles.Seller | HubRoles.Member)]
        public async Task SendProductBid(AuctionRoomProductBid dto)
        {
            // Find which groups this connection belongs to and broadcast
            if (_connections.TryGetValue(Context.ConnectionId, out var connectedUser))
            {
                var (room, error) = await _auctionRoomService.ProductBidPush(dto.RoomProductId, connectedUser.RoomId ?? 0, connectedUser.UserId ?? 0, dto.Amount);

                var roomGroup = _helperService.EncodeGroupName("room", connectedUser.RoomId ?? 0, connectedUser.GroupName ?? "");
                var lobbyGroup = _helperService.EncodeGroupName("lobby", connectedUser.RoomId ?? 0, connectedUser.GroupName ?? "");
                if( error is not null)
                {
                    await Clients.Group(roomGroup).SendAsync("ReceiveRoomMessage", connectedUser.Fullname ?? "Unknown", error);
                }
                else if( room is null)
                {
                    await Clients.Group(roomGroup).SendAsync("ReceiveRoomMessage", connectedUser.Fullname ?? "Unknown", "Unable to retrieve room update");
                }
                else
                {
                    var roomProduct = room.RoomProducts?.FirstOrDefault(p => p.ProductId == dto.RoomProductId);
                    var res = new AuctionRoomBidResponseDTO
                    {
                        Amount=dto.Amount,
                        RoomId=room.Id,
                        Bidder=connectedUser.Fullname ?? "Unknown",
                        RoomProductId=dto.RoomProductId,
                        Extended=roomProduct?.EndBidTimeExtended ?? false,
                        EndBidTime=roomProduct?.EndBidTime
                    };

                    await Clients.Group(roomGroup).SendAsync("ReceiveAuctionBid", res);
                    if (res.Extended)
                        await Clients.Group(roomGroup).SendAsync("ReceiveRoomMessage",
                            "System", "A bid was placed in the final seconds. The auction has been extended.");
                    await _hubContext.Clients.Group(lobbyGroup).SendAsync("UpdateLobbyAuctionRoomDetails", room);

                }
            }

        }
        [ValidateSignalRToken]
        public async Task SendRoomMessage(int roomId, string groupName, string message)
        {
            if (!_connections.TryGetValue(
                Context.ConnectionId,
                out var connectedUser))
            {
                throw new HubException("Invalid user ID.");
            }
            var roomGroup = _helperService.EncodeGroupName("room", roomId, groupName);
            await Clients.Group(roomGroup).SendAsync("ReceiveRoomMessage",
                    connectedUser.Fullname!, message);
        }
    }
}