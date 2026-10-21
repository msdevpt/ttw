namespace Ttw.Services.Usb;

/// <summary>
/// A file stored on the watch.
/// </summary>
/// <param name="FileId">The id of the file.</param>
/// <param name="Length">The length of the file, in bytes.</param>
public sealed record WatchFile(int FileId, int Length);
