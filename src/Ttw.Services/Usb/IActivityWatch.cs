namespace Ttw.Services.Usb;

/// <summary>
/// The operations on the watch that the activity extraction needs.
/// </summary>
public interface IActivityWatch
{
    /// <summary>
    /// Sends the sequence of commands the watch expects right after the
    /// connection has been opened.
    /// </summary>
    void SendStartupSequence();

    /// <summary>
    /// Lists the files of the given type that are stored on the watch.
    /// </summary>
    IReadOnlyList<WatchFile> GetFileList(int fileType);

    /// <summary>
    /// Reads a file from the watch.
    /// </summary>
    byte[] ReadFile(int fileId);

    /// <summary>
    /// Reads a preference from the preferences file of the watch.
    /// </summary>
    /// <returns>The value of the preference, or null when it is not present.</returns>
    string? GetPreference(string tag);

    /// <summary>
    /// Returns the serial number of the watch, or null when it cannot be read.
    /// </summary>
    string? GetDeviceSerialNumber();
}
