namespace Ttw.Core.Dates;

/// <summary>
/// The reason a date range could not be created.
/// </summary>
public enum DateRangeError
{
    /// <summary>The range is valid.</summary>
    None,

    /// <summary>The start of the range is missing or not a valid date.</summary>
    InvalidFrom,

    /// <summary>The end of the range is missing or not a valid date.</summary>
    InvalidTo,

    /// <summary>The start of the range is after its end.</summary>
    FromAfterTo,
}
