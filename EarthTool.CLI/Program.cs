using Autofac;
using Autofac.Extensions.DependencyInjection;
using EarthTool.CLI.Commands;
using EarthTool.Common;
using EarthTool.MSH;
using EarthTool.DAE;
using EarthTool.PAR;
using EarthTool.TEX;
using EarthTool.WD;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text;
using System.Threading.Tasks;

namespace EarthTool.CLI
{
  class Program
  {
    public static Task Main(string[] args)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      var hostBuilder = CreateHostBuilder(args);

      var app = new CommandApp(new CommandTypeRegistrar(hostBuilder));
      app.Configure(config =>
      {
        config.AddCommand<Commands.WD.ExtractCommand>("wd");
        config.AddCommand<Commands.MSH.ConvertCommand>("msh");
        config.AddCommand<Commands.DAE.ConvertCommand>("dae");
        config.AddCommand<Commands.TEX.ConvertCommand>("tex");
        config.AddCommand<Commands.PAR.ConvertCommand>("par");

#if DEBUG
        config.AddCommand<Commands.MSH.AnalyzeCommand>("msha");
#endif
        config.SetExceptionHandler(ex =>
        {
          AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        });
      });
      return app.RunAsync(args);
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
      var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
      return Host.CreateDefaultBuilder(args)
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .ConfigureLogging(config =>
        {
          config.AddConfiguration(configuration.GetSection("Logging"));
          config.AddDebug();
        })
        .ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
          containerBuilder.RegisterModule<CommonModule>();
          containerBuilder.RegisterModule<WDModule>();
          containerBuilder.RegisterModule<TEXModule>();
          containerBuilder.RegisterModule<MSHModule>();
          containerBuilder.RegisterModule<PARModule>();
          containerBuilder.RegisterModule<DAEModule>();
        });
    }
  }
}