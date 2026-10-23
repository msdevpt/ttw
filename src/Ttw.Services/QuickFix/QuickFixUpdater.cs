using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ttw.Services.Failures;
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
/// <param name="Error">The message shown to the user when the update failed, or null.</param>
/// <param name="Detail">The technical detail of the failure, shown in verbose mode.</param>
/// <param name="DataUrl">The URL the ephemeris data was downloaded from.</param>
/// <param name="Bytes">The number of bytes that were downloaded.</param>
/// <param name="Days">The number of days the data covers.</param>
public sealed record QuickFixResult(
    bool Success,
    string? Error,
    string? Detail,
    string DataUrl,
    int Bytes,
    int Days);

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

    /// <summary>
    /// The message shown when the ephemeris service cannot be reached.
    /// </summary>
    public const string EphemerisUnavailableMessage =
        "Could not reach the TomTom ephemeris service. "
        + "Check your internet connection and try again later.";

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
            return Failed(
                "Could not read the configuration URL from the watch.",
                detail: null,
                dataUrl: string.Empty);
        }

        configUrl = configUrl.Trim();
        _logger.LogInformation("Configuration URL: {Url}", configUrl);

        progress?.Report(new QuickFixProgress(QuickFixPhase.FetchingConfig, 0, -1));

        string? dataUrl = null;
        bool configAnswered = false;

        try
        {
            string config = await _fetcher.GetStringAsync(configUrl, cancellationToken).ConfigureAwait(false);

            configAnswered = true;

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
            _logger.LogInformation(
                "The TomTom config service did not answer; using the ephemeris service directly");
            _logger.LogDebug(exception, "The TomTom config service did not answer: {Url}", configUrl);
        }

        if (dataUrl is null)
        {
            if (configAnswered)
            {
                _logger.LogInformation(
                    "The TomTom config service did not name the ephemeris service; "
                    + "using the default ephemeris service directly");
            }

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
            return Failed(
                EphemerisUnavailableMessage,
                TechnicalDetail.From(exception, dataUrl),
                dataUrl);
        }

        if (data.Length == 0)
        {
            return Failed(
                "The ephemeris data downloaded from the TomTom service is empty.",
                detail: null,
                dataUrl);
        }

        _logger.LogInformation("Quickfix data downloaded: {Bytes} bytes", data.Length);

        progress?.Report(new QuickFixProgress(QuickFixPhase.Writing, 0, data.Length));

        try
        {
            _watch.WriteFile(WatchClient.FileIdQuickFixData, data);
        }
        catch (Exception exception) when (exception is UsbException or IOException)
        {
            return Failed(
                FailureMessages.For(exception),
                TechnicalDetail.From(exception),
                dataUrl);
        }

        progress?.Report(new QuickFixProgress(QuickFixPhase.Verifying, 0, data.Length));

        try
        {
            byte[] readBack = _watch.ReadFile(WatchClient.FileIdQuickFixData);

            if (readBack.Length != data.Length || !readBack.AsSpan().SequenceEqual(data))
            {
                return Failed(
                    "The ephemeris data written to the watch could not be read back.",
                    detail: null,
                    dataUrl);
            }
        }
        catch (Exception exception) when (exception is UsbException or IOException)
        {
            return Failed(
                FailureMessages.For(exception),
                TechnicalDetail.From(exception),
                dataUrl);
        }

        progress?.Report(new QuickFixProgress(QuickFixPhase.ResettingGps, 0, 0));

        try
        {
            _watch.ResetGpsProcessor();
        }
        catch (Exception exception) when (exception is UsbException or IOException)
        {
            return Failed(
                FailureMessages.For(exception),
                TechnicalDetail.From(exception),
                dataUrl);
        }

        _logger.LogInformation("GPS Quickfix data sent to watch");

        return new QuickFixResult(true, null, null, dataUrl, data.Length, Days);
    }

    private static QuickFixResult Failed(string error, string? detail, string dataUrl) =>
        new(false, error, detail, dataUrl, 0, Days);
}
