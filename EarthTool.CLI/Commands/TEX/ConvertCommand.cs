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
    
    [CommandOption("--debug")]
    [Description("Extract additional image information")]
    [DefaultValue(false)]
    public bool? Debug { get; set; }
  }

  public ConvertCommand(ITEXConverter converter)
  {
    _converter = converter;
  }

  protected override async Task InternalExecuteAsync(string filePath, Settings settings)
  {
    var options = new[]
    {
      new Common.Models.Option(nameof(Settings.HighResolutionOnly), settings.HighResolutionOnly),
      new Common.Models.Option(nameof(Settings.Debug), settings.Debug)

    };
    var converter = _converter.WithOptions(options);

    await converter.Convert(filePath, settings.OutputFolderPath.Value ?? Path.GetDirectoryName(filePath));
  }
}