using CapShop.OrderService.Application.Interfaces;
using CapShop.OrderService.DTOs.Address;
using CapShop.OrderService.DTOs.Cart;
using CapShop.OrderService.DTOs.Checkout;
using CapShop.OrderService.DTOs.Order;
using CapShop.OrderService.DTOs.Payment;
using CapShop.OrderService.Infrastructure.Repositories;

namespace CapShop.OrderService.Application.Services
{
    public class OrderAppService : IOrderAppService
    {
        private readonly IOrderRepository _repo;

        public OrderAppService(IOrderRepository repo)
        {
            _repo = repo;
        }

        public Task<CartResponseDto> GetOrCreateCartAsync(int userId) => _repo.GetOrCreateCartAsync(userId);
        public Task<CartResponseDto> AddToCartAsync(int userId, AddToCartRequestDto request) => _repo.AddToCartAsync(userId, request);
        public Task<CartResponseDto> UpdateCartItemAsync(int userId, int cartItemId, UpdateCartItemRequestDto request) => _repo.UpdateCartItemAsync(userId, cartItemId, request);
        public Task<bool> RemoveFromCartAsync(int userId, int cartItemId) => _repo.RemoveFromCartAsync(userId, cartItemId);
        public Task<bool> ClearCartAsync(int userId) => _repo.ClearCartAsync(userId);
        public Task<AddressResponseDto> SaveAddressAsync(int userId, AddressRequestDto request) => _repo.SaveAddressAsync(userId, request);
        public Task<CheckoutResponseDto> StartCheckoutAsync(int userId, CheckoutStartRequestDto request) => _repo.StartCheckoutAsync(userId, request);
        public Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(int userId, string? userEmail, CreatePaymentIntentRequestDto request) => _repo.CreatePaymentIntentAsync(userId, userEmail, request);
        public Task<VerifyPaymentResponseDto> VerifyPaymentAsync(int userId, string? userEmail, VerifyPaymentRequestDto request) => _repo.VerifyPaymentAsync(userId, userEmail, request);
        public Task<PaymentResponseDto> SimulatePaymentAsync(int userId, string? userEmail, PaymentSimulateRequestDto request) => _repo.SimulatePaymentAsync(userId, userEmail, request);
        public Task<CheckoutResponseDto> PlaceOrderAsync(int userId, string? userEmail, int orderId) => _repo.PlaceOrderAsync(userId, userEmail, orderId);
        public Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId) => _repo.GetOrderByIdAsync(orderId, userId);
        public Task<List<OrderResponseDto>> GetCustomerOrdersAsync(int userId) => _repo.GetCustomerOrdersAsync(userId);
        public Task<List<OrderResponseDto>> GetAllOrdersAsync() => _repo.GetAllOrdersAsync();
        public Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, string? notes = null, int? adminUserId = null)
        => _repo.UpdateOrderStatusAsync(orderId, newStatus, notes, adminUserId);
        public Task<bool> CancelOrderAsync(int orderId, int userId) => _repo.CancelOrderAsync(orderId, userId);
    }
}