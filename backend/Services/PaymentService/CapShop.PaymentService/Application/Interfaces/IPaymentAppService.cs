using CapShop.PaymentService.DTOs;

namespace CapShop.PaymentService.Application.Interfaces;

public interface IPaymentAppService
{
    Task<PaymentResponseDto> ProcessPaymentAsync(int userId, ProcessPaymentRequestDto request);
    Task<PaymentResponseDto?> GetByIdAsync(int id);
    Task<PaymentResponseDto?> GetLatestByOrderIdAsync(int orderId);
    Task<PaymentResponseDto?> UpdateStatusAsync(int id, UpdatePaymentStatusRequestDto request);
}
