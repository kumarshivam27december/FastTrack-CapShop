namespace CapShop.AuthService.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpAsync(string email, string otp);
    }
}