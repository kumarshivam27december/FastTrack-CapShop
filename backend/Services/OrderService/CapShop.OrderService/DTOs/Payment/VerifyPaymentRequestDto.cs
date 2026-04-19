namespace CapShop.OrderService.DTOs.Payment
{
    public class VerifyPaymentRequestDto
    {
        public int OrderId { get; set; }
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
    }
}
