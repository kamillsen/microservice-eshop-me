using Microsoft.EntityFrameworkCore;
using Discount.Grpc.Entities;

namespace Discount.Grpc.Data;

public static class SeedData
{
    public static async Task InitializeAsync(DiscountDbContext context)
    {
        // Zaten veri varsa ekleme
        if (await context.Coupons.AnyAsync())
            return;

        var coupons = new List<Coupon>
        {
            new()
            {
                ProductName = "iPhone 15",
                Description = "Yılbaşı indirimi",
                Amount = 5000
            },
            new()
            {
                ProductName = "Samsung Galaxy S24",
                Description = "Kış kampanyası",
                Amount = 6750
            },
            new()
            {
                ProductName = "MacBook Pro",
                Description = "Öğrenci indirimi",
                Amount = 5000
            }
        };

        await context.Coupons.AddRangeAsync(coupons);
        await context.SaveChangesAsync();
    }
}

