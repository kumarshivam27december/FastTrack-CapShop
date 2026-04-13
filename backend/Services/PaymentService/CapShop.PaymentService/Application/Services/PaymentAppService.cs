using CapShop.PaymentService.Application.Interfaces;
using CapShop.PaymentService.DTOs;
using CapShop.PaymentService.Infrastructure.Repositories;
using CapShop.PaymentService.Models;
using CapShop.Shared.Exceptions;

namespace CapShop.PaymentService.Application.Services;

public class PaymentAppService : IPaymentAppService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentAppService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentResponseDto> ProcessPaymentAsync(int userId, ProcessPaymentRequestDto request)
    {
        if (request.OrderId <= 0)
        {
            throw new ValidationException("OrderId must be greater than zero.");
        }

        if (request.Amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        var isSuccess = request.SimulateSuccess;
        var payment = new PaymentRecord
        {
            OrderId = request.OrderId,
            UserId = userId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethod = request.PaymentMethod,
            Status = isSuccess ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            TransactionId = isSuccess ? $"TXN-{Guid.NewGuid():N}" : null,
            FailureReason = isSuccess ? null : "Simulated payment failure"
        };

        var created = await _paymentRepository.CreateAsync(payment);
        return ToDto(created);
    }

    public async Task<PaymentResponseDto?> GetByIdAsync(int id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<PaymentResponseDto?> GetLatestByOrderIdAsync(int orderId)
    {
        var payment = await _paymentRepository.GetLatestByOrderIdAsync(orderId);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<PaymentResponseDto?> UpdateStatusAsync(int id, UpdatePaymentStatusRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Status) || !PaymentStatus.IsValid(request.Status))
        {
            throw new ValidationException("Invalid payment status.");
        }

        var updated = await _paymentRepository.UpdateStatusAsync(id, request.Status, request.FailureReason);
        return updated is null ? null : ToDto(updated);
    }

    private static PaymentResponseDto ToDto(PaymentRecord payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            TransactionId = payment.TransactionId,
            FailureReason = payment.FailureReason,
            CreatedAtUtc = payment.CreatedAtUtc,
            UpdatedAtUtc = payment.UpdatedAtUtc
        };
    }
}
