using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;

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
      _logger.LogInformation("Processing file {FilePath}", input);
      _extractor.Extract(input, output);
      _logger.LogInformation("Finished!");
    }
  }
}
