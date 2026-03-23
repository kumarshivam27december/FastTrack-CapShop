namespace CapShop.OrderService.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public OrderStatus FromStatus { get; set; }
        public OrderStatus ToStatus { get; set; }
        public string? Notes { get; set; }
        public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
        public int? ChangedByUserId { get; set; } // Admin user ID

        public Order Order { get; set; } = null!;
    }
}
