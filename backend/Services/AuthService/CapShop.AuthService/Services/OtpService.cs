namespace CapShop.AuthService.Services
{
    public interface IOtpService
    {
        string GenerateOtp(int length = 6);
        bool ValidateOtp(string otp, string storedOtp, DateTime? expiryTime);
    }

    public class OtpService : IOtpService
    {
        private readonly ILogger<OtpService> _logger;

        public OtpService(ILogger<OtpService> logger)
        {
            _logger = logger;
        }

        public string GenerateOtp(int length = 6)
        {
            var random = new Random();
            var otp = string.Empty;

            for (int i = 0; i < length; i++)
            {
                otp += random.Next(0, 10).ToString();
            }

            _logger.LogInformation("OTP generated with length {Length}", length);
            return otp;
        }

        public bool ValidateOtp(string otp, string storedOtp, DateTime? expiryTime)
        {
            try
            {
                // Check if OTP matches
                if (!otp.Equals(storedOtp, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("OTP mismatch");
                    return false;
                }

                // Check if OTP has expired
                if (expiryTime.HasValue && DateTime.UtcNow > expiryTime.Value)
                {
                    _logger.LogWarning("OTP expired");
                    return false;
                }

                _logger.LogInformation("OTP validated successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating OTP");
                return false;
            }
        }
    }
}
