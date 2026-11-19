using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DTO.Response.AuctionRoom;
using DTO.Request.AuctionRoom;
using SignalR.Hubs.Lobby;

namespace biddingServer.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuctionRooms : ControllerBase
    {
        private readonly IHelperService _helperService;
        private readonly IHubContext<AuctionLobby> _hubContext;
        // private readonly ApplicationDbContext _context;
        private readonly IAuctionRoomService _auctionRoomService;
        public AuctionRooms(
            IHubContext<AuctionLobby> hubContext,
            IAuctionRoomService auctionRoomService,
            IHelperService helperService)
        {
            // _context = context;
            _hubContext = hubContext;
            _auctionRoomService = auctionRoomService;
            _helperService = helperService;
        }


        [HttpGet("get-rooms")]
        [Authorize]
        public async Task<ActionResult<List<AuctionRoomResponseDTO>>> GetRooms()
        {
            var userId = _helperService.GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var rooms = await _auctionRoomService.GetAuctionRoomsAsync(userId);

            return Ok(new { rooms });

        }
        [HttpGet("get-public-rooms")]
        [Authorize]
        public async Task<ActionResult<List<AuctionRoomResponseDTO>>> GetPublicRooms()
        {
            var userId = _helperService.GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var rooms = await _auctionRoomService.PublicAuctionRoomsAsync(userId);

            return Ok(new { rooms });
        }

        [HttpGet("get-manage-rooms")]
        [Authorize]
        public async Task<ActionResult<List<AuctionRoomResponseDTO>>> GetManageRooms()
        {
            var userId = _helperService.GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var rooms = _auctionRoomService.ManageAuctionRoomsAsync(userId);
            return Ok(new { rooms });
        }

        [HttpPost("unsubscribe")]
        [Authorize]
        public async Task<ActionResult> UnsubscribeAuctionRoom(
            [FromBody] SubscribeAuctionRoomRequestDTO dto)
        {
            var userId = _helperService.GetCurrentUserId();
            var auctionRoom = await _auctionRoomService.LeaveRoomAsync(dto.RoomId, userId);
            if( auctionRoom != null ) {
                var roomGroup = _helperService.EncodeGroupName("lobby", auctionRoom.Value?.Id ?? 0, auctionRoom.Value?.GroupName ?? "");
                await _hubContext.Clients.Group(roomGroup).SendAsync("UpdateLobbyAuctionRoomDetails", auctionRoom.Value);
                return Ok();
            }
            return BadRequest();
        }
        [HttpPost("subscribe")]
        [Authorize]
        public async Task<ActionResult> SubscribeAuctionRoom(
            [FromBody] SubscribeAuctionRoomRequestDTO dto)
        {
            var userId = _helperService.GetCurrentUserId();
            var auctionRoom = await _auctionRoomService.JoinRoomAsync(dto.RoomId, userId);
            if(auctionRoom != null)
            {
                var roomGroup = _helperService.EncodeGroupName("lobby", auctionRoom.Value?.Id ?? 0, auctionRoom.Value?.GroupName ?? "");
                await _hubContext.Clients.Group(roomGroup).SendAsync("UpdateLobbyAuctionRoomDetails", auctionRoom.Value);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost("save-room")]
        [Authorize(Roles = "Administrator,Seller")]
        public async Task<ActionResult<AuctionRoomResponseDTO>> SaveAuctionRoom(
            [FromBody] CreateAuctionRoomRequestDTO dto)
        {

            var userId = _helperService.GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var role = _helperService.GetCurrentUserRole();
            if(role != "Administrator" && role != "Seller" )
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Not Allowed");
            }

            var room = await _auctionRoomService.SaveAuctionRoom(dto, userId);
            if(room == null) return StatusCode(StatusCodes.Status422UnprocessableEntity, "Something went wrong!");

            var roomGroup = _helperService.EncodeGroupName("lobby", room.Value?.Id ?? 0, room.Value?.GroupName ?? "");

            await _hubContext.Clients.Group(roomGroup).SendAsync("UpdateLobbyAuctionRoomDetails", room.Value);

            return Ok(room.Value);
        }

        // [HttpPost("{roomId}/products")]
        // [Authorize(Roles = "Administrator,Seller")]
        // public async Task<ActionResult<ResponseDTO.AuctionRoomProductResponseDTO>> AddProductToRoom(int roomId, [FromBody] RequestDTO.AuctionRoomProductRequestDTO dto)
        // {
        //     if (!ModelState.IsValid) return BadRequest(ModelState);

        //     var userId = _helperService.GetCurrentUserId();
        //     if (userId == 0) return Unauthorized();

        //     var room = await _context.AuctionRooms.FirstOrDefaultAsync(r => r.Id == roomId);
        //     if (room == null) return NotFound("Room not found.");

        //     if (room.HostId != userId)
        //     {
        //         var role = GetCurrentUserRole();
        //         if (role != "Administrator") return Forbid("Only the host can add products.");
        //     }

        //     var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);
        //     if (product == null) return NotFound("Product not found.");

        //     var exists = await _context.AuctionRoomProducts.AnyAsync(rp => rp.ProductId == dto.ProductId);
        //     if (exists) return BadRequest("Product already added to this room.");

        //     var roomProduct = new AuctionRoomProductModel
        //     {
        //         ProductId = dto.ProductId,
        //         LastBidPrice = dto.lastBidPrice,
        //         StartingBidPrice = dto.StartingBidPrice,
        //         MinimumBidIncrement = dto.MinimumBidIncrement,
        //         StartingBidTime = dto.StartingBidTime,
        //         EndBidTime = dto.EndBidTime,
        //         NumberOfBids = 0
        //     };

        //     _context.AuctionRoomProducts.Add(roomProduct);
        //     await _context.SaveChangesAsync();

        //     var response = new ResponseDTO.AuctionRoomProductResponseDTO
        //     {
        //         ProductId = roomProduct.Id,
        //         LastBidPrice = roomProduct.LastBidPrice,
        //         StartingBidPrice = roomProduct.StartingBidPrice,
        //         MinimumBidIncrement = roomProduct.MinimumBidIncrement,
        //         StartingBidTime = roomProduct.StartingBidTime,
        //         EndBidTime = roomProduct.EndBidTime,
        //         NumberOfBids = roomProduct.NumberOfBids,
        //         ProductDetails = new DTO.Response.Product.ProductResponseDTO
        //         {
        //             Id = product.Id,
        //             Title = product.Title,
        //             Description = product.Description,
        //             Price = product.Price,
        //             Quantity = product.Quantity,
        //             SKU = product.SKU,
        //             IsSerializable = product.IsSerializable,
        //             ProductCondition = product.ProductCondition,
        //             CreatedAt = product.CreatedAt,
        //             UpdatedAt = product.UpdatedAt
        //         }
        //     };

        //     return Ok(response);
        // }

        [HttpPost("join")]
        public async Task<IActionResult> JoinRoom([FromBody] JoinAuctionRoomRequestDTO dto)
        {
            var userId = _helperService.GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var res = await _auctionRoomService.JoinRoomAsync(dto.AuctionRoomId, userId);

            return Ok(new { message = "Joined room successfully." });
        }

        [HttpPost("leave")]
        public async Task<IActionResult> LeaveRoom([FromBody] LeaveAuctionRoomRequestDTO dto)
        {
            var userId = _helperService.GetCurrentUserId();
            if (userId == 0) return Unauthorized();
            var res = await _auctionRoomService.LeaveRoomAsync(dto.AuctionRoomId, userId);

            return Ok(new { message = "Left room successfully." });
        }

        [HttpPost("close/{roomId}")]
        [Authorize(Roles = "3,4")]
        public async Task<IActionResult> CloseRoom(int roomId)
        {
            var userId = _helperService.GetCurrentUserId();
            if (userId == 0) return Unauthorized();
            var res = await _auctionRoomService.CloseAuctionRoom(roomId, userId);

            return Ok(new { message = "Room closed." });
        }
    }
}
