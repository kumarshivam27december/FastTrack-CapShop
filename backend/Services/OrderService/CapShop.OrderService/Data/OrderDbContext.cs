using CapShop.OrderService.Models;
using CapShop.OrderService.Sagas;
using Microsoft.EntityFrameworkCore;


namespace CapShop.OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Address> Addresses => Set<Address>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
        public DbSet<OrderSagaState> OrderSagaStates => Set<OrderSagaState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.UserId).IsRequired();
                entity.HasIndex(x => x.UserId).IsUnique();
                entity.HasMany(x => x.Items).WithOne(x => x.Cart).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.ProductId).IsRequired();
                entity.Property(x => x.Quantity).IsRequired();
                entity.Property(x => x.UnitPrice).HasPrecision(10, 2);
            });

            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.FullName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Street).HasMaxLength(200).IsRequired();
                entity.Property(x => x.City).HasMaxLength(100).IsRequired();
                entity.Property(x => x.State).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Pincode).HasMaxLength(10).IsRequired();
                entity.Property(x => x.Phone).HasMaxLength(15).IsRequired();
                entity.HasIndex(x => new { x.UserId, x.IsDefault });
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
                entity.Property(x => x.UserId).IsRequired();
                entity.Property(x => x.TotalAmount).HasPrecision(12, 2);
                entity.Property(x => x.Status).HasConversion<int>();
                entity.HasIndex(x => x.OrderNumber).IsUnique();
                entity.HasIndex(x => x.UserId);
                entity.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(x => x.StatusHistory).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.ProductId).IsRequired();
                entity.Property(x => x.ProductName).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Quantity).IsRequired();
                entity.Property(x => x.UnitPrice).HasPrecision(10, 2);
            });

            modelBuilder.Entity<OrderStatusHistory>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.FromStatus).HasConversion<int>();
                entity.Property(x => x.ToStatus).HasConversion<int>();
                entity.Property(x => x.Notes).HasMaxLength(500);
                entity.HasIndex(x => x.OrderId);
            });

            modelBuilder.Entity<OrderSagaState>(entity =>
            {
                entity.HasKey(x => x.CorrelationId);
                entity.Property(x => x.CurrentState).HasMaxLength(64).IsRequired();
                entity.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
                entity.Property(x => x.UserEmail).HasMaxLength(256).IsRequired();
                entity.Property(x => x.TotalAmount).HasPrecision(12, 2);
                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasIndex(x => x.CurrentState);
                entity.HasIndex(x => x.OrderId);
                entity.ToTable("OrderSagaStates");
            });
        }
    }
}
