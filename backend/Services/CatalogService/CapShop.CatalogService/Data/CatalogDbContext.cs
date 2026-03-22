using Microsoft.EntityFrameworkCore;
using CapShop.CatalogService.Models;
namespace CapShop.CatalogService.Data
{
    public class CatalogDbContext : DbContext
    {
        public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<Product> Products => Set<Product>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.HasIndex(x => x.Name).IsUnique();
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(1000);
                entity.Property(x => x.Price).HasPrecision(10, 2);
                entity.Property(x => x.ImageUrl).HasMaxLength(500);
                entity.HasIndex(x => x.Name);
                entity.HasOne(x => x.Category)
                    .WithMany(x => x.Products)
                    .HasForeignKey(x => x.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
