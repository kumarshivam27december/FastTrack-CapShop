namespace CapShop.OrderService.DTOs.Payment
{
    public class PaymentIntentResponseDto
    {
        public int OrderId { get; set; }
        public string RazorpayOrderId { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string KeyId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
