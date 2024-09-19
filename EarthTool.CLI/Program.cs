using EarthTool.CLI.Commands;
using EarthTool.Common;
using EarthTool.MSH;
using EarthTool.DAE;
using EarthTool.PAR;
using EarthTool.TEX;
using EarthTool.WD;
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
      return Host.CreateDefaultBuilder(args)
        .ConfigureLogging((ctx, config) =>
        {
          config.AddConfiguration(ctx.Configuration.GetSection("Logging"));
          config.AddDebug();
        })
        .ConfigureServices(builder => builder
          .AddCommonServices()
          .AddDaeServices()
          .AddMshServices()
          .AddParServices()
          .AddTexServices()
          .AddWdServices());
    }
  }
}