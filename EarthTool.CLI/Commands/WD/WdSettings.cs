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
/// Settings for info command
/// </summary>
public class InfoSettings : WdSettings
{
  [CommandOption("--timestamp-only")]
  [Description("Output only the archive LastModification timestamp (Windows FileTime)")]
  [DefaultValue(false)]
  public bool TimestampOnly { get; set; }

  [CommandOption("--guid-only")]
  [Description("Output only the archive Guid identifier")]
  [DefaultValue(false)]
  public bool GuidOnly { get; set; }
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

  [CommandOption("--names-only")]
  [Description("Output only file names, one per line")]
  [DefaultValue(false)]
  public bool NamesOnly { get; set; }
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

  [CommandOption("--timestamp <Timestamp>")]
  [Description("Set archive last modification timestamp (format: yyyy-MM-dd HH:mm:ss, unix epoch, or filetime)")]
  public string Timestamp { get; set; }

  [CommandOption("--guid <Guid>")]
  [Description("Set archive GUID identifier (format: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX)")]
  public string Guid { get; set; }

  [CommandOption("-b|--base-dir <BaseDir>")]
  [Description("Base directory for relative paths (overrides default behavior)")]
  public string BaseDir { get; set; }
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

  [CommandOption("-b|--base-dir <BaseDir>")]
  [Description("Base directory for relative paths (default: parent directory of each file)")]
  public string BaseDir { get; set; }

  [CommandOption("--preserve-timestamp")]
  [Description("Preserve the original archive timestamp (don't update to current time)")]
  [DefaultValue(false)]
  public bool PreserveTimestamp { get; set; }
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
