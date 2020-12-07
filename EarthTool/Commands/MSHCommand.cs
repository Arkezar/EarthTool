using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace EarthTool.Commands
{
  public class MSHCommand : Command
  {
    private readonly IMSHConverter _converter;
    private readonly ILogger<MSHCommand> _logger;

    public MSHCommand(IMSHConverter converter, ILogger<MSHCommand> logger) : base("msh", "(experimental) Convert MSH files to Wavefront objects")
    {
      _converter = converter;
      _logger = logger;

      var input = new Argument<string>("input", "MSH file path");
      var output = new Option<string>(new[] { "--output", "-o" }, "Output directory. Current if not specified.");
      AddArgument(input);
      AddOption(output);
      Handler = CommandHandler.Create<string, string>(HandleCommand);
    }

    private void HandleCommand(string input, string output)
    {
      _logger.LogInformation("Processing file {FilePath}", input);
      try
      {
        _converter.Convert(input, output);
        _logger.LogInformation("Finished!");
      }
      catch (Exception e)
      {
        _logger.LogError(e, "Error occured");
      }
    }
  }
}
