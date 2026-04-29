using CapShop.ReviewService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.ReviewService.Data;

public class ReviewDbContext : DbContext
{
    public ReviewDbContext(DbContextOptions<ReviewDbContext> options) : base(options)
    {
    }

    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewEligibility> ReviewEligibilities => Set<ReviewEligibility>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.UserName).HasMaxLength(160).IsRequired();
            entity.Property(r => r.Title).HasMaxLength(120).IsRequired();
            entity.Property(r => r.Comment).HasMaxLength(2000).IsRequired();
            entity.Property(r => r.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(r => new { r.ProductId, r.Status });
            entity.HasIndex(r => new { r.UserId, r.ProductId }).IsUnique();
        });

        modelBuilder.Entity<ReviewEligibility>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProductName).HasMaxLength(240).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.ProductId, e.OrderId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.ProductId, e.HasReviewed });
        });
    }
}
