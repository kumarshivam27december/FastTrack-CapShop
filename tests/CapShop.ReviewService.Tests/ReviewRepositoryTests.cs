using CapShop.ReviewService.Data;
using CapShop.ReviewService.DTOs;
using CapShop.ReviewService.Infrastructure.Repositories;
using CapShop.ReviewService.Models;
using CapShop.Shared.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CapShop.ReviewService.Tests;

public class ReviewRepositoryTests
{
    private static ReviewDbContext CreateDbContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ReviewDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ReviewDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Test]
    public async Task GetProductSummaryAsync_WhenEligibilityExists_AllowsCurrentUserReview()
    {
        await using var db = CreateDbContext(out var connection);
        await using var _ = connection;

        db.ReviewEligibilities.Add(new ReviewEligibility
        {
            UserId = 22,
            ProductId = 5,
            OrderId = 77,
            ProductName = "Premium Cap",
            DeliveredAtUtc = DateTime.UtcNow.AddDays(-1),
            HasReviewed = false
        });

        db.Reviews.Add(new Review
        {
            ProductId = 5,
            UserId = 11,
            UserName = "Other User",
            OrderId = 70,
            Rating = 4,
            Title = "Nice",
            Comment = "Good",
            Status = ReviewStatus.Approved
        });

        await db.SaveChangesAsync();

        var sut = new ReviewRepository(db);

        var summary = await sut.GetProductSummaryAsync(5, 22, CancellationToken.None);

        Assert.That(summary.ReviewCount, Is.EqualTo(1));
        Assert.That(summary.AverageRating, Is.EqualTo(4m));
        Assert.That(summary.CanCurrentUserReview, Is.True);
    }

    [Test]
    public async Task CreateReviewAsync_WhenEligibilityExists_CreatesVerifiedReviewAndMarksEligibility()
    {
        await using var db = CreateDbContext(out var connection);
        await using var _ = connection;

        db.ReviewEligibilities.Add(new ReviewEligibility
        {
            UserId = 22,
            ProductId = 5,
            OrderId = 77,
            ProductName = "Premium Cap",
            DeliveredAtUtc = DateTime.UtcNow.AddDays(-1),
            HasReviewed = false
        });
        await db.SaveChangesAsync();

        var sut = new ReviewRepository(db);

        var review = await sut.CreateReviewAsync(5, 22, "Priya", new CreateReviewRequestDto
        {
            Rating = 5,
            Title = "Excellent",
            Comment = "Fits well",
            OrderId = 77
        }, CancellationToken.None);

        var eligibility = await db.ReviewEligibilities.SingleAsync();

        Assert.That(review.Id, Is.GreaterThan(0));
        Assert.That(review.IsVerifiedPurchase, Is.True);
        Assert.That(review.Status, Is.EqualTo("Approved"));
        Assert.That(eligibility.HasReviewed, Is.True);
        Assert.That(await db.Reviews.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void CreateReviewAsync_WhenNoEligibility_ThrowsConflictException()
    {
        using var db = CreateDbContext(out var connection);
        using var _ = connection;
        var sut = new ReviewRepository(db);

        var action = async () => await sut.CreateReviewAsync(5, 22, "Priya", new CreateReviewRequestDto
        {
            Rating = 5,
            Title = "Excellent",
            Comment = "Fits well"
        }, CancellationToken.None);

        Assert.ThrowsAsync<ConflictException>(async () => await action());
    }
}
