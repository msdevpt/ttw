namespace Ttw.Services.Usb;

/// <summary>
/// The message codes and the framing of the watch protocol.
/// </summary>
public static class WatchProtocol
{
    /// <summary>Marker of a request frame.</summary>
    public const byte RequestMarker = 0x09;

    /// <summary>Marker of a response frame.</summary>
    public const byte ResponseMarker = 0x01;

    /// <summary>Open a file for writing.</summary>
    public const byte OpenFileWrite = 0x02;

    /// <summary>Delete a file.</summary>
    public const byte DeleteFile = 0x03;

    /// <summary>Write a block of data to a file.</summary>
    public const byte WriteFileData = 0x04;

    /// <summary>Get the size of a file.</summary>
    public const byte GetFileSize = 0x05;

    /// <summary>Open a file for reading.</summary>
    public const byte OpenFileRead = 0x06;

    /// <summary>Request a block of file data.</summary>
    public const byte ReadFileDataRequest = 0x07;

    /// <summary>Response with a block of file data.</summary>
    public const byte ReadFileDataResponse = 0x09;

    /// <summary>Close the file search.</summary>
    public const byte FindClose = 0x0a;

    /// <summary>Close a file.</summary>
    public const byte CloseFile = 0x0c;

    /// <summary>Unknown command, part of the startup sequence.</summary>
    public const byte Unknown0d = 0x0d;

    /// <summary>Format the watch.</summary>
    public const byte FormatWatch = 0x0e;

    /// <summary>Reset the watch.</summary>
    public const byte ResetDevice = 0x10;

    /// <summary>Look up the first file.</summary>
    public const byte FindFirstFile = 0x11;

    /// <summary>Look up the next file.</summary>
    public const byte FindNextFile = 0x12;

    /// <summary>Get the current time of the watch.</summary>
    public const byte GetCurrentTime = 0x14;

    /// <summary>Unknown command.</summary>
    public const byte Unknown1a = 0x1a;

    /// <summary>Reset the GPS processor.</summary>
    public const byte ResetGpsProcessor = 0x1d;

    /// <summary>Unknown command, part of the startup sequence.</summary>
    public const byte Unknown1f = 0x1f;

    /// <summary>Get the product id.</summary>
    public const byte GetProductId = 0x20;

    /// <summary>Get the firmware version.</summary>
    public const byte GetFirmwareVersion = 0x21;

    /// <summary>Unknown command, part of the startup sequence.</summary>
    public const byte Unknown22 = 0x22;

    /// <summary>Unknown command.</summary>
    public const byte Unknown23 = 0x23;

    /// <summary>Get the Bluetooth Low Energy version.</summary>
    public const byte GetBleVersion = 0x28;

    /// <summary>
    /// Builds a request frame. The frame is padded to the packet size of the
    /// watch; the payload is copied to offset four.
    /// </summary>
    public static byte[] BuildRequest(byte messageType, byte[] payload, byte counter, int packetSize)
    {
        ArgumentNullException.ThrowIfNull(payload);

        byte[] packet = new byte[packetSize];
        packet[0] = RequestMarker;
        packet[1] = (byte)((payload.Length + 2) & 0xFF);
        packet[2] = counter;
        packet[3] = messageType;

        Buffer.BlockCopy(payload, 0, packet, 4, payload.Length);

        return packet;
    }

    /// <summary>
    /// Validates a response frame and returns its payload.
    /// </summary>
    /// <param name="buffer">The buffer the response was read into.</param>
    /// <param name="transferred">The number of bytes that were read.</param>
    /// <param name="expectedCounter">The counter of the request.</param>
    /// <param name="expectedMessageType">The message type the response must carry.</param>
    /// <param name="expectedPayloadLength">The expected payload length, or null when it may vary.</param>
    /// <param name="payload">The payload of the response.</param>
    /// <param name="error">The reason the response was rejected, or null.</param>
    public static bool TryParseResponse(
        byte[] buffer,
        int transferred,
        byte expectedCounter,
        byte expectedMessageType,
        int? expectedPayloadLength,
        out byte[] payload,
        out string? error)
    {
        payload = [];
        error = null;

        if (transferred < 4 || buffer[0] != ResponseMarker)
        {
            error = "USB: Invalid response";
            return false;
        }

        int payloadLength = buffer[1] - 2;

        if (expectedPayloadLength.HasValue && payloadLength != expectedPayloadLength.Value)
        {
            error = "USB: Invalid response length";
            return false;
        }

        if (buffer[2] != expectedCounter)
        {
            error = "USB: Response not synchonized with the request";
            return false;
        }

        if (buffer[3] != expectedMessageType)
        {
            error = "USB: Wrong response type";
            return false;
        }

        if (payloadLength < 0 || 4 + payloadLength > transferred)
        {
            error = "USB: Invalid response length";
            return false;
        }

        payload = new byte[payloadLength];
        Buffer.BlockCopy(buffer, 4, payload, 0, payloadLength);

        return true;
    }
}
