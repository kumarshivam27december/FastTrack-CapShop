namespace CapShop.CatalogService.DTOs.Catalog
{
    public class UpdateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
