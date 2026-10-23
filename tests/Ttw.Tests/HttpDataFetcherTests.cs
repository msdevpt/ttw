using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Ttw.Services.Http;

namespace Ttw.Tests;

public class HttpDataFetcherTests
{
    [Fact]
    public async Task ARequestThatDoesNotAnswerFailsWithATimeout()
    {
        using SilentServer server = new();
        HttpDataFetcher fetcher = new(
            configTimeout: TimeSpan.FromMilliseconds(300),
            dataTimeout: TimeSpan.FromSeconds(30));

        HttpTimeoutException failure = await Assert.ThrowsAsync<HttpTimeoutException>(
            () => fetcher.GetStringAsync(server.Url, CancellationToken.None));

        Assert.Equal(server.Url, failure.Url);
        Assert.Contains("did not answer", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ATimeoutFailsBeforeTheDeadline()
    {
        using SilentServer server = new();
        HttpDataFetcher fetcher = new(
            configTimeout: TimeSpan.FromMilliseconds(300),
            dataTimeout: TimeSpan.FromSeconds(30));

        Stopwatch stopwatch = Stopwatch.StartNew();

        await Assert.ThrowsAsync<HttpTimeoutException>(
            () => fetcher.GetStringAsync(server.Url, CancellationToken.None));

        stopwatch.Stop();

        // The request has to give up while the timeout is running, not after
        // the time of the fallback request.
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(10),
            "The request gave up after " + stopwatch.Elapsed + ".");
    }

    [Fact]
    public async Task ACancelledRequestIsNotReportedAsATimeout()
    {
        using SilentServer server = new();
        HttpDataFetcher fetcher = new(
            configTimeout: TimeSpan.FromSeconds(30),
            dataTimeout: TimeSpan.FromSeconds(30));

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fetcher.GetStringAsync(server.Url, cancellation.Token));
    }

    [Fact]
    public async Task ADocumentIsDownloaded()
    {
        using SilentServer server = new(body: "{\"service:ephemeris\":\"http://example.com/data\"}");
        HttpDataFetcher fetcher = new();

        string document = await fetcher.GetStringAsync(server.Url, CancellationToken.None);

        Assert.Equal("{\"service:ephemeris\":\"http://example.com/data\"}", document);
    }

    [Fact]
    public void ANonPositiveTimeoutIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HttpDataFetcher(TimeSpan.Zero, TimeSpan.FromSeconds(30)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HttpDataFetcher(TimeSpan.FromSeconds(5), TimeSpan.Zero));
    }

    /// <summary>
    /// A server that accepts the connection and either answers at once or
    /// stays silent, so a request that does not answer can be tested.
    /// </summary>
    private sealed class SilentServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _stopping = new();

        public SilentServer(string? body = null)
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

            _ = Task.Run(() => AcceptAsync(body));
        }

        public int Port { get; }

        public string Url => "http://127.0.0.1:" + Port + "/config.json";

        public void Dispose()
        {
            _stopping.Cancel();
            _listener.Stop();
            _stopping.Dispose();
        }

        private async Task AcceptAsync(string? body)
        {
            try
            {
                using TcpClient client = await _listener.AcceptTcpClientAsync(_stopping.Token);
                using NetworkStream stream = client.GetStream();

                if (body is null)
                {
                    // The connection is kept open without an answer, so the
                    // request times out.
                    await Task.Delay(Timeout.Infinite, _stopping.Token);
                    return;
                }

                byte[] payload = System.Text.Encoding.UTF8.GetBytes(body);
                byte[] headers = System.Text.Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\n"
                    + "Content-Type: application/json\r\n"
                    + "Content-Length: " + payload.Length + "\r\n"
                    + "Connection: close\r\n"
                    + "\r\n");

                await stream.WriteAsync(headers, _stopping.Token);
                await stream.WriteAsync(payload, _stopping.Token);
                await stream.FlushAsync(_stopping.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException)
            {
            }
            catch (SocketException)
            {
            }
        }
    }
}
