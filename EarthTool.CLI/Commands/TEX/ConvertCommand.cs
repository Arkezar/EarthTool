using EarthTool.Common.Interfaces;
using Spectre.Console.Cli;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.TEX;

public sealed class ConvertCommand : CommonCommand<ConvertCommand.Settings>
{
  private readonly ITEXConverter _converter;

  public sealed class Settings : CommonSettings
  {
    [CommandOption("--highres")]
    [Description("Extract only high res mipmaps.")]
    [DefaultValue(true)]
    public bool? HighResolutionOnly { get; set; }
  }

  public ConvertCommand(ITEXConverter converter)
  {
    _converter = converter;
  }

  protected override async Task InternalExecuteAsync(string filePath, Settings settings)
  {
    var options = new[] { new Common.Models.Option("HighResolutionOnly", settings.HighResolutionOnly) };
    var converter = _converter.WithOptions(options);

    await converter.Convert(filePath, settings.OutputFolderPath.Value ?? Path.GetDirectoryName(filePath));
  }
}