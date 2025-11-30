using EarthTool.Common.Enums;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands;

public abstract class CommonCommand<TSettings> : AsyncCommand<TSettings> where TSettings : CommonSettings
{
  protected abstract Task InternalExecuteAsync(string inputFilePath, TSettings settings);

  protected virtual Task InternalAnalyzeAsync(string inputFilePath, TSettings settings)
  {
    throw new NotSupportedException("Analyze is not supported for this command");
  }

  protected string GetOutputDirectory(string inputFilePath, string outputPath)
  {
    return outputPath ?? Path.GetDirectoryName(inputFilePath);
  }

  protected string GetOutputFilePath(string inputFilePath, string outputPath, FileType outputFileType)
  {
    var outputDirectory = GetOutputDirectory(inputFilePath, outputPath);
    var fileName = Path.GetFileName(inputFilePath);
    var outputFileName =
      Path.ChangeExtension(fileName, outputFileType.ToString().ToLowerInvariant());
    return Path.Combine(outputDirectory, outputFileName);
  }

  public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
  {
    var path = Path.GetDirectoryName(settings.InputFilePath);
    if (string.IsNullOrEmpty(path))
    {
      path = Environment.CurrentDirectory;
    }

    var filePattern = Path.GetFileName(settings.InputFilePath)!;

    var files = Directory.GetFiles(path, filePattern,
      new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = false });

    foreach (var file in files.OrderBy(f => f))
    {
      try
      {
        if (settings.Analyze)
        {
          await InternalAnalyzeAsync(file, settings);
        }
        else
        {
          await InternalExecuteAsync(file, settings);
        }
      }
      catch (Exception exception)
      {
        AnsiConsole.WriteException(exception);
      }
    }

    return 0;
  }
}
