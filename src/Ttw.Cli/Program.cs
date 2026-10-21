using System.CommandLine;
using Microsoft.Extensions.Logging;
using Ttw.Cli.Commands;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder
    .AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    })
    .SetMinimumLevel(LogLevel.Information));

RootCommand root = new("ttw - TomTom sports watch command line tool");
root.Add(UpdateQuickGpsCommand.Create(loggerFactory));
root.Add(ExtractActivitiesCommand.Create(loggerFactory));

return await root.Parse(args).InvokeAsync();
