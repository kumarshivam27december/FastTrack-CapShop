using CapShop.CatalogService.DTOs.Assistant;

namespace CapShop.CatalogService.Application.Interfaces
{
    public interface IInventoryAssistantService
    {
        Task<AssistantQueryResponseDto> QueryAsync(AssistantQueryRequestDto request, CancellationToken ct = default);
    }
}