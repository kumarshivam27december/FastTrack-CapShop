using CapShop.OrderService.Data;
using CapShop.OrderService.DTOs.Cart;
using CapShop.OrderService.DTOs.Address;
using CapShop.OrderService.DTOs.Checkout;
using CapShop.OrderService.DTOs.Payment;
using CapShop.OrderService.DTOs.Order;
using CapShop.OrderService.Models;
using CapShop.OrderService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using CapShop.Shared.Exceptions;
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
            if (product is null) throw new NotFoundException("Product not found.");
            if (!product.InStock || product.Stock <= 0) throw new ConflictException("Product is out of stock.");
            if (request.Quantity > product.Stock) throw new ConflictException("Requested quantity exceeds stock.");

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (existingItem != null)
            {
                var newQty = existingItem.Quantity + request.Quantity;
                if (newQty > product.Stock) throw new ConflictException("Requested quantity exceeds stock.");
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
            if (cart is null) throw new NotFoundException("Cart not found");

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (item is null) throw new NotFoundException("Item not in cart");

            if (request.Quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                var product = await GetCatalogProductAsync(item.ProductId);
                if (product is null) throw new NotFoundException("Product not found.");
                if (request.Quantity > product.Stock) throw new ConflictException("Requested quantity exceeds stock.");
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
                throw new ValidationException("Invalid pincode format");

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
            if (cart is null || !cart.Items.Any()) throw new ValidationException("Cart is empty");

            if (string.IsNullOrWhiteSpace(request.Address.Pincode) ||
                request.Address.Pincode.Length != 6 ||
                !request.Address.Pincode.All(char.IsDigit))
                throw new ValidationException("Invalid pincode format");

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

        public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(int userId, string? userEmail, CreatePaymentIntentRequestDto request)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);
            if (order is null)
            {
                throw new NotFoundException("Order not found.");
            }

            if (order.Status != OrderStatus.CheckoutStarted && order.Status != OrderStatus.PaymentPending)
            {
                throw new ConflictException("Payment can only be initiated for checkout-started orders.");
            }

            if (order.Status != OrderStatus.PaymentPending)
            {
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
                    Notes = $"Payment intent created via {request.PaymentMethod}"
                });
                await _db.SaveChangesAsync();
            }

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{_paymentBaseUrl}/payment/internal/razorpay/create-order",
                new
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    UserEmail = userEmail ?? string.Empty,
                    Amount = order.TotalAmount,
                    Currency = request.Currency,
                    PaymentMethod = request.PaymentMethod
                });

            if (!response.IsSuccessStatusCode)
            {
                var reason = await response.Content.ReadAsStringAsync();
                throw new ConflictException($"Unable to create Razorpay payment intent: {reason}");
            }

            var payload = await response.Content.ReadFromJsonAsync<PaymentIntentServiceResponse>();
            if (payload is null || string.IsNullOrWhiteSpace(payload.RazorpayOrderId))
            {
                throw new ConflictException("PaymentService returned an invalid payment intent response.");
            }

            return new PaymentIntentResponseDto
            {
                OrderId = payload.OrderId,
                RazorpayOrderId = payload.RazorpayOrderId,
                Amount = payload.Amount,
                Currency = payload.Currency,
                KeyId = payload.KeyId,
                Message = payload.Message
            };
        }

        public async Task<VerifyPaymentResponseDto> VerifyPaymentAsync(int userId, string? userEmail, VerifyPaymentRequestDto request)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);
            if (order is null)
            {
                throw new NotFoundException("Order not found.");
            }

            if (order.Status != OrderStatus.PaymentPending && order.Status != OrderStatus.Paid)
            {
                throw new ConflictException("Payment verification is only available for payment-pending orders.");
            }

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{_paymentBaseUrl}/payment/internal/razorpay/verify",
                new
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    UserEmail = userEmail ?? string.Empty,
                    RazorpayOrderId = request.RazorpayOrderId,
                    RazorpayPaymentId = request.RazorpayPaymentId,
                    RazorpaySignature = request.RazorpaySignature
                });

            if (!response.IsSuccessStatusCode)
            {
                var reason = await response.Content.ReadAsStringAsync();
                throw new ConflictException($"Unable to verify payment: {reason}");
            }

            var payload = await response.Content.ReadFromJsonAsync<VerifyPaymentServiceResponse>();
            if (payload is null)
            {
                throw new ConflictException("PaymentService returned an invalid verification response.");
            }

            if (payload.Verified)
            {
                await TryUpdateOrderStatusFromPaymentAsync(order);
            }

            return new VerifyPaymentResponseDto
            {
                OrderId = payload.OrderId,
                Verified = payload.Verified,
                TransactionId = payload.TransactionId,
                Message = payload.Message
            };
        }

        public async Task<PaymentResponseDto> SimulatePaymentAsync(int userId, string? userEmail, PaymentSimulateRequestDto request)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == userId);
            if (order is null) throw new NotFoundException("Order not found.");

            if (order.Status != OrderStatus.CheckoutStarted && order.Status != OrderStatus.PaymentPending)
                throw new ConflictException("Payment can only be simulated for checkout-started orders.");

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
            if (order is null) throw new NotFoundException("Order not found");

            if (order.Status == OrderStatus.Cancelled)
                throw new ConflictException("Order could not be completed.");

            if (order.Status == OrderStatus.CheckoutStarted)
                throw new ConflictException("Payment has not been initiated for this order.");

            if (order.Status == OrderStatus.PaymentPending)
            {
                var paymentUpdated = await TryUpdateOrderStatusFromPaymentAsync(order);
                if (paymentUpdated)
                {
                    order = await _db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId && o.UserId == userId);
                }
            }

            if (order.Status == OrderStatus.Paid)
            {
                await EnsureReserveStockCommandPublishedAsync(order, userEmail);
            }

            order = await WaitForOrderPlacementResultAsync(order.Id, userId, TimeSpan.FromSeconds(25));

            if (order.Status == OrderStatus.Cancelled)
                throw new ConflictException("Order could not be completed.");

            var placementConfirmed = await IsInventoryReservedAsync(order.Id);
            if (!placementConfirmed)
                throw new ConflictException("Order is still being finalized.");

            return new CheckoutResponseDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                Message = "Order placed successfully"
            };
        }

        private async Task<Order> WaitForOrderPlacementResultAsync(int orderId, int userId, TimeSpan timeout)
        {
            var end = DateTime.UtcNow.Add(timeout);
            var current = await _db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId && o.UserId == userId);

            while (DateTime.UtcNow < end)
            {
                var placementConfirmed = await IsInventoryReservedAsync(orderId);
                if (placementConfirmed || current.Status == OrderStatus.Cancelled)
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

        private async Task<bool> IsInventoryReservedAsync(int orderId)
        {
            return await _db.OrderStatusHistories.AsNoTracking().AnyAsync(h =>
                h.OrderId == orderId &&
                h.ToStatus == OrderStatus.Paid &&
                (h.Notes == "Inventory reserved. Order remains paid." ||
                 h.Notes == "Inventory reserved via recovery reserve command."));
        }

        private async Task EnsureReserveStockCommandPublishedAsync(Order order, string? userEmail)
        {
            var alreadyReserved = await IsInventoryReservedAsync(order.Id);
            if (alreadyReserved)
            {
                return;
            }

            var lastRecoveryPublish = await _db.OrderStatusHistories
                .Where(h =>
                    h.OrderId == order.Id &&
                    h.FromStatus == OrderStatus.Paid &&
                    h.ToStatus == OrderStatus.Paid &&
                    h.Notes == "Reserve stock command republished by place-order recovery")
                .OrderByDescending(h => h.ChangedAtUtc)
                .FirstOrDefaultAsync();

            if (lastRecoveryPublish is not null &&
                DateTime.UtcNow - lastRecoveryPublish.ChangedAtUtc < TimeSpan.FromSeconds(5))
            {
                return;
            }

            await _publishEndpoint.Publish<ReserveStockCommand>(new
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

            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = OrderStatus.Paid,
                ToStatus = OrderStatus.Paid,
                Notes = "Reserve stock command republished by place-order recovery"
            });

            await _db.SaveChangesAsync();
        }

        private sealed class PaymentStatusResponse
        {
            public int OrderId { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? TransactionId { get; set; }
            public string? FailureReason { get; set; }
        }

        private sealed class PaymentIntentServiceResponse
        {
            public int OrderId { get; set; }
            public string RazorpayOrderId { get; set; } = string.Empty;
            public int Amount { get; set; }
            public string Currency { get; set; } = "INR";
            public string KeyId { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        private sealed class VerifyPaymentServiceResponse
        {
            public int OrderId { get; set; }
            public bool Verified { get; set; }
            public string? TransactionId { get; set; }
            public string Message { get; set; } = string.Empty;
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

        public async Task<OrderTrackingDto?> GetOrderTrackingAsync(int orderId, int userId)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order is null)
            {
                return null;
            }

            var address = order.AddressId.HasValue
                ? await _db.Addresses.AsNoTracking().FirstOrDefaultAsync(a => a.Id == order.AddressId.Value)
                : null;

            var destination = ResolveDestination(address);
            var trackingSelection = await GetLatestTrackingSelectionAsync(order.Id);
            var originWarehouse = trackingSelection.Warehouse
                ?? DeliveryHubs
                    .Where(h => h.Kind == "warehouse")
                    .OrderBy(h => DistanceKm(h.Latitude, h.Longitude, destination.Latitude, destination.Longitude))
                    .First();
            var nearestHub = DeliveryHubs
                .Where(h => h.Kind == "regional-hub")
                .OrderBy(h => DistanceKm(h.Latitude, h.Longitude, destination.Latitude, destination.Longitude))
                .First();

            var route = BuildRoute(originWarehouse, nearestHub, destination);
            var adminCheckpoint = trackingSelection.Checkpoint;
            if (adminCheckpoint is not null && adminCheckpoint.Kind != "warehouse" && route.All(h => h.Code != adminCheckpoint.Code))
            {
                route.Insert(1, adminCheckpoint);
            }

            var currentIndex = order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled
                ? GetCurrentRouteIndex(order.Status, route.Count)
                : adminCheckpoint is null
                    ? GetCurrentRouteIndex(order.Status, route.Count)
                    : Math.Max(0, route.FindIndex(h => h.Code == adminCheckpoint.Code));
            var currentPoint = route[currentIndex];
            var now = DateTime.UtcNow;
            var estimatedDeliveryUtc = EstimateDeliveryUtc(order, currentIndex, route.Count, now);

            var routeDtos = route.Select((point, index) => new TrackingPointDto
            {
                Name = point.Name,
                Kind = point.Kind,
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                IsCompleted = index < currentIndex || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed,
                IsCurrent = index == currentIndex && order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Completed
            }).ToList();

            return new OrderTrackingDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status.ToString(),
                TrackingStatus = adminCheckpoint is null || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled
                    ? GetTrackingStatus(order.Status)
                    : $"Package reached {currentPoint.Name}",
                CurrentLocationName = currentPoint.Name,
                NearestHubName = nearestHub.Name,
                EstimatedDeliveryUtc = estimatedDeliveryUtc,
                EstimatedMinutesRemaining = Math.Max(0, (int)Math.Ceiling((estimatedDeliveryUtc - now).TotalMinutes)),
                CurrentLocation = routeDtos[currentIndex],
                Destination = routeDtos[^1],
                Route = routeDtos,
                Events = BuildTrackingEvents(order, route, currentIndex, estimatedDeliveryUtc)
            };
        }

        public Task<List<TrackingHubDto>> GetTrackingHubsAsync()
        {
            var hubs = DeliveryHubs
                .OrderBy(h => h.Kind == "warehouse" ? 0 : 1)
                .ThenBy(h => h.Name)
                .Select(h => new TrackingHubDto
                {
                    Code = h.Code,
                    Name = h.Name,
                    Kind = h.Kind,
                    Latitude = h.Latitude,
                    Longitude = h.Longitude
                })
                .ToList();

            return Task.FromResult(hubs);
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

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, string? notes = null, int? adminUserId = null, string? trackingCheckpointCode = null)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order is null) return false;

            if (!Enum.TryParse<OrderStatus>(newStatus, true, out var newStatusEnum))
                return false;

            if (!IsValidTransition(order.Status, newStatusEnum))
                return false;

            var trackingCheckpoint = ResolveTrackingCheckpoint(trackingCheckpointCode);
            if (!string.IsNullOrWhiteSpace(trackingCheckpointCode) && trackingCheckpoint is null)
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
                Notes = BuildTrackingNotes(notes, trackingCheckpoint),
                ChangedByUserId = adminUserId
            });

            await _db.SaveChangesAsync();

            if (newStatusEnum is OrderStatus.Delivered or OrderStatus.Completed)
            {
                await PublishReviewEligibilityAsync(order);
            }

            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order is null || order.UserId != userId) return false;

            if (order.Status == OrderStatus.Packed || order.Status == OrderStatus.Shipped ||
                order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled ||
                order.Status == OrderStatus.Completed)
                throw new ConflictException("Cannot cancel orders already packed or shipped");

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

        private const string TrackingNotePrefix = "[TRACKING:";

        private static readonly TrackingHub[] DeliveryHubs =
        [
            new("warehouse-gurugram", "CapShop Fulfillment Center - Gurugram", "warehouse", 28.4595m, 77.0266m),
            new("warehouse-delhi", "CapShop Fulfillment Center - Delhi", "warehouse", 28.6139m, 77.2090m),
            new("warehouse-mumbai", "CapShop Fulfillment Center - Mumbai", "warehouse", 19.0760m, 72.8777m),
            new("warehouse-bengaluru", "CapShop Fulfillment Center - Bengaluru", "warehouse", 12.9716m, 77.5946m),
            new("warehouse-kolkata", "CapShop Fulfillment Center - Kolkata", "warehouse", 22.5726m, 88.3639m),
            new("warehouse-hyderabad", "CapShop Fulfillment Center - Hyderabad", "warehouse", 17.3850m, 78.4867m),
            new("warehouse-bhubaneswar", "CapShop Fulfillment Center - Bhubaneswar", "warehouse", 20.2961m, 85.8245m),
            new("hub-delhi", "North Sorting Hub - Delhi", "regional-hub", 28.6139m, 77.2090m),
            new("hub-jaipur", "West Sorting Hub - Jaipur", "regional-hub", 26.9124m, 75.7873m),
            new("hub-bhopal", "Central Sorting Hub - Bhopal", "regional-hub", 23.2599m, 77.4126m),
            new("hub-bengaluru", "South Sorting Hub - Bengaluru", "regional-hub", 12.9716m, 77.5946m),
            new("hub-kolkata", "East Sorting Hub - Kolkata", "regional-hub", 22.5726m, 88.3639m),
            new("hub-mumbai", "West Coast Hub - Mumbai", "regional-hub", 19.0760m, 72.8777m),
            new("hub-chennai", "South Coast Hub - Chennai", "regional-hub", 13.0827m, 80.2707m),
            new("hub-hyderabad", "Deccan Hub - Hyderabad", "regional-hub", 17.3850m, 78.4867m),
            new("hub-pune", "Maharashtra Hub - Pune", "regional-hub", 18.5204m, 73.8567m),
            new("hub-ahmedabad", "Gujarat Hub - Ahmedabad", "regional-hub", 23.0225m, 72.5714m),
            new("hub-lucknow", "UP Hub - Lucknow", "regional-hub", 26.8467m, 80.9462m),
            new("hub-chandigarh", "Punjab-Haryana Hub - Chandigarh", "regional-hub", 30.7333m, 76.7794m),
            new("hub-indore", "MP Hub - Indore", "regional-hub", 22.7196m, 75.8577m),
            new("hub-patna", "Bihar Hub - Patna", "regional-hub", 25.5941m, 85.1376m),
            new("hub-surat", "South Gujarat Hub - Surat", "regional-hub", 21.1702m, 72.8311m),
            new("hub-kochi", "Kerala Hub - Kochi", "regional-hub", 9.9312m, 76.2673m),
            new("hub-guwahati", "North East Hub - Guwahati", "regional-hub", 26.1445m, 91.7362m),
            new("hub-bhubaneswar", "Odisha Hub - Bhubaneswar", "regional-hub", 20.2961m, 85.8245m)
        ];

        private static readonly Dictionary<string, (decimal Latitude, decimal Longitude)> CityCoordinates = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Delhi"] = (28.6139m, 77.2090m),
            ["New Delhi"] = (28.6139m, 77.2090m),
            ["Gurugram"] = (28.4595m, 77.0266m),
            ["Gurgaon"] = (28.4595m, 77.0266m),
            ["Noida"] = (28.5355m, 77.3910m),
            ["Jaipur"] = (26.9124m, 75.7873m),
            ["Bhopal"] = (23.2599m, 77.4126m),
            ["Mumbai"] = (19.0760m, 72.8777m),
            ["Pune"] = (18.5204m, 73.8567m),
            ["Bengaluru"] = (12.9716m, 77.5946m),
            ["Bangalore"] = (12.9716m, 77.5946m),
            ["Chennai"] = (13.0827m, 80.2707m),
            ["Hyderabad"] = (17.3850m, 78.4867m),
            ["Kolkata"] = (22.5726m, 88.3639m),
            ["Ahmedabad"] = (23.0225m, 72.5714m),
            ["Lucknow"] = (26.8467m, 80.9462m),
            ["Chandigarh"] = (30.7333m, 76.7794m),
            ["Indore"] = (22.7196m, 75.8577m),
            ["Patna"] = (25.5941m, 85.1376m),
            ["Surat"] = (21.1702m, 72.8311m)
        };

        private static TrackingHub ResolveDestination(Address? address)
        {
            if (address is not null && CityCoordinates.TryGetValue(address.City.Trim(), out var point))
            {
                return new TrackingHub($"destination-{NormalizeCode(address.City)}", $"{address.City} delivery address", "destination", point.Latitude, point.Longitude);
            }

            return new TrackingHub("destination-customer-area", "Customer delivery area", "destination", 28.6139m, 77.2090m);
        }

        private static List<TrackingHub> BuildRoute(TrackingHub originWarehouse, TrackingHub nearestHub, TrackingHub destination)
        {
            var route = new List<TrackingHub> { originWarehouse };

            if (nearestHub.Code != originWarehouse.Code)
            {
                route.Add(nearestHub);
            }

            route.Add(new TrackingHub($"local-{destination.Code}", $"Local delivery hub near {destination.Name.Replace(" delivery address", string.Empty)}", "local-hub", destination.Latitude, destination.Longitude));
            route.Add(destination);

            return route;
        }

        private static int GetCurrentRouteIndex(OrderStatus status, int routeCount)
        {
            return status switch
            {
                OrderStatus.Draft or OrderStatus.CheckoutStarted or OrderStatus.PaymentPending or OrderStatus.Paid => 0,
                OrderStatus.Packed => Math.Min(1, routeCount - 1),
                OrderStatus.Shipped => Math.Min(2, routeCount - 1),
                OrderStatus.Delivered or OrderStatus.Completed => routeCount - 1,
                OrderStatus.Cancelled => 0,
                _ => 0
            };
        }

        private static DateTime EstimateDeliveryUtc(Order order, int currentIndex, int routeCount, DateTime now)
        {
            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed)
            {
                return order.UpdatedAtUtc ?? now;
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                return now;
            }

            var remainingLegs = Math.Max(1, routeCount - currentIndex - 1);
            var hoursPerLeg = order.Status == OrderStatus.Shipped ? 8 : 14;
            return now.AddHours(remainingLegs * hoursPerLeg + 3);
        }

        private static string GetTrackingStatus(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Draft or OrderStatus.CheckoutStarted or OrderStatus.PaymentPending => "Waiting for payment confirmation",
                OrderStatus.Paid => "Order confirmed at warehouse",
                OrderStatus.Packed => "Packed and ready for dispatch",
                OrderStatus.Shipped => "In transit to your nearest delivery hub",
                OrderStatus.Delivered or OrderStatus.Completed => "Delivered",
                OrderStatus.Cancelled => "Order cancelled",
                _ => "Tracking in progress"
            };
        }

        private async Task<TrackingSelection> GetLatestTrackingSelectionAsync(int orderId)
        {
            var histories = await _db.OrderStatusHistories
                .AsNoTracking()
                .Where(h => h.OrderId == orderId && h.Notes != null && h.Notes.Contains(TrackingNotePrefix))
                .OrderByDescending(h => h.ChangedAtUtc)
                .ToListAsync();

            TrackingHub? latestCheckpoint = null;
            TrackingHub? latestWarehouse = null;

            foreach (var history in histories)
            {
                var checkpoint = ResolveTrackingCheckpoint(ExtractTrackingCode(history.Notes));
                if (checkpoint is null)
                {
                    continue;
                }

                latestCheckpoint ??= checkpoint;

                if (checkpoint.Kind == "warehouse")
                {
                    latestWarehouse ??= checkpoint;
                }

                if (latestCheckpoint is not null && latestWarehouse is not null)
                {
                    break;
                }
            }

            return new TrackingSelection(latestCheckpoint, latestWarehouse);
        }

        private static TrackingHub? ResolveTrackingCheckpoint(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            return DeliveryHubs.FirstOrDefault(h => string.Equals(h.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static string? ExtractTrackingCode(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                return null;
            }

            var start = notes.IndexOf(TrackingNotePrefix, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                return null;
            }

            start += TrackingNotePrefix.Length;
            var end = notes.IndexOf(']', start);
            return end > start ? notes[start..end] : null;
        }

        private static string? BuildTrackingNotes(string? notes, TrackingHub? checkpoint)
        {
            if (checkpoint is null)
            {
                return notes;
            }

            var trackingNote = $"{TrackingNotePrefix}{checkpoint.Code}] Package reached {checkpoint.Name}.";
            return string.IsNullOrWhiteSpace(notes)
                ? trackingNote
                : $"{notes.Trim()} {trackingNote}";
        }

        private static string NormalizeCode(string value)
        {
            return new string(value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        }

        private async Task PublishReviewEligibilityAsync(Order order)
        {
            await _publishEndpoint.Publish<OrderReviewEligibilityCreatedEvent>(new
            {
                CorrelationId = Guid.NewGuid(),
                OrderId = order.Id,
                UserId = order.UserId,
                OrderNumber = order.OrderNumber,
                DeliveredAtUtc = order.UpdatedAtUtc ?? DateTime.UtcNow,
                Items = order.Items.Select(i => new
                {
                    i.ProductId,
                    i.ProductName
                }).ToList(),
                OccurredAtUtc = DateTime.UtcNow
            });
        }

        private static List<TrackingEventDto> BuildTrackingEvents(Order order, List<TrackingHub> route, int currentIndex, DateTime estimatedDeliveryUtc)
        {
            var events = new List<TrackingEventDto>();

            for (var i = 0; i < route.Count; i++)
            {
                var completed = i < currentIndex || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Completed;
                var current = i == currentIndex && !completed && order.Status != OrderStatus.Cancelled;
                var occurredAt = i == 0
                    ? order.CreatedAtUtc
                    : order.CreatedAtUtc.AddHours(i * 14);

                events.Add(new TrackingEventDto
                {
                    Title = GetTrackingEventTitle(route[i], order.Status, completed, current),
                    Description = current
                        ? "Your package is currently here."
                        : completed
                            ? "This checkpoint is complete."
                            : "Expected next checkpoint.",
                    Status = current ? "Current" : completed ? "Completed" : "Pending",
                    LocationName = route[i].Name,
                    OccurredAtUtc = completed || current ? occurredAt : estimatedDeliveryUtc.AddHours(-(route.Count - i) * 6),
                    IsCompleted = completed,
                    IsCurrent = current
                });
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                events.Add(new TrackingEventDto
                {
                    Title = "Order cancelled",
                    Description = "Delivery tracking stopped because this order was cancelled.",
                    Status = "Cancelled",
                    LocationName = route[currentIndex].Name,
                    OccurredAtUtc = order.UpdatedAtUtc ?? DateTime.UtcNow,
                    IsCompleted = true
                });
            }

            return events;
        }

        private static string GetTrackingEventTitle(TrackingHub point, OrderStatus orderStatus, bool completed, bool current)
        {
            if (orderStatus == OrderStatus.Cancelled)
            {
                return "Tracking stopped";
            }

            return point.Kind switch
            {
                "warehouse" => completed || current ? "Order confirmed" : "Awaiting warehouse scan",
                "regional-hub" => completed || current ? "Reached sorting hub" : "Expected at sorting hub",
                "local-hub" => completed || current ? "At nearest delivery hub" : "Expected at nearest delivery hub",
                "destination" => orderStatus == OrderStatus.Delivered || orderStatus == OrderStatus.Completed
                    ? "Delivered to customer"
                    : "Delivery address",
                _ => current ? "Current checkpoint" : "Delivery checkpoint"
            };
        }

        private static double DistanceKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double radiusKm = 6371;
            var dLat = ToRadians((double)(lat2 - lat1));
            var dLon = ToRadians((double)(lon2 - lon1));
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return radiusKm * c;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180;

        private sealed record TrackingHub(string Code, string Name, string Kind, decimal Latitude, decimal Longitude);
        private sealed record TrackingSelection(TrackingHub? Checkpoint, TrackingHub? Warehouse);

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
