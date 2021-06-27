using Autofac;
using Autofac.Extensions.DependencyInjection;
using EarthTool.MSH;
using EarthTool.MSH.Converters.Collada;
using EarthTool.MSH.Converters.Wavefront;
using EarthTool.TEX;
using EarthTool.WD;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
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

    private static Parser BuildParser(IContainer serviceProvider)
    {
      var commandLineBuilder = new CommandLineBuilder();
      foreach (Command command in serviceProvider.Resolve<IEnumerable<Command>>())
      {
        commandLineBuilder.AddCommand(command);
      }
      return commandLineBuilder.UseDefaults().Build();
    }

    private static IContainer Initialize()
    {
      var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();

      var containerBuilder = new ContainerBuilder();

      var standardServiceCollection = new ServiceCollection()
               .AddLogging(config =>
               {
                 config.AddConfiguration(configuration.GetSection("Logging"));
                 config.AddConsole();
                 config.AddDebug();
               });

      containerBuilder.Populate(standardServiceCollection);
      containerBuilder.RegisterModule<WDModule>();
      containerBuilder.RegisterModule<TEXModule>();
      containerBuilder.RegisterModule<MSHModule>();
      containerBuilder.RegisterModule<MSHColladaModule>();
      containerBuilder.RegisterModule<MSHWavefrontModule>();
      return containerBuilder.Build();

    }
  }
}
