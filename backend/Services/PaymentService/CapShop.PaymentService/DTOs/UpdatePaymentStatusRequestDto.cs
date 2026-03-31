namespace CapShop.PaymentService.DTOs;

public class UpdatePaymentStatusRequestDto
{
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}
