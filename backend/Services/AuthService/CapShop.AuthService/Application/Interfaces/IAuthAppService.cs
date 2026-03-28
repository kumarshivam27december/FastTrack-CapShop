using CapShop.AuthService.DTOs.Auth;
using System.Security.Claims;

namespace CapShop.AuthService.Application.Interfaces
{
    public interface IAuthAppService
    {
        Task SignupAsync(SignupRequestDto request, CancellationToken ct = default);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
        Task<object> GetMeAsync(ClaimsPrincipal user, CancellationToken ct = default);
          Task<TwoFactorAuthResponseDto> LoginStep1Async(LoginRequestDto request, CancellationToken ct = default);
          Task<SendOtpResponseDto> SendOtpAsync(SendOtpRequestDto request, CancellationToken ct = default);
          Task<TwoFactorLoginResponseDto> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken ct = default);
          Task<SetupAuthenticatorResponseDto> SetupAuthenticatorAsync(SetupAuthenticatorRequestDto request, CancellationToken ct = default);
          Task<TwoFactorLoginResponseDto> VerifyAuthenticatorAsync(VerifyAuthenticatorRequestDto request, CancellationToken ct = default);
    }
}