#nullable enable

using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.CLI.Commands.MSH;

internal sealed class GltfCommandExecutor
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly GltfInterchange _interchange;
  private readonly GltfImportPlanSerializer _planSerializer;
  private readonly GltfCliReportSerializer _reportSerializer;
  private readonly TextWriter _output;

  public GltfCommandExecutor(
    IServiceScopeFactory scopeFactory,
    GltfInterchange interchange,
    GltfImportPlanSerializer planSerializer,
    GltfCliReportSerializer reportSerializer,
    CliOutput output)
  {
    _scopeFactory = scopeFactory;
    _interchange = interchange;
    _planSerializer = planSerializer;
    _reportSerializer = reportSerializer;
    _output = output.Writer;
  }

  public async Task<int> ImportEditAsync(
    ImportEditGltfSettings settings,
    CancellationToken cancellationToken)
  {
    if (!TryGetPackageKind(settings.Input, out var packageKind)
      || ContainsPattern(settings.Input)
      || !TryCreateBaseline(settings, out var expectedBaseline))
    {
      return CliExitCode.Usage;
    }

    var input = Path.GetFullPath(settings.Input);
    var destination = GetImportDestination(input, settings.OutputDirectory);
    var plan = await ReadPlanAsync(settings.PlanPath, cancellationToken).ConfigureAwait(false);
    OperationResult<GltfEditImportResult> imported;
    if (plan is not null && !plan.Succeeded)
    {
      imported = new OperationResult<GltfEditImportResult>(plan.Status, diagnostics: plan.Diagnostics);
    }
    else if (packageKind == GltfPackageKind.Glb)
    {
      try
      {
        await using var source = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
        imported = plan?.Value is null
          ? await _interchange.ImportEditGlbAsync(
            source,
            expectedBaseline!,
            cancellationToken: cancellationToken).ConfigureAwait(false)
          : await _interchange.ImportEditGlbWithPlanAsync(
            source,
            expectedBaseline!,
            plan.Value,
            cancellationToken: cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        imported = Cancelled<GltfEditImportResult>();
      }
      catch (Exception ex)
      {
        imported = IoFailure<GltfEditImportResult>(ex);
      }
    }
    else
    {
      imported = plan?.Value is null
        ? await _interchange.ImportEditGltfFileAsync(
          input,
          expectedBaseline!,
          cancellationToken: cancellationToken).ConfigureAwait(false)
        : await _interchange.ImportEditGltfFileWithPlanAsync(
          input,
          expectedBaseline!,
          plan.Value,
          cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    var complete = await WriteImportedAssetAsync(
      imported,
      result => result.Asset,
      destination,
      cancellationToken)
      .ConfigureAwait(false);
    var operation = GltfCliReportOperation.ForEditImport(
      input,
      destination,
      packageKind,
      expectedBaseline!,
      complete);
    var report = await WriteReportAsync(settings.ReportPath, operation)
      .ConfigureAwait(false);
    var finalStatus = AggregateStatus(complete.Status, report.Status);
    await WriteSummaryAsync(
      input,
      destination,
      finalStatus,
      complete.Diagnostics.Concat(report.Diagnostics).ToArray())
      .ConfigureAwait(false);
    return ToExitCode(finalStatus);
  }

  public async Task<int> ImportNewAsync(
    ImportNewGltfSettings settings,
    CancellationToken cancellationToken)
  {
    if (!TryGetPackageKind(settings.Input, out var packageKind))
    {
      return CliExitCode.Usage;
    }

    var input = Path.GetFullPath(settings.Input);
    var destination = GetImportDestination(input, settings.OutputDirectory);
    var plan = await ReadPlanAsync(settings.PlanPath, cancellationToken).ConfigureAwait(false);
    OperationResult<GltfNewModelImportResult> imported;
    if (plan is not null && !plan.Succeeded)
    {
      imported = new OperationResult<GltfNewModelImportResult>(plan.Status, diagnostics: plan.Diagnostics);
    }
    else if (packageKind == GltfPackageKind.Glb)
    {
      try
      {
        await using var source = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
        imported = plan?.Value is null
          ? await _interchange.ImportNewModelGlbAsync(
            source,
            cancellationToken: cancellationToken).ConfigureAwait(false)
          : await _interchange.ImportNewModelGlbWithPlanAsync(
            source,
            plan.Value,
            cancellationToken: cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        imported = Cancelled<GltfNewModelImportResult>();
      }
      catch (Exception ex)
      {
        imported = IoFailure<GltfNewModelImportResult>(ex);
      }
    }
    else
    {
      imported = plan?.Value is null
        ? await _interchange.ImportNewModelGltfFileAsync(
          input,
          cancellationToken: cancellationToken).ConfigureAwait(false)
        : await _interchange.ImportNewModelGltfFileWithPlanAsync(
          input,
          plan.Value,
          cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    var complete = await WriteImportedAssetAsync(
      imported,
      result => result.Asset,
      destination,
      cancellationToken)
      .ConfigureAwait(false);
    var operation = GltfCliReportOperation.ForNewModelImport(
      input,
      destination,
      packageKind,
      complete);
    var report = await WriteReportAsync(settings.ReportPath, operation)
      .ConfigureAwait(false);
    var finalStatus = AggregateStatus(complete.Status, report.Status);
    await WriteSummaryAsync(
      input,
      destination,
      finalStatus,
      complete.Diagnostics.Concat(report.Diagnostics).ToArray())
      .ConfigureAwait(false);
    return ToExitCode(finalStatus);
  }

  public async Task<int> ExportAsync(
    ExportGltfSettings settings,
    CancellationToken cancellationToken)
  {
    if (!Enum.IsDefined(typeof(GltfPackageKind), settings.Format)
      || settings.TextureSearchRoots.Any(root => !Path.IsPathRooted(root)))
    {
      return CliExitCode.Usage;
    }

    var input = Path.GetFullPath(settings.Input);
    var destination = GetDestination(input, settings.OutputDirectory, settings.Format);
    using var scope = _scopeFactory.CreateScope();
    var read = await scope.ServiceProvider.GetRequiredService<IMshReader>()
      .ReadFileAsync(input, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
    if (read.Value is not StaticMeshAsset asset)
    {
      var diagnostics = read.Diagnostics;
      var status = read.Status;
      if (read.Succeeded)
      {
        status = OperationStatus.Failed;
        diagnostics = read.Diagnostics.Concat(
        [
          new OperationDiagnostic(
            GltfDiagnosticCodes.UnsupportedDomain,
            1102,
            DiagnosticSeverity.Error,
            "$",
            "Dynamic mesh glTF transport is not supported.",
            data: new Dictionary<string, string> { ["domain"] = "DynamicMesh" })
        ]).ToArray();
      }
      var failed = new OperationResult<GltfExportReceipt>(status, diagnostics: diagnostics);
      var failedOperation = GltfCliReportOperation.ForFailedExport(
        input,
        destination,
        settings.Format,
        failed);
      var failedReport = await WriteReportAsync(settings.ReportPath, failedOperation)
        .ConfigureAwait(false);
      var finalStatus = AggregateStatus(failed.Status, failedReport.Status);
      await WriteSummaryAsync(
        input,
        destination,
        finalStatus,
        failed.Diagnostics.Concat(failedReport.Diagnostics).ToArray())
        .ConfigureAwait(false);
      return ToExitCode(finalStatus);
    }

    var options = new GltfExportOptions(textureSearchRoots: settings.TextureSearchRoots);
    var exported = settings.Format == GltfPackageKind.Glb
      ? await _interchange.ExportGlbFileAsync(
        asset,
        destination,
        options,
        cancellationToken: cancellationToken).ConfigureAwait(false)
      : await _interchange.ExportGltfFileAsync(
        asset,
        destination,
        options,
        cancellationToken: cancellationToken).ConfigureAwait(false);
    var combined = new OperationResult<GltfExportReceipt>(
      exported.Status,
      exported.Value,
      read.Diagnostics.Concat(exported.Diagnostics));
    var operation = GltfCliReportOperation.ForExport(
      input,
      destination,
      settings.Format,
      asset,
      combined);
    var report = await WriteReportAsync(settings.ReportPath, operation)
      .ConfigureAwait(false);
    var completeStatus = AggregateStatus(combined.Status, report.Status);
    await WriteSummaryAsync(
      input,
      destination,
      completeStatus,
      combined.Diagnostics.Concat(report.Diagnostics).ToArray())
      .ConfigureAwait(false);
    return ToExitCode(completeStatus);
  }

  private async Task<OperationResult> WriteReportAsync(
    string? reportPath,
    GltfCliReportOperation operation)
  {
    if (reportPath is null)
    {
      return new OperationResult(OperationStatus.Succeeded);
    }

    try
    {
      await using var destination = new FileStream(
        Path.GetFullPath(reportPath),
        FileMode.Create,
        FileAccess.Write,
        FileShare.None);
      var result = await _reportSerializer.SerializeAsync(
        new GltfCliReport([operation]),
        destination,
        cancellationToken: CancellationToken.None).ConfigureAwait(false);
      return result;
    }
    catch (OperationCanceledException)
    {
      return Cancelled();
    }
    catch (Exception ex)
    {
      return IoFailure(ex);
    }
  }

  private async Task<OperationResult<T>> WriteImportedAssetAsync<T>(
    OperationResult<T> imported,
    Func<T, MeshAsset> getAsset,
    string destination,
    CancellationToken cancellationToken)
    where T : class
  {
    if (!imported.Succeeded)
    {
      return imported;
    }

    using var scope = _scopeFactory.CreateScope();
    var write = await scope.ServiceProvider.GetRequiredService<IMshWriter>()
      .WriteFileAsync(getAsset(imported.Value!), destination, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
    var diagnostics = imported.Diagnostics.Concat(write.Diagnostics);
    return write.Succeeded
      ? new OperationResult<T>(
        OperationStatus.Succeeded,
        imported.Value,
        diagnostics)
      : new OperationResult<T>(write.Status, diagnostics: diagnostics);
  }

  private async Task<OperationResult<GltfImportPlan>?> ReadPlanAsync(
    string? planPath,
    CancellationToken cancellationToken)
  {
    if (planPath is null)
    {
      return null;
    }

    try
    {
      await using var source = new FileStream(
        Path.GetFullPath(planPath),
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read);
      return await _planSerializer.DeserializeAsync(source, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
      return Cancelled<GltfImportPlan>();
    }
    catch (Exception ex)
    {
      return IoFailure<GltfImportPlan>(ex);
    }
  }

  private async Task WriteSummaryAsync(
    string input,
    string destination,
    OperationStatus status,
    IReadOnlyList<OperationDiagnostic> diagnostics)
  {
    await _output.WriteLineAsync(
      $"outcome status={StatusName(status)} input={input} destination={destination} diagnostics={diagnostics.Count}")
      .ConfigureAwait(false);
    foreach (var diagnostic in diagnostics)
    {
      await _output.WriteLineAsync(
        $"diagnostic code={diagnostic.Code} eventId={diagnostic.EventId} severity={diagnostic.Severity} path={diagnostic.Path}")
        .ConfigureAwait(false);
    }
    var succeeded = status == OperationStatus.Succeeded ? 1 : 0;
    var failed = status == OperationStatus.Failed ? 1 : 0;
    var cancelled = status == OperationStatus.Cancelled ? 1 : 0;
    var warnings = diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    await _output.WriteLineAsync(
      $"summary total=1 succeeded={succeeded} failed={failed} cancelled={cancelled} warnings={warnings}")
      .ConfigureAwait(false);
  }

  private static string GetDestination(
    string input,
    string? outputDirectory,
    GltfPackageKind packageKind)
  {
    var directory = outputDirectory is null
      ? Path.GetDirectoryName(input)!
      : Path.GetFullPath(outputDirectory);
    var extension = packageKind == GltfPackageKind.Glb ? ".glb" : ".gltf";
    return Path.Combine(directory, Path.ChangeExtension(Path.GetFileName(input), extension));
  }

  private static string GetImportDestination(string input, string? outputDirectory)
  {
    var directory = outputDirectory is null
      ? Path.GetDirectoryName(input)!
      : Path.GetFullPath(outputDirectory);
    return Path.Combine(directory, Path.ChangeExtension(Path.GetFileName(input), ".msh"));
  }

  private static bool TryGetPackageKind(string input, out GltfPackageKind packageKind)
  {
    var extension = Path.GetExtension(input);
    if (extension.Equals(".glb", StringComparison.OrdinalIgnoreCase))
    {
      packageKind = GltfPackageKind.Glb;
      return true;
    }
    if (extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase))
    {
      packageKind = GltfPackageKind.Gltf;
      return true;
    }
    packageKind = default;
    return false;
  }

  private static bool TryCreateBaseline(
    ImportEditGltfSettings settings,
    out InterchangeBaseline? baseline)
  {
    try
    {
      baseline = new InterchangeBaseline(settings.ExpectedLineageId, settings.ExpectedDocumentId);
      return true;
    }
    catch (ArgumentException)
    {
      baseline = null;
      return false;
    }
  }

  private static bool ContainsPattern(string input)
  {
    return input.IndexOfAny(new[] { '*', '?', '[', ']' }) >= 0;
  }

  private static OperationResult<T> Cancelled<T>()
    where T : class
  {
    return new OperationResult<T>(OperationStatus.Cancelled, diagnostics:
    [
      new OperationDiagnostic(
        GltfDiagnosticCodes.Cancelled,
        1105,
        DiagnosticSeverity.Error,
        "$",
        "The operation was cancelled.")
    ]);
  }

  private static OperationResult Cancelled()
  {
    return new OperationResult(OperationStatus.Cancelled,
    [
      new OperationDiagnostic(
        GltfDiagnosticCodes.Cancelled,
        1105,
        DiagnosticSeverity.Error,
        "$",
        "The operation was cancelled.")
    ]);
  }

  private static OperationResult<T> IoFailure<T>(Exception exception)
    where T : class
  {
    return new OperationResult<T>(OperationStatus.Failed, diagnostics:
    [
      new OperationDiagnostic(
        GltfDiagnosticCodes.IoFailure,
        1104,
        DiagnosticSeverity.Error,
        "$",
        exception.Message)
    ]);
  }

  private static OperationResult IoFailure(Exception exception)
  {
    return new OperationResult(OperationStatus.Failed,
    [
      new OperationDiagnostic(
        GltfDiagnosticCodes.IoFailure,
        1104,
        DiagnosticSeverity.Error,
        "$",
        exception.Message)
    ]);
  }

  private static OperationStatus AggregateStatus(OperationStatus operation, OperationStatus report)
  {
    return operation == OperationStatus.Failed || report == OperationStatus.Failed
      ? OperationStatus.Failed
      : operation == OperationStatus.Cancelled || report == OperationStatus.Cancelled
        ? OperationStatus.Cancelled
        : OperationStatus.Succeeded;
  }

  private static int ToExitCode(OperationStatus status) => status switch
  {
    OperationStatus.Succeeded => CliExitCode.Success,
    OperationStatus.Failed => CliExitCode.Failure,
    OperationStatus.Cancelled => CliExitCode.Cancellation,
    _ => CliExitCode.Failure
  };

  private static string StatusName(OperationStatus status) => status switch
  {
    OperationStatus.Succeeded => "succeeded",
    OperationStatus.Failed => "failed",
    OperationStatus.Cancelled => "cancelled",
    _ => throw new ArgumentOutOfRangeException(nameof(status))
  };
}
