namespace Ttw.Services.Activities;

/// <summary>
/// Raised when the activities could not be extracted from the watch.
/// </summary>
public sealed class ExtractionException : Exception
{
    /// <summary>Creates an exception with a message.</summary>
    public ExtractionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and an inner exception.</summary>
    public ExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
