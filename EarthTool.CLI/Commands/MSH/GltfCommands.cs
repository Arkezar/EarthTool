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

internal sealed class ImportEditGltfCommand : AsyncCommand<ImportEditGltfSettings>
{
  private readonly GltfCommandExecutor _executor;

  public ImportEditGltfCommand(GltfCommandExecutor executor)
  {
    _executor = executor;
  }

  protected override Task<int> ExecuteAsync(
    CommandContext context,
    ImportEditGltfSettings settings,
    CancellationToken cancellationToken)
  {
    return _executor.ImportEditAsync(settings, cancellationToken);
  }
}

internal sealed class ImportNewGltfCommand : AsyncCommand<ImportNewGltfSettings>
{
  private readonly GltfCommandExecutor _executor;

  public ImportNewGltfCommand(GltfCommandExecutor executor)
  {
    _executor = executor;
  }

  protected override Task<int> ExecuteAsync(
    CommandContext context,
    ImportNewGltfSettings settings,
    CancellationToken cancellationToken)
  {
    return _executor.ImportNewAsync(settings, cancellationToken);
  }
}
