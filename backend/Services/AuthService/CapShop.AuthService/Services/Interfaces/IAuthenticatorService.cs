namespace CapShop.AuthService.Services.Interfaces
{
    public interface IAuthenticatorService
    {
        string GenerateSecretKey();
        string GenerateQrCode(string email, string secretKey, string issuer = "CapShop");
        bool VerifyCode(string secretKey, string code);
    }
}