using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR
{
  public class PARCommand : Command
  {
    private readonly IPARConverter _converter;
    private readonly ILogger<PARCommand> _logger;

    public PARCommand(IPARConverter converter, ILogger<PARCommand> logger) : base("par", "Convert PAR files to ?")
    {
      _converter = converter;
      _logger = logger;

      var input = new Argument<string>("input", "PAR file path");
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
          _converter.Convert(filePath, output);
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
