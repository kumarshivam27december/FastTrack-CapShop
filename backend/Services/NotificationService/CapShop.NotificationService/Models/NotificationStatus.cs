namespace CapShop.NotificationService.Models;

public static class NotificationStatus
{
    public const string Sent = "Sent";
    public const string Failed = "Failed";

    public static bool IsValid(string status)
    {
        return status is Sent or Failed;
    }
}
