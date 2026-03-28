namespace CapShop.AuthService.DTOs.Auth
{
    public class TwoFactorAuthResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public bool RequiresTwoFactor { get; set; } = true;
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public List<string> AvailableMethods { get; set; } = new();
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class SendOtpRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty; // "SMS" or "EMAIL"
    }

    public class SendOtpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty; // e.g., "****5678" for phone
    }

    public class VerifyOtpRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty; // "SMS" or "EMAIL"
    }

    public class SetupAuthenticatorRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
    }

    public class SetupAuthenticatorResponseDto
    {
        public string SecretKey { get; set; } = string.Empty;
        public string QrCodeImage { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class VerifyAuthenticatorRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class Enable2FaRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty; // "SMS", "EMAIL", or "AUTHENTICATOR"
        public string? AuthenticatorSecret { get; set; }
    }

    public class TwoFactorLoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool RequiresTwoFactor { get; set; }
        public string SessionToken { get; set; } = string.Empty;
    }
}
