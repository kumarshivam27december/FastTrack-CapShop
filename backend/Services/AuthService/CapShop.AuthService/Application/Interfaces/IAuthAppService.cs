using CapShop.AuthService.DTOs.Auth;
using System.Security.Claims;

namespace CapShop.AuthService.Application.Interfaces
{
    public interface IAuthAppService
    {
        Task SignupAsync(SignupRequestDto request, CancellationToken ct = default);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
        Task<object> GetMeAsync(ClaimsPrincipal user, CancellationToken ct = default);
    }
}