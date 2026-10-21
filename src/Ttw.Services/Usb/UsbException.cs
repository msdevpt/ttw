namespace Ttw.Services.Usb;

/// <summary>
/// Raised when the communication with the watch fails.
/// </summary>
public sealed class UsbException : Exception
{
    /// <summary>Creates an exception with a message.</summary>
    public UsbException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and an inner exception.</summary>
    public UsbException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
