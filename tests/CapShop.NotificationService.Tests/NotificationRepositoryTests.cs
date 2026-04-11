using CapShop.NotificationService.Data;
using CapShop.NotificationService.Infrastructure.Repositories;
using CapShop.NotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.NotificationService.Tests;

public class NotificationRepositoryTests
{
    private static NotificationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NotificationDbContext(options);
    }

    [Test]
    public async Task CreateAsync_PersistsNotification()
    {
        await using var db = CreateDbContext();
        var sut = new NotificationRepository(db);

        var created = await sut.CreateAsync(new NotificationRecord
        {
            UserId = 1,
            OrderId = 10,
            Channel = "Email",
            Recipient = "u@capshop.com",
            Subject = "Subject",
            Message = "Body",
            Status = NotificationStatus.Sent
        });

        Assert.That(created.Id, Is.GreaterThan(0));
        Assert.That(created.CreatedAtUtc, Is.Not.EqualTo(default(DateTime)));
        Assert.That(await db.Notifications.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetByUserAsync_ReturnsDescendingByCreatedAt()
    {
        await using var db = CreateDbContext();

        db.Notifications.Add(new NotificationRecord
        {
            UserId = 99,
            Subject = "Older",
            Recipient = "u@capshop.com",
            Message = "Body",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
        });

        db.Notifications.Add(new NotificationRecord
        {
            UserId = 99,
            Subject = "Newer",
            Recipient = "u@capshop.com",
            Message = "Body",
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var sut = new NotificationRepository(db);
        var list = await sut.GetByUserAsync(99);

        Assert.That(list.Count, Is.EqualTo(2));
        Assert.That(list[0].Subject, Is.EqualTo("Newer"));
        Assert.That(list[1].Subject, Is.EqualTo("Older"));
    }

    [Test]
    public async Task MarkAsReadAsync_WhenUserMatches_UpdatesReadFlags()
    {
        await using var db = CreateDbContext();
        var rec = new NotificationRecord
        {
            UserId = 7,
            Subject = "Unread",
            Recipient = "u@capshop.com",
            Message = "Body"
        };

        db.Notifications.Add(rec);
        await db.SaveChangesAsync();

        var sut = new NotificationRepository(db);
        var updated = await sut.MarkAsReadAsync(rec.Id, 7);

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.IsRead, Is.True);
        Assert.That(updated.ReadAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task MarkAsReadAsync_WhenDifferentUser_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var rec = new NotificationRecord
        {
            UserId = 7,
            Subject = "Unread",
            Recipient = "u@capshop.com",
            Message = "Body"
        };

        db.Notifications.Add(rec);
        await db.SaveChangesAsync();

        var sut = new NotificationRepository(db);
        var updated = await sut.MarkAsReadAsync(rec.Id, 8);

        Assert.That(updated, Is.Null);
    }
}
