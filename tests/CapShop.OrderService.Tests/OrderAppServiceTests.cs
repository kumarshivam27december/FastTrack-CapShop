using CapShop.OrderService.Application.Services;
using CapShop.OrderService.DTOs.Address;
using CapShop.OrderService.DTOs.Cart;
using CapShop.OrderService.Infrastructure.Repositories;
using Moq;

namespace CapShop.OrderService.Tests;

public class OrderAppServiceTests
{
    [Test]
    public async Task GetOrCreateCartAsync_DelegatesToRepository()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetOrCreateCartAsync(5)).ReturnsAsync(new CartResponseDto { CartId = 99 });

        var sut = new OrderAppService(repo.Object);

        var result = await sut.GetOrCreateCartAsync(5);

        Assert.That(result.CartId, Is.EqualTo(99));
        repo.Verify(r => r.GetOrCreateCartAsync(5), Times.Once);
    }

    [Test]
    public async Task SaveAddressAsync_DelegatesToRepository()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.SaveAddressAsync(5, It.IsAny<AddressRequestDto>()))
            .ReturnsAsync(new AddressResponseDto { Id = 7 });

        var sut = new OrderAppService(repo.Object);

        var result = await sut.SaveAddressAsync(5, new AddressRequestDto
        {
            FullName = "Test User",
            Street = "Street",
            City = "City",
            State = "State",
            Pincode = "560001",
            Phone = "9999999999"
        });

        Assert.That(result.Id, Is.EqualTo(7));
        repo.Verify(r => r.SaveAddressAsync(5, It.IsAny<AddressRequestDto>()), Times.Once);
    }
}
