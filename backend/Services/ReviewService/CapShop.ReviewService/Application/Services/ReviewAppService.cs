using CapShop.ReviewService.Application.Interfaces;
using CapShop.ReviewService.DTOs;
using CapShop.ReviewService.Infrastructure.Repositories;

namespace CapShop.ReviewService.Application.Services;

public class ReviewAppService : IReviewAppService
{
    private readonly IReviewRepository _repo;

    public ReviewAppService(IReviewRepository repo)
    {
        _repo = repo;
    }

    public Task<List<ReviewResponseDto>> GetApprovedProductReviewsAsync(int productId, CancellationToken ct) =>
        _repo.GetApprovedProductReviewsAsync(productId, ct);

    public Task<ProductReviewSummaryDto> GetProductSummaryAsync(int productId, int? userId, CancellationToken ct) =>
        _repo.GetProductSummaryAsync(productId, userId, ct);

    public Task<List<ReviewResponseDto>> GetMyReviewsAsync(int userId, CancellationToken ct) =>
        _repo.GetMyReviewsAsync(userId, ct);

    public Task<List<ReviewEligibilityResponseDto>> GetMyEligibilitiesAsync(int userId, CancellationToken ct) =>
        _repo.GetMyEligibilitiesAsync(userId, ct);

    public Task<ReviewResponseDto> CreateReviewAsync(int productId, int userId, string userName, CreateReviewRequestDto request, CancellationToken ct) =>
        _repo.CreateReviewAsync(productId, userId, userName, request, ct);

    public Task<ReviewResponseDto> UpdateReviewAsync(int reviewId, int userId, UpdateReviewRequestDto request, CancellationToken ct) =>
        _repo.UpdateReviewAsync(reviewId, userId, request, ct);

    public Task<bool> DeleteReviewAsync(int reviewId, int userId, CancellationToken ct) =>
        _repo.DeleteReviewAsync(reviewId, userId, ct);

    public Task<List<ReviewResponseDto>> GetPendingReviewsAsync(CancellationToken ct) =>
        _repo.GetPendingReviewsAsync(ct);

    public Task<bool> ApproveReviewAsync(int reviewId, CancellationToken ct) =>
        _repo.ApproveReviewAsync(reviewId, ct);

    public Task<bool> RejectReviewAsync(int reviewId, CancellationToken ct) =>
        _repo.RejectReviewAsync(reviewId, ct);

    public Task UpsertEligibilityAsync(int userId, int productId, int orderId, string productName, DateTime deliveredAtUtc, CancellationToken ct) =>
        _repo.UpsertEligibilityAsync(userId, productId, orderId, productName, deliveredAtUtc, ct);
}
