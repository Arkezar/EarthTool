#nullable enable

using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
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
  private readonly ICliReportFileSystem _reportFileSystem;
  private readonly TextWriter _output;

  public GltfCommandExecutor(
    IServiceScopeFactory scopeFactory,
    GltfInterchange interchange,
    GltfImportPlanSerializer planSerializer,
    GltfCliReportSerializer reportSerializer,
    ICliReportFileSystem reportFileSystem,
    CliOutput output)
  {
    _scopeFactory = scopeFactory;
    _interchange = interchange;
    _planSerializer = planSerializer;
    _reportSerializer = reportSerializer;
    _reportFileSystem = reportFileSystem;
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
    if (HasWritePathCollision([destination], settings.ReportPath))
    {
      await WritePreflightFailureAsync("The report path collides with the derived destination.")
        .ConfigureAwait(false);
      return CliExitCode.Failure;
    }
    var plan = await ReadPlanAsync(settings.PlanPath, cancellationToken).ConfigureAwait(false);
    OperationResult<GltfMeshEditImportResult> imported;
    if (plan is not null && !plan.Succeeded)
    {
      imported = new OperationResult<GltfMeshEditImportResult>(plan.Status, diagnostics: plan.Diagnostics);
    }
    else if (packageKind == GltfPackageKind.Glb)
    {
      try
      {
        await using var source = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read);
        imported = plan?.Value is null
          ? await _interchange.ImportEditMeshGlbAsync(
            source,
            expectedBaseline!,
            cancellationToken: cancellationToken).ConfigureAwait(false)
          : await _interchange.ImportEditMeshGlbWithPlanAsync(
            source,
            expectedBaseline!,
            plan.Value,
            cancellationToken: cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        imported = Cancelled<GltfMeshEditImportResult>();
      }
      catch (Exception ex)
      {
        imported = IoFailure<GltfMeshEditImportResult>(ex);
      }
    }
    else
    {
      imported = plan?.Value is null
        ? await _interchange.ImportEditMeshGltfFileAsync(
          input,
          expectedBaseline!,
          cancellationToken: cancellationToken).ConfigureAwait(false)
        : await _interchange.ImportEditMeshGltfFileWithPlanAsync(
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
    await WriteOutcomeAsync(operation).ConfigureAwait(false);
    return await CompleteInvocationAsync(settings.ReportPath, [operation]).ConfigureAwait(false);
  }

  public async Task<int> ImportNewAsync(
    ImportNewGltfSettings settings,
    CancellationToken cancellationToken)
  {
    if (settings.Inputs.Length == 0)
    {
      return CliExitCode.Usage;
    }

    var expansion = ExpandInputs(settings.Inputs);
    if (!expansion.Succeeded)
    {
      await WritePreflightFailureAsync(expansion.Error!).ConfigureAwait(false);
      return CliExitCode.Failure;
    }
    var inputs = expansion.Inputs!;
    var packageKinds = new GltfPackageKind[inputs.Count];
    for (var index = 0; index < inputs.Count; index++)
    {
      if (!TryGetPackageKind(inputs[index], out packageKinds[index]))
      {
        return CliExitCode.Usage;
      }
    }
    var destinations = inputs.Select(input =>
      GetImportDestination(input, settings.OutputDirectory)).ToArray();
    if (HasWritePathCollision(destinations, settings.ReportPath))
    {
      await WritePreflightFailureAsync("Derived destinations collide.").ConfigureAwait(false);
      return CliExitCode.Failure;
    }

    var plan = await ReadPlanAsync(settings.PlanPath, cancellationToken).ConfigureAwait(false);
    var operations = new List<GltfCliReportOperation>(inputs.Count);
    for (var index = 0; index < inputs.Count; index++)
    {
      var operation = await ImportNewOneAsync(
        inputs[index],
        destinations[index],
        packageKinds[index],
        plan,
        cancellationToken).ConfigureAwait(false);
      operations.Add(operation);
      await WriteOutcomeAsync(operation).ConfigureAwait(false);
      if (operation.Status == OperationStatus.Cancelled)
      {
        break;
      }
    }

    return await CompleteInvocationAsync(settings.ReportPath, operations).ConfigureAwait(false);
  }

  private async Task<GltfCliReportOperation> ImportNewOneAsync(
    string input,
    string destination,
    GltfPackageKind packageKind,
    OperationResult<GltfImportPlan>? plan,
    CancellationToken cancellationToken)
  {
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
    return operation;
  }

  public async Task<int> ExportAsync(
    ExportGltfSettings settings,
    CancellationToken cancellationToken)
  {
    if (!Enum.IsDefined(typeof(GltfPackageKind), settings.Format)
      || settings.TextureSearchRoots.Any(root => !Path.IsPathFullyQualified(root))
      || settings.MeshResourceSearchRoots.Any(root => !Path.IsPathFullyQualified(root))
      || settings.Inputs.Length == 0)
    {
      return CliExitCode.Usage;
    }

    var expansion = ExpandInputs(settings.Inputs);
    if (!expansion.Succeeded)
    {
      await WritePreflightFailureAsync(expansion.Error!).ConfigureAwait(false);
      return CliExitCode.Failure;
    }

    var inputs = expansion.Inputs!;
    var destinations = inputs.Select(input =>
      GetDestination(input, settings.OutputDirectory, settings.Format)).ToArray();
    if (HasWritePathCollision(
      destinations,
      settings.ReportPath,
      reservesContentAddressedSidecars: settings.Format == GltfPackageKind.Gltf))
    {
      await WritePreflightFailureAsync("Derived destinations collide.").ConfigureAwait(false);
      return CliExitCode.Failure;
    }
    if (settings.OutputDirectory is not null)
    {
      try
      {
        Directory.CreateDirectory(Path.GetFullPath(settings.OutputDirectory));
      }
      catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
      {
        await WritePreflightFailureAsync($"Output directory could not be created: {ex.Message}")
          .ConfigureAwait(false);
        return CliExitCode.Failure;
      }
    }

    var operations = new List<GltfCliReportOperation>(inputs.Count);
    for (var index = 0; index < inputs.Count; index++)
    {
      var operation = await ExportOneAsync(
        inputs[index],
        destinations[index],
        settings,
        cancellationToken).ConfigureAwait(false);
      operations.Add(operation);
      await WriteOutcomeAsync(operation).ConfigureAwait(false);
      if (operation.Status == OperationStatus.Cancelled)
      {
        break;
      }
    }

    return await CompleteInvocationAsync(settings.ReportPath, operations).ConfigureAwait(false);
  }

  private async Task<GltfCliReportOperation> ExportOneAsync(
    string input,
    string destination,
    ExportGltfSettings settings,
    CancellationToken cancellationToken)
  {
    using var scope = _scopeFactory.CreateScope();
    var read = await scope.ServiceProvider.GetRequiredService<IMshReader>()
      .ReadFileAsync(input, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
    if (read.Value is null)
    {
      var failed = new OperationResult<GltfExportReceipt>(read.Status, diagnostics: read.Diagnostics);
      var failedOperation = GltfCliReportOperation.ForFailedExport(
        input,
        destination,
        settings.Format,
        failed);
      return failedOperation;
    }

    var sourceBaseName = Path.GetFileNameWithoutExtension(input);
    var options = new GltfExportOptions(
      null,
      null,
      settings.TextureSearchRoots,
      null,
      settings.MeshResourceSearchRoots,
      string.IsNullOrWhiteSpace(sourceBaseName) ? null : sourceBaseName);
    var asset = read.Value;
    var exported = await asset.Match(
      onStatic: staticAsset => settings.Format == GltfPackageKind.Glb
        ? _interchange.ExportGlbFileAsync(
          staticAsset,
          destination,
          options,
          cancellationToken: cancellationToken)
        : _interchange.ExportGltfFileAsync(
          staticAsset,
          destination,
          options,
          cancellationToken: cancellationToken),
      onDynamic: dynamicAsset => settings.Format == GltfPackageKind.Glb
        ? _interchange.ExportGlbFileAsync(
          dynamicAsset,
          destination,
          options,
          cancellationToken: cancellationToken)
        : _interchange.ExportGltfFileAsync(
          dynamicAsset,
          destination,
          options,
          cancellationToken: cancellationToken)).ConfigureAwait(false);
    var combined = new OperationResult<GltfExportReceipt>(
      exported.Status,
      exported.Value,
      read.Diagnostics.Concat(exported.Diagnostics));
    var operation = asset.Match(
      onStatic: staticAsset => GltfCliReportOperation.ForExport(
        input,
        destination,
        settings.Format,
        staticAsset,
        combined),
      onDynamic: dynamicAsset => GltfCliReportOperation.ForExport(
        input,
        destination,
        settings.Format,
        dynamicAsset,
        combined));
    return operation;
  }

  private async Task<int> CompleteInvocationAsync(
    string? reportPath,
    IReadOnlyList<GltfCliReportOperation> operations)
  {
    var invocationReport = new GltfCliReport(operations);
    var report = await WriteReportAsync(reportPath, invocationReport).ConfigureAwait(false);
    foreach (var diagnostic in report.Diagnostics)
    {
      await WriteDiagnosticAsync(diagnostic).ConfigureAwait(false);
    }
    var finalStatus = AggregateStatus(invocationReport.Status, report.Status);
    await WriteAggregateSummaryAsync(operations, finalStatus, report)
      .ConfigureAwait(false);
    return ToExitCode(finalStatus);
  }

  private async Task WriteOutcomeAsync(GltfCliReportOperation operation)
  {
    await _output.WriteLineAsync(
      $"outcome status={StatusName(operation.Status)} input={operation.Input} destination={operation.Destination} diagnostics={operation.Diagnostics.Count}")
      .ConfigureAwait(false);
    foreach (var diagnostic in operation.Diagnostics)
    {
      await WriteDiagnosticAsync(diagnostic).ConfigureAwait(false);
    }
  }

  private Task WriteDiagnosticAsync(OperationDiagnostic diagnostic)
  {
    return _output.WriteLineAsync(
      $"diagnostic code={diagnostic.Code} eventId={diagnostic.EventId} severity={diagnostic.Severity} path={diagnostic.Path}");
  }

  private async Task WriteAggregateSummaryAsync(
    IReadOnlyList<GltfCliReportOperation> operations,
    OperationStatus finalStatus,
    OperationResult report)
  {
    var reportFailed = report.Status == OperationStatus.Failed;
    var succeeded = reportFailed ? 0 : operations.Count(operation => operation.Status == OperationStatus.Succeeded);
    var failed = reportFailed ? Math.Max(1, operations.Count) : operations.Count(operation => operation.Status == OperationStatus.Failed);
    var cancelled = reportFailed ? 0 : operations.Count(operation => operation.Status == OperationStatus.Cancelled);
    var warnings = operations.SelectMany(operation => operation.Diagnostics)
      .Concat(report.Diagnostics)
      .Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
    await _output.WriteLineAsync(
      $"summary total={operations.Count} succeeded={succeeded} failed={failed} cancelled={cancelled} warnings={warnings} status={StatusName(finalStatus)}")
      .ConfigureAwait(false);
  }

  private async Task WritePreflightFailureAsync(string message)
  {
    await _output.WriteLineAsync($"preflight status=failed message={message}").ConfigureAwait(false);
    await _output.WriteLineAsync("summary total=0 succeeded=0 failed=1 cancelled=0 warnings=0 status=failed")
      .ConfigureAwait(false);
  }

  private async Task<OperationResult> WriteReportAsync(
    string? reportPath,
    GltfCliReport report)
  {
    if (reportPath is null)
    {
      return new OperationResult(OperationStatus.Succeeded);
    }

    string? temporaryPath = null;
    try
    {
      var destinationPath = Path.GetFullPath(reportPath);
      temporaryPath = _reportFileSystem.GetTemporaryPath(destinationPath);
      await using var destination = _reportFileSystem.CreateTemporary(temporaryPath);
      var result = await _reportSerializer.SerializeAsync(
        report,
        destination,
        cancellationToken: CancellationToken.None).ConfigureAwait(false);
      if (!result.Succeeded)
      {
        return result;
      }
      await destination.FlushAsync(CancellationToken.None).ConfigureAwait(false);
      destination.Close();
      _reportFileSystem.Commit(temporaryPath, destinationPath);
      temporaryPath = null;
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
    finally
    {
      if (temporaryPath is not null)
      {
        _reportFileSystem.TryDelete(temporaryPath);
      }
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

  private static (
    bool Succeeded,
    IReadOnlyList<string>? Inputs,
    string? Error) ExpandInputs(IEnumerable<string> inputs)
  {
    var expanded = new List<string>();
    foreach (var value in inputs)
    {
      try
      {
        if (!ContainsPattern(value))
        {
          expanded.Add(Path.GetFullPath(value));
          continue;
        }

        var directory = Path.GetDirectoryName(value);
        var pattern = Path.GetFileName(value);
        if (string.IsNullOrEmpty(directory))
        {
          directory = Directory.GetCurrentDirectory();
        }
        if (string.IsNullOrEmpty(pattern)
          || ContainsPattern(directory)
          || !Directory.Exists(directory))
        {
          return (false, null, $"Input pattern matched no files: {value}");
        }

        var matches = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
          .Where(path => FileSystemName.MatchesSimpleExpression(
            pattern,
            Path.GetFileName(path),
            ignoreCase: true))
          .Select(Path.GetFullPath)
          .OrderBy(path => path, StringComparer.Ordinal)
          .ToArray();
        if (matches.Length == 0)
        {
          return (false, null, $"Input pattern matched no files: {value}");
        }
        expanded.AddRange(matches);
      }
      catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
      {
        return (false, null, $"Input expansion failed for {value}: {ex.Message}");
      }
    }

    return expanded.Count == 0
      ? (false, null, "No input files were specified.")
      : (true, expanded, null);
  }

  private static bool HasWritePathCollision(
    IEnumerable<string> destinations,
    string? reportPath,
    bool reservesContentAddressedSidecars = false)
  {
    var comparer = OperatingSystem.IsWindows()
      ? StringComparer.OrdinalIgnoreCase
      : StringComparer.Ordinal;
    var paths = new HashSet<string>(comparer);
    var directories = new HashSet<string>(comparer);
    foreach (var destination in destinations)
    {
      var fullPath = Path.GetFullPath(destination);
      if (!paths.Add(fullPath))
      {
        return true;
      }
      directories.Add(Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory());
    }
    if (reportPath is null)
    {
      return false;
    }

    var reportFullPath = Path.GetFullPath(reportPath);
    return !paths.Add(reportFullPath)
      || reservesContentAddressedSidecars
      && directories.Contains(Path.GetDirectoryName(reportFullPath) ?? Directory.GetCurrentDirectory())
      && IsContentAddressedSidecar(Path.GetFileName(reportFullPath));
  }

  private static bool IsContentAddressedSidecar(string fileName)
  {
    var extension = Path.GetExtension(fileName);
    if (!extension.Equals(".bin", StringComparison.OrdinalIgnoreCase)
      && !extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    var contentAddress = Path.GetFileNameWithoutExtension(fileName);
    return contentAddress.Length == 64 && contentAddress.All(character =>
      character is >= '0' and <= '9'
        or >= 'a' and <= 'f'
        or >= 'A' and <= 'F');
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
