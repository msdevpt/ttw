using System.Net;
using Microsoft.Extensions.Logging;
using Ttw.Services.Failures;
using Ttw.Services.QuickFix;
using Ttw.Services.Usb;

namespace Ttw.Tests;

public class QuickFixUpdaterTests
{
    private const string ConfigUrl = "https://mysports.tomtom.com/service/config/config.json";
    private const string EphemerisUrl = "http://gpsquickfix.services.tomtom.com/fitness/sifgps.f2p3enc.ee";

    private static QuickFixUpdater Create(
        FakeQuickFixWatch watch,
        FakeDataFetcher fetcher,
        RecordingLogger logger) =>
        new(watch, fetcher, logger);

    private static FakeQuickFixWatch WatchWithConfig() => new() { ConfigUrl = ConfigUrl };

    [Fact]
    public async Task AFailingConfigServiceFallsBackWithoutDumpingTheException()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        FakeDataFetcher fetcher = new()
        {
            ConfigFailure = new HttpRequestException("No such host is known.", new WebException("timeout")),
            Config = "{\"service:ephemeris\":\"" + EphemerisUrl + "\"}",
        };
        RecordingLogger logger = new();

        QuickFixResult result = await Create(watch, fetcher, logger)
            .UpdateAsync(progress: null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(EphemerisUrl, result.DataUrl);

        // The detail of the failure belongs to the debug logs, not to the
        // output the user reads.
        Assert.Empty(logger.EntriesWithException(LogLevel.Information));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug && entry.Exception is not null);
    }

    [Fact]
    public async Task AFailingConfigServiceIsReportedInOneDiscreteLine()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        FakeDataFetcher fetcher = new()
        {
            ConfigFailure = new HttpRequestException("No such host is known."),
        };
        RecordingLogger logger = new();

        await Create(watch, fetcher, logger).UpdateAsync(progress: null, CancellationToken.None);

        RecordingLogger.Entry? notice = Assert.Single(logger.Entries, entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("did not answer", StringComparison.Ordinal));

        Assert.Null(notice.Exception);
    }

    [Fact]
    public async Task AConfigServiceThatNamesNoServiceIsReportedInOneDiscreteLine()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        FakeDataFetcher fetcher = new() { Config = "{}" };
        RecordingLogger logger = new();

        await Create(watch, fetcher, logger).UpdateAsync(progress: null, CancellationToken.None);

        RecordingLogger.Entry? notice = Assert.Single(logger.Entries, entry =>
            entry.Level == LogLevel.Information
            && entry.Message.Contains("did not name the ephemeris service", StringComparison.Ordinal));

        Assert.Null(notice.Exception);
    }

    [Fact]
    public async Task AFailingEphemerisServiceGivesAFriendlyMessageWithoutTheDetail()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        FakeDataFetcher fetcher = new()
        {
            Config = "{\"service:ephemeris\":\"" + EphemerisUrl + "\"}",
            DataFailure = new HttpRequestException("No such host is known."),
        };
        RecordingLogger logger = new();

        QuickFixResult result = await Create(watch, fetcher, logger)
            .UpdateAsync(progress: null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(QuickFixUpdater.EphemerisUnavailableMessage, result.Error);
        Assert.DoesNotContain("No such host", result.Error!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AFailingEphemerisServiceKeepsTheTechnicalDetailForTheVerboseOutput()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        FakeDataFetcher fetcher = new()
        {
            Config = "{\"service:ephemeris\":\"" + EphemerisUrl + "\"}",
            DataFailure = new HttpRequestException("No such host is known."),
        };
        RecordingLogger logger = new();

        QuickFixResult result = await Create(watch, fetcher, logger)
            .UpdateAsync(progress: null, CancellationToken.None);

        Assert.NotNull(result.Detail);
        Assert.Contains("System.Net.Http.HttpRequestException", result.Detail!, StringComparison.Ordinal);
        Assert.Contains("No such host is known.", result.Detail!, StringComparison.Ordinal);
        Assert.Contains("URL: " + EphemerisUrl, result.Detail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AFailingWriteGivesAFriendlyMessageAndKeepsTheDetail()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        watch.WriteFailure = new UsbException("USB error while sending packet: broken pipe");
        FakeDataFetcher fetcher = new()
        {
            Config = "{\"service:ephemeris\":\"" + EphemerisUrl + "\"}",
        };
        RecordingLogger logger = new();

        QuickFixResult result = await Create(watch, fetcher, logger)
            .UpdateAsync(progress: null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(
            "Could not communicate with the TomTom watch. "
            + "Check that it is connected and that the USB permissions allow access.",
            result.Error);
        Assert.Contains("UsbException", result.Detail!, StringComparison.Ordinal);
        Assert.DoesNotContain("broken pipe", result.Error!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMissingConfigUrlFailsWithoutTouchingTheWatch()
    {
        FakeQuickFixWatch watch = new() { ConfigUrl = null };
        FakeDataFetcher fetcher = new();
        RecordingLogger logger = new();

        QuickFixResult result = await Create(watch, fetcher, logger)
            .UpdateAsync(progress: null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Could not read the configuration URL from the watch.", result.Error);
        Assert.Null(watch.WrittenData);
    }

    [Fact]
    public async Task AnEmptyDownloadIsReportedAsAFailure()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        FakeDataFetcher fetcher = new()
        {
            Config = "{\"service:ephemeris\":\"" + EphemerisUrl + "\"}",
            Data = [],
        };
        RecordingLogger logger = new();

        QuickFixResult result = await Create(watch, fetcher, logger)
            .UpdateAsync(progress: null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("The ephemeris data downloaded from the TomTom service is empty.", result.Error);
        Assert.Null(watch.WrittenData);
    }

    [Fact]
    public async Task AFailedUpdateNeverLogsAnExceptionAboveTheDebugLevel()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        watch.ResetFailure = new UsbException("USB error while sending packet: broken pipe");
        FakeDataFetcher fetcher = new()
        {
            Config = "{\"service:ephemeris\":\"" + EphemerisUrl + "\"}",
        };
        RecordingLogger logger = new();

        QuickFixResult result = await Create(watch, fetcher, logger)
            .UpdateAsync(progress: null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(logger.EntriesWithException(LogLevel.Information));
    }

    [Fact]
    public async Task ASuccessfulUpdateWritesTheDataAndResetsTheGpsProcessor()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        FakeDataFetcher fetcher = new()
        {
            Config = "{\"service:ephemeris\":\"" + EphemerisUrl + "\"}",
            Data = [9, 8, 7],
        };
        RecordingLogger logger = new();

        QuickFixResult result = await Create(watch, fetcher, logger)
            .UpdateAsync(progress: null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.Null(result.Detail);
        Assert.Equal([9, 8, 7], watch.WrittenData);
        Assert.True(watch.GpsReset);
        Assert.Equal(3, result.Bytes);
    }

    [Fact]
    public async Task TheFallbackServiceIsUsedWhenTheConfigServiceNamesNoService()
    {
        FakeQuickFixWatch watch = WatchWithConfig();
        FakeDataFetcher fetcher = new() { Config = "{}" };
        RecordingLogger logger = new();

        QuickFixResult result = await Create(watch, fetcher, logger)
            .UpdateAsync(progress: null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(
            QuickFixUpdater.FallbackEphemerisUrl.Replace("{DAYS}", "3", StringComparison.Ordinal),
            result.DataUrl);
    }
}
