using Spectre.Console.Cli;

namespace EarthTool.CLI.Commands.WD;

/// <summary>
/// Base class for WD archive commands
/// </summary>
public abstract class WdCommandBase<TSettings> : Command<TSettings>
  where TSettings : WdSettings
{
}