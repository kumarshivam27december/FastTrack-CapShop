using CapShop.AuthService.Application.Interfaces;
using CapShop.AuthService.DTOs.Auth;
using CapShop.AuthService.Infrastructure.Repositories;
using CapShop.AuthService.Models;
using CapShop.AuthService.Services;
using CapShop.AuthService.Services.Interfaces;
using Google.Apis.Auth;
using System.Security.Claims;

namespace CapShop.AuthService.Application.Services
{
    public class AuthAppService : IAuthAppService
    {
        private readonly IAuthRepository _repo;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;
        private readonly IAuthenticatorService _authenticatorService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthAppService> _logger;

        public AuthAppService(
            IAuthRepository repo,
            IJwtTokenService jwtTokenService,
            ISmsService smsService,
            IEmailService emailService,
            IOtpService otpService,
            IAuthenticatorService authenticatorService,
            IConfiguration configuration,
            ILogger<AuthAppService> logger)
        {
            _repo = repo;
            _jwtTokenService = jwtTokenService;
            _smsService = smsService;
            _emailService = emailService;
            _otpService = otpService;
            _authenticatorService = authenticatorService;
            _configuration = configuration;
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
                IsGoogleAccount = false,
                IsActive = true,
                IsSmsOtpEnabled = true,
                IsEmailOtpEnabled = true
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

            if (user is not null && user.IsGoogleAccount)
                throw new UnauthorizedAccessException("This account uses Google sign-in. Set a password in profile first.");

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

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto request, CancellationToken ct = default)
        {
            var idToken = request.IdToken?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(idToken))
                throw new InvalidOperationException("Google token is required.");

            var googleClientId = _configuration["GoogleAuth:ClientId"];
            if (string.IsNullOrWhiteSpace(googleClientId))
                throw new InvalidOperationException("Google sign-in is not configured on the server.");

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Google ID token received");
                throw new UnauthorizedAccessException("Invalid Google sign-in token.");
            }

            var email = payload.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedAccessException("Google account email is missing.");

            var user = await _repo.GetUserByEmailAsync(email, ct);
            if (user is null)
            {
                var customerRole = await _repo.GetRoleByNameAsync("Customer", ct);
                if (customerRole is null)
                    throw new InvalidOperationException("Default role Customer not found.");

                user = new User
                {
                    FullName = string.IsNullOrWhiteSpace(payload.Name) ? email : payload.Name.Trim(),
                    Email = email,
                    Phone = "0000000000",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                    IsGoogleAccount = true,
                    IsActive = true,
                    IsSmsOtpEnabled = true,
                    IsEmailOtpEnabled = true
                };

                user.UserRoles.Add(new UserRole
                {
                    User = user,
                    Role = customerRole
                });

                await _repo.AddUserAsync(user, ct);
                await _repo.SaveChangesAsync(ct);

                _logger.LogInformation("Created new user via Google sign-in for {Email}", email);
            }

            if (!user.IsActive)
                throw new UnauthorizedAccessException("User account is inactive.");

            var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
            var token = _jwtTokenService.GenerateToken(user, roles);

            _logger.LogInformation("User login successful via Google for {Email}", email);

            return new AuthResponseDto
            {
                Token = token,
                Role = roles.FirstOrDefault() ?? "Customer",
                Email = user.Email
            };
        }

        public async Task<MeResponseDto> GetMeAsync(ClaimsPrincipal user, CancellationToken ct = default)
        {
            var dbUser = await GetCurrentUserAsync(user, ct);
            return BuildMeResponse(dbUser);
        }

        public async Task<MeResponseDto> UpdateMeAsync(ClaimsPrincipal user, UpdateProfileRequestDto request, CancellationToken ct = default)
        {
            var dbUser = await GetCurrentUserAsync(user, ct);

            var fullName = request.FullName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fullName))
                throw new InvalidOperationException("Full name is required.");

            var phone = request.Phone?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(phone))
                throw new InvalidOperationException("Phone is required.");

            if (fullName.Length > 120)
                throw new InvalidOperationException("Full name is too long.");

            if (phone.Length > 20)
                throw new InvalidOperationException("Phone is too long.");

            var avatarUrl = request.AvatarUrl?.Trim();
            if (!string.IsNullOrWhiteSpace(avatarUrl) && avatarUrl.Length > 2_000_000)
                throw new InvalidOperationException("Avatar image is too large.");

            dbUser.FullName = fullName;
            dbUser.Phone = phone;
            dbUser.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl;

            await _repo.UpdateUserAsync(dbUser, ct);
            await _repo.SaveChangesAsync(ct);

            return BuildMeResponse(dbUser);
        }

        public async Task ChangePasswordAsync(ClaimsPrincipal user, ChangePasswordRequestDto request, CancellationToken ct = default)
        {
            var dbUser = await GetCurrentUserAsync(user, ct);

            var currentPassword = request.CurrentPassword?.Trim() ?? string.Empty;
            var newPassword = request.NewPassword?.Trim() ?? string.Empty;
            var requiresCurrentPassword = !dbUser.IsGoogleAccount;

            if (requiresCurrentPassword && string.IsNullOrWhiteSpace(currentPassword))
                throw new InvalidOperationException("Current password is required.");

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new InvalidOperationException("New password is required.");

            if (newPassword.Length < 6)
                throw new InvalidOperationException("New password must be at least 6 characters.");

            if (requiresCurrentPassword && !BCrypt.Net.BCrypt.Verify(currentPassword, dbUser.PasswordHash))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            if (BCrypt.Net.BCrypt.Verify(newPassword, dbUser.PasswordHash))
                throw new InvalidOperationException("New password must be different from current password.");

            dbUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            dbUser.IsGoogleAccount = false;

            await _repo.UpdateUserAsync(dbUser, ct);
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation("Password changed for {Email}", dbUser.Email);
        }

        public async Task<SetupAuthenticatorResponseDto> SetupAuthenticatorForCurrentUserAsync(ClaimsPrincipal user, CancellationToken ct = default)
        {
            var dbUser = await GetCurrentUserAsync(user, ct);

            if (dbUser.IsAuthenticatorEnabled && !string.IsNullOrWhiteSpace(dbUser.AuthenticatorSecret))
                throw new InvalidOperationException("Authenticator is already enabled.");

            var secretKey = _authenticatorService.GenerateSecretKey();
            var qrCode = _authenticatorService.GenerateQrCode(dbUser.Email, secretKey);

            dbUser.AuthenticatorSecret = secretKey;
            dbUser.IsAuthenticatorEnabled = false;

            await _repo.UpdateUserAsync(dbUser, ct);
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation("Authenticator setup prepared for {Email}", dbUser.Email);

            return new SetupAuthenticatorResponseDto
            {
                SecretKey = secretKey,
                QrCodeImage = qrCode,
                Message = "Scan the QR code and verify with a 6-digit app code to enable authenticator login."
            };
        }

        public async Task<MeResponseDto> EnableAuthenticatorForCurrentUserAsync(ClaimsPrincipal user, EnableAuthenticatorRequestDto request, CancellationToken ct = default)
        {
            var dbUser = await GetCurrentUserAsync(user, ct);
            var code = request.Code?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
                throw new InvalidOperationException("A valid 6-digit authenticator code is required.");

            if (string.IsNullOrWhiteSpace(dbUser.AuthenticatorSecret))
                throw new InvalidOperationException("Authenticator is not set up yet. Please generate a QR code first.");

            if (!_authenticatorService.VerifyCode(dbUser.AuthenticatorSecret, code))
                throw new UnauthorizedAccessException("Invalid authenticator code.");

            dbUser.IsAuthenticatorEnabled = true;

            await _repo.UpdateUserAsync(dbUser, ct);
            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation("Authenticator enabled for {Email}", dbUser.Email);

            return BuildMeResponse(dbUser);
        }

        public async Task<TwoFactorAuthResponseDto> LoginStep1Async(LoginRequestDto request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new UnauthorizedAccessException("Invalid credentials.");

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _repo.GetActiveUserByEmailWithRolesAsync(email, ct);

            if (user is not null && user.IsGoogleAccount)
                throw new UnauthorizedAccessException("This account uses Google sign-in. Set a password in profile first.");

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            var sessionToken = Guid.NewGuid().ToString();
            var availableMethods = new List<string>();
            if (user.IsSmsOtpEnabled)
                availableMethods.Add("SMS");
            if (user.IsEmailOtpEnabled)
                availableMethods.Add("EMAIL");
            if (user.IsAuthenticatorEnabled)
                availableMethods.Add("AUTHENTICATOR");

            if (availableMethods.Count == 0)
            {
                var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
                var token = _jwtTokenService.GenerateToken(user, roles);

                return new TwoFactorAuthResponseDto
                {
                    Message = "No 2FA required",
                    RequiresTwoFactor = false,
                    Token = token,
                    Role = roles.FirstOrDefault() ?? "Customer",
                    Email = user.Email
                };
            }

            _logger.LogInformation("User {Email} requires 2FA verification", email);

            return new TwoFactorAuthResponseDto
            {
                Message = "2FA required. Choose authentication method.",
                RequiresTwoFactor = true,
                SessionToken = sessionToken,
                AvailableMethods = availableMethods,
                Email = email,
                PhoneNumber = MaskPhoneNumber(user.Phone)
            };
        }

        public async Task<SendOtpResponseDto> SendOtpAsync(SendOtpRequestDto request, CancellationToken ct = default)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _repo.GetUserByEmailAsync(email, ct);

            if (user is null)
                throw new InvalidOperationException("User not found.");

            if (user.LastOtpSentUtc.HasValue && (DateTime.UtcNow - user.LastOtpSentUtc.Value).TotalSeconds < 30)
                throw new InvalidOperationException("Please wait before requesting a new OTP.");

            var otp = _otpService.GenerateOtp(6);
            var expiryMinutes = int.Parse(_configuration["Otp:ExpiryMinutes"] ?? "5");

            user.CurrentOtp = otp;
            user.OtpExpiryUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);
            user.LastOtpSentUtc = DateTime.UtcNow;

            if (string.Equals(request.Method, "SMS", StringComparison.OrdinalIgnoreCase))
            {
                if (!user.IsSmsOtpEnabled)
                    throw new UnauthorizedAccessException("SMS OTP is not enabled for this account.");

                await _smsService.SendOtpAsync(user.Phone, otp);
            }
            else if (string.Equals(request.Method, "EMAIL", StringComparison.OrdinalIgnoreCase))
            {
                if (!user.IsEmailOtpEnabled)
                    throw new UnauthorizedAccessException("Email OTP is not enabled for this account.");

                await _emailService.SendOtpAsync(user.Email, otp);
            }
            else
            {
                throw new InvalidOperationException("Invalid OTP method.");
            }

            await _repo.UpdateUserAsync(user, ct);
            await _repo.SaveChangesAsync(ct);

            var destination = string.Equals(request.Method, "SMS", StringComparison.OrdinalIgnoreCase)
                ? MaskPhoneNumber(user.Phone)
                : MaskEmail(user.Email);

            _logger.LogInformation("OTP sent via {Method} to {Destination}", request.Method, destination);

            return new SendOtpResponseDto
            {
                Success = true,
                Message = $"OTP sent successfully via {request.Method}",
                Destination = destination
            };
        }

        public async Task<TwoFactorLoginResponseDto> VerifyOtpAsync(VerifyOtpRequestDto request, CancellationToken ct = default)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _repo.GetActiveUserByEmailWithRolesAsync(email, ct);

            if (user is null)
                throw new UnauthorizedAccessException("User not found.");

            if (!_otpService.ValidateOtp(request.Otp, user.CurrentOtp ?? string.Empty, user.OtpExpiryUtc))
                throw new UnauthorizedAccessException("Invalid or expired OTP.");

            user.CurrentOtp = null;
            user.OtpExpiryUtc = null;

            await _repo.UpdateUserAsync(user, ct);
            await _repo.SaveChangesAsync(ct);

            var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
            var token = _jwtTokenService.GenerateToken(user, roles);

            _logger.LogInformation("User {Email} successfully verified OTP", email);

            return new TwoFactorLoginResponseDto
            {
                Token = token,
                Role = roles.FirstOrDefault() ?? "Customer",
                Email = user.Email,
                RequiresTwoFactor = false
            };
        }

        public async Task<SetupAuthenticatorResponseDto> SetupAuthenticatorAsync(SetupAuthenticatorRequestDto request, CancellationToken ct = default)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _repo.GetUserByEmailAsync(email, ct);

            if (user is null)
                throw new InvalidOperationException("User not found.");

            var secretKey = _authenticatorService.GenerateSecretKey();
            var qrCode = _authenticatorService.GenerateQrCode(email, secretKey);

            _logger.LogInformation("Authenticator setup initiated for {Email}", email);

            return new SetupAuthenticatorResponseDto
            {
                SecretKey = secretKey,
                QrCodeImage = qrCode,
                Message = "Scan the QR code with your authenticator app and enter the code to enable 2FA."
            };
        }

        public async Task<TwoFactorLoginResponseDto> VerifyAuthenticatorAsync(VerifyAuthenticatorRequestDto request, CancellationToken ct = default)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _repo.GetActiveUserByEmailWithRolesAsync(email, ct);

            if (user is null)
                throw new UnauthorizedAccessException("User not found.");

            if (!user.IsAuthenticatorEnabled)
                throw new UnauthorizedAccessException("Authenticator login is not enabled for this account.");

            if (string.IsNullOrWhiteSpace(user.AuthenticatorSecret))
                throw new UnauthorizedAccessException("Authenticator is not configured for this account.");

            if (!_authenticatorService.VerifyCode(user.AuthenticatorSecret, request.Code))
                throw new UnauthorizedAccessException("Invalid authenticator code.");

            await _repo.UpdateUserAsync(user, ct);
            await _repo.SaveChangesAsync(ct);

            var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
            var token = _jwtTokenService.GenerateToken(user, roles);

            _logger.LogInformation("User {Email} successfully verified authenticator", email);

            return new TwoFactorLoginResponseDto
            {
                Token = token,
                Role = roles.FirstOrDefault() ?? "Customer",
                Email = user.Email,
                RequiresTwoFactor = false
            };
        }

        private string MaskPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || phone.Length < 4)
                return "****";
            return "****" + phone.Substring(phone.Length - 4);
        }

        private async Task<User> GetCurrentUserAsync(ClaimsPrincipal user, CancellationToken ct)
        {
            var email = user.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
                throw new UnauthorizedAccessException("User identity not found.");

            var dbUser = await _repo.GetUserByEmailAsync(email, ct);
            if (dbUser is null)
                throw new UnauthorizedAccessException("User not found.");

            return dbUser;
        }

        private static MeResponseDto BuildMeResponse(User user)
        {
            return new MeResponseDto
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                IsGoogleAccount = user.IsGoogleAccount,
                IsAuthenticatorEnabled = user.IsAuthenticatorEnabled,
                Roles = user.UserRoles.Select(x => x.Role.Name).ToList()
            };
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "****";

            var parts = email.Split('@');
            if (parts.Length != 2)
                return "***";

            var localPart = parts[0];
            var domain = parts[1];

            if (localPart.Length <= 2)
                return "*" + localPart.Substring(localPart.Length - 1) + "@" + domain;

            return localPart.Substring(0, 2) + "***@" + domain;
        }
    }
}