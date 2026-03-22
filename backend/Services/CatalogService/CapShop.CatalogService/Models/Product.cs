namespace CapShop.CatalogService.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;
    }
}
