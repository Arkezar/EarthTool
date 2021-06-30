using Autofac;
using Autofac.Extensions.DependencyInjection;
using EarthTool.MSH;
using EarthTool.MSH.Converters.Collada;
using EarthTool.MSH.Converters.Wavefront;
using EarthTool.TEX;
using EarthTool.WD;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System.Threading.Tasks;

namespace EarthTool.CLI
{
  class Program
  {
    public static Task Main(string[] args)
    {
      return Initialize(args).Build().RunAsync();
    }

    private static IHostBuilder Initialize(string[] args)
    {
      var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();

      return Host.CreateDefaultBuilder(args)
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .ConfigureLogging(config =>
        {
          config.AddConfiguration(configuration.GetSection("Logging"));
          config.AddConsole();
          config.AddDebug();
        })
        .ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
          containerBuilder.RegisterModule<WDModule>();
          containerBuilder.RegisterModule<TEXModule>();
          containerBuilder.RegisterModule<MSHModule>();
          containerBuilder.RegisterModule<MSHColladaModule>();
          containerBuilder.RegisterModule<MSHWavefrontModule>();
        })
        .ConfigureServices((ctx, services) =>
        {
          services.AddHostedService<EarthToolService>();
        });
    }
  }
}
