using CapShop.ReviewService.DTOs;

namespace CapShop.ReviewService.Infrastructure.Repositories;

public interface IReviewRepository
{
    Task<List<ReviewResponseDto>> GetApprovedProductReviewsAsync(int productId, CancellationToken ct);
    Task<ProductReviewSummaryDto> GetProductSummaryAsync(int productId, int? userId, CancellationToken ct);
    Task<List<ReviewResponseDto>> GetMyReviewsAsync(int userId, CancellationToken ct);
    Task<List<ReviewEligibilityResponseDto>> GetMyEligibilitiesAsync(int userId, CancellationToken ct);
    Task<ReviewResponseDto> CreateReviewAsync(int productId, int userId, string userName, CreateReviewRequestDto request, CancellationToken ct);
    Task<ReviewResponseDto> UpdateReviewAsync(int reviewId, int userId, UpdateReviewRequestDto request, CancellationToken ct);
    Task<bool> DeleteReviewAsync(int reviewId, int userId, CancellationToken ct);
    Task<List<ReviewResponseDto>> GetPendingReviewsAsync(CancellationToken ct);
    Task<bool> ApproveReviewAsync(int reviewId, CancellationToken ct);
    Task<bool> RejectReviewAsync(int reviewId, CancellationToken ct);
    Task UpsertEligibilityAsync(int userId, int productId, int orderId, string productName, DateTime deliveredAtUtc, CancellationToken ct);
}
