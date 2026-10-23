namespace Ttw.Services.Usb;

/// <summary>
/// Raised when no supported watch is attached to the USB bus.
/// </summary>
public sealed class WatchNotFoundException : UsbException
{
    /// <summary>Creates the exception.</summary>
    public WatchNotFoundException()
        : base("No compatible TomTom watch found")
    {
    }
}
