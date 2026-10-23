using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ttw.Services.Http;
using Ttw.Services.Usb;

namespace Ttw.Services.QuickFix;

/// <summary>
/// The phases the QuickFix update goes through.
/// </summary>
public static class QuickFixPhase
{
    /// <summary>Reading the preferences of the watch.</summary>
    public const string ReadingPreferences = "READING_PREFERENCES";

    /// <summary>Fetching the configuration of the TomTom services.</summary>
    public const string FetchingConfig = "FETCHING_CONFIG";

    /// <summary>Downloading the ephemeris data.</summary>
    public const string Downloading = "DOWNLOADING";

    /// <summary>Writing the ephemeris data to the watch.</summary>
    public const string Writing = "WRITING";

    /// <summary>Reading the ephemeris data back from the watch.</summary>
    public const string Verifying = "VERIFYING";

    /// <summary>Resetting the GPS processor of the watch.</summary>
    public const string ResettingGps = "RESETTING_GPS";
}

/// <summary>
/// The progress of the QuickFix update.
/// </summary>
/// <param name="Phase">The phase the update is in.</param>
/// <param name="DoneBytes">The number of bytes processed in this phase.</param>
/// <param name="TotalBytes">The total number of bytes, or -1 when it is not known.</param>
public sealed record QuickFixProgress(string Phase, long DoneBytes, long TotalBytes);

/// <summary>
/// The outcome of the QuickFix update.
/// </summary>
/// <param name="Success">True when the data was written to the watch.</param>
/// <param name="Error">The reason the update failed, or null.</param>
/// <param name="DataUrl">The URL the ephemeris data was downloaded from.</param>
/// <param name="Bytes">The number of bytes that were downloaded.</param>
/// <param name="Days">The number of days the data covers.</param>
public sealed record QuickFixResult(bool Success, string? Error, string DataUrl, int Bytes, int Days);

/// <summary>
/// Downloads the GPS QuickFix data and writes it to the watch.
/// </summary>
public sealed class QuickFixUpdater
{
    /// <summary>The number of days the ephemeris data covers.</summary>
    public const int Days = 3;

    /// <summary>
    /// The ephemeris service that is used when the configuration service of
    /// TomTom cannot be reached.
    /// </summary>
    public const string FallbackEphemerisUrl =
        "http://gpsquickfix.services.tomtom.com/fitness/sifgps.f2p{DAYS}enc.ee";

    private readonly IQuickFixWatch _watch;
    private readonly IDataFetcher _fetcher;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates the updater.
    /// </summary>
    public QuickFixUpdater(IQuickFixWatch watch, IDataFetcher fetcher, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(watch);
        ArgumentNullException.ThrowIfNull(fetcher);
        ArgumentNullException.ThrowIfNull(logger);

        _watch = watch;
        _fetcher = fetcher;
        _logger = logger;
    }

    /// <summary>
    /// Downloads the ephemeris data and writes it to the watch. Nothing is
    /// written to the watch before the data has been downloaded successfully.
    /// </summary>
    public async Task<QuickFixResult> UpdateAsync(
        IProgress<QuickFixProgress>? progress,
        CancellationToken cancellationToken)
    {
        _watch.SendStartupSequence();

        progress?.Report(new QuickFixProgress(QuickFixPhase.ReadingPreferences, 0, -1));

        string? configUrl = _watch.GetPreference("ConfigURL");
        if (configUrl is null)
        {
            return new QuickFixResult(
                false,
                "Error reading preference ConfigURL from the watch",
                string.Empty,
                0,
                Days);
        }

        configUrl = configUrl.Trim();
        _logger.LogInformation("Configuration URL: {Url}", configUrl);

        progress?.Report(new QuickFixProgress(QuickFixPhase.FetchingConfig, 0, -1));

        string? dataUrl = null;

        try
        {
            string config = await _fetcher.GetStringAsync(configUrl, cancellationToken).ConfigureAwait(false);

            using JsonDocument document = JsonDocument.Parse(config);
            if (document.RootElement.TryGetProperty("service:ephemeris", out JsonElement ephemeris))
            {
                string? url = ephemeris.GetString();
                if (!string.IsNullOrEmpty(url))
                {
                    dataUrl = url;
                }
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogInformation(exception, "The TomTom config service is not available");
        }

        if (dataUrl is null)
        {
            _logger.LogInformation("TomTom config service not found: trying ephemeris service");
            dataUrl = FallbackEphemerisUrl;
        }

        dataUrl = dataUrl.Replace("{DAYS}", Days.ToString(CultureInfo.InvariantCulture));

        _logger.LogInformation("Requesting file for {Days} days ahead", Days);
        _logger.LogInformation("Quickfix data URL: {Url}", dataUrl);

        progress?.Report(new QuickFixProgress(QuickFixPhase.Downloading, 0, -1));

        byte[] data;
        try
        {
            IProgress<DownloadProgress> reporter = new InlineProgress<DownloadProgress>(download =>
                progress?.Report(new QuickFixProgress(
                    QuickFixPhase.Downloading,
                    download.DownloadedBytes,
                    download.TotalBytes)));

            data = await _fetcher.GetBytesAsync(dataUrl, reporter, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new QuickFixResult(
                false,
                "Error downloading quickfix data: " + exception.Message,
                dataUrl,
                0,
                Days);
        }

        if (data.Length == 0)
        {
            return new QuickFixResult(false, "Quickfix data is empty", dataUrl, 0, Days);
        }

        _logger.LogInformation("Quickfix data downloaded: {Bytes} bytes", data.Length);

        const string writeFailure =
            "Unable to send quickfix file to the watch (write, verification or GPS reset failed)";

        progress?.Report(new QuickFixProgress(QuickFixPhase.Writing, 0, data.Length));

        try
        {
            _watch.WriteFile(WatchClient.FileIdQuickFixData, data);
        }
        catch (Exception exception) when (exception is UsbException or IOException)
        {
            _logger.LogError(exception, "Cannot write the quickfix data to the watch");
            return new QuickFixResult(false, writeFailure, dataUrl, 0, Days);
        }

        progress?.Report(new QuickFixProgress(QuickFixPhase.Verifying, 0, data.Length));

        try
        {
            byte[] readBack = _watch.ReadFile(WatchClient.FileIdQuickFixData);

            if (readBack.Length != data.Length || !readBack.AsSpan().SequenceEqual(data))
            {
                _logger.LogError("The quickfix data read back from the watch does not match");
                return new QuickFixResult(false, writeFailure, dataUrl, 0, Days);
            }
        }
        catch (Exception exception) when (exception is UsbException or IOException)
        {
            _logger.LogError(exception, "Cannot verify the quickfix data on the watch");
            return new QuickFixResult(false, writeFailure, dataUrl, 0, Days);
        }

        progress?.Report(new QuickFixProgress(QuickFixPhase.ResettingGps, 0, 0));

        try
        {
            _watch.ResetGpsProcessor();
        }
        catch (Exception exception) when (exception is UsbException or IOException)
        {
            _logger.LogError(exception, "Cannot reset the GPS processor of the watch");
            return new QuickFixResult(false, writeFailure, dataUrl, 0, Days);
        }

        _logger.LogInformation("GPS Quickfix data sent to watch");

        return new QuickFixResult(true, null, dataUrl, data.Length, Days);
    }
}
