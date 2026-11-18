using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using biddingServer.dto.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.SignalR;

namespace SignalR.Hubs.Lobby
{
    [Authorize]
    public class AuctionLobby : Hub
    {
        private readonly IGroupNameEncoder _encodeGroupName;
        private readonly ConcurrentDictionary<string, RoomUsersDTO> _connections = new();
        private static ConcurrentDictionary<string, HashSet<string>> _auctionLobbyGroup = new();
        public AuctionLobby(ConcurrentDictionary<string, RoomUsersDTO> connections, IGroupNameEncoder encodeGroupName)
        {
            _connections = connections;
            _encodeGroupName = encodeGroupName;
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
            if (connectedUser.Email != null)
            {
                // Console.WriteLine("OnConnectedAsync: " + Context.ConnectionId);
                _connections[Context.ConnectionId] = connectedUser;
                Console.WriteLine("BiddingHub : OnConnectedAsync : " + connectedUser.Fullname);

                // var activeEmailList = _connections.Values.Distinct().ToList();
                var activeUserList = _connections.Values
                    .GroupBy(user => user.Email)
                    .Select(group => group.First()) // Take the first user for each email
                    .ToList();
                await Clients.All.SendAsync("UpdateLobbyOnlineUsersList", activeUserList);
            }

            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var disconnectedId = Context.ConnectionId;
            // _auctionLobbyGroup.TryRemove(disconnectedId, out _);

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

                await Clients.All.SendAsync("UpdateLobbyOnlineUsersList", activeUserList);
            }
            await base.OnDisconnectedAsync(exception);
        }

        [ValidateSignalRToken]
        public async Task RegisterLobbyAuctionRoomList(RoomIndexList rooms)
        {
            foreach(var room in rooms)
            {
                var roomGroup = _encodeGroupName.EncodeGroupName("lobby", room.RoomId, room.GroupName);
                await Groups.AddToGroupAsync(Context.ConnectionId, roomGroup);

                // Track which groups this connection is in
                _auctionLobbyGroup.AddOrUpdate(
                    Context.ConnectionId,
                    new HashSet<string> { roomGroup },
                    (key, existing) => {
                        existing.Add(roomGroup);
                        return existing;
                    }
                );
            }

            // await Clients.Group(roomGroup).SendAsync("UserJoinedLobby", Context.ConnectionId, roomGroup, _connections[Context.ConnectionId]);

        }
        public async Task UnregisterLobbyAuctionRoomList(int[] roomIds, string groupName)
        {
            foreach(var roomId in roomIds)
            {
                var roomGroup = _encodeGroupName.EncodeGroupName("lobby", roomId, groupName);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomGroup);

                if (_auctionLobbyGroup.TryGetValue(Context.ConnectionId, out var groups))
                {
                    groups.Remove(roomGroup);
                    if (groups.Count == 0)
                        _auctionLobbyGroup.TryRemove(Context.ConnectionId, out _);
                }
            }

            // await Clients.Group(roomGroup).SendAsync("UserLeftLobby", Context.ConnectionId, roomGroup, _connections[Context.ConnectionId]);
        }
        [ValidateSignalRToken]
        public Task SendLobbyMessage(string message)
        {
            var connectedUser = _connections[Context.ConnectionId];
            // Use the userEmail to identify the sender
            return Clients.All.SendAsync("UpdateLobbyMessage", connectedUser.Fullname, message);
        }
    }
}