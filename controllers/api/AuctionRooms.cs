using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using biddingServer.Models;
using RequestDTO = DTO.Request.AuctionRoom;
using ResponseDTO = DTO.Response.AuctionRoom;
using SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using DTO.Response.AuctionRoom;
using DTO.Response.Product;

namespace biddingServer.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuctionRooms : ControllerBase
    {
        private readonly IHubContext<Bidding> _hubContext;
        private readonly ApplicationDbContext _context;

        public AuctionRooms(ApplicationDbContext context, IHubContext<Bidding> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private int GetCurrentUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userId, out var id) ? id : 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        }


        [HttpGet("get-public-rooms")]
        [Authorize]
        public async Task<ActionResult<List<AuctionRoomResponseDTO>>> GetPublicRooms()
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var rooms = await _context.AuctionRooms
                .Include(r => r.Host)
                .Include(r => r.Members)
                .Include(r => r.AuctionRoomProducts)
                .Where(r => r.Status == RoomStatusEnum.Active || r.Visibility == RoomVisibilityEnum.Public ||
                    (r.Members != null && r.Members.Any(m => m.UserId == userId) ))
                .Select(r => new AuctionRoomResponseDTO
                {
                    Id = r.Id,
                    GroupName = r.GroupName,
                    Title = r.Title,
                    Description = r.Description,
                    HostId = r.HostId,
                    HostUserName = r.Host!.UserName,
                    Status = r.Status,
                    Visibility = r.Visibility,
                    CreatedAt = r.CreatedAt,
                    MemberCount = r.Members != null ? r.Members.Count : 0,
                    RoomProducts = r.AuctionRoomProducts == null ? null : r.AuctionRoomProducts.Select(rp => new ResponseDTO.AuctionRoomProductResponseDTO
                    {
                        ProductId = rp.Id,
                        LastBidPrice = rp.LastBidPrice,
                        StartingBidPrice = rp.StartingBidPrice,
                        MinimumBidIncrement = rp.MinimumBidIncrement,
                        StartingBidTime = rp.StartingBidTime,
                        EndBidTime = rp.EndBidTime,
                        NumberOfBids = rp.NumberOfBids,
                        CurrentHighBidder = rp.CurrentHighBidder,
                        ProductDetails = rp.Product == null ? null : new DTO.Response.Product.ProductResponseDTO
                        {
                            Id = rp.Product.Id,
                            Title = rp.Product.Title,
                            Description = rp.Product.Description,
                            Price = rp.Product.Price,
                            Quantity = rp.Product.Quantity,
                            SKU = rp.Product.SKU,
                            IsSerializable = rp.Product.IsSerializable,
                            ProductCondition = rp.Product.ProductCondition,
                            CreatedAt = rp.Product.CreatedAt,
                            UpdatedAt = rp.Product.UpdatedAt
                        }
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new { rooms });
        }

        [HttpGet("get-manage-rooms")]
        [Authorize]
        public async Task<ActionResult<List<AuctionRoomResponseDTO>>> GetManageRooms()
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var rooms = await _context.AuctionRooms
                .Include(r => r.Host)
                .Include(r => r.Members)
                .Include(r => r.AuctionRoomProducts)
                .Where(r => r.HostId == userId )
                .Select(r => new AuctionRoomResponseDTO
                {
                    Id = r.Id,
                    GroupName = r.GroupName,
                    Title = r.Title,
                    Description = r.Description,
                    HostId = r.HostId,
                    HostUserName = r.Host!.UserName,
                    Status = r.Status,
                    Visibility = r.Visibility,
                    CreatedAt = r.CreatedAt,
                    MemberCount = r.Members != null ? r.Members.Count : 0,
                    Members = r.Members != null ? r.Members.Select(m => new AuctionRoomMemberResponseDTO
                    {
                        UserId = m.Id,
                        UserName = m.User != null ? m.User.UserName : "",
                        Email = m.User != null ? m.User.Email : "",
                        JoinedAt = m.JoinedAt
                    }).ToList() : null,
                    RoomProducts = r.AuctionRoomProducts == null ? null : r.AuctionRoomProducts.Select(rp => new ResponseDTO.AuctionRoomProductResponseDTO
                    {
                        ProductId = rp.Id,
                        LastBidPrice = rp.LastBidPrice,
                        StartingBidPrice = rp.StartingBidPrice,
                        MinimumBidIncrement = rp.MinimumBidIncrement,
                        StartingBidTime = rp.StartingBidTime,
                        EndBidTime = rp.EndBidTime,
                        NumberOfBids = rp.NumberOfBids,
                        CurrentHighBidder = rp.CurrentHighBidder,
                        ProductDetails = rp.Product == null ? null : new DTO.Response.Product.ProductResponseDTO
                        {
                            Id = rp.Product.Id,
                            Title = rp.Product.Title,
                            Description = rp.Product.Description,
                            Price = rp.Product.Price,
                            Quantity = rp.Product.Quantity,
                            SKU = rp.Product.SKU,
                            IsSerializable = rp.Product.IsSerializable,
                            ProductCondition = rp.Product.ProductCondition,
                            CreatedAt = rp.Product.CreatedAt,
                            UpdatedAt = rp.Product.UpdatedAt
                        }
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new { rooms });
        }
        [HttpPost("save-room")]
        [Authorize(Roles = "Administrator,Seller")]
        public async Task<ActionResult<ResponseDTO.AuctionRoomResponseDTO>> SaveAuctionRoom(
            [FromBody] RequestDTO.CreateAuctionRoomRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var role = GetCurrentUserRole();
            if(role != "Administrator" && role != "Seller" )
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Not Allowed");
            }


            AuctionRoomModel room;

            if (dto.Id is > 0){
                // EDIT existing room
                var existingRoom = await _context.AuctionRooms
                    .Include(r => r.AuctionRoomProducts!)
                        .ThenInclude(rp => rp.Product)
                    .FirstOrDefaultAsync(r => r.Id == dto.Id.Value);

                if (existingRoom is null)
                    return NotFound("Room not found.");

                if (existingRoom.HostId != userId)
                    return StatusCode(StatusCodes.Status403Forbidden,
                        "Only the host or an administrator can edit this room.");

                if (dto.RoomProducts.Select(p => p.ProductId).Distinct().Count() != dto.RoomProducts.Count)
                    return BadRequest("Duplicate product");

                var links = existingRoom.AuctionRoomProducts;

                // delete links for deselected products
                var toRemove = links?
                    .Where(l => !dto.RoomProducts.Any(p => p.ProductId == l.ProductId))
                    .ToList();
                if(toRemove is not null) _context.RemoveRange(toRemove);

                foreach (var item in dto.RoomProducts)
                {
                    var link = links?.FirstOrDefault(l => l.ProductId == item.ProductId);

                    if (link is null)
                    {
                        // newly selected product
                        links?.Add(new AuctionRoomProductModel
                        {
                            ProductId = item.ProductId,
                            StartingBidPrice = item.StartingBidPrice,
                            MinimumBidIncrement = item.MinimumBidIncrement,
                            StartingBidTime = item.StartingBidTime,
                            EndBidTime = item.EndBidTime,
                            LastBidPrice = item.StartingBidPrice   // server-set; see note below
                        });
                    }
                    else
                    {
                        // product already in the room: update the editable fields only
                        link.StartingBidPrice = item.StartingBidPrice;
                        link.MinimumBidIncrement = item.MinimumBidIncrement;
                        link.StartingBidTime = item.StartingBidTime;
                        link.EndBidTime = item.EndBidTime;
                        // LastBidPrice is deliberately not copied from the client
                    }
                }

                existingRoom.GroupName = dto.GroupName;
                existingRoom.Title = dto.Title;
                existingRoom.Description = dto.Description;
                existingRoom.HostId = userId;
                existingRoom.Status = RoomStatusEnum.Active;
                existingRoom.Visibility = (RoomVisibilityEnum)dto.Visibility;
                room = existingRoom;
            }
            else
            {
                // CREATE new room
                if (await _context.AuctionRooms.AnyAsync(r => r.GroupName == dto.GroupName))
                    return BadRequest("A room with this group name already exists.");

                room = new AuctionRoomModel
                {
                    GroupName = dto.GroupName,
                    Title = dto.Title,
                    Description = dto.Description,
                    HostId = userId,
                    Status = RoomStatusEnum.Active,
                    Visibility = RoomVisibilityEnum.Private,
                    CreatedAt = DateTime.UtcNow,
                    AuctionRoomProducts = dto.RoomProducts
                        .DistinctBy(p => p.ProductId)
                        .Select(p => new AuctionRoomProductModel
                        {
                            ProductId = p.ProductId,
                            StartingBidPrice = p.StartingBidPrice,
                            MinimumBidIncrement = p.MinimumBidIncrement,
                            StartingBidTime = p.StartingBidTime,
                            EndBidTime = p.EndBidTime,
                            LastBidPrice = p.StartingBidPrice
                        })
                        .ToList()
                };
                _context.AuctionRooms.Add(room);
            }


            var response = new AuctionRoomResponseDTO
            {
                Id = room.Id,
                GroupName = room.GroupName,
                Title = room.Title,
                Description = room.Description,
                HostId = room.HostId,
                HostUserName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                Status = room.Status,
                Visibility = room.Visibility,
                CreatedAt = room.CreatedAt,
                MemberCount = 0,
                RoomProducts = room.AuctionRoomProducts != null ? room.AuctionRoomProducts
                    .Select(p => new AuctionRoomProductResponseDTO
                    {
                        // Map your properties here
                        ProductId = p.ProductId,
                        StartingBidPrice = p.StartingBidPrice,
                        MinimumBidIncrement = p.MinimumBidIncrement,
                        StartingBidTime = p.StartingBidTime,
                        EndBidTime = p.EndBidTime,
                        LastBidPrice = p.StartingBidPrice,
                        ProductDetails = p.Product != null
                            ? new ProductResponseDTO
                            {
                                // Map your Product properties here
                                Id = p.Product.Id,
                                Title = p.Product.Title,
                                Description = p.Product.Description,
                                Price = p.Product.Price
                                // Add other properties
                            }
                            : null
                    })
                    .ToList() : []
            };

            var roomGroup = $"{room.Id}-{room.GroupName}";

            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group(roomGroup).SendAsync("UpdateAuctionRoom", response);

            return Ok(response);
        }

        [HttpPost("{roomId}/products")]
        [Authorize(Roles = "Administrator,Seller")]
        public async Task<ActionResult<ResponseDTO.AuctionRoomProductResponseDTO>> AddProductToRoom(int roomId, [FromBody] RequestDTO.AuctionRoomProductRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var room = await _context.AuctionRooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null) return NotFound("Room not found.");

            if (room.HostId != userId)
            {
                var role = GetCurrentUserRole();
                if (role != "Administrator") return Forbid("Only the host can add products.");
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);
            if (product == null) return NotFound("Product not found.");

            var exists = await _context.AuctionRoomProducts.AnyAsync(rp => rp.ProductId == dto.ProductId);
            if (exists) return BadRequest("Product already added to this room.");

            var roomProduct = new AuctionRoomProductModel
            {
                ProductId = dto.ProductId,
                LastBidPrice = dto.lastBidPrice,
                StartingBidPrice = dto.StartingBidPrice,
                MinimumBidIncrement = dto.MinimumBidIncrement,
                StartingBidTime = dto.StartingBidTime,
                EndBidTime = dto.EndBidTime,
                NumberOfBids = 0
            };

            _context.AuctionRoomProducts.Add(roomProduct);
            await _context.SaveChangesAsync();

            var response = new ResponseDTO.AuctionRoomProductResponseDTO
            {
                ProductId = roomProduct.Id,
                LastBidPrice = roomProduct.LastBidPrice,
                StartingBidPrice = roomProduct.StartingBidPrice,
                MinimumBidIncrement = roomProduct.MinimumBidIncrement,
                StartingBidTime = roomProduct.StartingBidTime,
                EndBidTime = roomProduct.EndBidTime,
                NumberOfBids = roomProduct.NumberOfBids,
                ProductDetails = new DTO.Response.Product.ProductResponseDTO
                {
                    Id = product.Id,
                    Title = product.Title,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = product.Quantity,
                    SKU = product.SKU,
                    IsSerializable = product.IsSerializable,
                    ProductCondition = product.ProductCondition,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                }
            };

            return Ok(response);
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinRoom([FromBody] RequestDTO.JoinAuctionRoomRequestDTO dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var room = await _context.AuctionRooms
                .Include(r => r.Members)
                .FirstOrDefaultAsync(r => r.Id == dto.AuctionRoomId && r.Status == RoomStatusEnum.Active);

            if (room == null) return NotFound("Room not found or not active.");

            var alreadyMember = room.Members.Any(m => m.UserId == userId);
            if (alreadyMember) return BadRequest("You are already a member of this room.");

            var member = new AuctionRoomMemberModel
            {
                AuctionRoomId = room.Id,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            };

            _context.AuctionRoomMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Joined room successfully." });
        }

        [HttpPost("leave")]
        public async Task<IActionResult> LeaveRoom([FromBody] RequestDTO.LeaveAuctionRoomRequestDTO dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var member = await _context.AuctionRoomMembers
                .FirstOrDefaultAsync(m => m.AuctionRoomId == dto.AuctionRoomId && m.UserId == userId);

            if (member == null) return NotFound("You are not a member of this room.");

            _context.AuctionRoomMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Left room successfully." });
        }

        [HttpPost("close/{roomId}")]
        [Authorize(Roles = "3,4")]
        public async Task<IActionResult> CloseRoom(int roomId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var room = await _context.AuctionRooms.FirstOrDefaultAsync(r => r.Id == roomId);
            if (room == null) return NotFound("Room not found.");

            if (room.HostId != userId) return Forbid("Only the host can close this room.");

            room.Status = RoomStatusEnum.Closed;
            room.ClosedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Room closed." });
        }
    }
}
