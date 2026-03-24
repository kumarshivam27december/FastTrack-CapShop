namespace CapShop.AdminService.Contracts
{
    public class UpdateOrderStatusAdminRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
