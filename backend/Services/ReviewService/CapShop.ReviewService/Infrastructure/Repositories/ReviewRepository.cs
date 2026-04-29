using CapShop.ReviewService.Data;
using CapShop.ReviewService.DTOs;
using CapShop.ReviewService.Models;
using CapShop.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CapShop.ReviewService.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ReviewDbContext _db;

    public ReviewRepository(ReviewDbContext db)
    {
        _db = db;
    }

    public async Task<List<ReviewResponseDto>> GetApprovedProductReviewsAsync(int productId, CancellationToken ct)
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        return reviews.Select(MapReview).ToList();
    }

    public async Task<ProductReviewSummaryDto> GetProductSummaryAsync(int productId, int? userId, CancellationToken ct)
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .ToListAsync(ct);

        var canReview = false;
        if (userId is > 0)
        {
            canReview = await _db.ReviewEligibilities.AsNoTracking().AnyAsync(e =>
                e.UserId == userId &&
                e.ProductId == productId &&
                !e.HasReviewed, ct);
        }

        return new ProductReviewSummaryDto
        {
            ProductId = productId,
            ReviewCount = reviews.Count,
            AverageRating = reviews.Count == 0 ? 0 : Math.Round((decimal)reviews.Average(r => r.Rating), 1),
            FiveStarCount = reviews.Count(r => r.Rating == 5),
            FourStarCount = reviews.Count(r => r.Rating == 4),
            ThreeStarCount = reviews.Count(r => r.Rating == 3),
            TwoStarCount = reviews.Count(r => r.Rating == 2),
            OneStarCount = reviews.Count(r => r.Rating == 1),
            CanCurrentUserReview = canReview
        };
    }

    public async Task<List<ReviewResponseDto>> GetMyReviewsAsync(int userId, CancellationToken ct)
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        return reviews.Select(MapReview).ToList();
    }

    public async Task<List<ReviewEligibilityResponseDto>> GetMyEligibilitiesAsync(int userId, CancellationToken ct)
    {
        return await _db.ReviewEligibilities
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.DeliveredAtUtc)
            .Select(e => new ReviewEligibilityResponseDto
            {
                ProductId = e.ProductId,
                OrderId = e.OrderId,
                ProductName = e.ProductName,
                DeliveredAtUtc = e.DeliveredAtUtc,
                HasReviewed = e.HasReviewed
            })
            .ToListAsync(ct);
    }

    public async Task<ReviewResponseDto> CreateReviewAsync(int productId, int userId, string userName, CreateReviewRequestDto request, CancellationToken ct)
    {
        ValidateReview(request.Rating, request.Title, request.Comment);

        var existing = await _db.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == productId, ct);
        if (existing)
        {
            throw new ConflictException("You have already reviewed this product.");
        }

        var eligibilityQuery = _db.ReviewEligibilities
            .Where(e => e.UserId == userId && e.ProductId == productId && !e.HasReviewed);

        if (request.OrderId is > 0)
        {
            eligibilityQuery = eligibilityQuery.Where(e => e.OrderId == request.OrderId.Value);
        }

        var eligibility = await eligibilityQuery.OrderByDescending(e => e.DeliveredAtUtc).FirstOrDefaultAsync(ct);
        if (eligibility is null)
        {
            throw new ConflictException("Only delivered purchases can be reviewed.");
        }

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? $"Customer {userId}" : userName.Trim(),
            OrderId = eligibility.OrderId,
            Rating = request.Rating,
            Title = request.Title.Trim(),
            Comment = request.Comment.Trim(),
            IsVerifiedPurchase = true,
            Status = ReviewStatus.Approved
        };

        eligibility.HasReviewed = true;
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);

        return MapReview(review);
    }

    public async Task<ReviewResponseDto> UpdateReviewAsync(int reviewId, int userId, UpdateReviewRequestDto request, CancellationToken ct)
    {
        ValidateReview(request.Rating, request.Title, request.Comment);

        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId, ct);
        if (review is null)
        {
            throw new NotFoundException("Review not found.");
        }

        review.Rating = request.Rating;
        review.Title = request.Title.Trim();
        review.Comment = request.Comment.Trim();
        review.Status = ReviewStatus.Approved;
        review.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapReview(review);
    }

    public async Task<bool> DeleteReviewAsync(int reviewId, int userId, CancellationToken ct)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId, ct);
        if (review is null)
        {
            return false;
        }

        var eligibility = await _db.ReviewEligibilities.FirstOrDefaultAsync(e =>
            e.UserId == userId &&
            e.ProductId == review.ProductId &&
            e.OrderId == review.OrderId, ct);

        if (eligibility is not null)
        {
            eligibility.HasReviewed = false;
        }

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<ReviewResponseDto>> GetPendingReviewsAsync(CancellationToken ct)
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.Status == ReviewStatus.Pending)
            .OrderBy(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        return reviews.Select(MapReview).ToList();
    }

    public async Task<bool> ApproveReviewAsync(int reviewId, CancellationToken ct)
    {
        var review = await _db.Reviews.FindAsync([reviewId], ct);
        if (review is null)
        {
            return false;
        }

        review.Status = ReviewStatus.Approved;
        review.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RejectReviewAsync(int reviewId, CancellationToken ct)
    {
        var review = await _db.Reviews.FindAsync([reviewId], ct);
        if (review is null)
        {
            return false;
        }

        review.Status = ReviewStatus.Rejected;
        review.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task UpsertEligibilityAsync(int userId, int productId, int orderId, string productName, DateTime deliveredAtUtc, CancellationToken ct)
    {
        var existing = await _db.ReviewEligibilities.FirstOrDefaultAsync(e =>
            e.UserId == userId &&
            e.ProductId == productId &&
            e.OrderId == orderId, ct);

        if (existing is not null)
        {
            existing.ProductName = productName;
            existing.DeliveredAtUtc = deliveredAtUtc;
        }
        else
        {
            _db.ReviewEligibilities.Add(new ReviewEligibility
            {
                UserId = userId,
                ProductId = productId,
                OrderId = orderId,
                ProductName = productName,
                DeliveredAtUtc = deliveredAtUtc
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateReview(int rating, string title, string comment)
    {
        if (rating < 1 || rating > 5)
        {
            throw new ValidationException("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 120)
        {
            throw new ValidationException("Review title is required and must be 120 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length > 2000)
        {
            throw new ValidationException("Review comment is required and must be 2000 characters or fewer.");
        }
    }

    private static ReviewResponseDto MapReview(Review review)
    {
        return new ReviewResponseDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = review.UserName,
            OrderId = review.OrderId,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            Status = review.Status.ToString(),
            CreatedAtUtc = review.CreatedAtUtc,
            UpdatedAtUtc = review.UpdatedAtUtc
        };
    }
}
