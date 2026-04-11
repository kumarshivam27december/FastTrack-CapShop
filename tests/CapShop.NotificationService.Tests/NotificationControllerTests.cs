using System.Security.Claims;
using CapShop.NotificationService.Application.Interfaces;
using CapShop.NotificationService.Controllers;
using CapShop.NotificationService.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CapShop.NotificationService.Tests;

public class NotificationControllerTests
{
    private static NotificationController CreateController(Mock<INotificationAppService> service, params Claim[] claims)
    {
        var controller = new NotificationController(service.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    [Test]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<INotificationAppService>();
        service.Setup(x => x.GetByIdAsync(50)).ReturnsAsync((NotificationResponseDto?)null);

        var controller = CreateController(service, new Claim("userId", "10"));

        var result = await controller.GetById(50);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetById_WhenNotOwnerAndNotAdmin_ReturnsForbid()
    {
        var service = new Mock<INotificationAppService>();
        service.Setup(x => x.GetByIdAsync(51)).ReturnsAsync(new NotificationResponseDto
        {
            Id = 51,
            UserId = 99,
            Subject = "Order placed"
        });

        var controller = CreateController(service, new Claim("userId", "10"));

        var result = await controller.GetById(51);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task MarkAsRead_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<INotificationAppService>();
        service.Setup(x => x.MarkAsReadAsync(88, 10)).ReturnsAsync((NotificationResponseDto?)null);

        var controller = CreateController(service, new Claim("userId", "10"));

        var result = await controller.MarkAsRead(88);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
