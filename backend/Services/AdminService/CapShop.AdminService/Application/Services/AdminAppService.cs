using System.Text;
using CapShop.AdminService.Application.Interfaces;
using CapShop.AdminService.Contracts;
using CapShop.AdminService.Domain;
using CapShop.AdminService.Infrastructure;
using CapShop.AdminService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CapShop.AdminService.Application.Services
{

    public class AdminAppService : IAdminAppService
    {
        private readonly IOrderAdminRepository _orderRepo;
        private readonly ICatalogReadRepository _catalogRepo;
        private readonly AdminDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AdminAppService> _logger;

        public AdminAppService(
            IOrderAdminRepository orderRepo,
            ICatalogReadRepository catalogRepo,
            AdminDbContext db,
            IMemoryCache cache,
            ILogger<AdminAppService> logger)
        {
            _orderRepo = orderRepo;
            _catalogRepo = catalogRepo;
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(string bearerToken, CancellationToken ct)
        {
            const string cacheKey = "admin-dashboard-summary";
            if (_cache.TryGetValue(cacheKey, out DashboardSummaryDto? cached) && cached is not null)
            {
                return cached;
            }

            var orders = await _orderRepo.GetAllOrdersAsync(bearerToken, ct);
            var productCount = await _catalogRepo.GetProductCountAsync(ct);

            var summary = new DashboardSummaryDto
            {
                TotalOrders = orders.Count,
                OrdersToday = orders.Count(o => o.CreatedAtUtc.Date == DateTime.UtcNow.Date),
                RevenueTotal = orders
                    .Where(o => !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    .Sum(o => o.TotalAmount),
                TotalProducts = productCount,
                RecentOrders = orders
                    .OrderByDescending(o => o.CreatedAtUtc)
                    .Take(10)
                    .ToList()
            };

            _cache.Set(cacheKey, summary, TimeSpan.FromSeconds(30));
            return summary;
        }

        public Task<List<AdminOrderDto>> GetOrdersAsync(string bearerToken, CancellationToken ct)
        {
            return _orderRepo.GetAllOrdersAsync(bearerToken, ct);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusAdminRequest request, string bearerToken, string adminEmail, CancellationToken ct)
        {
            var updated = await _orderRepo.UpdateOrderStatusAsync(orderId, request, bearerToken, ct);
            if (!updated) return false;

            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                Action = "UpdateOrderStatus",
                EntityName = "Order",
                EntityId = orderId.ToString(),
                PerformedBy = adminEmail,
                Notes = $"Set to {request.NewStatus}. Notes: {request.Notes}"
            });

            await _db.SaveChangesAsync(ct);
            _cache.Remove("admin-dashboard-summary");
            _logger.LogInformation("Admin {AdminEmail} updated order {OrderId} to {Status}", adminEmail, orderId, request.NewStatus);

            return true;
        }

        public async Task<List<StatusSplitDto>> GetStatusSplitAsync(string bearerToken, CancellationToken ct)
        {
            var orders = await _orderRepo.GetAllOrdersAsync(bearerToken, ct);
            return orders
                .GroupBy(x => x.Status)
                .Select(g => new StatusSplitDto { Status = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        public async Task<List<SalesReportRowDto>> GetSalesReportAsync(DateOnly from, DateOnly to, string bearerToken, CancellationToken ct)
        {
            var cacheKey = $"sales-report-{from:yyyyMMdd}-{to:yyyyMMdd}";
            if (_cache.TryGetValue(cacheKey, out List<SalesReportRowDto>? cached) && cached is not null)
            {
                return cached;
            }

            var orders = await _orderRepo.GetAllOrdersAsync(bearerToken, ct);

            var filtered = orders
                .Where(o =>
                {
                    var d = DateOnly.FromDateTime(o.CreatedAtUtc);
                    return d >= from && d <= to && !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            var report = filtered
                .GroupBy(o => DateOnly.FromDateTime(o.CreatedAtUtc))
                .Select(g => new SalesReportRowDto
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            _cache.Set(cacheKey, report, TimeSpan.FromSeconds(30));
            return report;
        }

        public async Task<string> BuildSalesCsvAsync(DateOnly from, DateOnly to, string bearerToken, CancellationToken ct)
        {
            var rows = await GetSalesReportAsync(from, to, bearerToken, ct);
            var sb = new StringBuilder();
            sb.AppendLine("Date,OrderCount,Revenue");
            foreach (var row in rows)
            {
                sb.AppendLine($"{row.Date:yyyy-MM-dd},{row.OrderCount},{row.Revenue}");
            }

            return sb.ToString();
        }
    }

}