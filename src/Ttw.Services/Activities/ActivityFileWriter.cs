using System.Text;
using Ttw.Core.Activities;
using Ttw.Core.Exporting;

namespace Ttw.Services.Activities;

/// <summary>
/// Writes an activity to a file in the requested format.
/// </summary>
public static class ActivityFileWriter
{
    /// <summary>
    /// Writes the activity to the target path and returns the path of the file
    /// that was written.
    /// </summary>
    /// <param name="format">The format to write.</param>
    /// <param name="activity">The activity to write.</param>
    /// <param name="rawTtbin">The raw file the activity was read from.</param>
    /// <param name="appName">Name of the application, written into the file.</param>
    /// <param name="targetPath">The path of the file to write.</param>
    public static string Write(
        ExportFormat format,
        Activity activity,
        byte[] rawTtbin,
        string appName,
        string targetPath)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(rawTtbin);
        ArgumentException.ThrowIfNullOrEmpty(targetPath);

        byte[] content = format switch
        {
            ExportFormat.Ttbin => rawTtbin,
            ExportFormat.Gpx => Encoding.UTF8.GetBytes(
                GpxWriter.Write(activity, appName, garminExtensions: true, uGotMeExtensions: false)),
            ExportFormat.Tcx => Encoding.UTF8.GetBytes(TcxWriter.Write(activity, appName)),
            ExportFormat.Csv => Encoding.UTF8.GetBytes(CsvWriter.Write(activity)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown export format."),
        };

        File.WriteAllBytes(targetPath, content);

        return targetPath;
    }
}
