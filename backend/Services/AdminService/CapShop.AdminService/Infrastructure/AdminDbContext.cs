using CapShop.AdminService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CapShop.AdminService.Infrastructure
{

    public class AdminDbContext : DbContext
    {
        public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

        public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AdminAuditLog>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
                entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
                entity.Property(x => x.PerformedBy).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Notes).HasMaxLength(1000);
            });
        }
    }
}
