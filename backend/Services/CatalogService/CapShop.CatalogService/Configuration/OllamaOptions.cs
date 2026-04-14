namespace CapShop.CatalogService.Configuration
{
    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "gemma2:2b";
        public int TimeoutSeconds { get; set; } = 60;
    }
}