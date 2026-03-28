using System.CommandLine;
using FuzzySat.CLI.Commands;

var rootCommand = new RootCommand("FuzzySat - Fuzzy logic satellite image classifier");
rootCommand.Subcommands.Add(ClassifyCommand.Create());
rootCommand.Subcommands.Add(TrainCommand.Create());
rootCommand.Subcommands.Add(ValidateCommand.Create());
rootCommand.Subcommands.Add(InfoCommand.Create());

return rootCommand.Parse(args).Invoke();
