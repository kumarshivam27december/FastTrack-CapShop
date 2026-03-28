using System.Net;
using System.Net.Mail;

namespace CapShop.AuthService.Services
{
    public interface IEmailService
    {
        Task SendOtpAsync(string email, string otp);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOtpAsync(string email, string otp)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var senderEmail = _configuration["Email:SenderEmail"];
                var senderPassword = _configuration["Email:SenderPassword"];

                if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
                {
                    _logger.LogError("Email credentials missing in configuration");
                    throw new InvalidOperationException("Email service not configured");
                }

                using (var client = new SmtpClient(smtpHost)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                })
                {
                    var subject = "CapShop Verification Code";
                    var body = $@"
                        <html>
                            <body>
                                <h2>CapShop Verification Code</h2>
                                <p>Your verification code is: <strong>{otp}</strong></p>
                                <p>This code is valid for 5 minutes.</p>
                                <p>If you did not request this code, please ignore this email.</p>
                            </body>
                        </html>";

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, "CapShop"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);

                    _logger.LogInformation("Email OTP sent to {Email}", email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email OTP to {Email}", email);
                throw;
            }
        }
    }
}
