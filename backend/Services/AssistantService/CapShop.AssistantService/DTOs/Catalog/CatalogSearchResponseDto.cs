namespace CapShop.AssistantService.DTOs.Catalog
{
    public class CatalogSearchResponseDto
    {
        public List<ProductResponseDto> Products { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ProductResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool InStock => Stock > 0;
        public string ImageUrl { get; set; } = string.Empty;
        public CategoryResponseDto? Category { get; set; }
    }

    public class CategoryResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
