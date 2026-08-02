#nullable enable

using EarthTool.GLTF;
using EarthTool.MSH;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.MSH;

internal static class CliExitCode
{
  public const int Success = 0;
  public const int Failure = 1;
  public const int Usage = 2;
  public const int Cancellation = 130;
}

internal sealed class CliOutput
{
  public TextWriter Writer { get; }

  public CliOutput(TextWriter writer)
  {
    Writer = writer;
  }
}

internal static class InternalMshCommandHost
{
  public static async Task<int> RunAsync(
    IEnumerable<string> args,
    TextWriter? output = null,
    CancellationToken cancellationToken = default)
  {
    var hostBuilder = Host.CreateDefaultBuilder()
      .ConfigureServices(services => services
        .AddMshServices()
        .AddSingleton<GltfInterchange>()
        .AddSingleton<GltfImportPlanSerializer>()
        .AddSingleton<GltfCliReportSerializer>()
        .AddSingleton(new CliOutput(output ?? Console.Out))
        .AddSingleton<GltfCommandExecutor>());
    var app = new CommandApp(new CommandTypeRegistrar(hostBuilder));
    app.Configure(config =>
    {
      config.SetApplicationName("earthtool");
      config.AddBranch("msh", msh =>
      {
        msh.AddCommand<ExportGltfCommand>("export");
        msh.AddBranch("import", import =>
        {
          import.AddCommand<ImportEditGltfCommand>("edit");
          import.AddCommand<ImportNewGltfCommand>("new");
        });
      });
    });

    var status = await app.RunAsync(args, cancellationToken).ConfigureAwait(false);
    return status is CliExitCode.Success or CliExitCode.Failure or CliExitCode.Usage or CliExitCode.Cancellation
      ? status
      : CliExitCode.Usage;
  }
}
