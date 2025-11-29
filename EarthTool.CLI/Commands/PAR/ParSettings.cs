using Spectre.Console.Cli;
using System;
using System.ComponentModel;

namespace EarthTool.CLI.Commands.PAR;

public class ParSettings : CommonSettings
{
  [CommandOption("--guid <guid>")]
  [Description("GUID to assign when converting JSON to PAR.")]
  public Guid? Guid { get; set; }
}
