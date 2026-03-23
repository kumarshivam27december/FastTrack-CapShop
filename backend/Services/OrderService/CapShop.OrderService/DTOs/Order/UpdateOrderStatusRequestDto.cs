namespace CapShop.OrderService.DTOs.Order
{
    public class UpdateOrderStatusRequestDto
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Notes {  get; set; }
    }
}
