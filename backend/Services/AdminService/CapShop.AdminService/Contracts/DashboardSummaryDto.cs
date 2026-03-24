namespace CapShop.AdminService.Contracts
{
    public class DashboardSummaryDto
    {
        public int TotalOrders { get; set; }
        public int OrdersToday { get; set; }
        public decimal RevenueTotal { get; set; }
        public int TotalProducts { get; set; }
        public List<AdminOrderDto> RecentOrders { get; set; } = new();
    }
}
