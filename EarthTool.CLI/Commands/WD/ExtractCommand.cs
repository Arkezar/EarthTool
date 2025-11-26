using EarthTool.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
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

    // Expand wildcards in archive paths
    var expandedPaths = ExpandWildcards(settings.ArchivePaths);
    
    if (!expandedPaths.Any())
    {
      AnsiConsole.MarkupLine("[red]No archive files found matching the specified patterns[/]");
      return 1;
    }

    var totalExtracted = 0;
    var totalFailed = 0;
    var processedArchives = 0;

    foreach (var archivePath in expandedPaths)
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

    if (expandedPaths.Count > 1)
    {
      AnsiConsole.MarkupLine($"\n[bold]Summary:[/]");
      AnsiConsole.MarkupLine($"  Archives processed: {processedArchives}/{expandedPaths.Count}");
      AnsiConsole.MarkupLine($"  Files extracted: [green]{totalExtracted}[/]");
      if (totalFailed > 0)
      {
        AnsiConsole.MarkupLine($"  Failed: [red]{totalFailed}[/]");
      }
    }

    return totalFailed == 0 ? 0 : 1;
  }

  private List<string> ExpandWildcards(string[] paths)
  {
    var result = new List<string>();

    foreach (var path in paths)
    {
      // Check if path contains wildcard characters
      if (path.Contains('*') || path.Contains('?'))
      {
        try
        {
          var directory = Path.GetDirectoryName(path);
          var fileName = Path.GetFileName(path);

          // If no directory specified, use current directory
          if (string.IsNullOrEmpty(directory))
          {
            directory = Directory.GetCurrentDirectory();
          }

          // If directory doesn't exist, skip this pattern
          if (!Directory.Exists(directory))
          {
            AnsiConsole.MarkupLine($"[yellow]Directory not found: {directory}[/]");
            continue;
          }

          // Search for files matching the pattern
          var matchingFiles = Directory.GetFiles(directory, fileName, SearchOption.TopDirectoryOnly);
          
          if (matchingFiles.Length == 0)
          {
            AnsiConsole.MarkupLine($"[yellow]No files found matching pattern: {path}[/]");
          }
          else
          {
            result.AddRange(matchingFiles);
          }
        }
        catch (Exception ex)
        {
          AnsiConsole.MarkupLine($"[red]Error expanding wildcard pattern '{path}': {ex.Message}[/]");
        }
      }
      else
      {
        // No wildcard, add path as-is
        result.Add(path);
      }
    }

    return result;
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

    if (itemsList.Count == extracted + failed)
    {
      AnsiConsole.MarkupLine($"[green]Successfully extracted {extracted}/{itemsList.Count} file(s)[/]");
    }

    return (extracted, failed);
  }
}
