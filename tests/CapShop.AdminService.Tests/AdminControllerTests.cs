using System.Security.Claims;
using CapShop.AdminService.Application.Interfaces;
using CapShop.AdminService.Controllers;
using CapShop.AdminService.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CapShop.AdminService.Tests;

public class AdminControllerTests
{
    private static AdminController CreateController(Mock<IAdminAppService> service, bool includeAuthHeader = true)
    {
        var controller = new AdminController(service.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, "admin@capshop.com"),
                new Claim(ClaimTypes.Role, "Admin")
            },
            "Test"));

        var httpContext = new DefaultHttpContext { User = principal };
        if (includeAuthHeader)
        {
            httpContext.Request.Headers.Authorization = "Bearer sample-token";
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Test]
    public async Task GetSalesReport_WhenFromGreaterThanTo_ReturnsBadRequest()
    {
        var service = new Mock<IAdminAppService>();
        var controller = CreateController(service);

        var result = await controller.GetSalesReport(new DateOnly(2026, 4, 10), new DateOnly(2026, 4, 1), CancellationToken.None);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateOrderStatus_WhenServiceReturnsFalse_ReturnsBadRequest()
    {
        var service = new Mock<IAdminAppService>();
        service.Setup(x => x.UpdateOrderStatusAsync(55, It.IsAny<UpdateOrderStatusAdminRequest>(), "Bearer sample-token", "admin@capshop.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController(service);

        var result = await controller.UpdateOrderStatus(55, new UpdateOrderStatusAdminRequest
        {
            NewStatus = "Cancelled",
            Notes = "Issue"
        }, CancellationToken.None);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public void GetOrders_WhenAuthorizationHeaderMissing_ThrowsUnauthorizedAccessException()
    {
        var service = new Mock<IAdminAppService>();
        var controller = CreateController(service, includeAuthHeader: false);

        var action = async () => await controller.GetOrders(CancellationToken.None);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await action());
    }
}
