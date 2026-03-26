using CapShop.AuthService.Application.Interfaces;
using CapShop.AuthService.DTOs.Auth;
using CapShop.AuthService.Infrastructure.Repositories;
using CapShop.AuthService.Models;
using CapShop.AuthService.Services.Interfaces;
using System.Security.Claims;

namespace CapShop.AuthService.Application.Services
{
    public class AuthAppService : IAuthAppService
    {
        private readonly IAuthRepository _repo;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<AuthAppService> _logger;

        public AuthAppService(
            IAuthRepository repo,
            IJwtTokenService jwtTokenService,
            ILogger<AuthAppService> logger)
        {
            _repo = repo;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        public async Task SignupAsync(SignupRequestDto request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new InvalidOperationException("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new InvalidOperationException("Password is required.");

            var email = request.Email.Trim().ToLowerInvariant();
            var exists = await _repo.EmailExistsAsync(email, ct);
            if (exists)
                throw new InvalidOperationException("Email already exists.");

            var customerRole = await _repo.GetRoleByNameAsync("Customer", ct);
            if (customerRole is null)
                throw new InvalidOperationException("Default role Customer not found.");

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = request.Phone.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true
            };

            user.UserRoles.Add(new UserRole
            {
                User = user,
                Role = customerRole
            });

            await _repo.AddUserAsync(user, ct);
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation("User signup completed for {Email}", email);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new UnauthorizedAccessException("Invalid credentials.");

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _repo.GetActiveUserByEmailWithRolesAsync(email, ct);

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
            var token = _jwtTokenService.GenerateToken(user, roles);

            _logger.LogInformation("User login successful for {Email}", email);

            return new AuthResponseDto
            {
                Token = token,
                Role = roles.FirstOrDefault() ?? "Customer",
                Email = user.Email
            };
        }

        public async Task<object> GetMeAsync(ClaimsPrincipal user, CancellationToken ct = default)
        {
            var email = user.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedAccessException("User identity not found.");

            var dbUser = await _repo.GetUserByEmailAsync(email, ct);
            if (dbUser is null)
                throw new UnauthorizedAccessException("User not found.");

            var roles = dbUser.UserRoles.Select(x => x.Role.Name).ToList();

            return new
            {
                email = dbUser.Email,
                roles
            };
        }
    }
}