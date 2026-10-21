using Ttw.Core.Exporting;

namespace Ttw.Tests;

public class ExportFormatTests
{
    [Theory]
    [InlineData("gpx", ExportFormat.Gpx)]
    [InlineData("GPX", ExportFormat.Gpx)]
    [InlineData("tcx", ExportFormat.Tcx)]
    [InlineData("TCX", ExportFormat.Tcx)]
    [InlineData("ttbin", ExportFormat.Ttbin)]
    [InlineData("csv", ExportFormat.Csv)]
    [InlineData("CSV", ExportFormat.Csv)]
    public void TryParseAcceptsTheKnownFormats(string text, ExportFormat expected)
    {
        Assert.True(ExportFormats.TryParse(text, out ExportFormat format));
        Assert.Equal(expected, format);
    }

    [Theory]
    [InlineData("xml")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseRejectsUnknownFormats(string? text)
    {
        Assert.False(ExportFormats.TryParse(text, out _));
    }

    [Theory]
    [InlineData(ExportFormat.Gpx, "gpx")]
    [InlineData(ExportFormat.Tcx, "tcx")]
    [InlineData(ExportFormat.Ttbin, "ttbin")]
    [InlineData(ExportFormat.Csv, "csv")]
    public void ExtensionReturnsTheFileExtension(ExportFormat format, string expected)
    {
        Assert.Equal(expected, ExportFormats.Extension(format));
    }
}
