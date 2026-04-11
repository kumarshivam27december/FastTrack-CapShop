using System.Security.Claims;
using CapShop.AuthService.Application.Interfaces;
using CapShop.AuthService.Controllers;
using CapShop.AuthService.DTOs.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CapShop.AuthService.Tests;

public class AuthControllerTests
{
    private static AuthController CreateController(Mock<IAuthAppService> service, params Claim[] claims)
    {
        var controller = new AuthController(service.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return controller;
    }

    [Test]
    public async Task Login_ReturnsOkWithAuthResponse()
    {
        var service = new Mock<IAuthAppService>();
        service.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResponseDto
            {
                Token = "jwt",
                Role = "Customer",
                Email = "user@capshop.com"
            });

        var controller = CreateController(service);

        var result = await controller.Login(new LoginRequestDto
        {
            Email = "user@capshop.com",
            Password = "secret"
        }, CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task Me_ReturnsOk()
    {
        var service = new Mock<IAuthAppService>();
        service.Setup(x => x.GetMeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MeResponseDto
            {
                FullName = "User",
                Email = "user@capshop.com",
                Roles = new List<string> { "Customer" }
            });

        var controller = CreateController(service, new Claim(ClaimTypes.Name, "user@capshop.com"));

        var result = await controller.Me(CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
    }
}
