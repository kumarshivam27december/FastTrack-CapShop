namespace CapShop.AdminService.Infrastructure.Repositories
{
    public interface ICatalogReadRepository
    {
        Task<int> GetProductCountAsync(CancellationToken ct);

    }
}
