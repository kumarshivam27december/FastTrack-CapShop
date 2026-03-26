using CapShop.AuthService.Data;
using CapShop.AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.AuthService.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AuthDbContext _db;

        public AuthRepository(AuthDbContext db)
        {
            _db = db;
        }

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        {
            return _db.Users.AnyAsync(x => x.Email == email, ct);
        }

        public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
        {
            return _db.Roles.FirstOrDefaultAsync(x => x.Name == roleName, ct);
        }

        public Task AddUserAsync(User user, CancellationToken ct = default)
        {
            _db.Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<User?> GetActiveUserByEmailWithRolesAsync(string email, CancellationToken ct = default)
        {
            return _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Email == email && x.IsActive, ct);
        }

        public Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        {
            return _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Email == email, ct);
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _db.SaveChangesAsync(ct);
        }
    }
}