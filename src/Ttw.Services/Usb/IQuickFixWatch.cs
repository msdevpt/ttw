namespace Ttw.Services.Usb;

/// <summary>
/// The operations on the watch that the QuickFix update needs.
/// </summary>
public interface IQuickFixWatch
{
    /// <summary>
    /// Sends the sequence of commands the watch expects right after the
    /// connection has been opened.
    /// </summary>
    void SendStartupSequence();

    /// <summary>
    /// Reads a preference from the watch.
    /// </summary>
    /// <returns>The value of the preference, or null when it is not present.</returns>
    string? GetPreference(string tag);

    /// <summary>
    /// Writes a file to the watch.
    /// </summary>
    void WriteFile(int fileId, byte[] data);

    /// <summary>
    /// Reads a file from the watch.
    /// </summary>
    byte[] ReadFile(int fileId);

    /// <summary>
    /// Resets the GPS processor, so it reloads the QuickFix data.
    /// </summary>
    void ResetGpsProcessor();
}
