
using CapShop.AuthService.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using CapShop.AuthService.Services.Interfaces;
using System.Text;

namespace CapShop.AuthService.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user,IEnumerable<string> roles)
        {
            var jwt = _configuration.GetSection("JwtSettings");
            var key = jwt["SecretKey"] ?? throw new InvalidOperationException("JWt secret not found");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,user.Email),
                new(ClaimTypes.Name,user.Email),
                new("userId",user.Id.ToString())
            };

            foreach(var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(signingKey,SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiryMinutes"] ?? "120")),
                signingCredentials : creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}