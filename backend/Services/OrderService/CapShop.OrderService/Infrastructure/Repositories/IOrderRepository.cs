using CapShop.OrderService.DTOs.Address;
using CapShop.OrderService.DTOs.Cart;
using CapShop.OrderService.DTOs.Checkout;
using CapShop.OrderService.DTOs.Order;
using CapShop.OrderService.DTOs.Payment;

namespace CapShop.OrderService.Infrastructure.Repositories
{
    public interface IOrderRepository
    {
        Task<CartResponseDto> GetOrCreateCartAsync(int userId);
        Task<CartResponseDto> AddToCartAsync(int userId, AddToCartRequestDto request);
        Task<CartResponseDto> UpdateCartItemAsync(int userId, int cartItemId, UpdateCartItemRequestDto request);
        Task<bool> RemoveFromCartAsync(int userId, int cartItemId);
        Task<bool> ClearCartAsync(int userId);

        Task<AddressResponseDto> SaveAddressAsync(int userId, AddressRequestDto request);

        Task<CheckoutResponseDto> StartCheckoutAsync(int userId, CheckoutStartRequestDto request);
        Task<PaymentResponseDto> SimulatePaymentAsync(int userId, string? userEmail, PaymentSimulateRequestDto request);
        Task<CheckoutResponseDto> PlaceOrderAsync(int userId, string? userEmail, int orderId);

        Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId);
        Task<List<OrderResponseDto>> GetCustomerOrdersAsync(int userId);
        Task<List<OrderResponseDto>> GetAllOrdersAsync();
        Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, string? notes = null, int? adminUserId = null);
        Task<bool> CancelOrderAsync(int orderId, int userId);
    }
}