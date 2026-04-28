namespace CapShop.AssistantService.DTOs.Assistant
{
    public class AssistantQueryResponseDto
    {
        public string Reply { get; set; } = string.Empty;
        public AssistantAppliedFiltersDto AppliedFilters { get; set; } = new();
        public List<AssistantProductMatchDto> Products { get; set; } = new();
        public List<AssistantOrderMatchDto> Orders { get; set; } = new();
        public int TotalMatches { get; set; }
        public bool UsedFallbackResponse { get; set; }
        public bool UsedRelaxedSearch { get; set; }
        public string? RelaxationNote { get; set; }
    }

    public class AssistantAppliedFiltersDto
    {
        public string Query { get; set; } = string.Empty;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool StockOnly { get; set; }
        public string? SortBy { get; set; }
    }

    public class AssistantProductMatchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class AssistantOrderMatchDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
