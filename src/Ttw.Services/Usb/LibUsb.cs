using System.Runtime.InteropServices;

namespace Ttw.Services.Usb;

/// <summary>
/// The libusb functions and structures the watch communication needs.
/// </summary>
internal static class LibUsb
{
    /// <summary>
    /// The name of the native library. It resolves to libusb-1.0.so on Linux,
    /// libusb-1.0.dll on Windows and libusb-1.0.dylib on macOS.
    /// </summary>
    public const string LibraryName = "libusb-1.0";

    /// <summary>The vendor id of TomTom devices.</summary>
    public const ushort VendorIdTomTom = 0x1390;

    /// <summary>Endpoint transfer type: control.</summary>
    public const byte TransferTypeControl = 0;

    /// <summary>Endpoint transfer type: isochronous.</summary>
    public const byte TransferTypeIsochronous = 1;

    /// <summary>Endpoint transfer type: bulk.</summary>
    public const byte TransferTypeBulk = 2;

    /// <summary>Endpoint transfer type: interrupt.</summary>
    public const byte TransferTypeInterrupt = 3;

    /// <summary>Returns the error text of a libusb error code.</summary>
    public static string ErrorText(int errorCode)
    {
        try
        {
            IntPtr text = libusb_strerror(errorCode);
            return text == IntPtr.Zero ? $"libusb error {errorCode}" : Marshal.PtrToStringAnsi(text) ?? string.Empty;
        }
        catch (DllNotFoundException)
        {
            return "libusb is not available";
        }
        catch (EntryPointNotFoundException)
        {
            return $"libusb error {errorCode}";
        }
    }

    /// <summary>
    /// Throws when the libusb call failed.
    /// </summary>
    public static void Check(int errorCode, string operation)
    {
        if (errorCode != 0)
        {
            throw new UsbException($"{operation}: {ErrorText(errorCode)}");
        }
    }

    [DllImport(LibraryName, EntryPoint = "libusb_init")]
    public static extern int libusb_init(out IntPtr context);

    [DllImport(LibraryName, EntryPoint = "libusb_exit")]
    public static extern void libusb_exit(IntPtr context);

    [DllImport(LibraryName, EntryPoint = "libusb_get_device_list")]
    public static extern int libusb_get_device_list(IntPtr context, out IntPtr list);

    [DllImport(LibraryName, EntryPoint = "libusb_free_device_list")]
    public static extern void libusb_free_device_list(IntPtr list, int unrefDevices);

    [DllImport(LibraryName, EntryPoint = "libusb_get_device_descriptor")]
    public static extern int libusb_get_device_descriptor(IntPtr device, byte[] descriptor);

    [DllImport(LibraryName, EntryPoint = "libusb_open")]
    public static extern int libusb_open(IntPtr device, out IntPtr handle);

    [DllImport(LibraryName, EntryPoint = "libusb_close")]
    public static extern void libusb_close(IntPtr handle);

    [DllImport(LibraryName, EntryPoint = "libusb_set_auto_detach_kernel_driver")]
    public static extern int libusb_set_auto_detach_kernel_driver(IntPtr handle, int enable);

    [DllImport(LibraryName, EntryPoint = "libusb_claim_interface")]
    public static extern int libusb_claim_interface(IntPtr handle, int interfaceNumber);

    [DllImport(LibraryName, EntryPoint = "libusb_release_interface")]
    public static extern int libusb_release_interface(IntPtr handle, int interfaceNumber);

    [DllImport(LibraryName, EntryPoint = "libusb_get_active_config_descriptor")]
    public static extern int libusb_get_active_config_descriptor(IntPtr device, out IntPtr config);

    [DllImport(LibraryName, EntryPoint = "libusb_free_config_descriptor")]
    public static extern void libusb_free_config_descriptor(IntPtr config);

    [DllImport(LibraryName, EntryPoint = "libusb_bulk_transfer")]
    public static extern int libusb_bulk_transfer(
        IntPtr handle,
        byte endpoint,
        byte[] data,
        int length,
        out int transferred,
        uint timeout);

    [DllImport(LibraryName, EntryPoint = "libusb_interrupt_transfer")]
    public static extern int libusb_interrupt_transfer(
        IntPtr handle,
        byte endpoint,
        byte[] data,
        int length,
        out int transferred,
        uint timeout);

    [DllImport(LibraryName, EntryPoint = "libusb_strerror")]
    public static extern IntPtr libusb_strerror(int errorCode);

    [DllImport(LibraryName, EntryPoint = "libusb_get_string_descriptor_ascii")]
    public static extern int libusb_get_string_descriptor_ascii(
        IntPtr handle,
        byte descriptorIndex,
        byte[] data,
        int length);

    [DllImport(LibraryName, EntryPoint = "libusb_get_bus_number")]
    public static extern byte libusb_get_bus_number(IntPtr device);

    [DllImport(LibraryName, EntryPoint = "libusb_get_device_address")]
    public static extern byte libusb_get_device_address(IntPtr device);

    /// <summary>
    /// The configuration descriptor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ConfigDescriptor
    {
        /// <summary>Length of the descriptor.</summary>
        public byte bLength;

        /// <summary>Type of the descriptor.</summary>
        public byte bDescriptorType;

        /// <summary>Total length of the configuration.</summary>
        public ushort wTotalLength;

        /// <summary>Number of interfaces.</summary>
        public byte bNumInterfaces;

        /// <summary>Configuration value.</summary>
        public byte bConfigurationValue;

        /// <summary>Index of the configuration string.</summary>
        public byte iConfiguration;

        /// <summary>Configuration attributes.</summary>
        public byte bmAttributes;

        /// <summary>Maximum power consumption.</summary>
        public byte MaxPower;

        /// <summary>Pointer to the interfaces of the configuration.</summary>
        public IntPtr Interface;

        /// <summary>Pointer to the extra descriptor bytes.</summary>
        public IntPtr Extra;

        /// <summary>Length of the extra descriptor bytes.</summary>
        public int ExtraLength;
    }

    /// <summary>
    /// One interface of a configuration.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Interface
    {
        /// <summary>Pointer to the alternate settings of the interface.</summary>
        public IntPtr AltSetting;

        /// <summary>Number of alternate settings.</summary>
        public int NumAltSetting;
    }

    /// <summary>
    /// One alternate setting of an interface.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct InterfaceDescriptor
    {
        /// <summary>Length of the descriptor.</summary>
        public byte bLength;

        /// <summary>Type of the descriptor.</summary>
        public byte bDescriptorType;

        /// <summary>Number of the interface.</summary>
        public byte bInterfaceNumber;

        /// <summary>Number of the alternate setting.</summary>
        public byte bAlternateSetting;

        /// <summary>Number of endpoints.</summary>
        public byte bNumEndpoints;

        /// <summary>Interface class.</summary>
        public byte bInterfaceClass;

        /// <summary>Interface sub class.</summary>
        public byte bInterfaceSubClass;

        /// <summary>Interface protocol.</summary>
        public byte bInterfaceProtocol;

        /// <summary>Index of the interface string.</summary>
        public byte iInterface;

        /// <summary>Pointer to the endpoints of the alternate setting.</summary>
        public IntPtr Endpoint;

        /// <summary>Pointer to the extra descriptor bytes.</summary>
        public IntPtr Extra;

        /// <summary>Length of the extra descriptor bytes.</summary>
        public int ExtraLength;
    }

    /// <summary>
    /// One endpoint of an alternate setting.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EndpointDescriptor
    {
        /// <summary>Length of the descriptor.</summary>
        public byte bLength;

        /// <summary>Type of the descriptor.</summary>
        public byte bDescriptorType;

        /// <summary>Address of the endpoint.</summary>
        public byte bEndpointAddress;

        /// <summary>Endpoint attributes; the low two bits hold the transfer type.</summary>
        public byte bmAttributes;

        /// <summary>Maximum packet size.</summary>
        public ushort wMaxPacketSize;

        /// <summary>Polling interval.</summary>
        public byte bInterval;

        /// <summary>Refresh rate.</summary>
        public byte bRefresh;

        /// <summary>Synchronisation address.</summary>
        public byte bSynchAddress;

        /// <summary>Pointer to the extra descriptor bytes.</summary>
        public IntPtr Extra;

        /// <summary>Length of the extra descriptor bytes.</summary>
        public int ExtraLength;
    }
}
