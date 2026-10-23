namespace Ttw.Services.Usb;

/// <summary>
/// Raised when the libusb library cannot be loaded.
/// </summary>
public sealed class UsbLibraryException : UsbException
{
    /// <summary>Creates an exception with a message.</summary>
    public UsbLibraryException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and an inner exception.</summary>
    public UsbLibraryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
