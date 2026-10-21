namespace Ttw.Core.Exporting;

/// <summary>
/// Parsing and naming of the export formats.
/// </summary>
public static class ExportFormats
{
    /// <summary>
    /// Parses a format name, ignoring case.
    /// </summary>
    public static bool TryParse(string? text, out ExportFormat format)
    {
        format = default;

        if (text is null)
        {
            return false;
        }

        switch (text.Trim().ToLowerInvariant())
        {
            case "gpx":
                format = ExportFormat.Gpx;
                return true;
            case "tcx":
                format = ExportFormat.Tcx;
                return true;
            case "ttbin":
                format = ExportFormat.Ttbin;
                return true;
            case "csv":
                format = ExportFormat.Csv;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Returns the file extension of a format, without the dot.
    /// </summary>
    public static string Extension(ExportFormat format) => format switch
    {
        ExportFormat.Gpx => "gpx",
        ExportFormat.Tcx => "tcx",
        ExportFormat.Ttbin => "ttbin",
        ExportFormat.Csv => "csv",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown export format."),
    };
}
