using EarthTool.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Linq;

namespace EarthTool.CLI.Commands.WD;

public sealed class ListCommand : WdCommandBase<ListSettings>
{
  private readonly IArchiver _archiver;

  public ListCommand(IArchiver archiver)
  {
    _archiver = archiver;
  }

  public override int Execute(CommandContext context, ListSettings settings)
  {
    using var archive = _archiver.OpenArchive(settings.ArchivePath);

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

    var itemsList = items.ToList();

    if (!itemsList.Any())
    {
      if (!settings.NamesOnly)
      {
        AnsiConsole.MarkupLine("[yellow]No files found in archive[/]");
      }
      return 0;
    }

    if (settings.NamesOnly)
    {
      foreach (var item in itemsList)
      {
        Console.WriteLine(item.FileName);
      }
      return 0;
    }

    var tree = new Tree($"[green]{System.IO.Path.GetFileName(settings.ArchivePath)}[/]");
    var info = tree.AddNode($"[bold]Archive Information[/]");
    info.AddNode($"Total files: {archive.Items.Count}");
    info.AddNode($"Last modified: {archive.LastModification:yyyy-MM-dd HH:mm:ss}");

    if (!string.IsNullOrEmpty(settings.Filter))
    {
      info.AddNode($"Filtered files: {itemsList.Count}");
      info.AddNode($"Filter: {settings.Filter}");
    }

    var filesNode = tree.AddNode("[bold]Files[/]");

    if (settings.Detailed)
    {
      var table = new Table();
      table.AddColumn("File Name");
      table.AddColumn(new TableColumn("Compressed").RightAligned());
      table.AddColumn(new TableColumn("Decompressed").RightAligned());
      table.AddColumn(new TableColumn("Ratio").RightAligned());
      table.AddColumn("Flags");

      foreach (var item in itemsList)
      {
        var ratio = item.DecompressedSize > 0
          ? (1.0 - (double)item.CompressedSize / item.DecompressedSize) * 100
          : 0;

        var flags = item.Header.Flags.ToString();

        table.AddRow(
          item.FileName,
          FormatBytes(item.CompressedSize),
          FormatBytes(item.DecompressedSize),
          $"{ratio:F1}%",
          flags);
      }

      AnsiConsole.Write(tree);
      AnsiConsole.Write(table);
    }
    else
    {
      foreach (var item in itemsList)
      {
        var compressed = item.IsCompressed ? "[dim](compressed)[/]" : "";
        filesNode.AddNode($"{item.FileName} {compressed}");
      }

      AnsiConsole.Write(tree);
    }

    return 0;
  }

  private static string FormatBytes(long bytes)
  {
    string[] sizes = { "B", "KB", "MB", "GB" };
    double len = bytes;
    int order = 0;
    while (len >= 1024 && order < sizes.Length - 1)
    {
      order++;
      len /= 1024;
    }
    return $"{len:0.##} {sizes[order]}";
  }
}
