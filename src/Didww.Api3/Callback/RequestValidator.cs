using System.Security.Cryptography;
using System.Text;

namespace Didww.Api3.Callback;

public class RequestValidator
{
    public const string HeaderName = "X-DIDWW-Signature";

    private readonly string _apiKey;

    public RequestValidator(string apiKey)
    {
        _apiKey = apiKey;
    }

    public bool Validate(string url, Dictionary<string, string> payload, string? signature)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        try
        {
            var expected = Convert.FromHexString(ValidSignature(url, payload));
            var actual = Convert.FromHexString(signature);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string ValidSignature(string url, Dictionary<string, string> payload)
    {
        var sorted = new SortedDictionary<string, string>(payload);
        var data = new StringBuilder(NormalizeUrl(url));
        foreach (var entry in sorted)
        {
            data.Append(entry.Key).Append(entry.Value);
        }
        return HmacSha1(data.ToString(), _apiKey);
    }

    private static string NormalizeUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var scheme = uri.Scheme;
            var userInfo = string.IsNullOrEmpty(uri.UserInfo) ? "" : uri.UserInfo + "@";
            var host = uri.Host;

            int port;
            if (uri.Port != -1)
                port = uri.Port;
            else if (scheme == "https")
                port = 443;
            else
                port = 80;

            var path = uri.AbsolutePath;
            var query = string.IsNullOrEmpty(uri.Query) ? "" : uri.Query;
            var fragment = string.IsNullOrEmpty(uri.Fragment) ? "" : uri.Fragment;

            return $"{scheme}://{userInfo}{host}:{port}{path}{query}{fragment}";
        }
        catch
        {
            return "";
        }
    }

    private static string HmacSha1(string data, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
