using System.CommandLine;
using Microsoft.Extensions.Logging;
using Ttw.Core.Dates;
using Ttw.Core.Exporting;
using Ttw.Services.Activities;
using Ttw.Services.Usb;

namespace Ttw.Cli.Commands;

/// <summary>
/// The command that extracts the activities of a date range from the watch.
/// </summary>
public static class ExtractActivitiesCommand
{
    /// <summary>
    /// Creates the command.
    /// </summary>
    public static Command Create(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Option<string> from = new("--from")
        {
            Required = true,
            Description = "First day of the period (yyyy-MM-dd)",
        };

        Option<string> to = new("--to")
        {
            Required = true,
            Description = "Last day of the period (yyyy-MM-dd)",
        };

        Option<string> output = new("--output", "-o")
        {
            Description = "Directory the activity files are written to",
        };

        Option<string> format = new("--format")
        {
            DefaultValueFactory = _ => "gpx",
            Description = "Export format: gpx, tcx, ttbin or csv",
        };

        Command command = new("activities", "Extract the activities of a date range from the watch");
        command.Add(from);
        command.Add(to);
        command.Add(output);
        command.Add(format);

        command.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            ILogger logger = loggerFactory.CreateLogger("ttw.activities");

            string fromValue = parseResult.GetValue(from) ?? string.Empty;
            string toValue = parseResult.GetValue(to) ?? string.Empty;

            if (!DateRange.TryCreate(fromValue, toValue, out DateRange range, out DateRangeError error))
            {
                switch (error)
                {
                    case DateRangeError.InvalidFrom:
                        Console.Error.WriteLine(
                            $"Invalid value '{fromValue}' for option '--from'. Expected format yyyy-MM-dd.");
                        break;
                    case DateRangeError.InvalidTo:
                        Console.Error.WriteLine(
                            $"Invalid value '{toValue}' for option '--to'. Expected format yyyy-MM-dd.");
                        break;
                    default:
                        Console.Error.WriteLine(
                            "Invalid date range: --from must be less than or equal to --to.");
                        break;
                }

                return 1;
            }

            string formatValue = parseResult.GetValue(format) ?? "gpx";

            if (!ExportFormats.TryParse(formatValue, out ExportFormat exportFormat))
            {
                Console.Error.WriteLine(
                    $"Invalid value '{formatValue}' for option '--format'. Expected one of: gpx, tcx, ttbin, csv.");
                return 1;
            }

            string outputDirectory = parseResult.GetValue(output) ?? Directory.GetCurrentDirectory();

            try
            {
                using UsbConnection connection = UsbConnection.Open(logger);
                using WatchClient watch = new(connection);

                ActivityExtractionService service = new(watch, logger);

                ExtractionResult result = await service
                    .ExtractAsync(range, exportFormat, outputDirectory, cancellationToken)
                    .ConfigureAwait(false);

                if (result.SkippedFiles.Count > 0)
                {
                    logger.LogWarning(
                        "Skipped {Count} undecodable file(s): {Files}",
                        result.SkippedFiles.Count,
                        string.Join(", ", result.SkippedFiles));
                }

                if (result.Activities.Count == 0)
                {
                    logger.LogInformation(
                        "No activities found between {From} and {To}",
                        range.From.ToString("yyyy-MM-dd"),
                        range.To.ToString("yyyy-MM-dd"));
                    return 0;
                }

                logger.LogInformation(
                    "Extracted {Count} activity/activities to {Directory}",
                    result.Activities.Count,
                    outputDirectory);

                return 0;
            }
            catch (Exception exception) when (
                exception is ExtractionException or UsbException or IOException or OperationCanceledException)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        });

        return command;
    }
}
