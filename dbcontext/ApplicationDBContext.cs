using Microsoft.EntityFrameworkCore;
using Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    public DbSet<AccountModel> Accounts { get; set; }
}
