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
        // WD Archive commands
        config.AddBranch("wd", wd =>
        {
          wd.SetDescription("WD archive management commands");
          wd.AddCommand<Commands.WD.ListCommand>("list")
            .WithDescription("List archive contents");
          wd.AddCommand<Commands.WD.ExtractCommand>("extract")
            .WithDescription("Extract files from archive");
          wd.AddCommand<Commands.WD.CreateCommand>("create")
            .WithDescription("Create new archive");
          wd.AddCommand<Commands.WD.AddCommand>("add")
            .WithDescription("Add files to archive");
          wd.AddCommand<Commands.WD.RemoveCommand>("remove")
            .WithDescription("Remove files from archive");
          wd.AddCommand<Commands.WD.InfoCommand>("info")
            .WithDescription("Display archive information");
#if DEBUG
          wd.AddCommand<Commands.WD.DebugCommand>("debug")
            .WithDescription("Debug archive information");
#endif
        });

        // Other format converters
        config.AddCommand<Commands.MSH.ConvertCommand>("msh");
        config.AddCommand<Commands.DAE.ConvertCommand>("dae");
        config.AddCommand<Commands.TEX.ConvertCommand>("tex");
        config.AddCommand<Commands.PAR.ConvertCommand>("par");

        config.SetExceptionHandler((ex, _) =>
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