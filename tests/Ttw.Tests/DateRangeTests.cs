using Ttw.Core.Dates;

namespace Ttw.Tests;

public class DateRangeTests
{
    [Fact]
    public void TryCreateAcceptsAValidRange()
    {
        bool created = DateRange.TryCreate("2025-01-01", "2025-01-31", out DateRange range, out DateRangeError error);

        Assert.True(created);
        Assert.Equal(DateRangeError.None, error);
        Assert.Equal(new DateOnly(2025, 1, 1), range.From);
        Assert.Equal(new DateOnly(2025, 1, 31), range.To);
    }

    [Fact]
    public void TryCreateAcceptsASingleDay()
    {
        bool created = DateRange.TryCreate("2025-01-01", "2025-01-01", out DateRange range, out DateRangeError error);

        Assert.True(created);
        Assert.Equal(DateRangeError.None, error);
        Assert.Equal(range.From, range.To);
    }

    [Theory]
    [InlineData("01/01/2025")]
    [InlineData("2025-13-01")]
    [InlineData("2025-1-1")]
    [InlineData("")]
    [InlineData(null)]
    public void TryCreateRejectsAnInvalidStart(string? from)
    {
        bool created = DateRange.TryCreate(from, "2025-01-31", out _, out DateRangeError error);

        Assert.False(created);
        Assert.Equal(DateRangeError.InvalidFrom, error);
    }

    [Theory]
    [InlineData("31/01/2025")]
    [InlineData("2025-02-30")]
    [InlineData("")]
    [InlineData(null)]
    public void TryCreateRejectsAnInvalidEnd(string? to)
    {
        bool created = DateRange.TryCreate("2025-01-01", to, out _, out DateRangeError error);

        Assert.False(created);
        Assert.Equal(DateRangeError.InvalidTo, error);
    }

    [Fact]
    public void TryCreateRejectsAReversedRange()
    {
        bool created = DateRange.TryCreate("2025-02-01", "2025-01-31", out _, out DateRangeError error);

        Assert.False(created);
        Assert.Equal(DateRangeError.FromAfterTo, error);
    }

    [Fact]
    public void ContainsCoversBothEndsAndNothingOutside()
    {
        DateRange range = new(new DateOnly(2025, 1, 10), new DateOnly(2025, 1, 20));

        Assert.False(range.Contains(new DateOnly(2025, 1, 9)));
        Assert.True(range.Contains(new DateOnly(2025, 1, 10)));
        Assert.True(range.Contains(new DateOnly(2025, 1, 15)));
        Assert.True(range.Contains(new DateOnly(2025, 1, 20)));
        Assert.False(range.Contains(new DateOnly(2025, 1, 21)));
    }
}
