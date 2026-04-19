namespace CapShop.OrderService.DTOs.Payment
{
    public class VerifyPaymentResponseDto
    {
        public int OrderId { get; set; }
        public bool Verified { get; set; }
        public string? TransactionId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
