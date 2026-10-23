using System.Text;
using System.Text.RegularExpressions;

namespace Ttw.Services.Usb;

/// <summary>
/// The file operations the watch protocol offers, on top of a
/// <see cref="UsbConnection"/>.
/// </summary>
public sealed class WatchClient : IQuickFixWatch, IActivityWatch, IDisposable
{
    /// <summary>File id of the GPS QuickFix data.</summary>
    public const int FileIdQuickFixData = 0x00010100;

    /// <summary>File id of the preferences of the watch.</summary>
    public const int FileIdPreferencesXml = 0x00f20000;

    /// <summary>File type of the activity data.</summary>
    public const int FileTypeTtbinData = 0x00910000;

    /// <summary>Mask that selects the file type of a file id.</summary>
    public const uint FileTypeMask = 0xFFFF0000;

    /// <summary>Expected payload length for a response of unknown length.</summary>
    public const int VariableLength = -1;

    /// <summary>
    /// The preferences that are written to a watch that has none, which is the
    /// case after a factory reset.
    /// </summary>
    private const string DefaultPreferences =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
        + "<preferences version=\"1\" modified=\"\">\n"
        + "    <watchName>TomTom GPS Watch</watchName>\n"
        + "    <ConfigURL>https://mysports.tomtom.com/service/config/config.json</ConfigURL>\n"
        + "    <exporters>\n"
        + "        <offline>\n"
        + "        </offline>\n"
        + "    </exporters>\n"
        + "</preferences>";

    private readonly UsbConnection _connection;
    private byte _counter;

    /// <summary>
    /// Creates a client for the given connection.
    /// </summary>
    public WatchClient(UsbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    /// <summary>
    /// Sends a request to the watch and returns the payload of the response.
    /// </summary>
    /// <param name="messageType">The message type of the request.</param>
    /// <param name="payload">The payload of the request.</param>
    /// <param name="expectedMessageType">The message type the response must carry.</param>
    /// <param name="expectedPayloadLength">The expected length of the payload of the
    /// response, or <see cref="VariableLength"/> when it may vary.</param>
    public byte[] SendRequest(
        byte messageType,
        byte[] payload,
        byte expectedMessageType,
        int expectedPayloadLength)
    {
        byte counter = _counter;
        _counter = unchecked((byte)(_counter + 1));

        byte[] packet = WatchProtocol.BuildRequest(messageType, payload, counter, _connection.PacketSize);
        _connection.WriteFrame(packet);

        (byte[] buffer, int transferred) = _connection.ReadFrame();

        bool valid = WatchProtocol.TryParseResponse(
            buffer,
            transferred,
            counter,
            expectedMessageType,
            expectedPayloadLength == VariableLength ? null : expectedPayloadLength,
            out byte[] response,
            out string? error);

        if (!valid)
        {
            throw new UsbException(error ?? "USB: Invalid response");
        }

        return response;
    }

    /// <summary>
    /// Sends the sequence of commands the watch expects right after the
    /// connection has been opened.
    /// </summary>
    public void SendStartupSequence()
    {
        (byte MessageType, int PayloadLength)[] sequence =
        [
            (WatchProtocol.Unknown0d, 20),
            (WatchProtocol.FindClose, 0),
            (WatchProtocol.Unknown22, 1),
            (WatchProtocol.Unknown22, 1),
            (WatchProtocol.GetProductId, 4),
            (WatchProtocol.FindClose, 0),
            (WatchProtocol.GetBleVersion, 4),
            (WatchProtocol.GetBleVersion, 4),
            (WatchProtocol.Unknown1f, 4),
        ];

        foreach ((byte messageType, int payloadLength) in sequence)
        {
            SendRequest(messageType, [], messageType, payloadLength);
        }
    }

    /// <summary>
    /// Lists the files of the given type that are stored on the watch.
    /// </summary>
    public IReadOnlyList<WatchFile> GetFileList(int fileType)
    {
        List<WatchFile> files = [];

        bool isFirst = true;

        while (true)
        {
            byte messageType = isFirst ? WatchProtocol.FindFirstFile : WatchProtocol.FindNextFile;
            byte[] payload = isFirst ? new byte[8] : [];
            isFirst = false;

            byte[] response = SendRequest(messageType, payload, messageType, 20);

            int fileId = ReadInt(response, 4);
            int length = ReadInt(response, 12);
            bool endOfList = ReadInt(response, 16) != 0;

            if (!endOfList && (fileId & unchecked((int)FileTypeMask)) == fileType)
            {
                files.Add(new WatchFile(fileId, length));
            }

            if (endOfList)
            {
                break;
            }

            if (files.Count > 10000)
            {
                throw new UsbException("The watch returned an endless file list");
            }
        }

        return files;
    }

    /// <summary>
    /// Reads a file from the watch.
    /// </summary>
    public byte[] ReadFile(int fileId)
    {
        byte[] idPayload = ToBigEndian(fileId);

        byte[] openResponse = SendRequest(
            WatchProtocol.OpenFileRead, idPayload, WatchProtocol.OpenFileRead, 20);

        CheckFileOperationResult(openResponse, fileId, "opening");

        try
        {
            byte[] sizeResponse = SendRequest(
                WatchProtocol.GetFileSize, idPayload, WatchProtocol.GetFileSize, 20);

            if (ReadInt(sizeResponse, 4) != fileId)
            {
                throw new UsbException($"Error reading the size of file 0x{fileId:x8} from the watch");
            }

            int size = ReadInt(sizeResponse, 12);

            if (size < 0)
            {
                throw new UsbException($"Error reading the size of file 0x{fileId:x8} from the watch");
            }

            byte[] data = new byte[size];
            int offset = 0;

            while (offset < size)
            {
                int toRead = Math.Min(_connection.ReadChunkSize, size - offset);

                byte[] request = new byte[8];
                Buffer.BlockCopy(idPayload, 0, request, 0, 4);
                Buffer.BlockCopy(ToBigEndian(toRead), 0, request, 4, 4);

                byte[] response = SendRequest(
                    WatchProtocol.ReadFileDataRequest,
                    request,
                    WatchProtocol.ReadFileDataResponse,
                    toRead + 8);

                int responseId = ReadInt(response, 0);
                int bytesRead = ReadInt(response, 4);

                if (responseId != fileId || bytesRead != toRead)
                {
                    throw new UsbException($"Error reading file 0x{fileId:x8} from the watch");
                }

                Buffer.BlockCopy(response, 8, data, offset, bytesRead);
                offset += bytesRead;
            }

            return data;
        }
        finally
        {
            CloseFile(fileId);
        }
    }

    /// <summary>
    /// Writes a file to the watch.
    /// </summary>
    public void WriteFile(int fileId, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        byte[] idPayload = ToBigEndian(fileId);

        byte[] openResponse = SendRequest(
            WatchProtocol.OpenFileWrite, idPayload, WatchProtocol.OpenFileWrite, 20);

        CheckFileOperationResult(openResponse, fileId, "opening");

        try
        {
            int offset = 0;

            while (offset < data.Length)
            {
                int toWrite = Math.Min(_connection.WriteChunkSize, data.Length - offset);

                byte[] payload = new byte[4 + toWrite];
                Buffer.BlockCopy(idPayload, 0, payload, 0, 4);
                Buffer.BlockCopy(data, offset, payload, 4, toWrite);

                byte[] response = SendRequest(
                    WatchProtocol.WriteFileData, payload, WatchProtocol.WriteFileData, 20);

                if (ReadInt(response, 4) != fileId)
                {
                    throw new UsbException($"Error writing file 0x{fileId:x8} to the watch");
                }

                offset += toWrite;
            }
        }
        finally
        {
            CloseFile(fileId);
        }
    }

    /// <summary>
    /// Reads a preference from the preferences file of the watch. When the watch
    /// has no preferences file, the default one is written first.
    /// </summary>
    /// <returns>The value of the preference, or null when it is not present.</returns>
    public string? GetPreference(string tag)
    {
        byte[] data;

        try
        {
            data = ReadFile(FileIdPreferencesXml);
        }
        catch (UsbException)
        {
            WriteFile(FileIdPreferencesXml, Encoding.Latin1.GetBytes(DefaultPreferences));
            data = ReadFile(FileIdPreferencesXml);
        }

        string xml = Encoding.Latin1.GetString(data);

        Match match = Regex.Match(xml, "<" + tag + ">(.+?)</" + tag + ">");

        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Resets the GPS processor, so it reloads the QuickFix data.
    /// </summary>
    public void ResetGpsProcessor()
    {
        SendRequest(WatchProtocol.ResetGpsProcessor, [], WatchProtocol.ResetGpsProcessor, VariableLength);
    }

    /// <summary>
    /// Returns the serial number of the watch, or null when it cannot be read.
    /// </summary>
    public string? GetDeviceSerialNumber() => _connection.GetSerialNumber();

    /// <summary>
    /// Releases the connection to the watch.
    /// </summary>
    public void Dispose() => _connection.Dispose();

    private void CloseFile(int fileId)
    {
        byte[] idPayload = ToBigEndian(fileId);
        byte[] response = SendRequest(WatchProtocol.CloseFile, idPayload, WatchProtocol.CloseFile, 20);
        CheckFileOperationResult(response, fileId, "closing");
    }

    private static void CheckFileOperationResult(byte[] response, int fileId, string operation)
    {
        if (response.Length < 20)
        {
            throw new UsbException($"Error {operation} file 0x{fileId:x8} on the watch");
        }

        int error = ReadInt(response, 16);

        if (error != 0)
        {
            throw new UsbException($"Error {operation} file 0x{fileId:x8} on the watch");
        }
    }

    private static byte[] ToBigEndian(int value) =>
    [
        (byte)((value >> 24) & 0xFF),
        (byte)((value >> 16) & 0xFF),
        (byte)((value >> 8) & 0xFF),
        (byte)(value & 0xFF),
    ];

    private static int ReadInt(byte[] data, int offset) =>
        (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
}
