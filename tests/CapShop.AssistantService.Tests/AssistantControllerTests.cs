using System.Security.Claims;
using CapShop.AssistantService.Application.Interfaces;
using CapShop.AssistantService.Controllers;
using CapShop.AssistantService.DTOs.Assistant;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CapShop.AssistantService.Tests;

public class AssistantControllerTests
{
    [Test]
    public async Task Query_WhenMessageMissing_ReturnsBadRequest()
    {
        var controller = new AssistantController(Mock.Of<IInventoryAssistantService>(), Mock.Of<IHttpClientFactory>());

        var result = await controller.Query(new AssistantQueryRequestDto { Message = string.Empty }, CancellationToken.None);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Query_WhenMessagePresent_ForwardsUserIdAndAuthHeader()
    {
        var assistantService = new Mock<IInventoryAssistantService>();
        assistantService
            .Setup(service => service.QueryAsync(
                It.Is<AssistantQueryRequestDto>(request => request.Message == "show orders" && request.UserId == "42"),
                "Bearer token-123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssistantQueryResponseDto { Reply = "ok" });

        var controller = new AssistantController(assistantService.Object, Mock.Of<IHttpClientFactory>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim("sub", "42")],
                        "test"))
                }
            }
        };
        controller.HttpContext.Request.Headers["Authorization"] = "Bearer token-123";

        var result = await controller.Query(new AssistantQueryRequestDto { Message = "show orders" }, CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        assistantService.Verify(service => service.QueryAsync(
            It.Is<AssistantQueryRequestDto>(request => request.Message == "show orders" && request.UserId == "42"),
            "Bearer token-123",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
