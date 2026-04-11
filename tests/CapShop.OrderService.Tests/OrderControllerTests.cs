using System.Security.Claims;
using CapShop.OrderService.Application.Interfaces;
using CapShop.OrderService.Controllers;
using CapShop.OrderService.DTOs.Cart;
using CapShop.OrderService.DTOs.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CapShop.OrderService.Tests;

public class OrderControllerTests
{
    private static OrderController CreateController(Mock<IOrderAppService> service, params Claim[] claims)
    {
        var controller = new OrderController(service.Object);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    [Test]
    public async Task AddToCart_WhenInvalidInput_ReturnsBadRequest()
    {
        var service = new Mock<IOrderAppService>();
        var controller = CreateController(service, new Claim("userId", "10"));

        var result = await controller.AddToCart(new AddToCartRequestDto
        {
            ProductId = 0,
            Quantity = 1
        });

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RemoveFromCart_WhenItemMissing_ReturnsNotFound()
    {
        var service = new Mock<IOrderAppService>();
        service.Setup(x => x.RemoveFromCartAsync(10, 99)).ReturnsAsync(false);

        var controller = CreateController(service, new Claim("userId", "10"));

        var result = await controller.RemoveFromCart(99);

        Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task UpdateOrderStatus_WhenServiceRejects_ReturnsBadRequest()
    {
        var service = new Mock<IOrderAppService>();
        service.Setup(x => x.UpdateOrderStatusAsync(5, "Packed", "note", 10)).ReturnsAsync(false);

        var controller = CreateController(service, new Claim("userId", "10"), new Claim(ClaimTypes.Role, "Admin"));

        var result = await controller.UpdateOrderStatus(5, new UpdateOrderStatusRequestDto
        {
            NewStatus = "Packed",
            Notes = "note"
        });

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public void CustomerEndpoints_ShouldRequireCustomerOnlyPolicy()
    {
        var customerOnlyMethods = new[]
        {
            nameof(OrderController.GetCart),
            nameof(OrderController.AddToCart),
            nameof(OrderController.UpdateCartItem),
            nameof(OrderController.RemoveFromCart),
            nameof(OrderController.StartCheckout),
            nameof(OrderController.SimulatePayment),
            nameof(OrderController.PlaceOrder),
            nameof(OrderController.CancelOrder),
            nameof(OrderController.GetMyOrders),
            nameof(OrderController.GetOrderById)
        };

        foreach (var methodName in customerOnlyMethods)
        {
            var method = typeof(OrderController).GetMethod(methodName);
            Assert.That(method, Is.Not.Null, $"Method {methodName} not found");

            var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            Assert.That(authorize, Is.Not.Null, $"[Authorize] missing on {methodName}");
            Assert.That(authorize!.Policy, Is.EqualTo("CustomerOnly"), $"CustomerOnly policy missing on {methodName}");
        }
    }

    [Test]
    public void AdminEndpoints_ShouldRequireAdminRole()
    {
        var adminMethods = new[]
        {
            nameof(OrderController.GetAllOrders),
            nameof(OrderController.UpdateOrderStatus)
        };

        foreach (var methodName in adminMethods)
        {
            var method = typeof(OrderController).GetMethod(methodName);
            Assert.That(method, Is.Not.Null, $"Method {methodName} not found");

            var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            Assert.That(authorize, Is.Not.Null, $"[Authorize] missing on {methodName}");
            Assert.That(authorize!.Roles, Is.EqualTo("Admin"), $"Admin role missing on {methodName}");
        }
    }
}
