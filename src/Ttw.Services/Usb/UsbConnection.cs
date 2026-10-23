using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Ttw.Services.Usb;

/// <summary>
/// The connection to a TomTom watch. It talks to libusb directly: the data
/// endpoints of the watch are interrupt endpoints and on Linux the kernel
/// driver has to be detached before the interface can be claimed.
/// </summary>
public sealed class UsbConnection : IDisposable
{
    /// <summary>Product id of the TomTom Multisport and Runner.</summary>
    public const ushort ProductIdMultisport = 0x7474;

    /// <summary>Product id of the TomTom Spark Music, Runner Music and Adventurer.</summary>
    public const ushort ProductIdSparkMusic = 0x7475;

    /// <summary>Product id of the TomTom Spark Cardio and Runner Cardio.</summary>
    public const ushort ProductIdSparkCardio = 0x7477;

    /// <summary>Timeout of a single USB transfer, in milliseconds.</summary>
    private const uint TransferTimeoutMs = 10000;

    /// <summary>Length of the device descriptor.</summary>
    private const int DeviceDescriptorLength = 18;

    private readonly ILogger? _logger;
    private IntPtr _context;
    private IntPtr _device;
    private IntPtr _handle;
    private IntPtr _configDescriptor;
    private bool _interfaceClaimed;
    private bool _disposed;

    private UsbConnection(
        ILogger? logger,
        IntPtr context,
        IntPtr device,
        IntPtr handle,
        IntPtr configDescriptor,
        ushort productId,
        byte serialNumberIndex)
    {
        _logger = logger;
        _context = context;
        _device = device;
        _handle = handle;
        _configDescriptor = configDescriptor;

        ProductId = productId;
        SerialNumberIndex = serialNumberIndex;

        (PacketSize, WriteEndpointAddress, ReadEndpointAddress, ReadChunkSize, WriteChunkSize) =
            ParametersFor(productId);
    }

    /// <summary>
    /// A TomTom watch found on the USB bus.
    /// </summary>
    /// <param name="DeviceKey">Identifier of the device on the bus.</param>
    /// <param name="VendorId">The USB vendor id.</param>
    /// <param name="ProductId">The USB product id.</param>
    public sealed record UsbDeviceInfo(string DeviceKey, ushort VendorId, ushort ProductId);

    /// <summary>The product id of the watch that is open.</summary>
    public ushort ProductId { get; }

    /// <summary>The index of the serial number string in the device descriptor.</summary>
    public byte SerialNumberIndex { get; }

    /// <summary>The size of a USB packet for this watch.</summary>
    public int PacketSize { get; }

    /// <summary>The address of the endpoint the watch is written to.</summary>
    public byte WriteEndpointAddress { get; }

    /// <summary>The address of the endpoint the watch is read from.</summary>
    public byte ReadEndpointAddress { get; }

    /// <summary>The maximum number of bytes that can be read in one chunk.</summary>
    public int ReadChunkSize { get; }

    /// <summary>The maximum number of bytes that can be written in one chunk.</summary>
    public int WriteChunkSize { get; }

    /// <summary>
    /// Lists the TomTom watches that are attached to the USB bus.
    /// </summary>
    public static IReadOnlyList<UsbDeviceInfo> FindDevices()
    {
        List<UsbDeviceInfo> devices = [];

        IntPtr context = IntPtr.Zero;
        IntPtr list = IntPtr.Zero;

        try
        {
            InitializeLibUsb(out context);

            int count = LibUsb.libusb_get_device_list(context, out list);
            if (count < 0)
            {
                LibUsb.Check(count, "Cannot list the USB devices");
            }

            for (int index = 0; index < count; index++)
            {
                IntPtr device = Marshal.ReadIntPtr(list, index * IntPtr.Size);

                byte[] descriptor = new byte[DeviceDescriptorLength];
                if (LibUsb.libusb_get_device_descriptor(device, descriptor) != 0)
                {
                    continue;
                }

                ushort vendorId = (ushort)(descriptor[8] | (descriptor[9] << 8));
                ushort productId = (ushort)(descriptor[10] | (descriptor[11] << 8));

                if (vendorId != LibUsb.VendorIdTomTom || !IsSupportedProduct(productId))
                {
                    continue;
                }

                byte bus = LibUsb.libusb_get_bus_number(device);
                byte address = LibUsb.libusb_get_device_address(device);

                devices.Add(new UsbDeviceInfo(
                    $"{bus}:{address}:{vendorId:x4}:{productId:x4}",
                    vendorId,
                    productId));
            }
        }
        finally
        {
            if (list != IntPtr.Zero)
            {
                LibUsb.libusb_free_device_list(list, 1);
            }

            if (context != IntPtr.Zero)
            {
                LibUsb.libusb_exit(context);
            }
        }

        return devices;
    }

    /// <summary>
    /// Opens the first supported TomTom watch that is attached to the USB bus.
    /// </summary>
    /// <exception cref="UsbException">No watch was found or it could not be opened.</exception>
    public static UsbConnection Open(ILogger? logger = null)
    {
        IntPtr context = IntPtr.Zero;
        IntPtr list = IntPtr.Zero;
        IntPtr device = IntPtr.Zero;
        IntPtr handle = IntPtr.Zero;
        IntPtr configDescriptor = IntPtr.Zero;
        bool listFreed = false;

        try
        {
            InitializeLibUsb(out context);

            int count = LibUsb.libusb_get_device_list(context, out list);
            if (count < 0)
            {
                LibUsb.Check(count, "Cannot list the USB devices");
            }

            ushort productId = 0;
            byte[] descriptor = new byte[DeviceDescriptorLength];

            for (int index = 0; index < count && productId == 0; index++)
            {
                IntPtr candidate = Marshal.ReadIntPtr(list, index * IntPtr.Size);

                if (LibUsb.libusb_get_device_descriptor(candidate, descriptor) != 0)
                {
                    continue;
                }

                ushort vendorId = (ushort)(descriptor[8] | (descriptor[9] << 8));
                ushort candidateProductId = (ushort)(descriptor[10] | (descriptor[11] << 8));

                if (vendorId == LibUsb.VendorIdTomTom && IsSupportedProduct(candidateProductId))
                {
                    device = candidate;
                    productId = candidateProductId;
                }
            }

            if (device == IntPtr.Zero)
            {
                throw new WatchNotFoundException();
            }

            LibUsb.Check(LibUsb.libusb_open(device, out handle), "Cannot open the TomTom watch");

            // The device stays valid after the list is released, the handle keeps
            // a reference to it.
            LibUsb.libusb_free_device_list(list, 1);
            listFreed = true;

            LibUsb.Check(
                LibUsb.libusb_get_active_config_descriptor(device, out configDescriptor),
                "Cannot read the configuration of the TomTom watch");

            UsbConnection connection = new(
                logger,
                context,
                device,
                handle,
                configDescriptor,
                productId,
                descriptor[15]);

            connection.Prepare();

            return connection;
        }
        catch
        {
            if (configDescriptor != IntPtr.Zero)
            {
                LibUsb.libusb_free_config_descriptor(configDescriptor);
            }

            if (handle != IntPtr.Zero)
            {
                LibUsb.libusb_close(handle);
            }

            if (!listFreed && list != IntPtr.Zero)
            {
                LibUsb.libusb_free_device_list(list, 1);
            }

            if (context != IntPtr.Zero)
            {
                LibUsb.libusb_exit(context);
            }

            throw;
        }
    }

    /// <summary>
    /// Writes a full packet to the watch.
    /// </summary>
    public void WriteFrame(byte[] packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        int transferred;
        int result = WriteTransferType == LibUsb.TransferTypeBulk
            ? LibUsb.libusb_bulk_transfer(
                _handle, WriteEndpointAddress, packet, packet.Length, out transferred, TransferTimeoutMs)
            : LibUsb.libusb_interrupt_transfer(
                _handle, WriteEndpointAddress, packet, packet.Length, out transferred, TransferTimeoutMs);

        if (result != 0)
        {
            throw new UsbException($"USB error while sending packet: {LibUsb.ErrorText(result)}");
        }
    }

    /// <summary>
    /// Reads a full packet from the watch.
    /// </summary>
    public (byte[] Buffer, int Transferred) ReadFrame()
    {
        byte[] buffer = new byte[PacketSize];

        int transferred;
        int result = ReadTransferType == LibUsb.TransferTypeBulk
            ? LibUsb.libusb_bulk_transfer(
                _handle, ReadEndpointAddress, buffer, buffer.Length, out transferred, TransferTimeoutMs)
            : LibUsb.libusb_interrupt_transfer(
                _handle, ReadEndpointAddress, buffer, buffer.Length, out transferred, TransferTimeoutMs);

        if (result != 0)
        {
            throw new UsbException($"USB error while receiving: {LibUsb.ErrorText(result)}");
        }

        return (buffer, transferred);
    }

    /// <summary>
    /// Reads the serial number of the watch. Returns null when it cannot be read.
    /// </summary>
    public string? GetSerialNumber()
    {
        if (SerialNumberIndex == 0)
        {
            return null;
        }

        byte[] buffer = new byte[256];

        int result = LibUsb.libusb_get_string_descriptor_ascii(
            _handle, SerialNumberIndex, buffer, buffer.Length);

        if (result <= 0)
        {
            return null;
        }

        int length = Array.IndexOf(buffer, (byte)0);
        if (length < 0)
        {
            length = result;
        }

        return System.Text.Encoding.ASCII.GetString(buffer, 0, length);
    }

    /// <summary>
    /// Releases the interface, closes the device and shuts libusb down.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_handle != IntPtr.Zero)
        {
            if (_interfaceClaimed)
            {
                LibUsb.libusb_release_interface(_handle, 0);
                _interfaceClaimed = false;
            }

            LibUsb.libusb_close(_handle);
            _handle = IntPtr.Zero;
        }

        if (_configDescriptor != IntPtr.Zero)
        {
            LibUsb.libusb_free_config_descriptor(_configDescriptor);
            _configDescriptor = IntPtr.Zero;
        }

        if (_context != IntPtr.Zero)
        {
            LibUsb.libusb_exit(_context);
            _context = IntPtr.Zero;
        }
    }

    /// <summary>The transfer type of the endpoint the watch is written to.</summary>
    private byte WriteTransferType { get; set; } = LibUsb.TransferTypeInterrupt;

    /// <summary>The transfer type of the endpoint the watch is read from.</summary>
    private byte ReadTransferType { get; set; } = LibUsb.TransferTypeInterrupt;

    private static bool IsSupportedProduct(ushort productId) =>
        productId is ProductIdMultisport or ProductIdSparkMusic or ProductIdSparkCardio;

    /// <summary>
    /// Starts libusb, with a clear message when the library is missing.
    /// </summary>
    private static void InitializeLibUsb(out IntPtr context)
    {
        try
        {
            LibUsb.Check(LibUsb.libusb_init(out context), "Cannot initialize libusb");
        }
        catch (DllNotFoundException exception)
        {
            throw new UsbLibraryException(
                "The libusb library could not be loaded; install it with 'pacman -S libusb' on Arch Linux",
                exception);
        }
    }

    private static (int PacketSize, byte WriteEndpoint, byte ReadEndpoint, int ReadChunk, int WriteChunk)
        ParametersFor(ushort productId) => productId switch
        {
            ProductIdMultisport => (64, 0x05, 0x84, 50, 54),
            _ => (256, 0x02, 0x81, 242, 246),
        };

    /// <summary>
    /// Locates the endpoints of the watch, detaches the kernel driver and claims
    /// the interface.
    /// </summary>
    private void Prepare()
    {
        LibUsb.ConfigDescriptor config =
            Marshal.PtrToStructure<LibUsb.ConfigDescriptor>(_configDescriptor);

        // Detach the kernel driver, otherwise the interface cannot be claimed
        // on Linux. The call is not supported on other platforms.
        for (byte index = 0; index < config.bNumInterfaces; index++)
        {
            try
            {
                int result = LibUsb.libusb_set_auto_detach_kernel_driver(_handle, index);
                if (result != 0 && result != -1)
                {
                    _logger?.LogWarning(
                        "Could not detach the kernel driver of interface {Index}: {Error}",
                        index,
                        LibUsb.ErrorText(result));
                }
            }
            catch (EntryPointNotFoundException)
            {
                _logger?.LogWarning(
                    "The libusb library has no support for detaching kernel drivers; "
                    + "unbind the usbhid driver manually if the watch cannot be claimed");
                break;
            }
            catch (DllNotFoundException)
            {
                throw new UsbLibraryException("The libusb library could not be loaded");
            }
        }

        if (config.Interface == IntPtr.Zero)
        {
            throw new UsbException("USB: device does not expose the expected endpoints");
        }

        LibUsb.Interface usbInterface =
            Marshal.PtrToStructure<LibUsb.Interface>(config.Interface);

        if (usbInterface.AltSetting == IntPtr.Zero || usbInterface.NumAltSetting == 0)
        {
            throw new UsbException("USB: device does not expose the expected endpoints");
        }

        LibUsb.InterfaceDescriptor descriptor =
            Marshal.PtrToStructure<LibUsb.InterfaceDescriptor>(usbInterface.AltSetting);

        bool foundWrite = false;
        bool foundRead = false;

        int endpointSize = Marshal.SizeOf<LibUsb.EndpointDescriptor>();

        for (int index = 0; index < descriptor.bNumEndpoints; index++)
        {
            IntPtr pointer = IntPtr.Add(descriptor.Endpoint, index * endpointSize);
            LibUsb.EndpointDescriptor endpoint =
                Marshal.PtrToStructure<LibUsb.EndpointDescriptor>(pointer);

            if (endpoint.bEndpointAddress == WriteEndpointAddress)
            {
                foundWrite = true;
                WriteTransferType = (byte)(endpoint.bmAttributes & 0x03);
            }
            else if (endpoint.bEndpointAddress == ReadEndpointAddress)
            {
                foundRead = true;
                ReadTransferType = (byte)(endpoint.bmAttributes & 0x03);
            }
        }

        if (!foundWrite || !foundRead)
        {
            throw new UsbException("USB: device does not expose the expected endpoints");
        }

        LibUsb.Check(
            LibUsb.libusb_claim_interface(_handle, 0),
            "Cannot claim the USB interface of the TomTom watch");

        _interfaceClaimed = true;
    }
}
