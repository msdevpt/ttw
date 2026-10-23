using System.CommandLine;
using Ttw.Cli.Commands;

Option<bool> verbose = new("--verbose")
{
    Description = "Show the technical detail of a failure and the debug logs",
    Recursive = true,
};

RootCommand root = new("ttw - TomTom sports watch command line tool");
root.Add(verbose);
root.Add(UpdateQuickGpsCommand.Create(verbose));
root.Add(ExtractActivitiesCommand.Create(verbose));

return await root.Parse(args).InvokeAsync();
