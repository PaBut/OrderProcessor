using Microsoft.EntityFrameworkCore;
using OrderProcessor.Entities;

namespace OrderProcessor.DatabaseContext;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options){}

    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().ToTable("Orders");
        base.OnModelCreating(modelBuilder);
    }
}