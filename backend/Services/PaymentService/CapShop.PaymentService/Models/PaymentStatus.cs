namespace CapShop.PaymentService.Models;

public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";

    public static bool IsValid(string status)
    {
        return status is Pending or Succeeded or Failed or Refunded;
    }
}
