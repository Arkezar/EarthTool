using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.CLI
{
  class EarthToolService : IHostedService
  {
    private readonly Parser _cmdParser;
    private readonly ILogger<EarthToolService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    public EarthToolService(ILogger<EarthToolService> logger, IHostApplicationLifetime appLifetime, IEnumerable<Command> commands)
    {
      _appLifetime = appLifetime;
      _logger = logger;
      _cmdParser = BuildParser(commands);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _appLifetime.ApplicationStarted.Register(() =>
      {
        Task.Run(async () =>
        {
          try
          {
            var args = Environment.GetCommandLineArgs();
            await _cmdParser.InvokeAsync(args);
          }
          catch (Exception e)
          {
            _logger.LogError(e, "Unexpected error occured!");
          }
          finally
          {
            _appLifetime.StopApplication();
          }
        });
      });

      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      return Task.CompletedTask;
    }

    private Parser BuildParser(IEnumerable<Command> commands)
    {
      _logger.LogTrace("Preparing command-line parser.");
      var commandLineBuilder = new CommandLineBuilder();
      foreach (Command command in commands)
      {
        commandLineBuilder.AddCommand(command);
        _logger.LogTrace("Registered command {Command}.", command.Name);
      }
      return commandLineBuilder.UseDefaults().Build();
    }
  }
}
