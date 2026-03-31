using CapShop.NotificationService.DTOs;

namespace CapShop.NotificationService.Application.Interfaces;

public interface INotificationAppService
{
    Task<NotificationResponseDto> SendAsync(int? userIdFromToken, SendNotificationRequestDto request);
    Task<NotificationResponseDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<NotificationResponseDto>> GetByUserAsync(int userId);
    Task<IReadOnlyList<NotificationResponseDto>> GetAllAsync();
    Task<NotificationResponseDto?> MarkAsReadAsync(int id, int userId);
}
