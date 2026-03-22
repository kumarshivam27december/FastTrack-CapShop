using CapShop.AuthService.Models;
namespace CapShop.AuthService.Services.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user, IEnumerable<string> roles);
    }
}