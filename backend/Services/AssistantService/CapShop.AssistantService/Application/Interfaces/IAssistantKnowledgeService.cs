using System.IO;
using System.Threading.Tasks;
using CapShop.AssistantService.DTOs.Catalog;

namespace CapShop.AssistantService.Application.Interfaces
{
    public interface IAssistantKnowledgeService
    {
        Task<int> LoadFromStreamAsync(Stream stream, string fileName);

        Task<CatalogSearchResponseDto> SearchAsync(string query, decimal? minPrice, decimal? maxPrice, bool stockOnly, int page = 1, int pageSize = 20);

        Task PersistAsync();
    }
}
