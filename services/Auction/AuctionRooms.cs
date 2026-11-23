using DTO.Response.AuctionRoom;
using Microsoft.EntityFrameworkCore;
using DTO.Response.Product;
using biddingServer.Models;
using Microsoft.AspNetCore.Mvc;
using DTO.Request.AuctionRoom;
using DTO.Response.Account;
using Microsoft.AspNetCore.SignalR;
public interface IAuctionRoomService
{
    Task<List<AuctionRoomResponseDTO>> ManageAuctionRoomsAsync(int userId);
    Task<List<AuctionRoomResponseDTO>> PublicAuctionRoomsAsync(int userId);
    Task<List<AuctionRoomResponseDTO>> GetAuctionRoomsAsync(int userId);
    Task<AuctionRoomModel?> ExistingAuctionRoom(int RoomId, bool track = false);
    Task<ActionResult<AuctionRoomResponseDTO>> LeaveRoomAsync(int roomId, int userId);
    Task<ActionResult<AuctionRoomResponseDTO>> JoinRoomAsync(int roomId, int userId);
    Task<ActionResult<AuctionRoomResponseDTO>> SaveAuctionRoom(CreateAuctionRoomRequestDTO dto, int userId);
    Task<IActionResult> CloseAuctionRoom(int roomId, int userId);
    Task<bool> IsUserAuthorized(int roomId, int userId);
    Task<(AuctionRoomResponseDTO?, string?)> ProductBidPush(int productId, int roomId, int userId, decimal amount);
    Task<AuctionRoomResponseDTO> GetAuctionRoomByIdAsync(int roomId);

}
public class AuctionRoomService : IAuctionRoomService
{
    private readonly ApplicationDbContext _context;
    public AuctionRoomService(ApplicationDbContext context) => _context = context;
    public async Task<AuctionRoomResponseDTO> GetAuctionRoomByIdAsync(int roomId)
    {
        try
        {
            var room = await _context.AuctionRooms
            .Include(r => r.Host)
            .Include(r => r.Members)
            .Include(r => r.AuctionRoomProducts)
            .Where(r => r.Id == roomId )
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
                RoomProducts = r.AuctionRoomProducts == null ? null : r.AuctionRoomProducts.Select(rp => new AuctionRoomProductResponseDTO
                {
                    Id = rp.Id,
                    ProductId = rp.ProductId,
                    LastBidPrice = rp.LastBidPrice,
                    StartingBidPrice = rp.StartingBidPrice,
                    MinimumBidIncrement = rp.MinimumBidIncrement,
                    StartingBidTime = rp.StartingBidTime,
                    EndBidTime = rp.EndBidTime,
                    NumberOfBids = rp.NumberOfBids,
                    CurrentHighBidder = rp.CurrentHighBidder,
                    ProductDetails = rp.Product == null ? null : new ProductResponseDTO
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
                    },
                    BidHistory = _context.Set<AuctionRoomProductTransactionModel>()
                        .Where(t => t.AuctionRoomProductId == rp.Id)
                        .OrderByDescending(t => t.BidTimestamp)
                        .Select(t => new AuctionRoomProductTransactionResponseDTO
                        {
                            Id = t.Id,
                            Bid = t.Bid,
                            BidTimestamp = t.BidTimestamp,
                            AuctionRoomProductId = t.AuctionRoomProductId,
                            CurrentBidder = t.CurrentBidder == null ? null : new BasicInfoDTO
                            {
                                Id = t.CurrentBidder.Id,
                                UserName = t.CurrentBidder.UserName,
                                Email = t.CurrentBidder.Email,
                                FullName = t.CurrentBidder.FullName,
                            }
                        })
                        .ToList()
                }).ToList()
            }).FirstOrDefaultAsync();

            return room ?? new AuctionRoomResponseDTO();
        }catch(Exception ex)
        {
            Console.WriteLine(ex);
            return new AuctionRoomResponseDTO();
        }
    }
    public async Task<List<AuctionRoomResponseDTO>> ManageAuctionRoomsAsync(int userId)
    {
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
            RoomProducts = r.AuctionRoomProducts == null ? null : r.AuctionRoomProducts.Select(rp => new AuctionRoomProductResponseDTO
            {
                ProductId = rp.ProductId,
                LastBidPrice = rp.LastBidPrice,
                StartingBidPrice = rp.StartingBidPrice,
                MinimumBidIncrement = rp.MinimumBidIncrement,
                StartingBidTime = rp.StartingBidTime,
                EndBidTime = rp.EndBidTime,
                NumberOfBids = rp.NumberOfBids,
                CurrentHighBidder = rp.CurrentHighBidder,
                ProductDetails = rp.Product == null ? null : new ProductResponseDTO
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
        return rooms;
    }
    public async Task<List<AuctionRoomResponseDTO>> PublicAuctionRoomsAsync(int userId)
    {
        var rooms = await _context.AuctionRooms
        .Include(r => r.Host)
        .Include(r => r.Members)
        .Include(r => r.AuctionRoomProducts)
        .Where(r => r.Status == RoomStatusEnum.Active && r.Visibility == RoomVisibilityEnum.Public)
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
            Members = r.Members == null ? null : r.Members.Select(rm => new AuctionRoomMemberResponseDTO
            {
                UserId = rm.UserId,
                UserName = rm.User == null ? "": rm.User.UserName,
                Email = rm.User == null ? "": rm.User.Email,
                JoinedAt = rm.JoinedAt
            }).ToList(),
            RoomProducts = r.AuctionRoomProducts == null ? null : r.AuctionRoomProducts.Select(rp => new AuctionRoomProductResponseDTO
            {
                ProductId = rp.ProductId,
                LastBidPrice = rp.LastBidPrice,
                StartingBidPrice = rp.StartingBidPrice,
                MinimumBidIncrement = rp.MinimumBidIncrement,
                StartingBidTime = rp.StartingBidTime,
                EndBidTime = rp.EndBidTime,
                NumberOfBids = rp.NumberOfBids,
                CurrentHighBidder = rp.CurrentHighBidder,
                ProductDetails = rp.Product == null ? null : new ProductResponseDTO
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
        return rooms;
    }
    public async Task<List<AuctionRoomResponseDTO>> GetAuctionRoomsAsync(int userId)
    {
        List<AuctionRoomResponseDTO> manageRoom = await ManageAuctionRoomsAsync(userId);
        List<AuctionRoomResponseDTO> publicRoom = await PublicAuctionRoomsAsync(userId);
        var response = manageRoom
            .Concat(publicRoom)
            .DistinctBy(r => r.Id)
            .ToList();
        return response;
    }
    public async Task<AuctionRoomModel?> ExistingAuctionRoom(int RoomId, bool asNoTrack = true)
    {
        IQueryable<AuctionRoomModel> query = _context.AuctionRooms
            .Include(r => r.Host)
            .Include(r => r.Members)
            .Include(r => r.AuctionRoomProducts!)
                .ThenInclude(rp => rp.Product);

        if (asNoTrack)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(r => r.Id == RoomId);
    }
    public async Task<ActionResult<AuctionRoomResponseDTO>> LeaveRoomAsync(int roomId, int userId)
    {
        var room = await ExistingAuctionRoom(roomId);
        if (room is null) return new NotFoundObjectResult("Room not found!");

        var membership = room.Members?.FirstOrDefault(m => m.UserId == userId);
        if (membership is null) return new NotFoundObjectResult("Your not subscriber here.");

        _context.Remove(membership);
        await _context.SaveChangesAsync();
        return new AuctionRoomResponseDTO
        {
            Id = room.Id,
            GroupName = room.GroupName,
            Title = room.Title,
            Description = room.Description,
            HostId = room.HostId,
            HostUserName = room.Host!.UserName,
            Status = room.Status,
            Visibility = room.Visibility,
            CreatedAt = room.CreatedAt,
            MemberCount = room.Members != null ? room.Members.Count : 0,
            Members = room.Members == null ? null : room.Members.Select(rm => new AuctionRoomMemberResponseDTO
            {
                UserId = rm.UserId,
                UserName = rm.User == null ? "": rm.User.UserName,
                Email = rm.User == null ? "": rm.User.Email,
                JoinedAt = rm.JoinedAt
            }).ToList(),
            RoomProducts = room.AuctionRoomProducts == null ? null : room.AuctionRoomProducts.Select(rp => new AuctionRoomProductResponseDTO
            {
                ProductId = rp.Id,
                LastBidPrice = rp.LastBidPrice,
                StartingBidPrice = rp.StartingBidPrice,
                MinimumBidIncrement = rp.MinimumBidIncrement,
                StartingBidTime = rp.StartingBidTime,
                EndBidTime = rp.EndBidTime,
                NumberOfBids = rp.NumberOfBids,
                CurrentHighBidder = rp.CurrentHighBidder,
                ProductDetails = rp.Product == null ? null : new ProductResponseDTO
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
        };
    }
    public async Task<ActionResult<AuctionRoomResponseDTO>> JoinRoomAsync(int roomId, int userId)
    {
        var existingRoom = await ExistingAuctionRoom(roomId, false);

        if (existingRoom is null)
            return new NotFoundObjectResult("Room not found.");

        if (existingRoom.Status != RoomStatusEnum.Active)
            return new BadRequestObjectResult("This room is not open for subscriptions.");

        if (existingRoom.Members?.Any(m => m.UserId == userId) ?? false)
            return new ConflictObjectResult("You are already a member of this room.");

        existingRoom.Members?.Add(new AuctionRoomMemberModel
        {
            UserId = userId,
            AuctionRoomId = roomId,
            JoinedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return await _context.AuctionRooms
            .Include(r => r.Host)
            .Include(r => r.Members)
            .Include(r => r.AuctionRoomProducts)
            .AsNoTracking()
            .Where(r => r.Id == roomId)
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
                Members = r.Members == null ? null : r.Members.Select(rm => new AuctionRoomMemberResponseDTO
                {
                    UserId = rm.UserId,
                    UserName = rm.User == null ? "": rm.User.UserName,
                    Email = rm.User == null ? "": rm.User.Email,
                    JoinedAt = rm.JoinedAt
                }).ToList(),
                RoomProducts = r.AuctionRoomProducts == null ? null : r.AuctionRoomProducts.Select(rp => new AuctionRoomProductResponseDTO
                {
                    ProductId = rp.Id,
                    LastBidPrice = rp.LastBidPrice,
                    StartingBidPrice = rp.StartingBidPrice,
                    MinimumBidIncrement = rp.MinimumBidIncrement,
                    StartingBidTime = rp.StartingBidTime,
                    EndBidTime = rp.EndBidTime,
                    NumberOfBids = rp.NumberOfBids,
                    CurrentHighBidder = rp.CurrentHighBidder,
                    ProductDetails = rp.Product == null ? null : new ProductResponseDTO
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
            }).FirstAsync();

    }
    public async Task<ActionResult<AuctionRoomResponseDTO>> SaveAuctionRoom(CreateAuctionRoomRequestDTO dto, int userId)
    {
        var roomId = dto.Id ?? 0;
        if (roomId > 0)
        {
            // EDIT existing room
            var existingRoom = await _context.AuctionRooms
                .Include(r => r.AuctionRoomProducts!)
                    .ThenInclude(rp => rp.Product)
                .Include(m => m.Members)
                .FirstOrDefaultAsync(r => r.Id == dto.Id);

            if (existingRoom is null)
                return new NotFoundObjectResult("Room not found.");

            if (existingRoom.HostId != userId) return new ObjectResult("Only the host or an administrator can edit this room.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

            if (dto.RoomProducts.Select(p => p.ProductId).Distinct().Count() != dto.RoomProducts.Count)
                return new BadRequestObjectResult("Duplicate product");

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
                    link.NotifiedAt = null;     // reset Notification
                }
            }

            existingRoom.GroupName = dto.GroupName;
            existingRoom.Title = dto.Title;
            existingRoom.Description = dto.Description;
            existingRoom.HostId = userId;
            existingRoom.Status = RoomStatusEnum.Active;
            existingRoom.Visibility = (RoomVisibilityEnum)dto.Visibility;

        }
        else
        {
            if (await _context.AuctionRooms.AnyAsync(r => r.GroupName == dto.GroupName))
                return new BadRequestObjectResult("A room with this group name already exists.");

            var room = new AuctionRoomModel
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
            roomId = room.Id;
        }
        await _context.SaveChangesAsync();

        var _room = await ExistingAuctionRoom(roomId);

        if(_room == null) return new ConflictObjectResult("Something went wrong on saving");

        return new AuctionRoomResponseDTO
        {
            Id = _room.Id,
            GroupName = _room.GroupName,
            Title = _room.Title,
            Description = _room.Description,
            HostId = _room.HostId,
            Status = _room.Status,
            Visibility = _room.Visibility,
            CreatedAt = _room.CreatedAt,
            MemberCount = _room.Members != null ? _room.Members.Count : 0,
            Members = _room.Members == null ? null : _room.Members.Select(rm => new AuctionRoomMemberResponseDTO
            {
                UserId = rm.UserId,
                UserName = rm.User == null ? "": rm.User.UserName,
                Email = rm.User == null ? "": rm.User.Email,
                JoinedAt = rm.JoinedAt
            }).ToList(),
            RoomProducts = _room.AuctionRoomProducts != null ? _room.AuctionRoomProducts
                .Select(p => new AuctionRoomProductResponseDTO
                {
                    // Map your properties here
                    Id = p.Id,
                    ProductId = p.ProductId,
                    StartingBidPrice = p.StartingBidPrice,
                    MinimumBidIncrement = p.MinimumBidIncrement,
                    StartingBidTime = p.StartingBidTime,
                    EndBidTime = p.EndBidTime,
                    LastBidPrice = p.LastBidPrice,
                    NotifiedAt = p.NotifiedAt,
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
                }).ToList() : []

        };
    }
    public async Task<IActionResult> CloseAuctionRoom(int roomId, int userId)
    {

        var room = await _context.AuctionRooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null) return new NotFoundObjectResult("Room not found.");

        if (room.HostId != userId) return new ObjectResult("Only the host can close this room.")
        {
            StatusCode = StatusCodes.Status403Forbidden
        };

        room.Status = RoomStatusEnum.Closed;
        room.ClosedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return new NoContentResult();
    }
    public async Task<bool> IsUserAuthorized(int roomId, int userId)
    {
        return await _context.AuctionRooms
            .AsNoTracking()
            .AnyAsync(r =>
                r.Id == roomId &&
                (
                    r.HostId == (int)userId ||
                    (r.Members != null && r.Members.Any(m => m.UserId == userId))
                )
            );
    }
    public async Task<(AuctionRoomResponseDTO?, string?)> ProductBidPush(int productId, int roomId, int userId, decimal amount)
    {
        amount = Math.Round(amount, 2);                      // the column is decimal(10, 2)
        if (amount <= 0) throw new HubException("Invalid: Bid must be greater than zero.");

        // Any early "return false" below disposes the transaction without a commit,
        // which rolls back and releases the lock.
        await using var tx = await _context.Database.BeginTransactionAsync();

        // 1. Lock this product's row: other bidders wait here until we commit or roll back
        var roomProduct = await _context.AuctionRoomProducts
            .FromSqlInterpolated($@"
                SELECT * FROM AuctionRoomProducts WITH (UPDLOCK, ROWLOCK)
                WHERE ProductId = {productId} AND AuctionRoomId = {roomId}")
            .FirstOrDefaultAsync();

        if (roomProduct is null) throw new HubException("Not Found: This product is not in the room.");               // product isn't in this room

        // The room itself must be open
        var roomIsActive = await _context.AuctionRooms
            .AnyAsync(r => r.Id == roomId && r.Status == RoomStatusEnum.Active);
        if (!roomIsActive) throw new HubException("Unauthorized: This room is not open for bidding.");

        // 2. The bidding window must cover the current time
        var now = DateTime.UtcNow;
        if (now < roomProduct.StartingBidTime || now >= roomProduct.EndBidTime)
            throw new HubException("Unauthorized: Bidding has not started yet.");

        // 3. Minimum bid, read from the locked row so it can't change under us
        var minimum = roomProduct.LastBidPrice + roomProduct.MinimumBidIncrement;
        if (amount < minimum) throw new HubException("Unauthorized: Bidding has ended.");

        // 4. Check last bidder: if the same user is trying to bid again, reject it
        if (roomProduct.CurrentHighBidderID == userId)
            throw new HubException("Invalid: You're already the highest bidder.");

        // 5. Update the product and record the bid in the history table
        roomProduct.LastBidPrice = amount;
        roomProduct.CurrentHighBidderID = userId;
        roomProduct.NumberOfBids = (roomProduct.NumberOfBids ?? 0) + 1;

        _context.Add(new AuctionRoomProductTransactionModel
        {
            AuctionRoomProduct = roomProduct,
            Bid = amount,
            CurrentBidderID = userId,
            BidTimestamp = now
        });

        // 6. Save and release the lock
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        var room = await _context.AuctionRooms
            .Include(r => r.Host)
            .Include(r => r.Members)
            .Include(r => r.AuctionRoomProducts)
            .AsNoTracking()
            .Where(r => r.Id == roomId)
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
                Members = r.Members == null ? null : r.Members.Select(rm => new AuctionRoomMemberResponseDTO
                {
                    UserId = rm.UserId,
                    UserName = rm.User == null ? "": rm.User.UserName,
                    Email = rm.User == null ? "": rm.User.Email,
                    JoinedAt = rm.JoinedAt
                }).ToList(),
                RoomProducts = r.AuctionRoomProducts == null ? null : r.AuctionRoomProducts.Select(rp => new AuctionRoomProductResponseDTO
                {
                    ProductId = rp.Id,
                    LastBidPrice = rp.LastBidPrice,
                    StartingBidPrice = rp.StartingBidPrice,
                    MinimumBidIncrement = rp.MinimumBidIncrement,
                    StartingBidTime = rp.StartingBidTime,
                    EndBidTime = rp.EndBidTime,
                    NumberOfBids = rp.NumberOfBids,
                    CurrentHighBidder = rp.CurrentHighBidder,
                    ProductDetails = rp.Product == null ? null : new ProductResponseDTO
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
            }).FirstAsync();
        return (room != null) ? (room, null) : (null, "Someting went wrong");

    }
}
