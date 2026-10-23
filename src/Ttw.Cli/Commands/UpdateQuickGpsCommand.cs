using System.CommandLine;
using Microsoft.Extensions.Logging;
using Ttw.Cli;
using Ttw.Services.Failures;
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
    public static Command Create(Option<bool> verbose)
    {
        ArgumentNullException.ThrowIfNull(verbose);

        Command command = new("update", "Update the GPS QuickFix data on the watch");

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            bool isVerbose = parseResult.GetValue(verbose);

            using ILoggerFactory loggerFactory = CliLogging.CreateFactory(isVerbose);
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

                    if (isVerbose && result.Detail is not null)
                    {
                        Console.Error.WriteLine(result.Detail);
                    }

                    return 1;
                }

                return 0;
            }
            catch (Exception exception) when (
                exception is UsbException or HttpRequestException or IOException or OperationCanceledException)
            {
                Console.Error.WriteLine(FailureMessages.For(exception));

                if (isVerbose)
                {
                    Console.Error.WriteLine(TechnicalDetail.From(exception));
                }

                return 1;
            }
        });

        return command;
    }
}
