using EarthTool.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.CLI.Commands.WD;

public sealed class RemoveCommand : WdCommandBase<RemoveSettings>
{
  private readonly IArchiver _archiver;

  public RemoveCommand(IArchiver archiver)
  {
    _archiver = archiver;
  }

  public override int Execute(CommandContext context, RemoveSettings settings)
  {
    if (string.IsNullOrEmpty(settings.Filter) && string.IsNullOrEmpty(settings.FileList))
    {
      AnsiConsole.MarkupLine("[red]Either --filter or --list option must be specified[/]");
      return 1;
    }

    if (!File.Exists(settings.ArchivePath))
    {
      AnsiConsole.MarkupLine($"[red]Archive not found: {settings.ArchivePath}[/]");
      return 1;
    }

    using var archive = _archiver.OpenArchive(settings.ArchivePath);
    var outputPath = settings.OutputPath ?? settings.ArchivePath;

    var itemsToRemove = archive.Items.AsEnumerable();

    // Apply filter if specified
    if (!string.IsNullOrEmpty(settings.Filter))
    {
      var pattern = settings.Filter.Replace("*", ".*").Replace("?", ".");
      itemsToRemove = itemsToRemove.Where(i => System.Text.RegularExpressions.Regex.IsMatch(
        i.FileName,
        pattern,
        System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    // Apply file list filter if specified
    if (!string.IsNullOrEmpty(settings.FileList))
    {
      var fileNames = settings.FileList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      itemsToRemove = itemsToRemove.Where(i => fileNames.Contains(i.FileName, StringComparer.OrdinalIgnoreCase));
    }

    var itemsList = itemsToRemove.ToList();

    if (!itemsList.Any())
    {
      AnsiConsole.MarkupLine("[yellow]No files matched the specified criteria[/]");
      return 0;
    }

    // Show files to be removed and confirm
    AnsiConsole.MarkupLine($"[yellow]The following {itemsList.Count} file(s) will be removed:[/]");
    foreach (var item in itemsList)
    {
      AnsiConsole.MarkupLine($"[dim]  - {item.FileName}[/]");
    }

    if (!AnsiConsole.Confirm("Are you sure you want to remove these files?"))
    {
      AnsiConsole.MarkupLine("[yellow]Operation cancelled[/]");
      return 0;
    }

    var removed = 0;
    foreach (var item in itemsList)
    {
      try
      {
        archive.RemoveItem(item);
        removed++;
      }
      catch (Exception ex)
      {
        AnsiConsole.MarkupLine($"[red]  Failed to remove {item.FileName}: {ex.Message}[/]");
      }
    }

    if (removed == 0)
    {
      AnsiConsole.MarkupLine("[yellow]No files were removed from the archive[/]");
      return 1;
    }

    try
    {
      _archiver.SaveArchive(archive, outputPath);
      AnsiConsole.MarkupLine($"[green]Successfully removed {removed} file(s) from archive[/]");
      
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
