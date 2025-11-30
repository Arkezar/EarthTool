using EarthTool.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace EarthTool.CLI.Commands.WD;

public sealed class CreateCommand : WdCommandBase<CreateSettings>
{
  private readonly IArchiver _archiver;

  public CreateCommand(IArchiver archiver)
  {
    _archiver = archiver;
  }

  public override int Execute(CommandContext context, CreateSettings settings, CancellationToken cancellationToken)
  {
    if (string.IsNullOrEmpty(settings.InputPath))
    {
      AnsiConsole.MarkupLine("[red]Input path is required. Use -i or --input option.[/]");
      return 1;
    }

    if (File.Exists(settings.ArchivePath))
    {
      if (!AnsiConsole.Confirm($"Archive [yellow]{settings.ArchivePath}[/] already exists. Overwrite?"))
      {
        AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
        return 0;
      }
    }

    // Parse timestamp if provided
    DateTime? customTimestamp = null;
    if (!string.IsNullOrEmpty(settings.Timestamp))
    {
      if (long.TryParse(settings.Timestamp, out long timestampValue))
      {
        // Try to determine if this is a FileTime or Unix epoch
        // FileTime is much larger (> 100 billion for dates after 1970)
        // Unix epoch is smaller (< 3 billion for dates before 2065)
        if (timestampValue > 10000000000L) // Likely FileTime (> ~1970 in FileTime units)
        {
          // Windows FileTime format
          customTimestamp = DateTime.FromFileTimeUtc(timestampValue);
          AnsiConsole.MarkupLine($"[dim]Using FileTime timestamp: {customTimestamp:yyyy-MM-dd HH:mm:ss.fff}[/]");
        }
        else
        {
          // Unix epoch timestamp
          customTimestamp = DateTimeOffset.FromUnixTimeSeconds(timestampValue).UtcDateTime;
          AnsiConsole.MarkupLine($"[dim]Using Unix timestamp: {customTimestamp:yyyy-MM-dd HH:mm:ss}[/]");
        }
      }
      else if (DateTime.TryParse(settings.Timestamp, out DateTime parsedDateTime))
      {
        // DateTime string format
        customTimestamp = parsedDateTime;
        AnsiConsole.MarkupLine($"[dim]Using custom timestamp: {customTimestamp:yyyy-MM-dd HH:mm:ss}[/]");
      }
      else
      {
        AnsiConsole.MarkupLine($"[red]Invalid timestamp format: {settings.Timestamp}[/]");
        AnsiConsole.MarkupLine($"[yellow]Use format: yyyy-MM-dd HH:mm:ss, unix epoch, or Windows FileTime[/]");
        return 1;
      }
    }

    // Parse GUID if provided
    Guid? customGuid = null;
    if (!string.IsNullOrEmpty(settings.Guid))
    {
      if (System.Guid.TryParse(settings.Guid, out Guid parsedGuid))
      {
        customGuid = parsedGuid;
        AnsiConsole.MarkupLine($"[dim]Using custom GUID: {customGuid}[/]");
      }
      else
      {
        AnsiConsole.MarkupLine($"[red]Invalid GUID format: {settings.Guid}[/]");
        AnsiConsole.MarkupLine($"[yellow]Use format: XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX[/]");
        return 1;
      }
    }

    // Create archive with appropriate parameters
    IArchive archive;
    if (customTimestamp.HasValue && customGuid.HasValue)
    {
      archive = _archiver.CreateArchive(customTimestamp.Value, customGuid.Value);
    }
    else if (customTimestamp.HasValue)
    {
      archive = _archiver.CreateArchive(customTimestamp.Value);
    }
    else
    {
      archive = _archiver.CreateArchive();
    }
    var compress = !settings.NoCompress;

    try
    {
      string[] filesToAdd;

      if (Directory.Exists(settings.InputPath))
      {
        // Add files from directory
        var searchOption = settings.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        filesToAdd = Directory.GetFiles(settings.InputPath, "*", searchOption);

        if (!filesToAdd.Any())
        {
          AnsiConsole.MarkupLine($"[yellow]No files found in directory: {settings.InputPath}[/]");
          return 0;
        }
      }
      else if (File.Exists(settings.InputPath))
      {
        // Add single file
        filesToAdd = new[] { settings.InputPath };
      }
      else
      {
        AnsiConsole.MarkupLine($"[red]Input path does not exist: {settings.InputPath}[/]");
        return 1;
      }

      AnsiConsole.MarkupLine($"[green]Creating archive: {settings.ArchivePath}[/]");
      AnsiConsole.MarkupLine($"[dim]Compression: {(compress ? "enabled" : "disabled")}[/]");

      // Determine base directory for preserving structure
      string baseDir;
      if (!string.IsNullOrEmpty(settings.BaseDir))
      {
        // Use explicitly provided base directory
        baseDir = Path.GetFullPath(settings.BaseDir);
      }
      else if (settings.InputPath == ".")
      {
        // Use current directory itself as base (files will be at root of archive)
        baseDir = Path.GetFullPath(settings.InputPath);
      }
      else if (Directory.Exists(settings.InputPath))
      {
        // Use parent directory to include folder name in paths
        baseDir = Path.GetDirectoryName(Path.GetFullPath(settings.InputPath));
      }
      else
      {
        baseDir = Path.GetDirectoryName(settings.InputPath);
      }

      var added = 0;
      foreach (var file in filesToAdd)
      {
        try
        {
          _archiver.AddFile(archive, file, baseDir, compress);
          added++;

          // Show relative path for better visibility
          var displayName = baseDir != null
            ? Path.GetRelativePath(baseDir, file)
            : Path.GetFileName(file);
          AnsiConsole.MarkupLine($"[dim]  Added: {displayName}[/]");
        }
        catch (Exception ex)
        {
          AnsiConsole.MarkupLine($"[red]  Failed to add {Path.GetFileName(file)}: {ex.Message}[/]");
        }
      }

      if (added == 0)
      {
        AnsiConsole.MarkupLine("[yellow]No files were added to the archive[/]");
        return 1;
      }

      _archiver.SaveArchive(archive, settings.ArchivePath);
      AnsiConsole.MarkupLine($"[green]Successfully created archive with {added} file(s)[/]");

      return 0;
    }
    finally
    {
      archive?.Dispose();
    }
  }
}
