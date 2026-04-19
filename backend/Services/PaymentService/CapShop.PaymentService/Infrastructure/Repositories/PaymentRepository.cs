using CapShop.PaymentService.Data;
using CapShop.PaymentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.PaymentService.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _db;

    public PaymentRepository(PaymentDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentRecord> CreateAsync(PaymentRecord payment)
    {
        payment.CreatedAtUtc = DateTime.UtcNow;
        payment.UpdatedAtUtc = payment.CreatedAtUtc;

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    public Task<PaymentRecord?> GetByIdAsync(int id)
    {
        return _db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<PaymentRecord?> GetLatestByOrderIdAsync(int orderId)
    {
        return _db.Payments
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();
    }

    public Task<PaymentRecord?> GetLatestByOrderIdAndUserIdAsync(int orderId, int userId)
    {
        return _db.Payments
            .AsNoTracking()
            .Where(x => x.OrderId == orderId && x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();
    }

    public async Task<PaymentRecord?> UpdateStatusAsync(int id, string status, string? failureReason, string? transactionId = null)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(x => x.Id == id);
        if (payment is null)
        {
            return null;
        }

        payment.Status = status;
        payment.FailureReason = status == PaymentStatus.Failed ? failureReason : null;

        if (status == PaymentStatus.Succeeded && !string.IsNullOrWhiteSpace(transactionId))
        {
            payment.TransactionId = transactionId;
        }
        else if (status == PaymentStatus.Succeeded && string.IsNullOrWhiteSpace(payment.TransactionId))
        {
            payment.TransactionId = $"TXN-{Guid.NewGuid():N}";
        }

        payment.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return payment;
    }
}
