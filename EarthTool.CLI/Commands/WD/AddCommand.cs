using EarthTool.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.CLI.Commands.WD;

public sealed class AddCommand : WdCommandBase<AddSettings>
{
  private readonly IArchiver _archiver;

  public AddCommand(IArchiver archiver)
  {
    _archiver = archiver;
  }

  public override int Execute(CommandContext context, AddSettings settings)
  {
    if (settings.Files == null || !settings.Files.Any())
    {
      AnsiConsole.MarkupLine("[red]No files specified to add[/]");
      return 1;
    }

    if (!File.Exists(settings.ArchivePath))
    {
      AnsiConsole.MarkupLine($"[red]Archive not found: {settings.ArchivePath}[/]");
      return 1;
    }

    using var archive = _archiver.OpenArchive(settings.ArchivePath);
    var compress = !settings.NoCompress;
    var outputPath = settings.OutputPath ?? settings.ArchivePath;

    // Save original timestamp if preserving
    var originalTimestamp = settings.PreserveTimestamp ? archive.LastModification : DateTime.MinValue;

    AnsiConsole.MarkupLine($"[green]Adding files to archive: {settings.ArchivePath}[/]");
    AnsiConsole.MarkupLine($"[dim]Compression: {(compress ? "enabled" : "disabled")}[/]");
    if (settings.PreserveTimestamp)
    {
      AnsiConsole.MarkupLine($"[dim]Preserving timestamp: {originalTimestamp:yyyy-MM-dd HH:mm:ss}[/]");
    }

    var added = 0;
    var skipped = 0;

    foreach (var filePath in settings.Files)
    {
      if (!File.Exists(filePath))
      {
        AnsiConsole.MarkupLine($"[red]  File not found: {filePath}[/]");
        continue;
      }

      var fileName = Path.GetFileName(filePath);

      // Check if file already exists in archive
      if (archive.Items.Any(i => i.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
      {
        if (!AnsiConsole.Confirm($"File [yellow]{fileName}[/] already exists in archive. Replace?"))
        {
          AnsiConsole.MarkupLine($"[yellow]  Skipped: {fileName}[/]");
          skipped++;
          continue;
        }

        // Remove existing item
        var existingItem = archive.Items.First(i => i.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        archive.RemoveItem(existingItem);
      }

      try
      {
        // Use provided base directory or parent directory of file
        var baseDir = !string.IsNullOrEmpty(settings.BaseDir) 
          ? settings.BaseDir 
          : Path.GetDirectoryName(filePath);
        _archiver.AddFile(archive, filePath, baseDir, compress);
        added++;
        AnsiConsole.MarkupLine($"[dim]  Added: {fileName}[/]");
      }
      catch (Exception ex)
      {
        AnsiConsole.MarkupLine($"[red]  Failed to add {fileName}: {ex.Message}[/]");
      }
    }

    if (added == 0)
    {
      AnsiConsole.MarkupLine("[yellow]No files were added to the archive[/]");
      return skipped > 0 ? 0 : 1;
    }

    try
    {
      // Restore original timestamp if preserving
      if (settings.PreserveTimestamp && originalTimestamp != DateTime.MinValue)
      {
        ((EarthTool.WD.Models.Archive)archive).SetTimestamp(originalTimestamp);
      }

      _archiver.SaveArchive(archive, outputPath);
      AnsiConsole.MarkupLine($"[green]Successfully added {added} file(s) to archive[/]");
      
      if (!string.IsNullOrEmpty(settings.OutputPath))
      {
        AnsiConsole.MarkupLine($"[dim]Saved to: {outputPath}[/]");
      }

      return 0;
    }
    catch (Exception ex)
    {
      AnsiConsole.MarkupLine($"[red]Failed to save archive: {ex.Message}[/]");
      return 1;
    }
  }
}
