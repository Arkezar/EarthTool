using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.CLI.Commands.WD;

public sealed class DebugCommand : WdCommandBase<WdSettings>
{
  private readonly IArchiver _archiver;

  public DebugCommand(IArchiver archiver)
  {
    _archiver = archiver;
  }

  public override int Execute(CommandContext context, WdSettings settings)
  {
    if (!File.Exists(settings.ArchivePath))
    {
      AnsiConsole.MarkupLine($"[red]Archive not found: {settings.ArchivePath}[/]");
      return 1;
    }

    using var archive = _archiver.OpenArchive(settings.ArchivePath);

    var data = archive.Items.Where(i => i.Header.Flags.HasFlag(FileFlags.Resource))
      .Select(i => $"{i.FileName}\t{i.Header.ResourceType}");

    AnsiConsole.MarkupLine($"{string.Join("\n", data)}");

    return 0;
  }
}