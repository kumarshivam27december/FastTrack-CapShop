using CapShop.NotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationRecord>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Channel).HasMaxLength(30);
            entity.Property(x => x.Recipient).HasMaxLength(200);
            entity.Property(x => x.Subject).HasMaxLength(250);
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.ProviderMessageId).HasMaxLength(100);
            entity.Property(x => x.ErrorMessage).HasMaxLength(500);
            entity.HasIndex(x => x.UserId);
        });
    }
}
