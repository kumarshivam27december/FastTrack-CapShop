using CapShop.AdminService.Contracts;

namespace CapShop.AdminService.Application.Interfaces
{
    public interface IAdminAppService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(string bearerToken, CancellationToken ct);
        Task<List<AdminOrderDto>> GetOrdersAsync(string bearerToken, CancellationToken ct);
        Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusAdminRequest request, string bearerToken, string adminEmail, CancellationToken ct);
        Task<List<StatusSplitDto>> GetStatusSplitAsync(string bearerToken, CancellationToken ct);
        Task<List<SalesReportRowDto>> GetSalesReportAsync(DateOnly from, DateOnly to, string bearerToken, CancellationToken ct);
        Task<string> BuildSalesCsvAsync(DateOnly from, DateOnly to, string bearerToken, CancellationToken ct);
    }
}
