using System.Net.Http.Headers;

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
public sealed class HttpDataFetcher
{
    /// <summary>The size of the chunks the response body is copied in.</summary>
    private const int ChunkSize = 4096;

    /// <summary>The timeout of a request.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static readonly HttpClient Client = new() { Timeout = Timeout };

    /// <summary>
    /// Downloads a document and returns it as text.
    /// </summary>
    public async Task<string> GetStringAsync(string url, CancellationToken cancellationToken)
    {
        byte[] bytes = await GetBytesAsync(url, progress: null, cancellationToken).ConfigureAwait(false);

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
        ArgumentException.ThrowIfNullOrEmpty(url);

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        using HttpResponseMessage response = await Client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        long total = response.Content.Headers.ContentLength ?? -1;

        await using Stream stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        using MemoryStream buffer = new();

        byte[] chunk = new byte[ChunkSize];
        long downloaded = 0;

        while (true)
        {
            int read = await stream.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
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
}
