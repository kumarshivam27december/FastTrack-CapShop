namespace CapShop.AdminService.Contracts
{
    public class SalesReportRowDto
    {
        public DateOnly Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
