using OtpNet;
using QRCoder;

namespace CapShop.AuthService.Services
{
    public interface IAuthenticatorService
    {
        string GenerateSecretKey();
        string GenerateQrCode(string email, string secretKey, string issuer = "CapShop");
        bool VerifyCode(string secretKey, string code);
    }

    public class AuthenticatorService : IAuthenticatorService
    {
        private readonly ILogger<AuthenticatorService> _logger;

        public AuthenticatorService(ILogger<AuthenticatorService> logger)
        {
            _logger = logger;
        }

        public string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            var base32Key = Base32Encoding.ToString(key);
            _logger.LogInformation("Authenticator secret key generated");
            return base32Key;
        }

        public string GenerateQrCode(string email, string secretKey, string issuer = "CapShop")
        {
            try
            {
                var key = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(key);
                
                // Create the provisioning URI for the QR code
                var provisioningUri = $"otpauth://totp/{issuer}:{email}?secret={secretKey}&issuer={issuer}";

                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(provisioningUri, QRCodeGenerator.ECCLevel.Q);
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeImage = qrCode.GetGraphic(20);
                        string base64Image = Convert.ToBase64String(qrCodeImage);
                        _logger.LogInformation("QR code generated for {Email}", email);
                        return "data:image/png;base64," + base64Image;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for {Email}", email);
                throw;
            }
        }

        public bool VerifyCode(string secretKey, string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
                {
                    return false;
                }

                var key = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(key);

                // Verify the code (allows 1 window of 30 seconds on either side for clock skew)
                var window = new VerificationWindow(previous: 1, future: 1);
                var isValid = totp.VerifyTotp(code, out long timeStepMatched, window);

                if (isValid)
                {
                    _logger.LogInformation("Authenticator code verified successfully");
                }
                else
                {
                    _logger.LogWarning("Authenticator code verification failed");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying authenticator code");
                return false;
            }
        }
    }
}
