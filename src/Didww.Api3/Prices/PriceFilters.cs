namespace Didww.Api3.Prices;

/// <summary>
/// The lookup filters a prices service accepts: every required filter, any
/// optional one, and at least one of the any-of filters when there are some.
/// </summary>
internal sealed class PriceFilters
{
    internal static readonly PriceFilters VoiceIn = new(["number"], ["src_country_iso"], []);
    internal static readonly PriceFilters VoiceOut = new(["number"], [], []);
    internal static readonly PriceFilters Sms = new([], [], ["dst_number", "dst_country_iso"]);

    private readonly string[] _required;
    private readonly string[] _optional;
    private readonly string[] _anyOf;

    private PriceFilters(string[] required, string[] optional, string[] anyOf)
    {
        _required = required;
        _optional = optional;
        _anyOf = anyOf;
    }

    private IEnumerable<string> Allowed => _required.Concat(_optional).Concat(_anyOf);

    /// <summary>
    /// Checks <paramref name="filters"/> and returns them as a query string
    /// (without the leading <c>?</c>), sorted by name. Null and blank values are dropped.
    /// Throws <see cref="ArgumentException"/> for an unknown or missing filter.
    /// </summary>
    internal string BuildQuery(IReadOnlyDictionary<string, string?>? filters, string lookupPath)
    {
        var given = (filters ?? new Dictionary<string, string?>())
            .Where(f => !string.IsNullOrWhiteSpace(f.Value))
            .OrderBy(f => f.Key, StringComparer.Ordinal)
            .ToList();
        var names = given.Select(f => f.Key).ToList();

        var allowed = Allowed.ToList();
        var unknown = names.Where(name => !allowed.Contains(name, StringComparer.Ordinal)).ToList();
        var missing = _required.Where(name => !names.Contains(name, StringComparer.Ordinal)).ToList();
        var anyOfGiven = _anyOf.Any(name => names.Contains(name, StringComparer.Ordinal));

        if (unknown.Count > 0)
        {
            throw new ArgumentException(
                $"Filter {string.Join(", ", unknown)} is not allowed on {lookupPath} (allowed: {string.Join(", ", allowed)})",
                nameof(filters));
        }
        if (missing.Count > 0)
            throw new ArgumentException($"Filter {string.Join(", ", missing)} is required on {lookupPath}", nameof(filters));
        if (_anyOf.Length > 0 && !anyOfGiven)
        {
            throw new ArgumentException($"One of filters {string.Join(", ", _anyOf)} is required on {lookupPath}",
                nameof(filters));
        }

        return string.Join("&", given.Select(f =>
            "filter[" + Uri.EscapeDataString(f.Key) + "]=" + Uri.EscapeDataString(f.Value!)));
    }
}
