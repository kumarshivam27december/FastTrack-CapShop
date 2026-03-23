namespace CapShop.OrderService.DTOs.Payment
{
    public class PaymentSimulateRequestDto
    {
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // UPI, Card, COD
        public bool SimulateSuccess { get; set; } = true;
    }
}
