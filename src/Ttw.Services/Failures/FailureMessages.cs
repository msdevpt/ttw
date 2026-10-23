using Ttw.Services.Activities;
using Ttw.Services.Usb;

namespace Ttw.Services.Failures;

/// <summary>
/// Builds the message that is shown to the user when an operation fails. The
/// message names the service or the device that failed, without stating a
/// cause the tool cannot know.
/// </summary>
public static class FailureMessages
{
    /// <summary>
    /// Returns the message for the given failure.
    /// </summary>
    public static string For(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            WatchNotFoundException =>
                "No compatible TomTom watch found. Connect the watch to the USB port and try again.",
            UsbLibraryException =>
                "The libusb library could not be loaded. Install libusb and try again.",
            UsbException =>
                "Could not communicate with the TomTom watch. "
                + "Check that it is connected and that the USB permissions allow access.",
            HttpRequestException =>
                "Could not reach the TomTom service. Check your internet connection and try again later.",
            IOException =>
                "Could not read or write a file. Check the permissions and the free space, and try again.",
            OperationCanceledException =>
                "The operation was cancelled.",
            ExtractionException =>
                exception.Message,
            _ =>
                "The operation failed unexpectedly.",
        };
    }
}
