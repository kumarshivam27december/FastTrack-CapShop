using System.Security.Claims;
using CapShop.PaymentService.Application.Interfaces;
using CapShop.PaymentService.Controllers;
using CapShop.PaymentService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CapShop.PaymentService.Tests;

public class PaymentControllerTests
{
    private static PaymentController CreateController(Mock<IPaymentAppService> service, params Claim[] claims)
    {
        var controller = new PaymentController(service.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    [Test]
    public async Task GetById_WhenPaymentMissing_ReturnsNotFound()
    {
        var service = new Mock<IPaymentAppService>();
        service.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((PaymentResponseDto?)null);

        var controller = CreateController(service, new Claim("userId", "10"));

        var result = await controller.GetById(99);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetById_WhenUserDoesNotOwnAndNotAdmin_ReturnsForbid()
    {
        var service = new Mock<IPaymentAppService>();
        service.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new PaymentResponseDto
        {
            Id = 7,
            UserId = 22,
            OrderId = 100,
            Amount = 200
        });

        var controller = CreateController(service, new Claim("userId", "10"));

        var result = await controller.GetById(7);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task GetById_WhenUserIsAdmin_ReturnsOkEvenIfDifferentOwner()
    {
        var service = new Mock<IPaymentAppService>();
        service.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new PaymentResponseDto
        {
            Id = 7,
            UserId = 22,
            OrderId = 100,
            Amount = 200
        });

        var controller = CreateController(
            service,
            new Claim("userId", "10"),
            new Claim(ClaimTypes.Role, "Admin"));

        var result = await controller.GetById(7);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public void CustomerEndpoints_ShouldRequireCustomerOnlyPolicy()
    {
        var customerOnlyMethods = new[]
        {
            nameof(PaymentController.Process),
            nameof(PaymentController.GetById),
            nameof(PaymentController.GetLatestByOrderId)
        };

        foreach (var methodName in customerOnlyMethods)
        {
            var method = typeof(PaymentController).GetMethod(methodName);
            Assert.That(method, Is.Not.Null, $"Method {methodName} not found");

            var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault(a => a.Policy == "CustomerOnly");

            Assert.That(authorize, Is.Not.Null, $"CustomerOnly policy missing on {methodName}");
        }
    }

    [Test]
    public void UpdateStatus_ShouldRequireAdminRole()
    {
        var method = typeof(PaymentController).GetMethod(nameof(PaymentController.UpdateStatus));
        Assert.That(method, Is.Not.Null, "Method UpdateStatus not found");

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault(a => a.Roles == "Admin");

        Assert.That(authorize, Is.Not.Null, "Admin role missing on UpdateStatus");
    }
}
