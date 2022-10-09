using Spectre.Console.Cli;
using System.ComponentModel;

namespace EarthTool.CLI.Commands;

public class CommonSettings : CommandSettings
{
  [CommandArgument(0, "<InputFilePath>")]
  [Description("Input file to process.")]
  public string InputFilePath { get; set; }
    
  [CommandOption("-o|--output [OutputFolderPath]")]
  [Description("Output directory. Current if not specified.")]
  public FlagValue<string> OutputFolderPath { get; set; }
}