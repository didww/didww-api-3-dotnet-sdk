using Didww.Api3.Exception;
using Didww.Api3.Prices;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Prices;

namespace Didww.Api3.Examples;

// Looks up Voice IN, Voice OUT, SMS OUT and SMS IN prices (2026-10-22) for one
// number.
//
// Prices are served by separate hosts (api-vin, api-vout and api-sms); the SDK
// mints a token for each of them on first use. A read-scoped client
// (Builder.SetScope(DidwwScope.Read)) is enough for prices. The account needs a
// price plan of the service, otherwise the call fails with a
// DidwwPricesException of kind PricePlanNotAssigned.
//
// Usage: ... -- prices-lookup [number]
public static class PricesLookupExample
{
    public static async Task RunAsync(DidwwClient client, string? number = null)
    {
        number ??= "18005550123";

        await ShowAsync("Voice IN", client.VoiceInPrices(), Filters(("number", number)));
        await ShowAsync("Voice IN, calls from the US", client.VoiceInPrices(),
            Filters(("number", number), ("src_country_iso", "US")));
        await ShowAsync("Voice OUT", client.VoiceOutPrices(), Filters(("number", number)));
        await ShowAsync("SMS OUT A2P", client.SmsOutA2PPrices(), Filters(("dst_number", number)));
        await ShowAsync("SMS OUT P2P, whole country", client.SmsOutP2PPrices(), Filters(("dst_country_iso", "US")));
        await ShowAsync("SMS OUT OTP", client.SmsOutOtpPrices(),
            Filters(("dst_number", number), ("dst_country_iso", "US")));
        await ShowAsync("SMS IN", client.SmsInPrices(), Filters(("dst_number", number)));

        // Filters are checked before any request is sent: Voice OUT takes number only.
        try
        {
            await client.VoiceOutPrices().LookupAsync(Filters(("number", number), ("src_country_iso", "US")));
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"\nRejected locally: {ex.Message}");
        }
    }

    private static Dictionary<string, string?> Filters(params (string Name, string? Value)[] filters) =>
        filters.ToDictionary(f => f.Name, f => f.Value);

    private static async Task ShowAsync<TPrice>(string title, PricesService<TPrice> prices,
        IReadOnlyDictionary<string, string?> filters) where TPrice : BaseResource
    {
        Console.WriteLine($"\n=== {title} ===");
        PriceLookup<TPrice> result;
        try
        {
            result = await prices.LookupAsync(filters);
        }
        catch (DidwwPricesException ex) when (ex.Kind is DidwwPricesErrorKind.PricePlanNotAssigned
                                                  or DidwwPricesErrorKind.PriceNotFound)
        {
            Console.WriteLine($"  {ex.Message}");
            return;
        }

        Console.WriteLine($"{result.TotalRecords} matching price(s)");
        foreach (var row in result.Rows)
            Console.WriteLine($"  {Describe(row)}");
    }

    // Prices and fees are decimals in USD; missing values print as "null".
    private static string Describe(BaseResource row) => row switch
    {
        VoiceInPrice p =>
            $"{p.Country} {p.Destination} {p.SourceType} from {Show(p.SrcCountryName)}: rate {Show(p.Rate)}, " +
            $"connection fee {Show(p.ConnectionFee)}, intervals {p.InitialInterval}/{p.NextInterval}, reject calls {p.RejectCalls}",
        VoiceOutPrice p =>
            $"{p.Category} {p.Country} {p.Network} {p.Prefix}: rate {Show(p.Rate)}, connection fee {Show(p.ConnectionFee)}, " +
            $"increments {p.InitialIncrement}/{p.RecurringIncrement}, next rate {Show(p.NextRate)} from {Show(p.EffectiveAt)}",
        SmsOutPrice p =>
            $"{p.Country} {Show(p.MccName)} {Show(p.MncName)} (MCC {Show(p.Mcc)}, MNC {Show(p.Mnc)}) " +
            $"{Show(p.SourcePrefix)} -> {Show(p.DestinationPrefix)}: price {Show(p.Price)}, reject SMS {p.RejectSms}",
        SmsInPrice p =>
            $"{p.Country} {Show(p.SourcePrefix)} -> {Show(p.DestinationPrefix)}: price {p.Price}",
        _ => row.ToString() ?? string.Empty
    };

    private static string Show(object? value) => value?.ToString() ?? "null";
}
