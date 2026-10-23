using Microsoft.Extensions.Logging;

namespace Ttw.Tests;

/// <summary>
/// A logger that records the log entries, so a test can check what was
/// written and at which level.
/// </summary>
internal sealed class RecordingLogger : ILogger
{
    /// <summary>A recorded log entry.</summary>
    /// <param name="Level">The level of the entry.</param>
    /// <param name="Message">The formatted message of the entry.</param>
    /// <param name="Exception">The exception the entry carries, if any.</param>
    public sealed record Entry(LogLevel Level, string Message, Exception? Exception);

    private readonly List<Entry> _entries = [];

    /// <summary>The recorded entries.</summary>
    public IReadOnlyList<Entry> Entries => _entries;

    /// <summary>The entries that carry an exception, at the given level or above.</summary>
    public IEnumerable<Entry> EntriesWithException(LogLevel level) =>
        _entries.Where(entry => entry.Exception is not null && entry.Level >= level);

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc/>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new Entry(logLevel, formatter(state, exception), exception));
    }
}
