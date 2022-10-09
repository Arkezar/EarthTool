using EarthTool.Common.Interfaces;
using Spectre.Console.Cli;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.MSH;

public sealed class ConvertCommand : AsyncCommand<CommonSettings>
{
  private readonly IMSHConverter _converter;

  public ConvertCommand(IMSHConverter converter)
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

    files.AsParallel().ForAll(async filePath =>
    {
      await _converter.Convert(filePath, settings.OutputFolderPath.Value);
    });

    return Task.FromResult(0);
  }
}