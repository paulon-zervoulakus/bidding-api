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
            .WithMany()
            .HasForeignKey(t => t.AuctionRoomProductId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
