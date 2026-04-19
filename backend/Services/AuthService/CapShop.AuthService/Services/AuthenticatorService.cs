using OtpNet;
using QRCoder;
using CapShop.AuthService.Services.Interfaces;
namespace CapShop.AuthService.Services
{
    public class AuthenticatorService : IAuthenticatorService
    {
        // dependency injection of logger to log important events and errors in the authenticator service
        private readonly ILogger<AuthenticatorService> _logger;

        public AuthenticatorService(ILogger<AuthenticatorService> logger)
        {
            _logger = logger;
        }

        // Generate a random secret key for the authenticator app and return it as a base32 string which can be stored in the database and used for generating OTPs and QR codes for the user
        public string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            var base32Key = Base32Encoding.ToString(key);
            _logger.LogInformation("Authenticator secret key generated");
            return base32Key;
        }

        // Generate a QR code image as a base64 string that can be displayed to the user for scanning with their authenticator app using the provided email, secret key, and optional issuer name (default is "CapShop") to create the provisioning URI for the QR code which follows the otpauth URI format and then using the QRCoder library to generate the QR code image and convert it to a base64 string that can be easily embedded in an HTML img tag for display on the frontend
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
        // Verify the provided OTP code against the secret key using the TOTP algorithm and return true if the code is valid within the allowed time window (usually 30 seconds) and false otherwise
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
