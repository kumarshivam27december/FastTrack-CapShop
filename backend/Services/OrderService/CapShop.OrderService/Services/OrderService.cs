using CapShop.OrderService.Data;
using CapShop.OrderService.DTOs.Cart;
using CapShop.OrderService.DTOs.Address;
using CapShop.OrderService.DTOs.Checkout;
using CapShop.OrderService.DTOs.Payment;
using CapShop.OrderService.DTOs.Order;
using CapShop.OrderService.Models;
using CapShop.OrderService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CapShop.OrderService.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _db;

    public OrderService(OrderDbContext db)
    {
        _db = db;
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
        if (cart is null) throw new InvalidOperationException("Cart not found");

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            var item = new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice
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
            cart.Items.Remove(item);
        else
            item.Quantity = request.Quantity;

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

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            Status = OrderStatus.CheckoutStarted,
            TotalAmount = cart.Items.Sum(i => i.Quantity * i.UnitPrice)
        };

      
        foreach (var cartItem in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = $"Product-{cartItem.ProductId}",
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

    public async Task<PaymentResponseDto> SimulatePaymentAsync(PaymentSimulateRequestDto request)
    {
        var order = await _db.Orders.FindAsync(request.OrderId);
        if (order is null) throw new InvalidOperationException("Order not found");

        var transactionId = Guid.NewGuid().ToString().Substring(0, 12);

        if (request.SimulateSuccess)
        {
            order.Status = OrderStatus.Paid;
            order.PaymentMethod = request.PaymentMethod;
            order.PaymentTransactionId = transactionId;
            order.UpdatedAtUtc = DateTime.UtcNow;

            _db.Orders.Update(order);
            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = OrderStatus.CheckoutStarted,
                ToStatus = OrderStatus.Paid,
                Notes = $"Payment received via {request.PaymentMethod}"
            });
            await _db.SaveChangesAsync();

            return new PaymentResponseDto
            {
                OrderId = order.Id,
                TransactionId = transactionId,
                Success = true,
                Message = "Payment successful"
            };
        }
        else
        {
            return new PaymentResponseDto
            {
                OrderId = order.Id,
                TransactionId = transactionId,
                Success = false,
                Message = "Payment failed"
            };
        }
    }

    public async Task<CheckoutResponseDto> PlaceOrderAsync(int userId, int orderId)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order is null) throw new InvalidOperationException("Order not found");

        if (order.Status != OrderStatus.Paid)
            throw new InvalidOperationException("Order must be paid before placing");

    
        await ClearCartAsync(userId);

        return new CheckoutResponseDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            Message = "Order placed successfully"
        };
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
            order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
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
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
    }
}