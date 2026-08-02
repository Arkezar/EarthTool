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
        .WithDescription("Export static MSH assets to GLB or separate glTF");
      msh.AddBranch("import", import =>
      {
        import.SetDescription("Import glTF into MSH");
        import.AddCommand<ImportEditGltfCommand>("edit")
          .WithDescription("Import an edit into an expected interchange lineage");
        import.AddCommand<ImportNewGltfCommand>("new")
          .WithDescription("Author canonical MSH assets from metadata-free glTF");
      });
    });
  }
}
