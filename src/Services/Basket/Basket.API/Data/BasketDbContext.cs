using Basket.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Basket.API.Data;

public class BasketDbContext : DbContext
{
    public BasketDbContext(DbContextOptions<BasketDbContext> options) : base(options)
    {
    }

    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShoppingCart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserName).IsUnique();
            // Navigation property olmadan relationship tanımı (best practice - döngüsel referans yok)
            entity.HasMany(e => e.Items)
                .WithOne() // Navigation property yok
                .HasForeignKey(e => e.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ShoppingCartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}

