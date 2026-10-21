namespace Ttw.Tests;

/// <summary>
/// Access to the reference files of the original Java implementation.
/// </summary>
internal static class TestResources
{
    /// <summary>Returns the path of a reference file.</summary>
    public static string Path(string name) => System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", name);

    /// <summary>Reads a reference file.</summary>
    public static byte[] ReadBytes(string name) => File.ReadAllBytes(Path(name));

    /// <summary>Reads a reference file as text.</summary>
    public static string ReadText(string name) => File.ReadAllText(Path(name));
}
