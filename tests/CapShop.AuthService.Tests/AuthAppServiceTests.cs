using CapShop.AuthService.Application.Services;
using CapShop.AuthService.DTOs.Auth;
using CapShop.AuthService.Infrastructure.Repositories;
using CapShop.AuthService.Models;
using CapShop.AuthService.Services;
using CapShop.AuthService.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace CapShop.AuthService.Tests;

public class AuthAppServiceTests
{
    private static AuthAppService CreateSut(
        Mock<IAuthRepository> repo,
        Mock<IJwtTokenService>? jwt = null,
        Mock<ISmsService>? sms = null,
        Mock<IEmailService>? email = null,
        Mock<IOtpService>? otp = null,
        Mock<IAuthenticatorService>? authenticator = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Otp:ExpiryMinutes"] = "5"
            })
            .Build();

        return new AuthAppService(
            repo.Object,
            (jwt ?? new Mock<IJwtTokenService>()).Object,
            (sms ?? new Mock<ISmsService>()).Object,
            (email ?? new Mock<IEmailService>()).Object,
            (otp ?? new Mock<IOtpService>()).Object,
            (authenticator ?? new Mock<IAuthenticatorService>()).Object,
            config,
            new Mock<ILogger<AuthAppService>>().Object);
    }

    [Test]
    public void SignupAsync_WhenEmailAlreadyExists_Throws()
    {
        var repo = new Mock<IAuthRepository>();
        repo.Setup(r => r.EmailExistsAsync("user@capshop.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateSut(repo);

        var action = async () => await sut.SignupAsync(new SignupRequestDto
        {
            FullName = "User",
            Email = "user@capshop.com",
            Phone = "9999999999",
            Password = "password123"
        });

        Assert.ThrowsAsync<InvalidOperationException>(async () => await action());
    }

    [Test]
    public async Task SignupAsync_WhenValid_CreatesUserAndAssignsCustomerRole()
    {
        var repo = new Mock<IAuthRepository>();
        User? addedUser = null;

        repo.Setup(r => r.EmailExistsAsync("new@capshop.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.GetRoleByNameAsync("Customer", It.IsAny<CancellationToken>())).ReturnsAsync(new Role { Id = 1, Name = "Customer" });
        repo.Setup(r => r.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => addedUser = u)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut(repo);

        await sut.SignupAsync(new SignupRequestDto
        {
            FullName = "New User",
            Email = "new@capshop.com",
            Phone = "9999999999",
            Password = "password123"
        });

        Assert.That(addedUser, Is.Not.Null);
        Assert.That(addedUser!.Email, Is.EqualTo("new@capshop.com"));
        Assert.That(addedUser.UserRoles.Count, Is.EqualTo(1));
        Assert.That(addedUser.UserRoles.First().Role.Name, Is.EqualTo("Customer"));

        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsTokenAndRole()
    {
        var repo = new Mock<IAuthRepository>();
        var jwt = new Mock<IJwtTokenService>();

        var hashed = BCrypt.Net.BCrypt.HashPassword("secret123");
        var user = new User
        {
            Id = 3,
            Email = "user@capshop.com",
            PasswordHash = hashed,
            IsGoogleAccount = false,
            IsActive = true,
            UserRoles = new List<UserRole>
            {
                new() { Role = new Role { Name = "Customer" } }
            }
        };

        repo.Setup(r => r.GetActiveUserByEmailWithRolesAsync("user@capshop.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        jwt.Setup(j => j.GenerateToken(user, It.IsAny<IEnumerable<string>>())).Returns("jwt-token");

        var sut = CreateSut(repo, jwt: jwt);

        var response = await sut.LoginAsync(new LoginRequestDto
        {
            Email = "user@capshop.com",
            Password = "secret123"
        });

        Assert.That(response.Token, Is.EqualTo("jwt-token"));
        Assert.That(response.Role, Is.EqualTo("Customer"));
        Assert.That(response.Email, Is.EqualTo("user@capshop.com"));
    }

    [Test]
    public async Task ForgotPasswordAsync_WhenUserNotFound_ReturnsSafeSuccessResponse()
    {
        var repo = new Mock<IAuthRepository>();
        repo.Setup(r => r.GetUserByEmailAsync("missing@capshop.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var sut = CreateSut(repo);

        var response = await sut.ForgotPasswordAsync(new ForgotPasswordRequestDto
        {
            Email = "missing@capshop.com"
        });

        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Does.Contain("If this email exists"));
    }

    [Test]
    public void ResetPasswordAsync_WhenOtpIsInvalid_Throws()
    {
        var repo = new Mock<IAuthRepository>();
        var otp = new Mock<IOtpService>();

        repo.Setup(r => r.GetUserByEmailAsync("user@capshop.com", It.IsAny<CancellationToken>())).ReturnsAsync(new User
        {
            Email = "user@capshop.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("old-password")
        });
        otp.Setup(o => o.ValidateOtp("111111", It.IsAny<string>(), It.IsAny<DateTime?>())).Returns(false);

        var sut = CreateSut(repo, otp: otp);

        var action = async () => await sut.ResetPasswordAsync(new ResetPasswordRequestDto
        {
            Email = "user@capshop.com",
            Otp = "111111",
            NewPassword = "new-password-1"
        });

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await action());
    }
}
