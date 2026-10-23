using Microsoft.Extensions.Logging;

namespace Ttw.Cli;

/// <summary>
/// Creates the logger factory of the command line tool.
/// </summary>
public static class CliLogging
{
    /// <summary>
    /// Creates a logger factory. In verbose mode the logs are written at the
    /// debug level, so the technical detail of a failure is available.
    /// </summary>
    /// <param name="verbose">True when the verbose flag was given.</param>
    public static ILoggerFactory CreateFactory(bool verbose)
    {
        return LoggerFactory.Create(builder => builder
            .AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            })
            .SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information));
    }
}
