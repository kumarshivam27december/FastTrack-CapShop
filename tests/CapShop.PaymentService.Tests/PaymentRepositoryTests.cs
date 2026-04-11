using CapShop.PaymentService.Data;
using CapShop.PaymentService.Infrastructure.Repositories;
using CapShop.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.PaymentService.Tests;

public class PaymentRepositoryTests
{
    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }

    [Test]
    public async Task CreateAsync_SetsCreatedAndUpdatedTimestamps()
    {
        await using var db = CreateDbContext();
        var sut = new PaymentRepository(db);

        var payment = new PaymentRecord
        {
            OrderId = 1,
            UserId = 2,
            Amount = 450,
            Currency = "INR",
            PaymentMethod = "UPI",
            Status = PaymentStatus.Pending
        };

        var created = await sut.CreateAsync(payment);

        Assert.That(created.Id, Is.GreaterThan(0));
        Assert.That(created.CreatedAtUtc, Is.Not.EqualTo(default(DateTime)));
        Assert.That(created.UpdatedAtUtc, Is.EqualTo(created.CreatedAtUtc));
    }

    [Test]
    public async Task GetLatestByOrderIdAsync_ReturnsMostRecentPayment()
    {
        await using var db = CreateDbContext();

        db.Payments.Add(new PaymentRecord
        {
            OrderId = 77,
            UserId = 3,
            Amount = 100,
            Status = PaymentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3),
            UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-3)
        });

        db.Payments.Add(new PaymentRecord
        {
            OrderId = 77,
            UserId = 3,
            Amount = 150,
            Status = PaymentStatus.Succeeded,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var sut = new PaymentRepository(db);
        var latest = await sut.GetLatestByOrderIdAsync(77);

        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Amount, Is.EqualTo(150));
        Assert.That(latest.Status, Is.EqualTo(PaymentStatus.Succeeded));
    }

    [Test]
    public async Task UpdateStatusAsync_WhenSucceeded_AssignsTransactionIdAndClearsFailureReason()
    {
        await using var db = CreateDbContext();

        var payment = new PaymentRecord
        {
            OrderId = 99,
            UserId = 8,
            Amount = 780,
            Status = PaymentStatus.Failed,
            FailureReason = "Bank timeout"
        };

        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var sut = new PaymentRepository(db);
        var updated = await sut.UpdateStatusAsync(payment.Id, PaymentStatus.Succeeded, "ignored");

        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Status, Is.EqualTo(PaymentStatus.Succeeded));
        Assert.That(updated.FailureReason, Is.Null);
        Assert.That(updated.TransactionId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task UpdateStatusAsync_WhenPaymentNotFound_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var sut = new PaymentRepository(db);

        var updated = await sut.UpdateStatusAsync(404, PaymentStatus.Failed, "No record");

        Assert.That(updated, Is.Null);
    }
}
