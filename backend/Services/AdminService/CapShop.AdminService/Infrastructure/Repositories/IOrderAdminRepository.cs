using CapShop.AdminService.Contracts;

namespace CapShop.AdminService.Infrastructure.Repositories
{
    public interface IOrderAdminRepository
    {
        Task<List<AdminOrderDto>> GetAllOrdersAsync(string bearerToken, CancellationToken ct);
        Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusAdminRequest request, string bearerToken, CancellationToken ct);

    }
}
