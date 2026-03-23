namespace CapShop.OrderService.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;

        public Cart Cart { get; set; } = null!;
    }
}
