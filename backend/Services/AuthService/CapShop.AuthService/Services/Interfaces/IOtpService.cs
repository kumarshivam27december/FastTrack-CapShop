namespace CapShop.AuthService.Services.Interfaces
{
    public interface IOtpService
    {
        string GenerateOtp(int length = 6);
        bool ValidateOtp(string otp, string storedOtp, DateTime? expiryTime);
    }
}