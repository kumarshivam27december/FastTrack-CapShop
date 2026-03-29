namespace CapShop.AuthService.DTOs.Auth
{
    public class UpdateProfileRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
