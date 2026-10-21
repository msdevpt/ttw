namespace Ttw.Core.Exporting;

/// <summary>
/// The formats an activity can be exported to.
/// </summary>
public enum ExportFormat
{
    /// <summary>GPS Exchange format.</summary>
    Gpx,

    /// <summary>Garmin Training Center XML format.</summary>
    Tcx,

    /// <summary>The raw TTBIN file as it is stored on the watch.</summary>
    Ttbin,

    /// <summary>Comma separated values.</summary>
    Csv,
}
