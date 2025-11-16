using Spectre.Console.Cli;
using System.ComponentModel;

namespace EarthTool.CLI.Commands.WD;

/// <summary>
/// Base settings for WD archive commands (single archive)
/// </summary>
public class WdSettings : CommandSettings
{
  [CommandArgument(0, "<ArchivePath>")]
  [Description("Path to the WD archive file")]
  public string ArchivePath { get; set; }
}

/// <summary>
/// Base settings for WD archive commands (multiple archives)
/// </summary>
public class WdMultiSettings : CommandSettings
{
  [CommandArgument(0, "<ArchivePaths>")]
  [Description("Path(s) to the WD archive file(s)")]
  public string[] ArchivePaths { get; set; }
}

/// <summary>
/// Settings for extract command with filtering options
/// </summary>
public class ExtractSettings : WdMultiSettings
{
  [CommandOption("-o|--output <OutputPath>")]
  [Description("Output directory for extracted files")]
  public string OutputPath { get; set; }

  [CommandOption("-f|--filter <Pattern>")]
  [Description("File pattern filter (e.g., '*.msh', '*.tex')")]
  public string Filter { get; set; }

  [CommandOption("-l|--list <Files>")]
  [Description("Comma-separated list of specific files to extract")]
  public string FileList { get; set; }
}

/// <summary>
/// Settings for list command
/// </summary>
public class ListSettings : WdSettings
{
  [CommandOption("-d|--detailed")]
  [Description("Show detailed information for each file")]
  [DefaultValue(false)]
  public bool Detailed { get; set; }

  [CommandOption("-f|--filter <Pattern>")]
  [Description("File pattern filter (e.g., '*.msh', '*.tex')")]
  public string Filter { get; set; }
}

/// <summary>
/// Settings for create command
/// </summary>
public class CreateSettings : WdSettings
{
  [CommandOption("-i|--input <InputPath>")]
  [Description("Input directory or files to add to archive")]
  public string InputPath { get; set; }

  [CommandOption("--no-compress")]
  [Description("Do not compress files in the archive")]
  [DefaultValue(false)]
  public bool NoCompress { get; set; }

  [CommandOption("-r|--recursive")]
  [Description("Include subdirectories recursively")]
  [DefaultValue(false)]
  public bool Recursive { get; set; }
}

/// <summary>
/// Settings for add command
/// </summary>
public class AddSettings : WdSettings
{
  [CommandArgument(1, "<Files>")]
  [Description("Files to add to the archive (can be multiple)")]
  public string[] Files { get; set; }

  [CommandOption("--no-compress")]
  [Description("Do not compress files being added")]
  [DefaultValue(false)]
  public bool NoCompress { get; set; }

  [CommandOption("-o|--output <OutputPath>")]
  [Description("Output path for modified archive (default: overwrites original)")]
  public string OutputPath { get; set; }
}

/// <summary>
/// Settings for remove command
/// </summary>
public class RemoveSettings : WdSettings
{
  [CommandOption("-f|--filter <Pattern>")]
  [Description("File pattern filter for files to remove (e.g., '*.tmp')")]
  public string Filter { get; set; }

  [CommandOption("-l|--list <Files>")]
  [Description("Comma-separated list of specific files to remove")]
  public string FileList { get; set; }

  [CommandOption("-o|--output <OutputPath>")]
  [Description("Output path for modified archive (default: overwrites original)")]
  public string OutputPath { get; set; }
}
