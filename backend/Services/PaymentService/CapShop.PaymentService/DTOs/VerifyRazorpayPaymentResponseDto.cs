namespace CapShop.PaymentService.DTOs;

public class VerifyRazorpayPaymentResponseDto
{
    public int OrderId { get; set; }
    public bool Verified { get; set; }
    public string? TransactionId { get; set; }
    public string Message { get; set; } = string.Empty;
}
