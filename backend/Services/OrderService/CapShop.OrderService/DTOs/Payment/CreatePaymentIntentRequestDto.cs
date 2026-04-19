namespace CapShop.OrderService.DTOs.Payment
{
    public class CreatePaymentIntentRequestDto
    {
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = "UPI";
        public string Currency { get; set; } = "INR";
    }
}
