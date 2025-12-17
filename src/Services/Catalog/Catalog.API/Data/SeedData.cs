using Catalog.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Data;

public static class SeedData
{
    public static async Task InitializeAsync(CatalogDbContext context)
    {
        // Kategorileri kontrol et ve ekle
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category { Id = Guid.NewGuid(), Name = "Elektronik" },
                new Category { Id = Guid.NewGuid(), Name = "Giyim" },
                new Category { Id = Guid.NewGuid(), Name = "Ev & Yaşam" }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // Ürünleri kontrol et ve ekle
        if (!await context.Products.AnyAsync())
        {
            var elektronik = await context.Categories.FirstAsync(c => c.Name == "Elektronik");
            var giyim = await context.Categories.FirstAsync(c => c.Name == "Giyim");
            var evYasam = await context.Categories.FirstAsync(c => c.Name == "Ev & Yaşam");

            var products = new List<Product>
            {
                // Elektronik kategorisi ürünleri
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "iPhone 15",
                    Description = "Apple iPhone 15 128GB",
                    Price = 35000.00m,
                    ImageUrl = "https://images.pexels.com/photos/18525574/pexels-photo-18525574.jpeg",
                    CategoryId = elektronik.Id
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Samsung Galaxy S24",
                    Description = "Samsung Galaxy S24 256GB",
                    Price = 32000.00m,
                    ImageUrl = "https://images.pexels.com/photos/15493878/pexels-photo-15493878.jpeg",
                    CategoryId = elektronik.Id
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "MacBook Pro",
                    Description = "Apple MacBook Pro 14 inch M3",
                    Price = 55000.00m,
                    ImageUrl = "https://images.pexels.com/photos/249538/pexels-photo-249538.jpeg",
                    CategoryId = elektronik.Id
                },

                // Giyim kategorisi ürünleri
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Beyaz T-Shirt",
                    Description = "Rahat kesim pamuklu t-shirt",
                    Price = 150.00m,
                    ImageUrl = "https://images.pexels.com/photos/35203728/pexels-photo-35203728.jpeg",
                    CategoryId = giyim.Id
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Siyah Pantolon",
                    Description = "Klasik kesim siyah pantolon",
                    Price = 450.00m,
                    ImageUrl = "https://images.pexels.com/photos/10360630/pexels-photo-10360630.jpeg",
                    CategoryId = giyim.Id
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Spor Ayakkabı",
                    Description = "Rahat koşu ayakkabısı",
                    Price = 1200.00m,
                    ImageUrl = "https://images.pexels.com/photos/2996261/pexels-photo-2996261.jpeg",
                    CategoryId = giyim.Id
                },

                // Ev & Yaşam kategorisi ürünleri
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Ofis Masası",
                    Description = "Ahşap ofis masası 120x60cm",
                    Price = 2500.00m,
                    ImageUrl = "https://images.pexels.com/photos/4348395/pexels-photo-4348395.jpeg",
                    CategoryId = evYasam.Id
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Rahat Sandalye",
                    Description = "Ergonomik ofis koltuğu",
                    Price = 1800.00m,
                    ImageUrl = "https://images.pexels.com/photos/116910/pexels-photo-116910.jpeg",
                    CategoryId = evYasam.Id
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "LED Lamba",
                    Description = "Modern LED masa lambası",
                    Price = 350.00m,
                    ImageUrl = "https://images.pexels.com/photos/7439754/pexels-photo-7439754.jpeg",
                    CategoryId = evYasam.Id
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }
    }
}

