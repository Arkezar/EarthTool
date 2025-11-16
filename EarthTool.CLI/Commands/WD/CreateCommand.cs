using EarthTool.Common.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.CLI.Commands.WD;

public sealed class CreateCommand : WdCommandBase<CreateSettings>
{
  private readonly IArchiver _archiver;

  public CreateCommand(IArchiver archiver)
  {
    _archiver = archiver;
  }

  public override int Execute(CommandContext context, CreateSettings settings)
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

    var archive = _archiver.CreateArchive();
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

      var added = 0;
      foreach (var file in filesToAdd)
      {
        try
        {
          _archiver.AddFile(archive, file, compress);
          added++;
          AnsiConsole.MarkupLine($"[dim]  Added: {Path.GetFileName(file)}[/]");
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
