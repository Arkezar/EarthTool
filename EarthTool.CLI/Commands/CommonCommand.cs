using EarthTool.Common.Enums;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands;

public abstract class CommonCommand<TSettings> : AsyncCommand<TSettings> where TSettings : CommonSettings
{
  protected abstract Task InternalExecuteAsync(string inputFilePath, TSettings settings);

  protected string GetOutputFilePath(string inputFilePath, string outputPath, FileType outputFileType)
  {
    var outputDirectory = outputPath ?? Path.GetDirectoryName(inputFilePath);
    var fileName = Path.GetFileName(inputFilePath);
    var outputFileName =
      Path.ChangeExtension(fileName, outputFileType.ToString().ToLowerInvariant());
    return Path.Combine(outputDirectory, outputFileName);
  }

  public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
  {
    var path = Path.GetDirectoryName(settings.InputFilePath);
    if (string.IsNullOrEmpty(path))
    {
      path = Environment.CurrentDirectory;
    }

    var filePattern = Path.GetFileName(settings.InputFilePath)!;
    var files = Directory.GetFiles(path, filePattern, SearchOption.TopDirectoryOnly);

    foreach (var file in files)
    {
      await InternalExecuteAsync(file, settings);
    }

    return 0;
  }
}