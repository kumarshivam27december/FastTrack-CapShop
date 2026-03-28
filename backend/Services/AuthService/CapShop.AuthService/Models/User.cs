
using System.Collections;

namespace CapShop.AuthService.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // 2FA Fields
        public bool IsSmsOtpEnabled { get; set; } = false;
        public bool IsEmailOtpEnabled { get; set; } = false;
        public bool IsAuthenticatorEnabled { get; set; } = false;

        public string? AuthenticatorSecret { get; set; } // Stores TOTP secret
        public string? CurrentOtp { get; set; } // Temporarily stores sent OTP
        public DateTime? OtpExpiryUtc { get; set; } // OTP expiration time
        public DateTime? LastOtpSentUtc { get; set; } // Prevent OTP spam

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
