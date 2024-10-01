using Microsoft.EntityFrameworkCore;
using biddingServer.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    public DbSet<AccountModel> Accounts { get; set; }
    public DbSet<ProductModel> Products { get; set; }
    public DbSet<ProductBiddingModel> ProductBiddings { get; set; }
    public DbSet<ProductBiddingTransactionModel> ProductBiddingTransactions { get; set; }
    public DbSet<ProductCategoryModel> ProductCategories { get; set; }
    public DbSet<ProductImagesModel> ProductImages { get; set; }
    public DbSet<ProductSerialModel> ProductSerials { get; set; }
}
