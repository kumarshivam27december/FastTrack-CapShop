using System.Text.Json;
namespace CapShop.AdminService.Infrastructure.Repositories
{

    public class CatalogReadRepository : ICatalogReadRepository
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CatalogReadRepository> _logger;

        public CatalogReadRepository(IHttpClientFactory httpClientFactory, ILogger<CatalogReadRepository> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<int> GetProductCountAsync(CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("catalog-api");
            var response = await client.GetAsync("/catalog/products?page=1&pageSize=1", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Catalog service list call failed with status {StatusCode}", response.StatusCode);
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("total", out var totalElement) &&
                totalElement.TryGetInt32(out var total))
            {
                return total;
            }

            return 0;
        }
    }
}
