using Didww.Api3.Exception;
using Didww.Api3.Prices;
using Didww.Api3.Resource.Prices;

namespace Didww.Api3.Examples;

// Downloads the Voice IN price file (2026-10-22), checks it again with
// If-Modified-Since, and reads a few rows.
//
// The file is written exactly as served, still gzipped, and dated with the
// response's Last-Modified. FetchAsync(path) sends the modification time of
// the file at path as If-Modified-Since, so while the price file is unchanged
// the second call, and any later run against the same file, is answered 304
// and keeps it.
//
// The file goes to the directory given as an argument, or to didww-prices in
// the user cache directory. A missing directory is created for the current
// user only.
//
// Usage: ... -- prices-download [dir]
public static class PricesDownloadExample
{
    private const int FirstRows = 5;

    public static async Task RunAsync(DidwwClient client, string? dir = null)
    {
        dir = CreateDirectory(dir ?? DefaultPriceDir());
        var path = Path.Combine(dir, "voice_in_prices.csv.gz");
        Console.WriteLine($"Price files go to {dir}");

        try
        {
            await RunAsync(client.VoiceInPrices(), path);
        }
        catch (DidwwPricesException ex) when (ex.Kind == DidwwPricesErrorKind.PricePlanNotAssigned)
        {
            Console.WriteLine($"No Voice IN price plan on this account: {ex.Message}");
        }
        catch (DidwwPricesException ex) when (ex.Kind == DidwwPricesErrorKind.ServiceUnavailable)
        {
            Console.WriteLine($"The price file is not available yet, retry in {ex.RetryAfter}: {ex.Message}");
        }
    }

    private static async Task RunAsync(PricesService<VoiceInPrice> prices, string path)
    {
        Console.WriteLine("=== HEAD ===");
        var head = await prices.HeadAsync();
        Console.WriteLine($"Last-Modified: {head.LastModified:R}, {head.ContentLength} bytes, {head.FileName}");

        Console.WriteLine($"\n=== Fetch {path} ===");
        await FetchAsync(prices, path);

        Console.WriteLine("\n=== Fetch again ===");
        await FetchAsync(prices, path);

        Console.WriteLine("\n=== First rows ===");
        await using var file = File.OpenRead(path);
        var n = 0;
        await foreach (var row in PriceFileReader.ReadRowsAsync(file))
        {
            Console.WriteLine($"  {row["Country"]} {row["Destination"]} {row["Source type"]}: {row["Rate (USD, excl. VAT)"]}");
            if (++n == FirstRows)
                break;
        }
    }

    private static async Task FetchAsync(PricesService<VoiceInPrice> prices, string path)
    {
        var file = await prices.FetchAsync(path);
        Console.WriteLine(file.NotModified ? "Not modified, file kept" : $"Downloaded {file.BytesWritten} bytes");
    }

    private static string DefaultPriceDir()
    {
        var cache = UserCacheDir();
        if (string.IsNullOrEmpty(cache))
            throw new InvalidOperationException("No user cache directory; pass a directory: prices-download <dir>");
        return Path.Combine(cache, "didww-prices");
    }

    // Creates the directory when missing, with mode 0700 on Unix; an existing
    // directory is left as it is.
    private static string CreateDirectory(string dir)
    {
        if (OperatingSystem.IsWindows())
            Directory.CreateDirectory(dir);
        else
            Directory.CreateDirectory(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return dir;
    }

    // %LOCALAPPDATA% on Windows, ~/Library/Caches on macOS, otherwise
    // $XDG_CACHE_HOME or ~/.cache. Empty when it cannot be determined.
    private static string UserCacheDir()
    {
        if (OperatingSystem.IsWindows())
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (!OperatingSystem.IsMacOS())
        {
            var xdgCache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (!string.IsNullOrEmpty(xdgCache))
                return xdgCache;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
            return string.Empty;
        return OperatingSystem.IsMacOS() ? Path.Combine(home, "Library", "Caches") : Path.Combine(home, ".cache");
    }
}
