namespace Ttw.Services.Http;

/// <summary>
/// The failure of a request that did not answer within the timeout.
/// </summary>
public sealed class HttpTimeoutException : HttpRequestException
{
    /// <summary>
    /// Creates the failure.
    /// </summary>
    /// <param name="url">The URL that was being fetched.</param>
    /// <param name="timeout">The timeout that elapsed.</param>
    public HttpTimeoutException(string url, TimeSpan timeout)
        : base($"The request to {url} did not answer within {timeout.TotalSeconds:0.#} seconds.")
    {
        Url = url;
        Timeout = timeout;
    }

    /// <summary>The URL that was being fetched.</summary>
    public string Url { get; }

    /// <summary>The timeout that elapsed.</summary>
    public TimeSpan Timeout { get; }
}
