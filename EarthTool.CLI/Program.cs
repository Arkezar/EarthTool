using EarthTool.CLI.Commands;
using EarthTool.CLI.Commands.MSH;
using EarthTool.Common;
using EarthTool.PAR;
using EarthTool.TEX;
using EarthTool.WD;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Text;
using System.Threading.Tasks;

namespace EarthTool.CLI
{
  class Program
  {
    public static async Task<int> Main(string[] args)
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

        MshCommandComposition.AddCommands(config);
        config.AddCommand<Commands.TEX.ConvertCommand>("tex");

        // PAR commands
        config.AddBranch("par", par =>
        {
          par.SetDescription("PAR parameter file commands");
          par.AddCommand<Commands.PAR.ConvertCommand>("convert")
            .WithDescription("Convert between PAR and JSON formats");
          par.AddCommand<Commands.PAR.ItemCommand>("item")
            .WithDescription("Display detailed information about an item by name");
        });

        config.Settings.CancellationExitCode = CliExitCode.Cancellation;
        config.Settings.ExceptionHandler = (ex, _) =>
        {
          AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
          return ex is CommandParseException or CommandRuntimeException
            ? CliExitCode.Usage
            : CliExitCode.Failure;
        };
      });
      var status = await app.RunAsync(args).ConfigureAwait(false);
      return status is CliExitCode.Success or CliExitCode.Failure or CliExitCode.Usage or CliExitCode.Cancellation
        ? status
        : CliExitCode.Usage;
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
          .AddMshCliServices(Console.Out)
          .AddParServices()
          .AddTexServices()
          .AddWdServices());
    }
  }
}
