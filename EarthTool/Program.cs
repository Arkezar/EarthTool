using Autofac;
using Autofac.Extensions.DependencyInjection;
using EarthTool.Commands;
using EarthTool.Common.Interfaces;
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
      containerBuilder.RegisterType<WDEXtractor>().AsImplementedInterfaces();
      containerBuilder.RegisterType<TEXConverter>().AsImplementedInterfaces();
      containerBuilder.RegisterType<MSHWavefrontConverter>().AsImplementedInterfaces().Keyed<IMSHConverter>("obj");
      containerBuilder.RegisterType<MSHColladaConverter>().AsImplementedInterfaces().Keyed<IMSHConverter>("dae");
      containerBuilder.RegisterType<WDCommand>().As<Command>().SingleInstance();
      containerBuilder.RegisterType<TEXCommand>().As<Command>().SingleInstance();
      containerBuilder.RegisterType<MSHCommand>().As<Command>().SingleInstance();
      return containerBuilder.Build();

    }
  }
}
