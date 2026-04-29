using System.Text;
using CapShop.AssistantService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CapShop.AssistantService.Tests;

public class AdminCatalogControllerTests
{
    [Test]
    public async Task Upload_WhenFileMissing_ReturnsBadRequest()
    {
        var knowledge = Mock.Of<IAssistantKnowledgeService>();
        var controller = new AdminCatalogController(knowledge);

        var result = await controller.Upload(null!);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Upload_WhenFilePresent_ReturnsImportedCount()
    {
        var knowledge = new Mock<IAssistantKnowledgeService>();
        knowledge
            .Setup(service => service.LoadFromStreamAsync(It.IsAny<Stream>(), "catalog.csv"))
            .ReturnsAsync(7);

        var controller = new AdminCatalogController(knowledge.Object);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("name,price\ncap,100"));
        var file = new FormFile(stream, 0, stream.Length, "file", "catalog.csv");

        var result = await controller.Upload(file);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.Not.Null);
        knowledge.Verify(service => service.LoadFromStreamAsync(It.IsAny<Stream>(), "catalog.csv"), Times.Once);
    }
}
