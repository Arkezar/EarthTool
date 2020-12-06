using EarthTool.Commands;
using EarthTool.Common.Interfaces;
using EarthTool.MSH;
using EarthTool.TEX;
using EarthTool.WD;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace EarthTool
{
  class Program
  {
    public static async Task<int> Main(string[] args)
    {
      var serviceProvider = Initialize();
      var parser = BuildParser(serviceProvider);
      return await parser.InvokeAsync(args);
    }

    private static Parser BuildParser(IServiceProvider serviceProvider)
    {
      var commandLineBuilder = new CommandLineBuilder();
      foreach (Command command in serviceProvider.GetServices<Command>())
      {
        commandLineBuilder.AddCommand(command);
      }
      return commandLineBuilder.UseDefaults().Build();
    }

    private static IServiceProvider Initialize()
    {
      return new ServiceCollection()
              .AddLogging(config =>
              {
                config.AddConsole();
                config.AddDebug();
              })
              .AddTransient<IWDExtractor, WDEXtractor>()
              .AddTransient<ITEXConverter, TEXConverter>()
              .AddTransient<IMSHConverter, MSHConverter>()
              .AddSingleton<Command, WDCommand>()
              .AddSingleton<Command, TEXCommand>()
              .AddSingleton<Command, MSHCommand>()
              .BuildServiceProvider();
    }
  }
}
