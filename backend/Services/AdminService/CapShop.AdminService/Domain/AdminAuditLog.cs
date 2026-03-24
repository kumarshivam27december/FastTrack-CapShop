namespace CapShop.AdminService.Domain
{
    public class AdminAuditLog
    {
        public long Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }
}
