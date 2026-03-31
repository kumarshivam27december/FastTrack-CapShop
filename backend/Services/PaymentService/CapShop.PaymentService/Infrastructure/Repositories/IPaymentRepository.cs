using CapShop.PaymentService.Models;

namespace CapShop.PaymentService.Infrastructure.Repositories;

public interface IPaymentRepository
{
    Task<PaymentRecord> CreateAsync(PaymentRecord payment);
    Task<PaymentRecord?> GetByIdAsync(int id);
    Task<PaymentRecord?> GetLatestByOrderIdAsync(int orderId);
    Task<PaymentRecord?> UpdateStatusAsync(int id, string status, string? failureReason);
}
