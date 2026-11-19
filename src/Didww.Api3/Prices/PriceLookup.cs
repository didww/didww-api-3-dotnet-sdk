namespace Didww.Api3.Prices;

/// <summary>
/// The answer of a price lookup. It always holds at least one row: no match
/// raises <see cref="Didww.Api3.Exception.DidwwPricesException"/> with
/// <see cref="Didww.Api3.Exception.DidwwPricesErrorKind.PriceNotFound"/>.
/// </summary>
/// <typeparam name="TPrice">The row type of the price list.</typeparam>
public sealed class PriceLookup<TPrice>
{
    internal PriceLookup(IReadOnlyList<TPrice> rows, int totalRecords)
    {
        Rows = rows;
        TotalRecords = totalRecords;
    }

    /// <summary>The matching rows.</summary>
    public IReadOnlyList<TPrice> Rows { get; }

    /// <summary>The number of matching rows (<c>meta.total_records</c>).</summary>
    public int TotalRecords { get; }
}
