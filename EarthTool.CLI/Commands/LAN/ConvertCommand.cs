using EarthTool.Common.Interfaces;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.LAN;

public sealed class ConvertCommand : CommonCommand<CommonSettings>
{
  private readonly ILANConverter _converter;

  public ConvertCommand(ILANConverter converter)
  {
    _converter = converter;
  }

  protected override async Task InternalExecuteAsync(string filePath, CommonSettings settings)
  {
    await _converter.Convert(filePath, settings.OutputFolderPath.Value ?? Path.GetDirectoryName(filePath));
  }
}