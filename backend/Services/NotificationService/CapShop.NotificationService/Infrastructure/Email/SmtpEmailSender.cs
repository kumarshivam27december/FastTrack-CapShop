using System.Net;
using System.Net.Mail;
using CapShop.NotificationService.Application.Interfaces;
using CapShop.NotificationService.Configuration;
using Microsoft.Extensions.Options;

namespace CapShop.NotificationService.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public SmtpEmailSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var host = string.IsNullOrWhiteSpace(_settings.SmtpHost) ? _settings.Host : _settings.SmtpHost;
        var port = _settings.SmtpPort > 0 ? _settings.SmtpPort : _settings.Port;
        var fromEmail = string.IsNullOrWhiteSpace(_settings.SenderEmail) ? _settings.FromEmail : _settings.SenderEmail;
        var fromName = string.IsNullOrWhiteSpace(_settings.SenderName) ? _settings.FromName : _settings.SenderName;
        var username = string.IsNullOrWhiteSpace(_settings.Username) ? fromEmail : _settings.Username;
        var password = string.IsNullOrWhiteSpace(_settings.SenderPassword) ? _settings.Password : _settings.SenderPassword;

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(fromEmail) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("SMTP is not configured. Set Email:SmtpHost, Email:SenderEmail and Email:SenderPassword.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(username, password)
        };

        await client.SendMailAsync(message);
    }
}
