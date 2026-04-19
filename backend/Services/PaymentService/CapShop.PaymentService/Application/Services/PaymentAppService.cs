using CapShop.PaymentService.Application.Interfaces;
using CapShop.PaymentService.DTOs;
using CapShop.PaymentService.Infrastructure.Gateways;
using CapShop.PaymentService.Infrastructure.Repositories;
using CapShop.PaymentService.Models;
using CapShop.Shared.Events;
using CapShop.Shared.Exceptions;
using MassTransit;
using Microsoft.Extensions.Configuration;

namespace CapShop.PaymentService.Application.Services;

public class PaymentAppService : IPaymentAppService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRazorpayGatewayService? _razorpayGateway;
    private readonly IPublishEndpoint? _publishEndpoint;
    private readonly IConfiguration? _configuration;

    public PaymentAppService(
        IPaymentRepository paymentRepository,
        IRazorpayGatewayService? razorpayGateway = null,
        IPublishEndpoint? publishEndpoint = null,
        IConfiguration? configuration = null)
    {
        _paymentRepository = paymentRepository;
        _razorpayGateway = razorpayGateway;
        _publishEndpoint = publishEndpoint;
        _configuration = configuration;
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

    public async Task<CreateRazorpayOrderResponseDto> CreateRazorpayOrderAsync(CreateRazorpayOrderRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureRazorpayDependencies();

        if (request.OrderId <= 0)
        {
            throw new ValidationException("OrderId must be greater than zero.");
        }

        if (request.UserId <= 0)
        {
            throw new ValidationException("UserId must be greater than zero.");
        }

        if (request.Amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant();
        var receipt = $"order_{request.OrderId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var razorpayOrder = await _razorpayGateway!.CreateOrderAsync(request.Amount, currency, receipt, cancellationToken);

        var payment = new PaymentRecord
        {
            OrderId = request.OrderId,
            UserId = request.UserId,
            Amount = request.Amount,
            Currency = razorpayOrder.Currency,
            PaymentMethod = request.PaymentMethod,
            Status = PaymentStatus.Pending,
            TransactionId = null,
            FailureReason = null
        };

        await _paymentRepository.CreateAsync(payment);

        return new CreateRazorpayOrderResponseDto
        {
            OrderId = request.OrderId,
            RazorpayOrderId = razorpayOrder.RazorpayOrderId,
            Amount = razorpayOrder.AmountInSubunits,
            Currency = razorpayOrder.Currency,
            KeyId = _configuration!["Razorpay:KeyId"] ?? string.Empty,
            Message = "Razorpay order created."
        };
    }

    public async Task<VerifyRazorpayPaymentResponseDto> VerifyRazorpayPaymentAsync(VerifyRazorpayPaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureRazorpayDependencies();

        if (request.OrderId <= 0)
        {
            throw new ValidationException("OrderId must be greater than zero.");
        }

        if (request.UserId <= 0)
        {
            throw new ValidationException("UserId must be greater than zero.");
        }

        var payment = await _paymentRepository.GetLatestByOrderIdAndUserIdAsync(request.OrderId, request.UserId);
        if (payment is null)
        {
            throw new NotFoundException("No pending payment record found for order.");
        }

        if (payment.Status == PaymentStatus.Succeeded)
        {
            return new VerifyRazorpayPaymentResponseDto
            {
                OrderId = request.OrderId,
                Verified = true,
                TransactionId = payment.TransactionId,
                Message = "Payment already verified."
            };
        }

        var verified = _razorpayGateway!.VerifySignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature);
        if (!verified)
        {
            await _paymentRepository.UpdateStatusAsync(payment.Id, PaymentStatus.Failed, "Invalid Razorpay signature.");

            await _publishEndpoint!.Publish<PaymentFailedEvent>(new
            {
                CorrelationId = Guid.NewGuid(),
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                UserEmail = request.UserEmail,
                PaymentId = payment.Id,
                FailureReason = "Invalid Razorpay signature.",
                OccurredAtUtc = DateTime.UtcNow
            }, cancellationToken);

            return new VerifyRazorpayPaymentResponseDto
            {
                OrderId = request.OrderId,
                Verified = false,
                Message = "Payment signature verification failed."
            };
        }

        await _paymentRepository.UpdateStatusAsync(payment.Id, PaymentStatus.Succeeded, null, request.RazorpayPaymentId);

        await _publishEndpoint!.Publish<PaymentSucceededEvent>(new
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = payment.OrderId,
            UserId = payment.UserId,
            UserEmail = request.UserEmail,
            PaymentId = payment.Id,
            TransactionId = request.RazorpayPaymentId,
            Amount = payment.Amount,
            OccurredAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return new VerifyRazorpayPaymentResponseDto
        {
            OrderId = request.OrderId,
            Verified = true,
            TransactionId = request.RazorpayPaymentId,
            Message = "Payment verified successfully."
        };
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

    private void EnsureRazorpayDependencies()
    {
        if (_razorpayGateway is null || _publishEndpoint is null || _configuration is null)
        {
            throw new InvalidOperationException("Razorpay services are not configured.");
        }
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
