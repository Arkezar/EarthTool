using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;

namespace EarthTool.Commands
{
  public class WDCommand : Command
  {
    private readonly IWDExtractor _extractor;
    private readonly ILogger<WDCommand> _logger;

    public WDCommand(IWDExtractor extractor, ILogger<WDCommand> logger) : base("wd", "Extract WD file content")
    {
      _extractor = extractor;
      _logger = logger;
      var input = new Argument<string>("input", "WD file path");
      var output = new Option<string>(new[] { "--output", "-o" }, "Output directory. Current if not specified.");
      AddArgument(input);
      AddOption(output);
      Handler = CommandHandler.Create<string, string>(HandleCommand);
    }

    private void HandleCommand(string input, string output)
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
          _extractor.Extract(filePath, output);
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
