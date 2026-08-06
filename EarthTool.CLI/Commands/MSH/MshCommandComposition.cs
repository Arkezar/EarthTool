#nullable enable

using EarthTool.GLTF;
using EarthTool.MSH;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System.IO;

namespace EarthTool.CLI.Commands.MSH;

internal static class MshCommandComposition
{
  public static IServiceCollection AddMshCliServices(this IServiceCollection services, TextWriter output)
  {
    return services
      .AddMshServices()
      .AddGltfServices()
      .AddSingleton<ICliReportFileSystem, CliReportFileSystem>()
      .AddSingleton(new CliOutput(output))
      .AddSingleton<GltfCommandExecutor>();
  }

  public static void AddCommands(IConfigurator config)
  {
    config.AddBranch("msh", msh =>
    {
      msh.SetDescription("MSH and glTF interchange commands");
      msh.AddCommand<ExportGltfCommand>("export")
        .WithDescription("Export supported MSH assets to GLB or separate glTF");
      msh.AddCommand<ImportGltfCommand>("import")
        .WithDescription("Create MSH assets from GLB or separate glTF");
    });
  }
}
