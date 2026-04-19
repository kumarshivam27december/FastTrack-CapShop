namespace CapShop.PaymentService.DTOs;

public class CreateRazorpayOrderRequestDto
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentMethod { get; set; } = "UPI";
}
