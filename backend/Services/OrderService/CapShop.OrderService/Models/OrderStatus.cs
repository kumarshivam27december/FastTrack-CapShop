namespace CapShop.OrderService.Models
{
    public enum OrderStatus
    {
        Draft = 0,
        CheckoutStarted = 1,
        PaymentPending = 2,
        Paid = 3,
        Packed = 4,
        Shipped = 5,
        Delivered = 6,
        Cancelled = 7
    }
}
