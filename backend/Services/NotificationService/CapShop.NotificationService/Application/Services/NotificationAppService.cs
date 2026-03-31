using CapShop.NotificationService.Application.Interfaces;
using CapShop.NotificationService.DTOs;
using CapShop.NotificationService.Infrastructure.Repositories;
using CapShop.NotificationService.Models;

namespace CapShop.NotificationService.Application.Services;

public class NotificationAppService : INotificationAppService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationAppService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<NotificationResponseDto> SendAsync(int? userIdFromToken, SendNotificationRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Recipient))
        {
            throw new InvalidOperationException("Recipient is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new InvalidOperationException("Subject is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Message is required.");
        }

        var success = request.SimulateSuccess;

        var record = new NotificationRecord
        {
            UserId = request.UserId ?? userIdFromToken,
            OrderId = request.OrderId,
            PaymentId = request.PaymentId,
            Channel = request.Channel,
            Recipient = request.Recipient,
            Subject = request.Subject,
            Message = request.Message,
            Status = success ? NotificationStatus.Sent : NotificationStatus.Failed,
            ProviderMessageId = success ? $"MSG-{Guid.NewGuid():N}" : null,
            ErrorMessage = success ? null : "Simulated send failure",
            SentAtUtc = success ? DateTime.UtcNow : null
        };

        var created = await _notificationRepository.CreateAsync(record);
        return ToDto(created);
    }

    public async Task<NotificationResponseDto?> GetByIdAsync(int id)
    {
        var item = await _notificationRepository.GetByIdAsync(id);
        return item is null ? null : ToDto(item);
    }

    public async Task<IReadOnlyList<NotificationResponseDto>> GetByUserAsync(int userId)
    {
        var items = await _notificationRepository.GetByUserAsync(userId);
        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<NotificationResponseDto>> GetAllAsync()
    {
        var items = await _notificationRepository.GetAllAsync();
        return items.Select(ToDto).ToList();
    }

    public async Task<NotificationResponseDto?> MarkAsReadAsync(int id, int userId)
    {
        var item = await _notificationRepository.MarkAsReadAsync(id, userId);
        return item is null ? null : ToDto(item);
    }

    private static NotificationResponseDto ToDto(NotificationRecord x)
    {
        return new NotificationResponseDto
        {
            Id = x.Id,
            UserId = x.UserId,
            OrderId = x.OrderId,
            PaymentId = x.PaymentId,
            Channel = x.Channel,
            Recipient = x.Recipient,
            Subject = x.Subject,
            Message = x.Message,
            Status = x.Status,
            ProviderMessageId = x.ProviderMessageId,
            ErrorMessage = x.ErrorMessage,
            IsRead = x.IsRead,
            CreatedAtUtc = x.CreatedAtUtc,
            SentAtUtc = x.SentAtUtc,
            ReadAtUtc = x.ReadAtUtc
        };
    }
}
