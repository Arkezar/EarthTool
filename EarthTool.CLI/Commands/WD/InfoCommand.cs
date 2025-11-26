using EarthTool.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.CLI.Commands.WD;

public sealed class InfoCommand : WdCommandBase<InfoSettings>
{
  private readonly IArchiver _archiver;

  public InfoCommand(IArchiver archiver)
  {
    _archiver = archiver;
  }

  public override int Execute(CommandContext context, InfoSettings settings)
  {
    if (!File.Exists(settings.ArchivePath))
    {
      AnsiConsole.MarkupLine($"[red]Archive not found: {settings.ArchivePath}[/]");
      return 1;
    }

    using var archive = _archiver.OpenArchive(settings.ArchivePath);

    // If timestamp-only mode, output just the FileTime timestamp
    if (settings.TimestampOnly)
    {
      // Output Windows FileTime (100-nanosecond intervals since Jan 1, 1601 UTC)
      // This preserves full precision including milliseconds
      var fileTime = archive.LastModification.ToFileTimeUtc();
      AnsiConsole.WriteLine(fileTime.ToString());
      return 0;
    }

    // If guid-only mode, output just the archive guid
    if (settings.GuidOnly)
    {
      AnsiConsole.WriteLine(archive.Header.Guid.ToString());
      return 0;
    }

    var fileInfo = new FileInfo(settings.ArchivePath);
    var totalCompressed = archive.Items.Sum(i => (long)i.CompressedSize);
    var totalDecompressed = archive.Items.Sum(i => (long)i.DecompressedSize);
    var compressionRatio = totalDecompressed > 0
      ? (1.0 - (double)totalCompressed / totalDecompressed) * 100
      : 0;

    var compressedCount = archive.Items.Count(i => i.IsCompressed);
    var uncompressedCount = archive.Items.Count - compressedCount;

    var tree = new Tree($"[bold green]{Path.GetFileName(settings.ArchivePath)}[/]");

    // File information
    var fileInfoNode = tree.AddNode("[bold]File Information[/]");
    fileInfoNode.AddNode($"Path: {settings.ArchivePath}");
    fileInfoNode.AddNode($"Size: {FormatBytes(fileInfo.Length)}");
    fileInfoNode.AddNode($"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
    fileInfoNode.AddNode($"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");

    // Archive header
    var headerNode = tree.AddNode("[bold]Archive Header[/]");
    headerNode.AddNode($"Last modified: {archive.LastModification:yyyy-MM-dd HH:mm:ss}");
    headerNode.AddNode($"Flags: {archive.Header.Flags}");
    if (archive.Header.ResourceType.HasValue)
    {
      headerNode.AddNode($"Resource type: {archive.Header.ResourceType.Value}");
    }
    if (archive.Header.Guid.HasValue)
    {
      headerNode.AddNode($"GUID: {archive.Header.Guid.Value}");
    }

    // Content statistics
    var statsNode = tree.AddNode("[bold]Content Statistics[/]");
    statsNode.AddNode($"Total files: {archive.Items.Count}");
    statsNode.AddNode($"Compressed files: {compressedCount}");
    statsNode.AddNode($"Uncompressed files: {uncompressedCount}");
    statsNode.AddNode($"Total compressed size: {FormatBytes(totalCompressed)}");
    statsNode.AddNode($"Total decompressed size: {FormatBytes(totalDecompressed)}");
    statsNode.AddNode($"Average compression ratio: {compressionRatio:F1}%");

    // File type breakdown
    var fileTypes = archive.Items
      .GroupBy(i => Path.GetExtension(i.FileName).ToLowerInvariant())
      .OrderByDescending(g => g.Count())
      .Take(10)
      .ToList();

    if (fileTypes.Any())
    {
      var typesNode = tree.AddNode("[bold]File Types (Top 10)[/]");
      foreach (var type in fileTypes)
      {
        var ext = string.IsNullOrEmpty(type.Key) ? "(no extension)" : type.Key;
        var typeSize = type.Sum(i => (long)i.DecompressedSize);
        typesNode.AddNode($"{ext}: {type.Count()} files ({FormatBytes(typeSize)})");
      }
    }

    // Largest files
    var largestFiles = archive.Items
      .OrderByDescending(i => i.DecompressedSize)
      .Take(5)
      .ToList();

    if (largestFiles.Any())
    {
      var largestNode = tree.AddNode("[bold]Largest Files (Top 5)[/]");
      foreach (var item in largestFiles)
      {
        var ratio = item.DecompressedSize > 0
          ? (1.0 - (double)item.CompressedSize / item.DecompressedSize) * 100
          : 0;
        largestNode.AddNode($"{item.FileName}: {FormatBytes(item.DecompressedSize)} (ratio: {ratio:F1}%)");
      }
    }

    AnsiConsole.Write(tree);

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