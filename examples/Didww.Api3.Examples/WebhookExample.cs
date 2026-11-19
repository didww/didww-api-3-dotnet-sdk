using System.Security.Cryptography;
using System.Text;
using Didww.Api3.Callback;

namespace Didww.Api3.Examples;

// Validates the signature of a DIDWW callback with RequestValidator.
//
// DIDWW signs every callback with the callback secret enabled in the User Panel
// and sends the signature in the X-DIDWW-Signature header: the HMAC-SHA1, as
// lowercase hex, of the callback URL with its port (https://example.com:443/...)
// followed by each payload key and its value, in key order.
//
// The example signs a sample callback that way, so the signature validates
// (True), then changes the payload, so the same signature no longer does (False).
public static class WebhookExample
{
    public static void Run()
    {
        Console.WriteLine("--- Webhook Callback Validation ---");
        const string callbackSecret = "your-callback-secret";
        const string callbackUrl = "https://example.com/webhooks/didww";
        var payload = new Dictionary<string, string>
        {
            { "status", "completed" },
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "type", "orders" }
        };

        var signature = Sign(callbackSecret, callbackUrl, payload);
        Console.WriteLine($"  {RequestValidator.HeaderName}: {signature}");

        var validator = new RequestValidator(callbackSecret);
        Console.WriteLine($"  Signature valid: {validator.Validate(callbackUrl, payload, signature)}");

        var tampered = new Dictionary<string, string>(payload) { ["status"] = "canceled" };
        Console.WriteLine($"  Tampered payload, signature valid: {validator.Validate(callbackUrl, tampered, signature)}");
    }

    // Computes the X-DIDWW-Signature of a callback the way DIDWW signs it.
    public static string Sign(string callbackSecret, string url, IReadOnlyDictionary<string, string> payload)
    {
        var uri = new Uri(url);
        var data = new StringBuilder($"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.PathAndQuery}");
        foreach (var (key, value) in payload.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            data.Append(key).Append(value);

        var hash = HMACSHA1.HashData(Encoding.UTF8.GetBytes(callbackSecret), Encoding.UTF8.GetBytes(data.ToString()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
