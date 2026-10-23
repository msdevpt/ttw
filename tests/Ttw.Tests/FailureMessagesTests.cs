using System.Net;
using Ttw.Services.Failures;
using Ttw.Services.Usb;

namespace Ttw.Tests;

public class FailureMessagesTests
{
    [Fact]
    public void AMissingWatchIsNamedInTheMessage()
    {
        string message = FailureMessages.For(new WatchNotFoundException());

        Assert.Contains("watch", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UsbException", message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingLibraryIsNamedInTheMessage()
    {
        string message = FailureMessages.For(new UsbLibraryException("could not be loaded"));

        Assert.Contains("libusb", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AUsbFailureIsReportedAsACommunicationProblem()
    {
        string message = FailureMessages.For(new UsbException("USB error while sending packet: broken pipe"));

        Assert.Contains("watch", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("broken pipe", message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANetworkFailureIsReportedWithoutStatingTheCause()
    {
        string message = FailureMessages.For(new HttpRequestException("No such host is known."));

        Assert.Contains("TomTom", message, StringComparison.Ordinal);
        Assert.DoesNotContain("No such host", message, StringComparison.Ordinal);
        Assert.DoesNotContain("down", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheDetailNamesTheTypeTheMessageAndTheUrl()
    {
        string detail = TechnicalDetail.From(
            new HttpRequestException("No such host is known."),
            "http://example.invalid/data");

        Assert.Contains("System.Net.Http.HttpRequestException", detail, StringComparison.Ordinal);
        Assert.Contains("No such host is known.", detail, StringComparison.Ordinal);
        Assert.Contains("URL: http://example.invalid/data", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDetailNamesTheCauseOfTheFailure()
    {
        string detail = TechnicalDetail.From(
            new UsbException("Cannot open the TomTom watch", new InvalidOperationException("no device")));

        Assert.Contains("Caused by: System.InvalidOperationException: no device", detail, StringComparison.Ordinal);
    }
}
