using Autofac;
using Autofac.Extensions.DependencyInjection;
using EarthTool.Common;
using EarthTool.MSH;
using EarthTool.MSH.Converters.Collada;
using EarthTool.PAR;
using EarthTool.TEX;
using EarthTool.WD;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace EarthTool.CLI
{
  class Program
  {
    public static Task Main(string[] args)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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
          config.AddSimpleConsole(opts =>
          {
            opts.SingleLine = true;
          });
          config.AddDebug();
        })
        .ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
          containerBuilder.RegisterModule<CommonModule>();
          containerBuilder.RegisterModule<WDModule>();
          containerBuilder.RegisterModule<TEXModule>();
          containerBuilder.RegisterModule<MSHModule>();
          containerBuilder.RegisterModule<PARModule>();
          containerBuilder.RegisterModule<MSHColladaModule>();
        })
        .ConfigureServices((ctx, services) =>
        {
          services.AddHostedService<EarthToolService>();
        });
    }
  }
}
