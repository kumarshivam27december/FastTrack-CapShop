using System.Data;
using CapShop.AuthService.Models;
using Microsoft.EntityFrameworkCore;
namespace CapShop.AuthService.Data
{
    public class AuthDbSeeder
    {
        public static async Task SeedAsync(AuthDbContext db)
        {
            await db.Database.MigrateAsync();
            if(!await db.Roles.AnyAsync())
            {
                db.Roles.AddRange(
                    new Role { Name = "Customer" },
                    new Role { Name = "Admin" }
                );
                await db.SaveChangesAsync();
            }

            var adminEmail = "admin@capshop.com";
            var adminExists = await db.Users.AnyAsync(u => u.Email == adminEmail);
            if(!adminExists)
            {
               var adminRole = await db.Roles.FirstAsync(x=>x.Name=="Admin");
               var admin = new User
               {
                   FullName = "CapShop Admin",
                   Email = adminEmail,
                   Phone = "9999999999",
                   PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                   IsActive = true
               };

               admin.UserRoles.Add(new UserRole {User = admin,Role = adminRole});
               db.Users.Add(admin);
               await db.SaveChangesAsync();
            }
        }
    }
}