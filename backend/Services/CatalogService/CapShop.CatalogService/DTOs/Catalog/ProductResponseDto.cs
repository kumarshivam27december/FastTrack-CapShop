namespace CapShop.CatalogService.DTOs.Catalog
{
    public class ProductResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; }  = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public bool InStock => Stock > 0;

        public string ImageUrl { get; set; } = string.Empty;

        public CategoryResponseDto Category { get; set; }
    }
}
