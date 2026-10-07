using Didww.Api3.Callback;

namespace Didww.Api3.Examples;

public static class WebhookExample
{
    public static void Run()
    {
        Console.WriteLine("--- Webhook Callback Validation ---");
        var validator = new RequestValidator("your-callback-secret");
        var callbackUrl = "https://example.com/webhook";
        var callbackPayload = new Dictionary<string, string>
        {
            { "status", "completed" },
            { "id", "order-id" },
            { "type", "orders" }
        };
        var isValid = validator.Validate(callbackUrl, callbackPayload, "some-signature");
        Console.WriteLine($"  Signature valid: {isValid}");
    }
}
