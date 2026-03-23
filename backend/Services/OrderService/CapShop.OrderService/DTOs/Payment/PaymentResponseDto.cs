namespace CapShop.OrderService.DTOs.Payment
{
    public class PaymentResponseDto
    {
        public int OrderId { get; set; }
        public string TransactionId   { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

    }
}
