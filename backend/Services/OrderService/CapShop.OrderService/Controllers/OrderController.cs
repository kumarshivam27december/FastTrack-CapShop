using CapShop.OrderService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CapShop.OrderService.DTOs.Address;
using CapShop.OrderService.DTOs.Cart;
using CapShop.OrderService.DTOs.Checkout;
using CapShop.OrderService.DTOs.Order;
using CapShop.OrderService.DTOs.Payment;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


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

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { service = "OrderService", status = "Healthy" });

        //cart endpoint

        [HttpGet("cart")]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cart = await _orderService.GetOrCreateCartAsync(userId);
            return Ok(cart);
        }

        [HttpPost("cart/items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(request.ProductId.ToString()) || request.Quantity <= 0)
                return BadRequest("Invalid product or quantity");

            var cart = await _orderService.AddToCartAsync(userId, request);
            return Ok(cart);
        }

        [HttpPut("cart/items/{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemRequestDto request)
        {
            var userId = GetUserId();
            if (request.Quantity < 0) return BadRequest("Quantity cannot be negative");

            var cart = await _orderService.UpdateCartItemAsync(userId, id, request);
            return Ok(cart);
        }

        [HttpDelete("cart/items/{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = GetUserId();
            var result = await _orderService.RemoveFromCartAsync(userId, id);
            if (!result) return NotFound("Item not found");

            return Ok(new { message = "Item removed from cart" });
        }

        // ========== CHECKOUT ENDPOINTS ==========

        [HttpPost("checkout/start")]
        public async Task<IActionResult> StartCheckout([FromBody] CheckoutStartRequestDto request)
        {
            var userId = GetUserId();
            try
            {
                var checkout = await _orderService.StartCheckoutAsync(userId, request);
                return Ok(checkout);
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
                var payment = await _orderService.SimulatePaymentAsync(request);
                return Ok(payment);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequestDto request)
        {
            var userId = GetUserId();
            try
            {
                var order = await _orderService.PlaceOrderAsync(userId, request.OrderId);
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ========== ORDER ENDPOINTS ==========

        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            var orders = await _orderService.GetCustomerOrdersAsync(userId);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userId = GetUserId();
            var order = await _orderService.GetOrderByIdAsync(id, userId);
            if (order is null) return NotFound();

            return Ok(order);
        }

        // ========== ADMIN ENDPOINTS ==========

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
            var adminUserId = GetUserId();
            var result = await _orderService.UpdateOrderStatusAsync(id, request.NewStatus, request.Notes, adminUserId);
            if (!result) return NotFound();

            return Ok(new { message = "Order status updated" });
        }
    }

    public class PlaceOrderRequestDto
    {
        public int OrderId { get; set; }
    }
}
