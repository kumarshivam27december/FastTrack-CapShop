using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CapShop.PaymentService.Configuration;
using CapShop.Shared.Exceptions;
using Microsoft.Extensions.Options;

namespace CapShop.PaymentService.Infrastructure.Gateways;

public class RazorpayGatewayService : IRazorpayGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly RazorpayOptions _options;

    public RazorpayGatewayService(HttpClient httpClient, IOptions<RazorpayOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.KeyId) || string.IsNullOrWhiteSpace(_options.KeySecret))
        {
            throw new InvalidOperationException("Razorpay credentials are missing.");
        }

        _httpClient.BaseAddress = new Uri("https://api.razorpay.com/v1/");

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.KeyId}:{_options.KeySecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<RazorpayOrderResult> CreateOrderAsync(decimal amount, string currency, string receipt, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            throw new ValidationException("Amount must be greater than zero.");
        }

        var amountInSubunits = (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        var payload = new
        {
            amount = amountInSubunits,
            currency = string.IsNullOrWhiteSpace(currency) ? "INR" : currency.ToUpperInvariant(),
            receipt,
            payment_capture = 1
        };

        using var response = await _httpClient.PostAsJsonAsync("orders", payload, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ConflictException($"Razorpay order creation failed: {raw}");
        }

        using var json = JsonDocument.Parse(raw);
        var root = json.RootElement;

        var razorpayOrderId = root.GetProperty("id").GetString();
        var responseAmount = root.GetProperty("amount").GetInt32();
        var responseCurrency = root.GetProperty("currency").GetString() ?? payload.currency;

        if (string.IsNullOrWhiteSpace(razorpayOrderId))
        {
            throw new ConflictException("Razorpay did not return an order id.");
        }

        return new RazorpayOrderResult
        {
            RazorpayOrderId = razorpayOrderId,
            AmountInSubunits = responseAmount,
            Currency = responseCurrency
        };
    }

    public bool VerifySignature(string razorpayOrderId, string razorpayPaymentId, string razorpaySignature)
    {
        if (string.IsNullOrWhiteSpace(razorpayOrderId) ||
            string.IsNullOrWhiteSpace(razorpayPaymentId) ||
            string.IsNullOrWhiteSpace(razorpaySignature))
        {
            return false;
        }

        var payload = $"{razorpayOrderId}|{razorpayPaymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.KeySecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var generated = Convert.ToHexString(hash).ToLowerInvariant();

        return string.Equals(generated, razorpaySignature.Trim().ToLowerInvariant(), StringComparison.Ordinal);
    }
}
