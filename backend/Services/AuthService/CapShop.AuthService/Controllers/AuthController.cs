using CapShop.AuthService.Application.Interfaces;
using CapShop.AuthService.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.AuthService.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthAppService _authAppService;

        public AuthController(IAuthAppService authAppService)
        {
            _authAppService = authAppService;
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { service = "AuthService", status = "Healthy" });

        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<IActionResult> Signup([FromBody] SignupRequestDto request, CancellationToken ct)
        {
            await _authAppService.SignupAsync(request, ct);
            return Ok(new { message = "Signup successful." });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken ct)
        {
            var response = await _authAppService.LoginAsync(request, ct);
            return Ok(response);
        }

        [HttpPost("login-step1")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginStep1([FromBody] LoginRequestDto request, CancellationToken ct)
        {
            var response = await _authAppService.LoginStep1Async(request, ct);
            return Ok(response);
        }

        [HttpPost("send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto request, CancellationToken ct)
        {
            var response = await _authAppService.SendOtpAsync(request, ct);
            return Ok(response);
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request, CancellationToken ct)
        {
            var response = await _authAppService.VerifyOtpAsync(request, ct);
            return Ok(response);
        }

        [HttpPost("setup-authenticator")]
        [AllowAnonymous]
        public async Task<IActionResult> SetupAuthenticator([FromBody] SetupAuthenticatorRequestDto request, CancellationToken ct)
        {
            var response = await _authAppService.SetupAuthenticatorAsync(request, ct);
            return Ok(response);
        }

        [HttpPost("verify-authenticator")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAuthenticator([FromBody] VerifyAuthenticatorRequestDto request, CancellationToken ct)
        {
            var response = await _authAppService.VerifyAuthenticatorAsync(request, ct);
            return Ok(response);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            var me = await _authAppService.GetMeAsync(User, ct);
            return Ok(me);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequestDto request, CancellationToken ct)
        {
            var me = await _authAppService.UpdateMeAsync(User, request, ct);
            return Ok(me);
        }
    }
}