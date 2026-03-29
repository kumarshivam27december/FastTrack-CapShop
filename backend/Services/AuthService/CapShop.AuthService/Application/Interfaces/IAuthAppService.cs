using CapShop.AuthService.DTOs.Auth;
using System.Security.Claims;

namespace CapShop.AuthService.Application.Interfaces
{
    public interface IAuthAppService
    {
        Task SignupAsync(SignupRequestDto request, CancellationToken ct = default);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto request, CancellationToken ct = default);

        Task<SendOtpResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken ct = default);
        Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default);

        Task<MeResponseDto> GetMeAsync(ClaimsPrincipal user, CancellationToken ct = default);
        Task<MeResponseDto> UpdateMeAsync(ClaimsPrincipal user, UpdateProfileRequestDto request, CancellationToken ct = default);
        Task ChangePasswordAsync(ClaimsPrincipal user, ChangePasswordRequestDto request, CancellationToken ct = default);
        Task<SetupAuthenticatorResponseDto> SetupAuthenticatorForCurrentUserAsync(ClaimsPrincipal user, CancellationToken ct = default);
        Task<MeResponseDto> EnableAuthenticatorForCurrentUserAsync(ClaimsPrincipal user, EnableAuthenticatorRequestDto request, CancellationToken ct = default);

        Task<TwoFactorAuthResponseDto> LoginStep1Async(LoginRequestDto request, CancellationToken ct = default);
        Task<SendOtpResponseDto> SendOtpAsync(SendOtpRequestDto request, CancellationToken ct = default);
        Task<TwoFactorLoginResponseDto> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken ct = default);
        Task<SetupAuthenticatorResponseDto> SetupAuthenticatorAsync(SetupAuthenticatorRequestDto request, CancellationToken ct = default);
        Task<TwoFactorLoginResponseDto> VerifyAuthenticatorAsync(VerifyAuthenticatorRequestDto request, CancellationToken ct = default);
    }
}