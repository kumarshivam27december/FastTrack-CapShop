namespace CapShop.OrderService.DTOs.Cart
{
    public class CartResponseDto 
    {
        public int CartId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public decimal TotalAmount => Items.Sum(x => x.TotalPrice);
        public int ItemCount => Items.Count;
    }
}
