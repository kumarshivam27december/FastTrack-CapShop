namespace CapShop.ReviewService.DTOs;

public class CreateReviewRequestDto
{
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int? OrderId { get; set; }
}

public class UpdateReviewRequestDto
{
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

public class ReviewResponseDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public int Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class ProductReviewSummaryDto
{
    public int ProductId { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
    public bool CanCurrentUserReview { get; set; }
}

public class ReviewEligibilityResponseDto
{
    public int ProductId { get; set; }
    public int OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime DeliveredAtUtc { get; set; }
    public bool HasReviewed { get; set; }
}
