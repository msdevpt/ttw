namespace Ttw.Services.Http;

/// <summary>
/// Downloads the configuration and the ephemeris data of the TomTom services.
/// </summary>
public interface IDataFetcher
{
    /// <summary>
    /// Downloads a document and returns it as text.
    /// </summary>
    Task<string> GetStringAsync(string url, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a document and returns its bytes, reporting the progress.
    /// </summary>
    Task<byte[]> GetBytesAsync(
        string url,
        IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken);
}
