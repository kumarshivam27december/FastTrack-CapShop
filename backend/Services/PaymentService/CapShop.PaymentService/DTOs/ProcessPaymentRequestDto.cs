namespace CapShop.PaymentService.DTOs;

public class ProcessPaymentRequestDto
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentMethod { get; set; } = "UPI";
    public bool SimulateSuccess { get; set; } = true;
}
