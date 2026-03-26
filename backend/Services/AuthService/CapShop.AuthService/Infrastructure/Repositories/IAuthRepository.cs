using CapShop.AuthService.Models;

namespace CapShop.AuthService.Infrastructure.Repositories
{
    public interface IAuthRepository
    {
        Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
        Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default);
        Task AddUserAsync(User user, CancellationToken ct = default);
        Task<User?> GetActiveUserByEmailWithRolesAsync(string email, CancellationToken ct = default);
        Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}