using EarthTool.Common.Interfaces;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.WD;

public sealed class ExtractCommand : AsyncCommand<CommonSettings>
{
  private readonly IWDExtractor _extractor;

  public ExtractCommand(IWDExtractor extractor)
  {
    _extractor = extractor;
  }

  public override Task<int> ExecuteAsync(CommandContext context, CommonSettings settings)
  {
    var path = Path.GetDirectoryName(settings.InputFilePath);
    if (string.IsNullOrEmpty(path))
    {
      path = Environment.CurrentDirectory;
    }
    
    var filePattern = Path.GetFileName(settings.InputFilePath);
    var files = Directory.GetFiles(path, filePattern, SearchOption.TopDirectoryOnly);

    files.AsParallel().ForAll(async filePath =>
    {
        await _extractor.Extract(filePath, settings.OutputFolderPath.Value);
    });
    return Task.FromResult(0);
  }
}