using EarthTool.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.CLI.Commands.WD;

public sealed class ExtractCommand : Command<ExtractSettings>
{
  private readonly IArchiver _archiver;

  public ExtractCommand(IArchiver archiver)
  {
    _archiver = archiver;
  }

  public override int Execute(CommandContext context, ExtractSettings settings)
  {
    if (settings.ArchivePaths == null || !settings.ArchivePaths.Any())
    {
      AnsiConsole.MarkupLine("[red]No archive files specified[/]");
      return 1;
    }

    var totalExtracted = 0;
    var totalFailed = 0;
    var processedArchives = 0;

    foreach (var archivePath in settings.ArchivePaths)
    {
      if (!File.Exists(archivePath))
      {
        AnsiConsole.MarkupLine($"[red]Archive not found: {archivePath}[/]");
        totalFailed++;
        continue;
      }

      try
      {
        var result = ExtractArchive(archivePath, settings);
        totalExtracted += result.extracted;
        totalFailed += result.failed;
        processedArchives++;
      }
      catch (Exception ex)
      {
        AnsiConsole.MarkupLine($"[red]Failed to process archive {archivePath}: {ex.Message}[/]");
        totalFailed++;
      }
    }

    if (settings.ArchivePaths.Length > 1)
    {
      AnsiConsole.MarkupLine($"\n[bold]Summary:[/]");
      AnsiConsole.MarkupLine($"  Archives processed: {processedArchives}/{settings.ArchivePaths.Length}");
      AnsiConsole.MarkupLine($"  Files extracted: [green]{totalExtracted}[/]");
      if (totalFailed > 0)
      {
        AnsiConsole.MarkupLine($"  Failed: [red]{totalFailed}[/]");
      }
    }

    return totalFailed == 0 ? 0 : 1;
  }

  private (int extracted, int failed) ExtractArchive(string archivePath, ExtractSettings settings)
  {
    using var archive = _archiver.OpenArchive(archivePath);

    var outputPath = settings.OutputPath ?? Path.GetDirectoryName(archivePath) ?? Directory.GetCurrentDirectory();

    var items = archive.Items.AsEnumerable();

    // Apply filter if specified
    if (!string.IsNullOrEmpty(settings.Filter))
    {
      var pattern = settings.Filter.Replace("*", ".*").Replace("?", ".");
      items = items.Where(i => System.Text.RegularExpressions.Regex.IsMatch(
        i.FileName,
        pattern,
        System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    // Apply file list filter if specified
    if (!string.IsNullOrEmpty(settings.FileList))
    {
      var fileNames = settings.FileList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      items = items.Where(i => fileNames.Contains(i.FileName, StringComparer.OrdinalIgnoreCase));
    }

    var itemsList = items.ToList();

    if (!itemsList.Any())
    {
      AnsiConsole.MarkupLine($"[yellow]No files matched criteria in {Path.GetFileName(archivePath)}[/]");
      return (0, 0);
    }

    AnsiConsole.MarkupLine($"[green]Extracting from {Path.GetFileName(archivePath)}: {itemsList.Count} file(s) to {outputPath}[/]");

    var extracted = 0;
    var failed = 0;

    foreach (var item in itemsList)
    {
      try
      {
        _archiver.Extract(item, outputPath);
        extracted++;
        AnsiConsole.MarkupLine($"[dim]  {item.FileName}[/]");
      }
      catch (Exception ex)
      {
        AnsiConsole.MarkupLine($"[red]  Failed to extract {item.FileName}: {ex.Message}[/]");
        failed++;
      }
    }

    if (settings.ArchivePaths.Length == 1)
    {
      AnsiConsole.MarkupLine($"[green]Successfully extracted {extracted}/{itemsList.Count} file(s)[/]");
    }

    return (extracted, failed);
  }
}