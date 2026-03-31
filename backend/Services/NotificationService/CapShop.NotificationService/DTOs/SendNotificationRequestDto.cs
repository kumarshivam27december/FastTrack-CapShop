namespace CapShop.NotificationService.DTOs;

public class SendNotificationRequestDto
{
    public int? UserId { get; set; }
    public int? OrderId { get; set; }
    public int? PaymentId { get; set; }
    public string Channel { get; set; } = "Email";
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool SimulateSuccess { get; set; } = true;
}
