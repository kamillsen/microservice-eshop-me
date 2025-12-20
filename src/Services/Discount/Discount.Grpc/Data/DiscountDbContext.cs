using Microsoft.EntityFrameworkCore;
using Discount.Grpc.Entities;

namespace Discount.Grpc.Data;

public class DiscountDbContext : DbContext
{
    public DiscountDbContext(DbContextOptions<DiscountDbContext> options) 
        : base(options) { }

    public DbSet<Coupon> Coupons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id)
                .ValueGeneratedOnAdd();
            
            entity.HasIndex(c => c.ProductName)
                .IsUnique();
            
            entity.Property(c => c.ProductName)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(c => c.Description)
                .HasMaxLength(500);
            
            entity.Property(c => c.Amount)
                .IsRequired();
            
            entity.ToTable("Coupons");
        });
    }
}

