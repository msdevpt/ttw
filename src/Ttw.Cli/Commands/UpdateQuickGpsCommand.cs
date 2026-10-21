using System.CommandLine;
using Microsoft.Extensions.Logging;
using Ttw.Cli;
using Ttw.Services.Http;
using Ttw.Services.QuickFix;
using Ttw.Services.Usb;

namespace Ttw.Cli.Commands;

/// <summary>
/// The command that updates the GPS QuickFix data of the watch.
/// </summary>
public static class UpdateQuickGpsCommand
{
    /// <summary>
    /// Creates the command.
    /// </summary>
    public static Command Create(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Command command = new("update", "Update the GPS QuickFix data on the watch");

        command.SetAction(async (ParseResult _, CancellationToken cancellationToken) =>
        {
            ILogger logger = loggerFactory.CreateLogger("ttw.update");

            try
            {
                using UsbConnection connection = UsbConnection.Open(logger);
                using WatchClient watch = new(connection);

                QuickFixUpdater updater = new(watch, new HttpDataFetcher(), logger);

                QuickFixResult result = await updater
                    .UpdateAsync(new ConsoleProgressReporter(logger), cancellationToken)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    Console.Error.WriteLine(result.Error ?? "The QuickFix update failed");
                    return 1;
                }

                return 0;
            }
            catch (Exception exception) when (
                exception is UsbException or HttpRequestException or IOException or OperationCanceledException)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        });

        return command;
    }
}
