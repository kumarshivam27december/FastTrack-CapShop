using CapShop.OrderService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CapShop.OrderService.DTOs.Cart;
using CapShop.OrderService.DTOs.Checkout;
using CapShop.OrderService.DTOs.Order;
using CapShop.OrderService.DTOs.Payment;
using Microsoft.AspNetCore.Authorization;

namespace CapShop.OrderService.Controllers
{
    [ApiController]
    [Route("orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserIdStrict()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            {
                throw new UnauthorizedAccessException("Invalid token userId claim.");
            }

            return userId;
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { service = "OrderService", status = "Healthy" });

        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = GetUserIdStrict();
                var cart = await _orderService.GetOrCreateCartAsync(userId);
                return Ok(cart);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("cart/items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
        {
            try
            {
                var userId = GetUserIdStrict();
                if (request.ProductId <= 0 || request.Quantity <= 0)
                {
                    return BadRequest("Invalid product or quantity");
                }

                var cart = await _orderService.AddToCartAsync(userId, request);
                return Ok(cart);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("cart/items/{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemRequestDto request)
        {
            try
            {
                var userId = GetUserIdStrict();
                if (request.Quantity < 0) return BadRequest("Quantity cannot be negative");

                var cart = await _orderService.UpdateCartItemAsync(userId, id, request);
                return Ok(cart);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("cart/items/{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            try
            {
                var userId = GetUserIdStrict();
                var result = await _orderService.RemoveFromCartAsync(userId, id);
                if (!result) return NotFound("Item not found");

                return Ok(new { message = "Item removed from cart" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("checkout/start")]
        public async Task<IActionResult> StartCheckout([FromBody] CheckoutStartRequestDto request)
        {
            try
            {
                var userId = GetUserIdStrict();
                var checkout = await _orderService.StartCheckoutAsync(userId, request);
                return Ok(checkout);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("payment/simulate")]
        public async Task<IActionResult> SimulatePayment([FromBody] PaymentSimulateRequestDto request)
        {
            try
            {
                var userId = GetUserIdStrict();
                var payment = await _orderService.SimulatePaymentAsync(userId, request);
                return Ok(payment);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequestDto request)
        {
            try
            {
                var userId = GetUserIdStrict();
                var order = await _orderService.PlaceOrderAsync(userId, request.OrderId);
                return Ok(order);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                var userId = GetUserIdStrict();
                var result = await _orderService.CancelOrderAsync(id, userId);
                if (!result) return NotFound("Order not found");

                return Ok(new { message = "Order cancelled successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var userId = GetUserIdStrict();
                var orders = await _orderService.GetCustomerOrdersAsync(userId);
                return Ok(orders);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var userId = GetUserIdStrict();
                var order = await _orderService.GetOrderByIdAsync(id, userId);
                if (order is null) return NotFound();

                return Ok(order);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequestDto request)
        {
            try
            {
                var adminUserId = GetUserIdStrict();
                var result = await _orderService.UpdateOrderStatusAsync(id, request.NewStatus, request.Notes, adminUserId);
                if (!result) return BadRequest("Invalid status transition or order not found");

                return Ok(new { message = "Order status updated" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }
    }

    public class PlaceOrderRequestDto
    {
        public int OrderId { get; set; }
    }
}