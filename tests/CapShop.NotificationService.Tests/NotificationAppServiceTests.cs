using CapShop.NotificationService.Application.Services;
using CapShop.NotificationService.DTOs;
using CapShop.NotificationService.Infrastructure.Repositories;
using CapShop.NotificationService.Models;
using Moq;

namespace CapShop.NotificationService.Tests;

public class NotificationAppServiceTests
{
    [Test]
    public void SendAsync_WhenRecipientMissing_Throws()
    {
        var repo = new Mock<INotificationRepository>();
        var sut = new NotificationAppService(repo.Object);

        var action = async () => await sut.SendAsync(9, new SendNotificationRequestDto
        {
            Subject = "Subject",
            Message = "Body",
            Recipient = ""
        });

        Assert.ThrowsAsync<InvalidOperationException>(async () => await action());
    }

    [Test]
    public async Task SendAsync_WhenSimulateSuccess_ReturnsSentNotification()
    {
        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<NotificationRecord>()))
            .ReturnsAsync((NotificationRecord n) =>
            {
                n.Id = 23;
                return n;
            });

        var sut = new NotificationAppService(repo.Object);

        var result = await sut.SendAsync(9, new SendNotificationRequestDto
        {
            Recipient = "test@capshop.com",
            Subject = "Order update",
            Message = "Your order is packed",
            SimulateSuccess = true
        });

        Assert.That(result.Id, Is.EqualTo(23));
        Assert.That(result.UserId, Is.EqualTo(9));
        Assert.That(result.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(result.ProviderMessageId, Is.Not.Null.And.Not.Empty);
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.SentAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task SendAsync_WhenSimulateFailure_ReturnsFailedNotification()
    {
        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<NotificationRecord>()))
            .ReturnsAsync((NotificationRecord n) => n);

        var sut = new NotificationAppService(repo.Object);

        var result = await sut.SendAsync(12, new SendNotificationRequestDto
        {
            Recipient = "test@capshop.com",
            Subject = "Order update",
            Message = "Your order is packed",
            SimulateSuccess = false
        });

        Assert.That(result.UserId, Is.EqualTo(12));
        Assert.That(result.Status, Is.EqualTo(NotificationStatus.Failed));
        Assert.That(result.ProviderMessageId, Is.Null);
        Assert.That(result.ErrorMessage, Is.EqualTo("Simulated send failure"));
        Assert.That(result.SentAtUtc, Is.Null);
    }

    [Test]
    public async Task MarkAsReadAsync_WhenFound_ReturnsMappedDto()
    {
        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.MarkAsReadAsync(15, 3)).ReturnsAsync(new NotificationRecord
        {
            Id = 15,
            UserId = 3,
            Subject = "Sub",
            Recipient = "a@b.com",
            Message = "Body",
            Channel = "Email",
            Status = NotificationStatus.Sent,
            IsRead = true,
            ReadAtUtc = DateTime.UtcNow
        });

        var sut = new NotificationAppService(repo.Object);

        var result = await sut.MarkAsReadAsync(15, 3);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(15));
        Assert.That(result.IsRead, Is.True);
        Assert.That(result.ReadAtUtc, Is.Not.Null);
    }
}
