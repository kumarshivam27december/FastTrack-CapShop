using CapShop.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentRecord>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Currency).HasMaxLength(10);
            entity.Property(x => x.PaymentMethod).HasMaxLength(50);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.Property(x => x.TransactionId).HasMaxLength(100);
            entity.Property(x => x.FailureReason).HasMaxLength(500);
            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.UserId);
        });
    }
}
