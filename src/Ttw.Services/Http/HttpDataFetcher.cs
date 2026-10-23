namespace Ttw.Services.Http;

/// <summary>
/// The progress of a download.
/// </summary>
/// <param name="TotalBytes">The total size of the download, or -1 when it is not known.</param>
/// <param name="DownloadedBytes">The number of bytes received so far.</param>
public readonly record struct DownloadProgress(long TotalBytes, long DownloadedBytes);

/// <summary>
/// Downloads the configuration and the ephemeris data of the TomTom services.
/// </summary>
public sealed class HttpDataFetcher : IDataFetcher
{
    /// <summary>The size of the chunks the response body is copied in.</summary>
    private const int ChunkSize = 4096;

    /// <summary>
    /// The default timeout of the configuration request. The configuration only
    /// names the ephemeris service, and there is a fallback for it, so the
    /// request is given little time before it is given up on.
    /// </summary>
    public static readonly TimeSpan DefaultConfigTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The default timeout of the data request. The data is the payload that
    /// has to arrive, so it is given more time than the configuration.
    /// </summary>
    public static readonly TimeSpan DefaultDataTimeout = TimeSpan.FromSeconds(60);

    private readonly HttpClient _configClient;
    private readonly HttpClient _dataClient;
    private readonly TimeSpan _configTimeout;
    private readonly TimeSpan _dataTimeout;

    /// <summary>
    /// Creates the fetcher with the default timeouts.
    /// </summary>
    public HttpDataFetcher()
        : this(DefaultConfigTimeout, DefaultDataTimeout)
    {
    }

    /// <summary>
    /// Creates the fetcher with the given timeouts.
    /// </summary>
    /// <param name="configTimeout">The timeout of the configuration request.</param>
    /// <param name="dataTimeout">The timeout of the data request.</param>
    public HttpDataFetcher(TimeSpan configTimeout, TimeSpan dataTimeout)
    {
        if (configTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(configTimeout));
        }

        if (dataTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(dataTimeout));
        }

        _configTimeout = configTimeout;
        _dataTimeout = dataTimeout;

        // The timeout of the client is disabled: the linked token source below
        // applies the timeout to the whole request, the headers and the body,
        // and it lets the timeout be reported as a timeout and not as a
        // cancellation.
        _configClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        _dataClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
    }

    /// <summary>
    /// Downloads a document and returns it as text.
    /// </summary>
    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        byte[] bytes = await DownloadAsync(
                _configClient,
                url,
                progress: null,
                _configTimeout,
                cancellationToken)
            .ConfigureAwait(false);

        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Downloads a document and returns its bytes, reporting the progress.
    /// </summary>
    public async Task<byte[]> GetBytesAsync(
        string url,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        return await DownloadAsync(_dataClient, url, progress, _dataTimeout, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<byte[]> DownloadAsync(
        HttpClient client,
        string url,
        IProgress<DownloadProgress>? progress,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        // The timeout covers the response headers and the body, so a download
        // that stalls halfway does not hang.
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            using HttpResponseMessage response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            long total = response.Content.Headers.ContentLength ?? -1;

            await using Stream stream = await response.Content
                .ReadAsStreamAsync(deadline.Token)
                .ConfigureAwait(false);

            using MemoryStream buffer = new();

            byte[] chunk = new byte[ChunkSize];
            long downloaded = 0;

            while (true)
            {
                int read = await stream.ReadAsync(chunk, deadline.Token).ConfigureAwait(false);
                if (read <= 0)
                {
                    break;
                }

                buffer.Write(chunk, 0, read);
                downloaded += read;

                progress?.Report(new DownloadProgress(total, downloaded));
            }

            return buffer.ToArray();
        }
        catch (OperationCanceledException) when (
            deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new HttpTimeoutException(url, timeout);
        }
    }
}
