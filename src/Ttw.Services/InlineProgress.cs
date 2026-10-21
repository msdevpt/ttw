namespace Ttw.Services;

/// <summary>
/// Reports progress on the thread that reports it, so the order of the
/// messages is preserved.
/// </summary>
/// <typeparam name="T">The type of the progress value.</typeparam>
internal sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
{
    private readonly Action<T> _handler = handler;

    /// <inheritdoc/>
    public void Report(T value) => _handler(value);
}
