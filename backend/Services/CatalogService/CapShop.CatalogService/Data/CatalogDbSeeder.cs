using CapShop.CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.CatalogService.Data
{
    public static class CatalogDbSeeder
    {
        public static async Task SeedAsync(CatalogDbContext db)
        {
            await db.Database.MigrateAsync();

            if (!await db.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    new Category { Name = "Electronics", Description = "Electronic gadgets and devices" },
                    new Category { Name = "Clothing", Description = "Apparel and fashion items" },
                    new Category { Name = "Books", Description = "Educational and entertainment books" },
                    new Category { Name = "Home", Description = "Home and kitchen appliances" }
                };

                db.Categories.AddRange(categories);
                await db.SaveChangesAsync();
            }
            if (!await db.Products.AnyAsync())
            {
                var electronics = await db.Categories.FirstAsync(x => x.Name == "Electronics");
                var clothing = await db.Categories.FirstAsync(x => x.Name == "Clothing");
                var books = await db.Categories.FirstAsync(x => x.Name == "Books");

                var products = new[]
                {
                    new Product { Name = "Wireless Headphones", Description = "Premium noise-cancelling headphones", Price = 79.99m, Stock = 50, Category = electronics, ImageUrl = "https://via.placeholder.com/300?text=Headphones" },
                    new Product { Name = "USB-C Cable", Description = "Durable 2m USB-C cable", Price = 12.99m, Stock = 200, Category = electronics, ImageUrl = "https://via.placeholder.com/300?text=USB-C" },
                    new Product { Name = "Cotton T-Shirt", Description = "100% organic cotton t-shirt", Price = 24.99m, Stock = 100, Category = clothing, ImageUrl = "https://via.placeholder.com/300?text=T-Shirt" },
                    new Product { Name = "Denim Jeans", Description = "Classic blue denim jeans", Price = 59.99m, Stock = 75, Category = clothing, ImageUrl = "https://via.placeholder.com/300?text=Jeans" },
                    new Product { Name = "C# Programming", Description = "Learn C# from scratch", Price = 34.99m, Stock = 30, Category = books, ImageUrl = "https://via.placeholder.com/300?text=C%23+Book" },
                    new Product { Name = "Web Development Guide", Description = "Complete web development course", Price = 44.99m, Stock = 40, Category = books, ImageUrl = "https://via.placeholder.com/300?text=Web+Dev" }
                };

                db.Products.AddRange(products);
                await db.SaveChangesAsync();
            }
        }
    }
}