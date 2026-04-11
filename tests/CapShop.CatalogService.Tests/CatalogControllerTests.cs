using CapShop.CatalogService.Application.Interfaces;
using CapShop.CatalogService.Controllers;
using CapShop.CatalogService.DTOs.Catalog;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CapShop.CatalogService.Tests;

public class CatalogControllerTests
{
    [Test]
    public async Task ProductController_GetProductById_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IProductAppService>();
        service.Setup(x => x.GetProductByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync((ProductResponseDto?)null);

        var controller = new ProductController(service.Object);

        var result = await controller.GetProductById(10, CancellationToken.None);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task ProductController_UpdateProduct_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<IProductAppService>();
        service.Setup(x => x.UpdateProductAsync(10, It.IsAny<UpdateProductDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var controller = new ProductController(service.Object);

        var result = await controller.UpdateProduct(10, new UpdateProductDto
        {
            Name = "Cap",
            Description = "Desc",
            Price = 50,
            Stock = 2,
            ImageUrl = "img",
            CategoryId = 1,
            IsActive = true
        }, CancellationToken.None);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task CategoryController_GetCategoryById_WhenMissing_ReturnsNotFound()
    {
        var service = new Mock<ICategoryAppService>();
        service.Setup(x => x.GetCategoryByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync((CategoryResponseDto?)null);

        var controller = new CategoryController(service.Object);

        var result = await controller.GetCategoryById(3, CancellationToken.None);

        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }
}
