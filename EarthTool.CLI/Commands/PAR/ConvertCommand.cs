using EarthTool.Common.Interfaces;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.PAR;

public class ConvertCommand : CommonCommand<CommonSettings>
{
  private readonly IPARConverter _converter;

  public ConvertCommand(IPARConverter converter)
  {
    _converter = converter;
  }

  protected override async Task InternalExecuteAsync(string filePath, CommonSettings settings)
  {
    await _converter.Convert(filePath, settings.OutputFolderPath.Value ?? Path.GetDirectoryName(filePath));
  }
}