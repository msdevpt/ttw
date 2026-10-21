using Microsoft.Extensions.Logging;
using Ttw.Services.QuickFix;

namespace Ttw.Cli;

/// <summary>
/// Reports the progress of the QuickFix update on the console.
/// </summary>
public sealed class ConsoleProgressReporter : IProgress<QuickFixProgress>
{
    private readonly ILogger _logger;
    private string? _phase;

    /// <summary>
    /// Creates the reporter.
    /// </summary>
    public ConsoleProgressReporter(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Report(QuickFixProgress value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Phase != _phase)
        {
            if (_phase == QuickFixPhase.Downloading)
            {
                // Terminate the line the byte counter was written to.
                Console.WriteLine();
            }

            _phase = value.Phase;
            _logger.LogInformation("{Phase}", value.Phase);
        }

        if (value.Phase == QuickFixPhase.Downloading && value.DoneBytes > 0)
        {
            string text = value.TotalBytes >= 0
                ? $"Downloading quickfix data: {value.DoneBytes}/{value.TotalBytes} bytes"
                : $"Downloading quickfix data: {value.DoneBytes} bytes";

            Console.Write($"\r{text}");
        }
    }
}
