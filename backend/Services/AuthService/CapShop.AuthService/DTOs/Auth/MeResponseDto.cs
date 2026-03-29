namespace CapShop.AuthService.DTOs.Auth
{
    public class MeResponseDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsGoogleAccount { get; set; }
        public bool IsAuthenticatorEnabled { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
