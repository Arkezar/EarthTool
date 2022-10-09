using EarthTool.Common.Interfaces;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.TEX;

public sealed class ConvertCommand : AsyncCommand<ConvertCommand.Settings>
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

  public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
  {
    var path = Path.GetDirectoryName(settings.InputFilePath);
    if (string.IsNullOrEmpty(path))
    {
      path = Environment.CurrentDirectory;
    }
    var filePattern = Path.GetFileName(settings.InputFilePath);
    var files = Directory.GetFiles(path, filePattern, SearchOption.TopDirectoryOnly);

    var options = new Common.Models.Option[] { new Common.Models.Option("HighResolutionOnly", settings.HighResolutionOnly) };
    var converter = _converter.WithOptions(options);

    files.AsParallel().ForAll(filePath =>
    {
        converter.Convert(filePath, settings.OutputFolderPath.Value);
    });

    return Task.FromResult(0);
  }
}