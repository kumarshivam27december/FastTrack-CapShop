namespace CapShop.CatalogService.DTOs.Assistant
{
    public class AssistantQueryRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public int PageSize { get; set; } = 5;
        public bool ReturnAllMatches { get; set; }
    }
}