using EarthTool.Common.Interfaces;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.PAR;

public class ConvertCommand : AsyncCommand<CommonSettings>
{
  private readonly IPARConverter _converter;

  public ConvertCommand(IPARConverter converter)
  {
    _converter = converter;
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

    files.AsParallel().ForAll(filePath =>
    {
        _converter.Convert(filePath, settings.OutputFolderPath.Value);
    });

    return Task.FromResult(0);
  }
}