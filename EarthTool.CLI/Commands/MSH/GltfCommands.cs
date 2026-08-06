#nullable enable

using Spectre.Console.Cli;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.MSH;

internal sealed class ExportGltfCommand : AsyncCommand<ExportGltfSettings>
{
  private readonly GltfCommandExecutor _executor;

  public ExportGltfCommand(GltfCommandExecutor executor)
  {
    _executor = executor;
  }

  protected override Task<int> ExecuteAsync(
    CommandContext context,
    ExportGltfSettings settings,
    CancellationToken cancellationToken)
  {
    return _executor.ExportAsync(settings, cancellationToken);
  }
}

internal sealed class ImportGltfCommand : AsyncCommand<ImportGltfSettings>
{
  private readonly GltfCommandExecutor _executor;

  public ImportGltfCommand(GltfCommandExecutor executor)
  {
    _executor = executor;
  }

  protected override Task<int> ExecuteAsync(
    CommandContext context,
    ImportGltfSettings settings,
    CancellationToken cancellationToken)
  {
    return _executor.ImportAsync(settings, cancellationToken);
  }
}
