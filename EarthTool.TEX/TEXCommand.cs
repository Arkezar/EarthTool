using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;

namespace EarthTool.Commands
{
  public class TEXCommand : Command
  {
    private readonly ITEXConverter _converter;
    private readonly ILogger<TEXCommand> _logger;

    public TEXCommand(ITEXConverter converter, ILogger<TEXCommand> logger) : base("tex", "Convert TEX files to PNGs")
    {
      _converter = converter;
      _logger = logger;

      var input = new Argument<string>("input", "TEX file path");
      var output = new Option<string>(new[] { "--output", "-o" }, "Output directory. Current if not specified.");
      var highres = new Option<bool>(new[] { "--highres", "-hr" }, "Extract only high res mipmaps.");
      AddArgument(input);
      AddOption(highres);
      AddOption(output);
      Handler = CommandHandler.Create<string, string, bool>(HandleCommand);
    }

    private void HandleCommand(string input, string output, bool highres)
    {
      var path = Path.GetDirectoryName(input);
      if (string.IsNullOrEmpty(path))
      {
        path = Environment.CurrentDirectory;
      }
      var filePattern = Path.GetFileName(input);
      var files = Directory.GetFiles(path, filePattern, SearchOption.TopDirectoryOnly);

      var options = new Common.Models.Option[] { new Common.Models.Option("HighResolutionOnly", highres) };
      var converter = _converter.WithOptions(options);

      files.AsParallel().ForAll(filePath =>
      {
        try
        {
          converter.Convert(filePath, output);
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
