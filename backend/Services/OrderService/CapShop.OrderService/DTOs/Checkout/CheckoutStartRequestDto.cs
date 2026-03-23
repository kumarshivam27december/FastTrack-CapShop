namespace CapShop.OrderService.DTOs.Checkout
{
    public class CheckoutStartRequestDto
    {
        public AddressRequestFromCheckout Address { get; set; } = null!;
    }

    public class AddressRequestFromCheckout
    {
        public string FullName { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
