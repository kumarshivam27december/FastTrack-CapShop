using CapShop.NotificationService.Models;

namespace CapShop.NotificationService.Infrastructure.Repositories;

public interface INotificationRepository
{
    Task<NotificationRecord> CreateAsync(NotificationRecord notification);
    Task<NotificationRecord?> GetByIdAsync(int id);
    Task<List<NotificationRecord>> GetByUserAsync(int userId);
    Task<List<NotificationRecord>> GetAllAsync();
    Task<NotificationRecord?> MarkAsReadAsync(int id, int userId);
}
