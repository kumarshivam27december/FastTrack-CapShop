using CapShop.AssistantService.DTOs.Assistant;

namespace CapShop.AssistantService.Application.Interfaces
{
    public interface IInventoryAssistantService
    {
        Task<AssistantQueryResponseDto> QueryAsync(AssistantQueryRequestDto request, CancellationToken ct = default);
    }
}
