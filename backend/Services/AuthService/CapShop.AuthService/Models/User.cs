
using System.Collections;

namespace CapShop.AuthService.Models
{
    public class User
    {
        //this class represents the User entity in the database and contains properties for user information such as FullName, Email, Phone, AvatarUrl, PasswordHash, IsGoogleAccount, IsActive, CreatedAtUtc, and fields related to 2FA such as IsSmsOtpEnabled, IsEmailOtpEnabled, IsAuthenticatorEnabled, AuthenticatorSecret, CurrentOtp, OtpExpiryUtc, and LastOtpSentUtc as well as a navigation property for the many-to-many relationship with Role through UserRole which allows us to easily access the roles assigned to a user when querying the database using Entity Framework Core
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsGoogleAccount { get; set; } = false;

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
