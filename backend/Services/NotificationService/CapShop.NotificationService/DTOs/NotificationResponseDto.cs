namespace CapShop.NotificationService.DTOs;

public class NotificationResponseDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int? OrderId { get; set; }
    public int? PaymentId { get; set; }
    public string Channel { get; set; } = "Email";
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Sent";
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
