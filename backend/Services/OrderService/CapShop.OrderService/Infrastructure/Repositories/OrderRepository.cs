using CapShop.OrderService.Data;
using CapShop.OrderService.DTOs.Cart;
using CapShop.OrderService.DTOs.Address;
using CapShop.OrderService.DTOs.Checkout;
using CapShop.OrderService.DTOs.Payment;
using CapShop.OrderService.DTOs.Order;
using CapShop.OrderService.Models;
using CapShop.OrderService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using CapShop.Shared.Events;
using MassTransit;

namespace CapShop.OrderService.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly string _catalogBaseUrl;
        private readonly string _paymentBaseUrl;

        public OrderRepository(OrderDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration, IPublishEndpoint publishEndpoint)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _publishEndpoint = publishEndpoint;
            _catalogBaseUrl = configuration["CatalogServiceUrl"] ?? "http://localhost:5014";
            _paymentBaseUrl = configuration["PaymentServiceUrl"] ?? "http://localhost:5017";
        }

        public async Task<CartResponseDto> GetOrCreateCartAsync(int userId)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart is null)
            {
                cart = new Cart { UserId = userId };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }

            return MapCartToDto(cart);
        }

        public async Task<CartResponseDto> AddToCartAsync(int userId, AddToCartRequestDto request)
        {
            var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart is null)
            {
                cart = new Cart { UserId = userId };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
                await _db.Entry(cart).Collection(c => c.Items).LoadAsync();
            }

            var product = await GetCatalogProductAsync(request.ProductId);
            if (product is null) throw new InvalidOperationException("Product not found.");
            if (!product.InStock || product.Stock <= 0) throw new InvalidOperationException("Product is out of stock.");
            if (request.Quantity > product.Stock) throw new InvalidOperationException("Requested quantity exceeds stock.");

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (existingItem != null)
            {
                var newQty = existingItem.Quantity + request.Quantity;
                if (newQty > product.Stock) throw new InvalidOperationException("Requested quantity exceeds stock.");
                existingItem.Quantity = newQty;
                existingItem.UnitPrice = product.Price;
            }
            else
            {
                var item = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    UnitPrice = product.Price
                };
                cart.Items.Add(item);
            }

            cart.UpdatedAtUtc = DateTime.UtcNow;
            _db.Carts.Update(cart);
            await _db.SaveChangesAsync();

            return MapCartToDto(cart);
        }

        public async Task<CartResponseDto> UpdateCartItemAsync(int userId, int cartItemId, UpdateCartItemRequestDto request)
        {
            var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart is null) throw new InvalidOperationException("Cart not found");

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (item is null) throw new InvalidOperationException("Item not in cart");

            if (request.Quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                var product = await GetCatalogProductAsync(item.ProductId);
                if (product is null) throw new InvalidOperationException("Product not found.");
                if (request.Quantity > product.Stock) throw new InvalidOperationException("Requested quantity exceeds stock.");
                item.Quantity = request.Quantity;
                item.UnitPrice = product.Price;
            }

            cart.UpdatedAtUtc = DateTime.UtcNow;
            _db.Carts.Update(cart);
            await _db.SaveChangesAsync();

            return MapCartToDto(cart);
        }

        public async Task<bool> RemoveFromCartAsync(int userId, int cartItemId)
        {
            var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart is null) return false;

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (item is null) return false;

            cart.Items.Remove(item);
            cart.UpdatedAtUtc = DateTime.UtcNow;
            _db.Carts.Update(cart);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart is null) return false;

            cart.Items.Clear();
            cart.UpdatedAtUtc = DateTime.UtcNow;
            _db.Carts.Update(cart);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<AddressResponseDto> SaveAddressAsync(int userId, AddressRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Pincode) || request.Pincode.Length != 6 || !request.Pincode.All(char.IsDigit))
                throw new InvalidOperationException("Invalid pincode format");

            var address = new Address
            {
                UserId = userId,
                FullName = request.FullName,
                Street = request.Street,
                City = request.City,
                State = request.State,
                Pincode = request.Pincode,
                Phone = request.Phone,
                IsDefault = true
            };

            _db.Addresses.Add(address);
            await _db.SaveChangesAsync();

            return MapAddressToDto(address);
        }

        public async Task<CheckoutResponseDto> StartCheckoutAsync(int userId, CheckoutStartRequestDto request)
        {
            var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart is null || !cart.Items.Any()) throw new InvalidOperationException("Cart is empty");

            if (string.IsNullOrWhiteSpace(request.Address.Pincode) ||
                request.Address.Pincode.Length != 6 ||
                !request.Address.Pincode.All(char.IsDigit))
                throw new InvalidOperationException("Invalid pincode format");

            var address = new Address
            {
                UserId = userId,
                FullName = request.Address.FullName,
                Street = request.Address.Street,
                City = request.Address.City,
                State = request.Address.State,
                Pincode = request.Address.Pincode,
                Phone = request.Address.Phone,
                IsDefault = true
            };
            _db.Addresses.Add(address);
            await _db.SaveChangesAsync();

            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                UserId = userId,
                AddressId = address.Id,
                Status = OrderStatus.CheckoutStarted,
                TotalAmount = cart.Items.Sum(i => i.Quantity * i.UnitPrice)
            };

            foreach (var cartItem in cart.Items)
            {
                var product = await GetCatalogProductAsync(cartItem.ProductId);
                var productName = product?.Name ?? $"Product-{cartItem.ProductId}";

                order.Items.Add(new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = productName,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice
                });
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = OrderStatus.Draft,
                ToStatus = OrderStatus.CheckoutStarted
            });
            await _db.SaveChangesAsync();

            return new CheckoutResponseDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                Message = "Checkout started. Ready for payment."
            };
        }

        public async Task<PaymentResponseDto> SimulatePaymentAsync(int userId, string? userEmail, PaymentSimulateRequestDto request)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);
            if (order is null) throw new InvalidOperationException("Order not found.");

            if (order.Status != OrderStatus.CheckoutStarted && order.Status != OrderStatus.PaymentPending)
                throw new InvalidOperationException("Payment can only be simulated for checkout-started orders.");

            var oldStatus = order.Status;
            order.Status = OrderStatus.PaymentPending;
            order.PaymentMethod = request.PaymentMethod;
            order.UpdatedAtUtc = DateTime.UtcNow;

            _db.Orders.Update(order);
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = OrderStatus.PaymentPending,
                Notes = $"Payment initiated via {request.PaymentMethod}"
            });
            await _db.SaveChangesAsync();

            await _publishEndpoint.Publish<OrderCreatedEvent>(new
            {
                CorrelationId = Guid.NewGuid(),
                OrderId = order.Id,
                UserId = order.UserId,
                UserEmail = userEmail ?? string.Empty,
                TotalAmount = order.TotalAmount,
                OrderNumber = order.OrderNumber,
                PaymentMethod = request.PaymentMethod,
                SimulateSuccess = request.SimulateSuccess,
                OccurredAtUtc = DateTime.UtcNow
            });

            return new PaymentResponseDto
            {
                OrderId = order.Id,
                TransactionId = string.Empty,
                Success = true,
                Message = "Payment request accepted and sent to PaymentService."
            };
        }

        public async Task<CheckoutResponseDto> PlaceOrderAsync(int userId, string? userEmail, int orderId)
        {
            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order is null) throw new InvalidOperationException("Order not found");

            if (order.Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("Order could not be completed.");

            if (order.Status == OrderStatus.PaymentPending || order.Status == OrderStatus.CheckoutStarted)
            {
                var paymentUpdated = await TryUpdateOrderStatusFromPaymentAsync(order);
                if (paymentUpdated)
                {
                    order = await _db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId && o.UserId == userId);
                }
            }

            if (order.Status == OrderStatus.Paid)
            {
                var finalizedSynchronously = await TryFinalizePaidOrderSynchronouslyAsync(order, userEmail);
                if (finalizedSynchronously)
                {
                    order = await _db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId && o.UserId == userId);
                }
            }

            if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Completed)
            {
                order = await WaitForOrderCompletionAsync(order.Id, userId, TimeSpan.FromSeconds(12));
            }

            if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Completed)
                throw new InvalidOperationException("Order is still being finalized.");

            return new CheckoutResponseDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                Message = "Order placed successfully"
            };
        }

        private async Task<Order> WaitForOrderCompletionAsync(int orderId, int userId, TimeSpan timeout)
        {
            var end = DateTime.UtcNow.Add(timeout);
            var current = await _db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId && o.UserId == userId);

            while (DateTime.UtcNow < end)
            {
                if (current.Status == OrderStatus.Paid || current.Status == OrderStatus.Completed || current.Status == OrderStatus.Cancelled)
                {
                    return current;
                }

                await Task.Delay(500);

                current = await _db.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .FirstAsync(o => o.Id == orderId && o.UserId == userId);
            }

            return current;
        }

        private async Task<bool> TryFinalizePaidOrderSynchronouslyAsync(Order order, string? userEmail)
        {
            if (order.Status != OrderStatus.Paid)
            {
                return false;
            }

            var sagaAlreadyStartedReservation = await _db.OrderStatusHistories.AnyAsync(h =>
                h.OrderId == order.Id &&
                h.FromStatus == OrderStatus.Paid &&
                h.ToStatus == OrderStatus.Paid &&
                h.Notes == "Reserve stock command published by saga");

            if (sagaAlreadyStartedReservation)
            {
                return false;
            }

            var fallbackAlreadyStartedReservation = await _db.OrderStatusHistories.AnyAsync(h =>
                h.OrderId == order.Id &&
                h.FromStatus == OrderStatus.Paid &&
                h.ToStatus == OrderStatus.Paid &&
                h.Notes == "Reserve stock started by place-order fallback");

            if (fallbackAlreadyStartedReservation)
            {
                return false;
            }

            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = OrderStatus.Paid,
                ToStatus = OrderStatus.Paid,
                Notes = "Reserve stock started by place-order fallback"
            });

            await _db.SaveChangesAsync();

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{_catalogBaseUrl}/catalog/internal/reserve-stock",
                order.Items.Select(i => new
                {
                    ProductId = i.ProductId,
                    Title = i.ProductName,
                    Description = $"Product ID: {i.ProductId}",
                    Price = i.UnitPrice,
                    Quantity = i.Quantity,
                    Amount = i.TotalPrice
                }).ToList());

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var reserveResult = await response.Content.ReadFromJsonAsync<ReserveStockResponse>();
            if (reserveResult is null)
            {
                return false;
            }

            if (!reserveResult.Success)
            {
                var oldStatus = order.Status;
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAtUtc = DateTime.UtcNow;

                _db.OrderStatusHistories.Add(new OrderStatusHistory
                {
                    OrderId = order.Id,
                    FromStatus = oldStatus,
                    ToStatus = OrderStatus.Cancelled,
                    Notes = "Synchronous reserve-stock failed during PlaceOrder"
                });

                await _db.SaveChangesAsync();
                return true;
            }

            var fromStatus = order.Status;
            order.Status = OrderStatus.Paid;
            order.UpdatedAtUtc = DateTime.UtcNow;

            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = fromStatus,
                ToStatus = OrderStatus.Paid,
                Notes = "Inventory reserved via synchronous fallback"
            });

            var cart = await _db.Carts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.UserId == order.UserId);

            if (cart is not null)
            {
                cart.Items.Clear();
                cart.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            await _publishEndpoint.Publish<OrderPlacedEvent>(new
            {
                CorrelationId = Guid.NewGuid(),
                OrderId = order.Id,
                UserId = order.UserId,
                UserEmail = userEmail ?? string.Empty,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Items = order.Items.Select(i => new
                {
                    ProductId = i.ProductId,
                    Title = i.ProductName,
                    Description = $"Product ID: {i.ProductId}",
                    Price = i.UnitPrice,
                    Quantity = i.Quantity,
                    Amount = i.TotalPrice
                }).ToList(),
                OccurredAtUtc = DateTime.UtcNow
            });

            return true;
        }

        private sealed class ReserveStockResponse
        {
            public bool Success { get; set; }
        }

        private sealed class PaymentStatusResponse
        {
            public int OrderId { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? TransactionId { get; set; }
            public string? FailureReason { get; set; }
        }

        private async Task<bool> TryUpdateOrderStatusFromPaymentAsync(Order order)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_paymentBaseUrl}/payment/internal/order/{order.Id}/latest");
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var payment = await response.Content.ReadFromJsonAsync<PaymentStatusResponse>();
            if (payment is null || payment.OrderId != order.Id)
            {
                return false;
            }

            if (string.Equals(payment.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
            {
                if (order.Status != OrderStatus.Paid)
                {
                    var oldStatus = order.Status;
                    order.Status = OrderStatus.Paid;
                    order.PaymentTransactionId = payment.TransactionId;
                    order.UpdatedAtUtc = DateTime.UtcNow;

                    _db.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        FromStatus = oldStatus,
                        ToStatus = OrderStatus.Paid,
                        Notes = "Recovered from PaymentService status check"
                    });

                    await _db.SaveChangesAsync();
                }

                return true;
            }

            if (string.Equals(payment.Status, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                if (order.Status != OrderStatus.Cancelled)
                {
                    var oldStatus = order.Status;
                    order.Status = OrderStatus.Cancelled;
                    order.UpdatedAtUtc = DateTime.UtcNow;

                    _db.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        FromStatus = oldStatus,
                        ToStatus = OrderStatus.Cancelled,
                        Notes = string.IsNullOrWhiteSpace(payment.FailureReason)
                            ? "Recovered from PaymentService status check: payment failed"
                            : $"Recovered from PaymentService status check: {payment.FailureReason}"
                    });

                    await _db.SaveChangesAsync();
                }

                return true;
            }

            return false;
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            return order is null ? null : MapOrderToDto(order);
        }

        public async Task<List<OrderResponseDto>> GetCustomerOrdersAsync(int userId)
        {
            var orders = await _db.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAtUtc)
                .ToListAsync();

            return orders.Select(MapOrderToDto).ToList();
        }

        public async Task<List<OrderResponseDto>> GetAllOrdersAsync()
        {
            var orders = await _db.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAtUtc)
                .ToListAsync();

            return orders.Select(MapOrderToDto).ToList();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, string? notes = null, int? adminUserId = null)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order is null) return false;

            if (!Enum.TryParse<OrderStatus>(newStatus, true, out var newStatusEnum))
                return false;

            if (!IsValidTransition(order.Status, newStatusEnum))
                return false;

            var oldStatus = order.Status;
            order.Status = newStatusEnum;
            order.UpdatedAtUtc = DateTime.UtcNow;

            _db.Orders.Update(order);
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = newStatusEnum,
                Notes = notes,
                ChangedByUserId = adminUserId
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order is null || order.UserId != userId) return false;

            if (order.Status == OrderStatus.Packed || order.Status == OrderStatus.Shipped ||
                order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled ||
                order.Status == OrderStatus.Completed)
                throw new InvalidOperationException("Cannot cancel orders already packed or shipped");

            var oldStatus = order.Status;
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAtUtc = DateTime.UtcNow;

            _db.Orders.Update(order);
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = OrderStatus.Cancelled,
                Notes = "Cancelled by customer"
            });

            await _db.SaveChangesAsync();
            return true;
        }

        private bool IsValidTransition(OrderStatus from, OrderStatus to)
        {
            if (from == to) return true;

            if (to == OrderStatus.Cancelled)
            {
                return from != OrderStatus.Delivered && from != OrderStatus.Cancelled;
            }

            return (from, to) switch
            {
                (OrderStatus.Paid, OrderStatus.Completed) => true,
                (OrderStatus.Paid, OrderStatus.Packed) => true,
                (OrderStatus.Completed, OrderStatus.Packed) => true,
                (OrderStatus.Packed, OrderStatus.Shipped) => true,
                (OrderStatus.Shipped, OrderStatus.Delivered) => true,
                _ => false
            };
        }

        private async Task<CatalogProductDto?> GetCatalogProductAsync(int productId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_catalogBaseUrl}/catalog/products/{productId}");
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<CatalogProductDto>();
        }

        private CartResponseDto MapCartToDto(Cart cart)
        {
            return new CartResponseDto
            {
                CartId = cart.Id,
                Items = cart.Items.Select(i => new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = $"Product-{i.ProductId}",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
        }

        private AddressResponseDto MapAddressToDto(Address address)
        {
            return new AddressResponseDto
            {
                Id = address.Id,
                FullName = address.FullName,
                Street = address.Street,
                City = address.City,
                State = address.State,
                Pincode = address.Pincode,
                Phone = address.Phone
            };
        }

        private OrderResponseDto MapOrderToDto(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                CreatedAtUtc = order.CreatedAtUtc,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        }

        private sealed class CatalogProductDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public bool InStock { get; set; }
        }
    }
}