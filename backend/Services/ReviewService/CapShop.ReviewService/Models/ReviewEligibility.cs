namespace CapShop.ReviewService.Models;

public class ReviewEligibility
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime DeliveredAtUtc { get; set; }
    public bool HasReviewed { get; set; }
}
