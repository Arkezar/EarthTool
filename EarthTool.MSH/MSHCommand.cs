using Autofac.Features.Indexed;
using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;

namespace EarthTool.Commands
{
  public class MSHCommand : Command
  {
    private readonly IIndex<string, IMSHConverter> _converter;
    private readonly ILogger<MSHCommand> _logger;

    public MSHCommand(IIndex<string, IMSHConverter> converter, ILogger<MSHCommand> logger) : base("msh", "Convert MSH files to 3D gfx file formats")
    {
      _converter = converter;
      _logger = logger;

      var input = new Argument<string>("input", "MSH file path");
      var output = new Option<string>(new[] { "--output", "-o" }, "Output directory. Current if not specified.");
      var format = new Option<string>(new[] { "--format", "-f" }, () => "dae", "Output format. obj for Wavefront, dae for Collada");
      AddArgument(input);
      AddOption(output);
      AddOption(format);
      Handler = CommandHandler.Create<string, string, string>(HandleCommand);
    }

    private void HandleCommand(string input, string output, string format)
    {
      var path = Path.GetDirectoryName(input);
      if (string.IsNullOrEmpty(path))
      {
        path = Environment.CurrentDirectory;
      }
      var filePattern = Path.GetFileName(input);
      var files = Directory.GetFiles(path, filePattern, SearchOption.TopDirectoryOnly);

      files.AsParallel().ForAll(filePath =>
      {
        try
        {
          _converter[format].Convert(filePath, output);
          _logger.LogInformation("Processed file {FilePath}", filePath);
        }
        catch (Exception e)
        {
          _logger.LogError(e, "Error occured while processing file {FilePath}", filePath);
        }
      });
    }
  }
}
