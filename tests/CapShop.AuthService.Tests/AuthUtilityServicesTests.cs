using CapShop.AuthService.Models;
using CapShop.AuthService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CapShop.AuthService.Tests;

public class AuthUtilityServicesTests
{
    [Test]
    public void OtpService_GenerateOtp_ReturnsOnlyDigitsWithExpectedLength()
    {
        var logger = new Mock<ILogger<OtpService>>();
        var sut = new OtpService(logger.Object);

        var otp = sut.GenerateOtp(6);

        Assert.That(otp.Length, Is.EqualTo(6));
        Assert.That(otp.All(char.IsDigit), Is.True);
    }

    [Test]
    public void OtpService_ValidateOtp_ReturnsFalseWhenExpired()
    {
        var logger = new Mock<ILogger<OtpService>>();
        var sut = new OtpService(logger.Object);

        var ok = sut.ValidateOtp("123456", "123456", DateTime.UtcNow.AddSeconds(-1));

        Assert.That(ok, Is.False);
    }

    [Test]
    public void JwtTokenService_GenerateToken_ContainsUserAndRoleClaims()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "this-is-a-test-secret-key-with-sufficient-length",
                ["JwtSettings:Issuer"] = "CapShop",
                ["JwtSettings:Audience"] = "CapShopUsers",
                ["JwtSettings:ExpiryMinutes"] = "120"
            })
            .Build();

        var sut = new JwtTokenService(config);

        var token = sut.GenerateToken(new User
        {
            Id = 15,
            Email = "user@capshop.com"
        }, new[] { "Customer" });

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.That(jwt.Claims.Any(c => c.Type == "userId" && c.Value == "15"), Is.True);
        Assert.That(jwt.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Customer"), Is.True);
        Assert.That(jwt.Claims.Any(c => c.Type == ClaimTypes.Name && c.Value == "user@capshop.com"), Is.True);
    }
}
