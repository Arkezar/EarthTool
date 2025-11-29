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

public class ItemSettings : CommandSettings
{
  [CommandArgument(0, "<ParFilePath>")]
  [Description("Path to the PAR file")]
  public string ParFilePath { get; set; }

  [CommandArgument(1, "<ItemName>")]
  [Description("Name of the item to display (supports partial matching)")]
  public string ItemName { get; set; }

  [CommandOption("--exact")]
  [Description("Use exact name matching instead of partial matching")]
  [DefaultValue(false)]
  public bool ExactMatch { get; set; }
}
