namespace Ttw.Services.Failures;

/// <summary>
/// Builds the technical detail of a failure, for the verbose output. The
/// detail names the type of the failure, its message, the cause it wraps and
/// the URL that was being fetched. The stack trace stays in the debug logs.
/// </summary>
public static class TechnicalDetail
{
    /// <summary>
    /// Returns the technical detail of a failure.
    /// </summary>
    /// <param name="exception">The failure.</param>
    /// <param name="url">The URL that was being fetched, when there is one.</param>
    public static string From(Exception exception, string? url = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        List<string> lines = [Describe(exception)];

        for (Exception? cause = exception.InnerException; cause is not null; cause = cause.InnerException)
        {
            lines.Add("Caused by: " + Describe(cause));
        }

        if (url is not null)
        {
            lines.Add("URL: " + url);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Describe(Exception exception) =>
        $"{exception.GetType().FullName}: {exception.Message}";
}
