namespace CapShop.PaymentService.Infrastructure.Gateways;

public interface IRazorpayGatewayService
{
    Task<RazorpayOrderResult> CreateOrderAsync(decimal amount, string currency, string receipt, CancellationToken cancellationToken);
    bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature);
}

public sealed class RazorpayOrderResult
{
    public string RazorpayOrderId { get; init; } = string.Empty;
    public int AmountInSubunits { get; init; }
    public string Currency { get; init; } = "INR";
}
