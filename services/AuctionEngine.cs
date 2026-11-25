using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Hubs.Room;

public interface IAuctionEngineScheduler
{
    void Schedule(int roomProductId, DateTime endUtc);   // start, or move to a new end time
    void Cancel(int roomProductId);
    int ActiveTimerCount { get; }
    IReadOnlyCollection<int> ActiveRoomIds { get; }

}

public class AuctionEngineScheduler : IHostedService, IAuctionEngineScheduler
{
    private static readonly TimeSpan MaxDelay = TimeSpan.FromDays(20);   // Task.Delay can't wait ~25 days or more
    private readonly IHelperService _helperService;
    private readonly ConcurrentDictionary<int, CancellationTokenSource> _timers = new();
    public int ActiveTimerCount => _timers.Count;
    public IReadOnlyCollection<int> ActiveRoomIds => _timers.Keys.ToArray();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<AuctionRoom> _hubRoom;
    private readonly ILogger<AuctionEngineScheduler> _logger;

    public AuctionEngineScheduler(IServiceScopeFactory scopeFactory,
        IHubContext<AuctionRoom> hub, ILogger<AuctionEngineScheduler> logger, IHelperService helperService)
    {
        _scopeFactory = scopeFactory;
        _hubRoom = hub;
        _logger = logger;
        _helperService = helperService;
    }

    // Runs once when the app starts: one timer per product that has not been notified yet
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pending = await db.AuctionRoomProducts
            .AsNoTracking()
            .Where(p => p.NotifiedAt == null)
            .Select(p => new { p.Id, p.EndBidTime })
            .ToListAsync(ct);

        foreach (var p in pending) Schedule(p.Id, p.EndBidTime);   // already-ended ones fire immediately
    }

    public Task StopAsync(CancellationToken ct)
    {
        foreach (var cts in _timers.Values) cts.Cancel();
        return Task.CompletedTask;
    }

    public void Schedule(int roomProductId, DateTime endUtc)
    {
        Cancel(roomProductId);                                  // replace any existing timer
        var cts = new CancellationTokenSource();
        _timers[roomProductId] = cts;
        _ = RunAsync(roomProductId, endUtc, cts.Token);
    }

    public void Cancel(int roomProductId)
    {
        if (_timers.TryRemove(roomProductId, out var cts)) cts.Cancel();
    }

    // Waits until the end time, then notifies once
    private async Task RunAsync(int roomProductId, DateTime endUtc, CancellationToken ct)
    {
        try
        {
            var delay = endUtc - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay > MaxDelay ? MaxDelay : delay, ct);

            await NotifyAsync(roomProductId, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ending auction product {Id} failed.", roomProductId);
        }
    }

    private async Task NotifyAsync(int roomProductId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        // Claim it: only one caller can flip NotifiedAt from null, so it is sent once
        var claimed = await db.AuctionRoomProducts
            .Where(p => p.Id == roomProductId && p.NotifiedAt == null && p.EndBidTime <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.NotifiedAt, now), ct);

        if (claimed == 0)
        {
            // Not due yet (the end time was moved later, or this was the long-delay cap): set a new timer
            var end = await db.AuctionRoomProducts
                .Where(p => p.Id == roomProductId && p.NotifiedAt == null)
                .Select(p => (DateTime?)p.EndBidTime)
                .FirstOrDefaultAsync(ct);
            if (end is not null) Schedule(roomProductId, end.Value);
            return;
        }

        var item = await db.AuctionRoomProducts
            .AsNoTracking()
            .Include(p => p.Product)
            .Include(p => p.CurrentHighBidder)
            .Include(a => a.AuctionRoom)
            .FirstAsync(p => p.Id == roomProductId, ct);

        var title = item.Product?.Title ?? "your product";

        // to the winner
        // TODO: Notification receiver
        if (item.CurrentHighBidderID is int winnerId)
            await _hubRoom.Clients.User(winnerId.ToString())
                .SendAsync("Notification", $"You won \"{title}\" for ₱{item.LastBidPrice:N2}!", ct);

        // to the owner
        // TODO: Notification
        if (item.Product is not null)
        {
            var message = item.CurrentHighBidderID is null
                ? $"The auction for \"{title}\" ended with no bids."
                : $"\"{title}\" was sold to {item.CurrentHighBidder?.UserName} for ₱{item.LastBidPrice:N2}.";

            await _hubRoom.Clients.User(item.Product.SellerID.ToString())
                .SendAsync("Notification", message, ct);
        }

        // to Room chat
        var roomGroup = _helperService.EncodeGroupName("room", item.AuctionRoom?.Id ?? 0, item.AuctionRoom?.GroupName ?? "");
        await _hubRoom.Clients.Group(roomGroup).SendAsync("ReceiveRoomMessage",
                    item.CurrentHighBidder?.FullName, "Winner of the bid.", ct);
    }
}