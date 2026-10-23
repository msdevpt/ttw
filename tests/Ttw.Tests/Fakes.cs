using Ttw.Services.Http;
using Ttw.Services.Usb;

namespace Ttw.Tests;

/// <summary>
/// A watch that serves canned answers, for the tests of the QuickFix update.
/// </summary>
internal sealed class FakeQuickFixWatch : IQuickFixWatch
{
    /// <summary>The preference the watch reports.</summary>
    public string? ConfigUrl { get; set; }

    /// <summary>The exception the write of the data throws, if any.</summary>
    public Exception? WriteFailure { get; set; }

    /// <summary>The exception the read back of the data throws, if any.</summary>
    public Exception? ReadFailure { get; set; }

    /// <summary>The exception the reset of the GPS processor throws, if any.</summary>
    public Exception? ResetFailure { get; set; }

    /// <summary>The data that was written to the watch.</summary>
    public byte[]? WrittenData { get; private set; }

    /// <summary>True when the GPS processor was reset.</summary>
    public bool GpsReset { get; private set; }

    /// <inheritdoc/>
    public void SendStartupSequence()
    {
    }

    /// <inheritdoc/>
    public string? GetPreference(string tag) => ConfigUrl;

    /// <inheritdoc/>
    public void WriteFile(int fileId, byte[] data)
    {
        if (WriteFailure is not null)
        {
            throw WriteFailure;
        }

        WrittenData = data;
    }

    /// <inheritdoc/>
    public byte[] ReadFile(int fileId)
    {
        if (ReadFailure is not null)
        {
            throw ReadFailure;
        }

        return WrittenData ?? [];
    }

    /// <inheritdoc/>
    public void ResetGpsProcessor()
    {
        if (ResetFailure is not null)
        {
            throw ResetFailure;
        }

        GpsReset = true;
    }
}

/// <summary>
/// A data fetcher that serves canned answers, for the tests of the QuickFix
/// update.
/// </summary>
internal sealed class FakeDataFetcher : IDataFetcher
{
    /// <summary>The exception the configuration download throws, if any.</summary>
    public Exception? ConfigFailure { get; set; }

    /// <summary>The exception the data download throws, if any.</summary>
    public Exception? DataFailure { get; set; }

    /// <summary>The configuration document the fetcher returns.</summary>
    public string Config { get; set; } = "{}";

    /// <summary>The data the fetcher returns.</summary>
    public byte[] Data { get; set; } = [1, 2, 3];

    /// <inheritdoc/>
    public Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        if (ConfigFailure is not null)
        {
            return Task.FromException<string>(ConfigFailure);
        }

        return Task.FromResult(Config);
    }

    /// <inheritdoc/>
    public Task<byte[]> GetBytesAsync(
        string url,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (DataFailure is not null)
        {
            return Task.FromException<byte[]>(DataFailure);
        }

        progress?.Report(new DownloadProgress(Data.Length, Data.Length));

        return Task.FromResult(Data);
    }
}
