using Microsoft.EntityFrameworkCore;
using biddingServer.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    public DbSet<AccountModel> Accounts { get; set; }
    public DbSet<ProductModel> Products { get; set; }
    public DbSet<ProductCategoryModel> ProductCategories { get; set; }
    public DbSet<ProductImagesModel> ProductImages { get; set; }
    public DbSet<ProductSerialModel> ProductSerials { get; set; }
    public DbSet<AuctionRoomModel> AuctionRooms { get; set; }
    public DbSet<AuctionRoomMemberModel> AuctionRoomMembers { get; set; }
    public DbSet<AuctionRoomProductTransactionModel> AuctionRoomProductTransactions { get; set; }
    public DbSet<AuctionRoomProductModel> AuctionRoomProducts { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }
    public DbSet<WalletTopUp> WalletTopUps { get; set; }
    public DbSet<WalletWithdrawal> WalletWithdrawals { get; set; }
    public DbSet<ReservationHold> ReservationHolds { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<AuctionRoomMemberModel>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AuctionRoomMemberModel>()
            .HasOne(m => m.AuctionRoom)
            .WithMany(r => r.Members)
            .HasForeignKey(m => m.AuctionRoomId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AuctionRoomModel>()
            .HasOne(r => r.Host)
            .WithMany()
            .HasForeignKey(r => r.HostId)
            .OnDelete(DeleteBehavior.NoAction);
        // Product -> AuctionRoom: many products to one room
        modelBuilder.Entity<AuctionRoomProductModel>()
            .HasOne(p => p.AuctionRoom)
            .WithMany(r => r.AuctionRoomProducts)
            .HasForeignKey(p => p.AuctionRoomId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AuctionRoomProductModel>()
            .HasOne(arp => arp.Product)
            .WithMany()
            .HasForeignKey(arp => arp.ProductId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AuctionRoomProductTransactionModel>()
            .HasOne(t => t.AuctionRoomProduct)
            .WithMany(p => p.BidHistory)
            .HasForeignKey(t => t.AuctionRoomProductId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<Wallet>(e =>
        {
            e.HasIndex(w => w.UserId).IsUnique();
            e.Property(w => w.CashPoints).HasColumnType("decimal(18,2)");
            e.Property(w => w.MeritPoints).HasColumnType("decimal(18,2)");
            e.ToTable(t => t.HasCheckConstraint("CK_Wallet_NonNegative", "CashPoints >= 0 AND MeritPoints >= 0"));
        });
        modelBuilder.Entity<WalletTransaction>(e =>
        {
            e.HasIndex(t => t.IdempotencyKey).IsUnique();
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.BalanceAfter).HasColumnType("decimal(18,2)");
            e.Property(t => t.MeritRate).HasColumnType("decimal(18,2)");
            e.Property(t => t.CashEquivalent).HasColumnType("decimal(18,2)");
        });
        modelBuilder.Entity<WalletTopUp>(e =>
        {
            e.HasIndex(t => t.XenditReferenceId).IsUnique();
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.Fee).HasColumnType("decimal(18,2)");
        });
        modelBuilder.Entity<WalletWithdrawal>(e =>
        {
            e.HasIndex(w => w.XenditReferenceId).IsUnique();
            e.Property(w => w.Amount).HasColumnType("decimal(18,2)");
            e.Property(w => w.Fee).HasColumnType("decimal(18,2)");
            e.Property(w => w.NetAmount).HasColumnType("decimal(18,2)");
        });
        modelBuilder.Entity<ReservationHold>(e =>
        {
            e.Property(r => r.CashAmount).HasColumnType("decimal(18,2)");
            e.Property(r => r.MeritAmount).HasColumnType("decimal(18,2)");
            e.Property(r => r.MeritRate).HasColumnType("decimal(18,2)");
        });
    }
}
