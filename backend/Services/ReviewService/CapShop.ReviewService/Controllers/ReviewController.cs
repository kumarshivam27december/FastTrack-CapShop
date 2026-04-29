using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CapShop.ReviewService.Application.Interfaces;
using CapShop.ReviewService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.ReviewService.Controllers;

[ApiController]
[Route("reviews")]
public class ReviewController : ControllerBase
{
    private readonly IReviewAppService _reviewService;

    public ReviewController(IReviewAppService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { service = "ReviewService", status = "Healthy" });

    [HttpGet("products/{productId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviews(int productId, CancellationToken ct)
    {
        var reviews = await _reviewService.GetApprovedProductReviewsAsync(productId, ct);
        return Ok(reviews);
    }

    [HttpGet("products/{productId:int}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductSummary(int productId, CancellationToken ct)
    {
        var summary = await _reviewService.GetProductSummaryAsync(productId, TryGetUserId(), ct);
        return Ok(summary);
    }

    [HttpGet("my")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> GetMyReviews(CancellationToken ct)
    {
        var reviews = await _reviewService.GetMyReviewsAsync(GetUserIdStrict(), ct);
        return Ok(reviews);
    }

    [HttpGet("my/eligibilities")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> GetMyEligibilities(CancellationToken ct)
    {
        var eligibilities = await _reviewService.GetMyEligibilitiesAsync(GetUserIdStrict(), ct);
        return Ok(eligibilities);
    }

    [HttpPost("products/{productId:int}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> CreateReview(int productId, [FromBody] CreateReviewRequestDto request, CancellationToken ct)
    {
        var review = await _reviewService.CreateReviewAsync(productId, GetUserIdStrict(), GetUserDisplayName(), request, ct);
        return CreatedAtAction(nameof(GetProductReviews), new { productId }, review);
    }

    [HttpPut("{reviewId:int}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] UpdateReviewRequestDto request, CancellationToken ct)
    {
        var review = await _reviewService.UpdateReviewAsync(reviewId, GetUserIdStrict(), request, ct);
        return Ok(review);
    }

    [HttpDelete("{reviewId:int}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> DeleteReview(int reviewId, CancellationToken ct)
    {
        var deleted = await _reviewService.DeleteReviewAsync(reviewId, GetUserIdStrict(), ct);
        if (!deleted) return NotFound();
        return Ok(new { message = "Review deleted successfully" });
    }

    [HttpGet("admin/pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPendingReviews(CancellationToken ct)
    {
        var reviews = await _reviewService.GetPendingReviewsAsync(ct);
        return Ok(reviews);
    }

    [HttpPut("admin/{reviewId:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveReview(int reviewId, CancellationToken ct)
    {
        var updated = await _reviewService.ApproveReviewAsync(reviewId, ct);
        if (!updated) return NotFound();
        return Ok(new { message = "Review approved" });
    }

    [HttpPut("admin/{reviewId:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectReview(int reviewId, CancellationToken ct)
    {
        var updated = await _reviewService.RejectReviewAsync(reviewId, ct);
        if (!updated) return NotFound();
        return Ok(new { message = "Review rejected" });
    }

    private int GetUserIdStrict()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            throw new UnauthorizedAccessException("Invalid token userId claim.");
        }

        return userId;
    }

    private int? TryGetUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out var userId) && userId > 0 ? userId : null;
    }

    private string GetUserDisplayName()
    {
        return User.FindFirst("name")?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("email")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? "CapShop Customer";
    }
}
