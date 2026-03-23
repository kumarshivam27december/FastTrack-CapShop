using Microsoft.EntityFrameworkCore;

namespace CapShop.OrderService.Data;

public static class OrderDbSeeder
{
    public static async Task SeedAsync(OrderDbContext db)
    {
        await db.Database.MigrateAsync();
        // Sample data will be created dynamically on cart/order operations
    }
}