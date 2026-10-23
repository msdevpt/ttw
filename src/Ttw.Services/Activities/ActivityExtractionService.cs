using System.Globalization;
using Microsoft.Extensions.Logging;
using Ttw.Core.Activities;
using Ttw.Core.Dates;
using Ttw.Core.Exporting;
using Ttw.Core.Ttbins;
using Ttw.Services.Usb;

namespace Ttw.Services.Activities;

/// <summary>
/// An activity that was extracted from the watch.
/// </summary>
/// <param name="FileId">The id of the file the activity was stored in.</param>
/// <param name="StartDateTime">The start of the activity, in watch local time.</param>
/// <param name="Description">The description of the activity type.</param>
/// <param name="FilePath">The path of the file the activity was written to.</param>
public sealed record ExportedActivity(int FileId, DateTime StartDateTime, string Description, string FilePath);

/// <summary>
/// The outcome of an extraction.
/// </summary>
/// <param name="Activities">The activities that were extracted, ordered by start time.</param>
/// <param name="WrittenFiles">The files that were written.</param>
/// <param name="SkippedFiles">The files that could not be decoded, as hexadecimal ids.</param>
public sealed record ExtractionResult(
    IReadOnlyList<ExportedActivity> Activities,
    IReadOnlyList<string> WrittenFiles,
    IReadOnlyList<string> SkippedFiles);

/// <summary>
/// Downloads the activities from the watch and writes the ones that fall in a
/// date range to disk.
/// </summary>
public sealed class ActivityExtractionService
{
    /// <summary>The name of the application, written into the exported files.</summary>
    public const string AppName = "ttw";

    private readonly IActivityWatch _watch;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates the service.
    /// </summary>
    public ActivityExtractionService(IActivityWatch watch, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(watch);
        ArgumentNullException.ThrowIfNull(logger);

        _watch = watch;
        _logger = logger;
    }

    /// <summary>
    /// Downloads every activity from the watch and writes the ones that started
    /// in the given range to the output directory, one file per activity.
    /// </summary>
    public async Task<ExtractionResult> ExtractAsync(
        DateRange range,
        ExportFormat format,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        _watch.SendStartupSequence();

        IReadOnlyList<WatchFile> files;
        try
        {
            files = _watch.GetFileList(WatchClient.FileTypeTtbinData);
        }
        catch (Exception exception) when (exception is UsbException or IOException)
        {
            throw new ExtractionException("Error retrieving the file list from the watch", exception);
        }

        List<(WatchFile File, Activity Activity, byte[] Raw)> downloaded = [];
        List<string> skipped = [];

        string? deviceName = SafePreference("watchName");
        string? deviceSerial = _watch.GetDeviceSerialNumber();

        foreach (WatchFile file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Reading file 0x{FileId:x8}", file.FileId);

            byte[] raw;
            try
            {
                raw = _watch.ReadFile(file.FileId);
            }
            catch (Exception exception) when (exception is UsbException or IOException)
            {
                throw new ExtractionException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Error reading file 0x{0:x8} from the watch",
                        file.FileId),
                    exception);
            }

            Activity activity;
            try
            {
                activity = TtbinReader.Read(raw);
            }
            catch (Exception exception)
            {
                _logger.LogInformation(
                    "Skipping undecodable file 0x{FileId:x8}",
                    file.FileId);
                _logger.LogDebug(
                    exception,
                    "Cannot decode file 0x{FileId:x8}",
                    file.FileId);
                skipped.Add(string.Format(CultureInfo.InvariantCulture, "0x{0:x8}", file.FileId));
                continue;
            }

            if (activity.SegmentCount == 0)
            {
                _logger.LogInformation("File 0x{FileId:x8} holds no track data", file.FileId);
                skipped.Add(string.Format(CultureInfo.InvariantCulture, "0x{0:x8}", file.FileId));
                continue;
            }

            activity.DeviceName = deviceName ?? "TomTom";
            activity.DeviceSerialNumber = deviceSerial ?? "Unknown";

            if (range.Contains(DateOnly.FromDateTime(activity.StartDateTime)))
            {
                downloaded.Add((file, activity, raw));
            }
        }

        downloaded.Sort((left, right) => left.Activity.StartDateTime.CompareTo(right.Activity.StartDateTime));

        try
        {
            Directory.CreateDirectory(outputDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new ExtractionException(
                "Cannot create output directory " + outputDirectory,
                exception);
        }

        string extension = ExportFormats.Extension(format);

        List<ExportedActivity> exported = [];
        List<string> written = [];

        foreach ((WatchFile file, Activity activity, byte[] raw) in downloaded)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd}_{0:HH-mm-ss}_{1}.{2}",
                activity.StartDateTime,
                activity.ActivityDescription,
                extension);

            string path = System.IO.Path.Combine(outputDirectory, fileName);

            try
            {
                ActivityFileWriter.Write(format, activity, raw, AppName, path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                throw new ExtractionException("Error writing " + fileName, exception);
            }

            exported.Add(new ExportedActivity(
                file.FileId,
                activity.StartDateTime,
                activity.ActivityDescription,
                path));
            written.Add(path);
        }

        return new ExtractionResult(exported, written, skipped);
    }

    private string? SafePreference(string tag)
    {
        try
        {
            return _watch.GetPreference(tag);
        }
        catch (Exception exception) when (exception is UsbException or IOException)
        {
            _logger.LogWarning("Cannot read the preference {Tag} from the watch", tag);
            _logger.LogDebug(exception, "Cannot read the preference {Tag} from the watch", tag);
            return null;
        }
    }
}
