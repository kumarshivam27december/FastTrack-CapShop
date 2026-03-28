using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CapShop.AuthService.Services
{
    public interface ISmsService
    {
        Task SendOtpAsync(string phoneNumber, string otp);
    }

    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOtpAsync(string phoneNumber, string otp)
        {
            try
            {
                var accountSid = _configuration["Twilio:AccountSid"];
                var authToken = _configuration["Twilio:AuthToken"];
                var fromNumber = _configuration["Twilio:PhoneNumber"];

                if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
                {
                    _logger.LogError("Twilio credentials missing in configuration");
                    throw new InvalidOperationException("SMS service not configured");
                }

                if (string.IsNullOrWhiteSpace(fromNumber))
                {
                    _logger.LogError("Twilio from phone number is missing in configuration");
                    throw new InvalidOperationException("SMS service not configured");
                }

                var normalizedToNumber = NormalizePhoneNumber(phoneNumber);

                TwilioClient.Init(accountSid, authToken);

                var message = $"Your CapShop verification code is: {otp}. Valid for 5 minutes.";

                await Task.Run(() =>
                {
                    MessageResource.Create(
                        body: message,
                        from: new PhoneNumber(fromNumber),
                        to: new PhoneNumber(normalizedToNumber)
                    );
                });

                _logger.LogInformation("SMS OTP sent to {PhoneNumber}", normalizedToNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS OTP to {PhoneNumber}", phoneNumber);
                throw;
            }
        }

        private static string NormalizePhoneNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new InvalidOperationException("Phone number is required for SMS OTP.");

            var trimmed = input.Trim();

            // Keep existing E.164 numbers if they are valid enough.
            if (trimmed.StartsWith('+'))
            {
                var plusDigits = "+" + new string(trimmed.Skip(1).Where(char.IsDigit).ToArray());
                if (plusDigits.Length < 8 || plusDigits.Length > 16)
                    throw new InvalidOperationException("Invalid international phone number format.");

                return plusDigits;
            }

            // Convert 00-prefixed international number to + format.
            if (trimmed.StartsWith("00"))
            {
                var digitsAfter00 = new string(trimmed.Skip(2).Where(char.IsDigit).ToArray());
                if (digitsAfter00.Length < 7 || digitsAfter00.Length > 15)
                    throw new InvalidOperationException("Invalid international phone number format.");

                return "+" + digitsAfter00;
            }

            var digitsOnly = new string(trimmed.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length == 0)
                throw new InvalidOperationException("Phone number is required for SMS OTP.");

            // India local mobile number: 10 digits -> prepend +91.
            if (digitsOnly.Length == 10)
                return "+91" + digitsOnly;

            // India with country code but no plus.
            if (digitsOnly.Length == 12 && digitsOnly.StartsWith("91"))
                return "+" + digitsOnly;

            // Fallback for other countries when user entered country code without +.
            if (digitsOnly.Length >= 8 && digitsOnly.Length <= 15)
                return "+" + digitsOnly;

            throw new InvalidOperationException("Invalid phone number. Please use a valid mobile number.");
        }
    }
}
