using CapShop.NotificationService.Data;
using CapShop.NotificationService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.NotificationService.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public NotificationRepository(NotificationDbContext db)
    {
        _db = db;
    }

    public async Task<NotificationRecord> CreateAsync(NotificationRecord notification)
    {
        notification.CreatedAtUtc = DateTime.UtcNow;
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        return notification;
    }

    public Task<NotificationRecord?> GetByIdAsync(int id)
    {
        return _db.Notifications.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<NotificationRecord>> GetByUserAsync(int userId)
    {
        return _db.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();
    }

    public Task<List<NotificationRecord>> GetAllAsync()
    {
        return _db.Notifications
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<NotificationRecord?> MarkAsReadAsync(int id, int userId)
    {
        var item = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (item is null)
        {
            return null;
        }

        item.IsRead = true;
        item.ReadAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return item;
    }
}
