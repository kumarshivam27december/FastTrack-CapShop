using CapShop.AdminService.Contracts;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
namespace CapShop.AdminService.Infrastructure.Repositories
{
    public class OrderAdminRepository : IOrderAdminRepository
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrderAdminRepository> _logger;

        public OrderAdminRepository(IHttpClientFactory httpClientFactory, ILogger<OrderAdminRepository> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<List<AdminOrderDto>> GetAllOrdersAsync(string bearerToken, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("orders-api");
            client.DefaultRequestHeaders.Authorization = BuildAuthHeader(bearerToken);

            var response = await client.GetAsync("/orders/admin/all", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Order service list call failed with status {StatusCode}", response.StatusCode);
                return new List<AdminOrderDto>();
            }

            var stream = await response.Content.ReadAsStreamAsync(ct);
            var data = await JsonSerializer.DeserializeAsync<List<AdminOrderDto>>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, ct);

            return data ?? new List<AdminOrderDto>();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusAdminRequest request, string bearerToken, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("orders-api");
            client.DefaultRequestHeaders.Authorization = BuildAuthHeader(bearerToken);

            var payload = JsonSerializer.Serialize(request);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"/orders/{orderId}/status", content, ct);
            return response.IsSuccessStatusCode;
        }

        private static AuthenticationHeaderValue BuildAuthHeader(string token)
        {
            var raw = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token["Bearer ".Length..]
                : token;
            return new AuthenticationHeaderValue("Bearer", raw);
        }
    }
}
