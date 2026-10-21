using System.Globalization;

namespace Ttw.Core.Dates;

/// <summary>
/// A range of days, both ends inclusive.
/// </summary>
public readonly record struct DateRange(DateOnly From, DateOnly To)
{
    /// <summary>
    /// Indicates whether a day falls inside the range.
    /// </summary>
    public bool Contains(DateOnly day) => day >= From && day <= To;

    /// <summary>
    /// Creates a date range from its textual representation. Both dates must be
    /// written as yyyy-MM-dd.
    /// </summary>
    public static bool TryCreate(string? from, string? to, out DateRange range, out DateRangeError error)
    {
        range = default;

        if (string.IsNullOrEmpty(from))
        {
            error = DateRangeError.InvalidFrom;
            return false;
        }

        if (!DateOnly.TryParseExact(
                from,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly fromDate))
        {
            error = DateRangeError.InvalidFrom;
            return false;
        }

        if (string.IsNullOrEmpty(to))
        {
            error = DateRangeError.InvalidTo;
            return false;
        }

        if (!DateOnly.TryParseExact(
                to,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly toDate))
        {
            error = DateRangeError.InvalidTo;
            return false;
        }

        if (fromDate > toDate)
        {
            error = DateRangeError.FromAfterTo;
            return false;
        }

        range = new DateRange(fromDate, toDate);
        error = DateRangeError.None;
        return true;
    }
}
