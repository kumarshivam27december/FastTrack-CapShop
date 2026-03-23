namespace CapShop.OrderService.DTOs.Address
{
    public class AddressResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

    }
}
