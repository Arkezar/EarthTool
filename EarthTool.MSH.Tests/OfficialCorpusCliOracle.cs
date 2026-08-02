using EarthTool.Common.Operations;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace EarthTool.MSH.Tests;

internal static class OfficialCorpusCliOracle
{
  internal static async Task<CliOracleResult> RunAsync(
    byte[] canonicalMsh,
    string package,
    string workingDirectory,
    string textureRoot)
  {
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var directory = Path.Combine(workingDirectory, "cli-" + package);
    var exportDirectory = Path.Combine(directory, "export");
    var importDirectory = Path.Combine(directory, "import");
    Directory.CreateDirectory(exportDirectory);
    Directory.CreateDirectory(importDirectory);
    var inputPath = Path.Combine(directory, "source.msh");
    var exportReport = Path.Combine(directory, "export-report.json");
    var importReport = Path.Combine(directory, "import-report.json");
    await File.WriteAllBytesAsync(inputPath, canonicalMsh);

    try
    {
      var export = await RunProcessAsync(root, [
        "msh", "export", inputPath,
        "--format", package == "glb" ? "Glb" : "Gltf",
        "--tex-root", textureRoot,
        "--output", exportDirectory,
        "--report", exportReport
      ]);
      var exportOperation = await ReadOperationAsync(exportReport);
      if (export.ExitCode != 0 || !exportOperation.Succeeded)
      {
        return new CliOracleResult(false, false, null, 0, 0, exportOperation.Diagnostics, []);
      }

      var packagePath = Path.Combine(exportDirectory, "source." + package);
      var packageBytes = Directory.EnumerateFiles(exportDirectory, "*", SearchOption.AllDirectories)
        .Sum(path => new FileInfo(path).Length);
      var import = await RunProcessAsync(root, [
        "msh", "import", "edit", packagePath,
        "--expected-lineage", exportOperation.AssetLineageId!.Value.ToString("D"),
        "--expected-document", exportOperation.DocumentId!.Value.ToString("D"),
        "--output", importDirectory,
        "--report", importReport
      ]);
      var importOperation = await ReadOperationAsync(importReport);
      var outputPath = Path.Combine(importDirectory, "source.msh");
      var importedBytes = File.Exists(outputPath)
        ? await File.ReadAllBytesAsync(outputPath)
        : [];
      var importedMatches = import.ExitCode == 0
        && importOperation.Succeeded
        && importedBytes.AsSpan().SequenceEqual(canonicalMsh);
      return new CliOracleResult(
        true,
        importedMatches,
        packagePath,
        packageBytes,
        importedBytes.LongLength,
        exportOperation.Diagnostics,
        importOperation.Diagnostics);
    }
    catch
    {
      return new CliOracleResult(false, false, null, 0, 0, [], []);
    }
  }

  private static async Task<CliProcessResult> RunProcessAsync(
    string root,
    IReadOnlyList<string> arguments)
  {
    var startInfo = new ProcessStartInfo(ResolveExecutable(root))
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = root
    };
    foreach (var argument in arguments)
    {
      startInfo.ArgumentList.Add(argument);
    }
    using var process = Process.Start(startInfo)
      ?? throw new InvalidOperationException("The built EarthTool CLI process could not start.");
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    try
    {
      await process.WaitForExitAsync(timeout.Token);
    }
    catch (OperationCanceledException)
    {
      process.Kill(entireProcessTree: true);
      await process.WaitForExitAsync();
      throw new TimeoutException("The EarthTool CLI qualification process timed out.");
    }
    await Task.WhenAll(output, error);
    return new CliProcessResult(process.ExitCode);
  }

  private static string ResolveExecutable(string root)
  {
    var platform = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
    var architecture = RuntimeInformation.OSArchitecture switch
    {
      Architecture.X64 => "x64",
      Architecture.Arm64 => "arm64",
      var unsupported => throw new PlatformNotSupportedException(
        $"Unsupported CLI qualification architecture: {unsupported}.")
    };
    return Path.Combine(
      root,
      "EarthTool.CLI",
      "bin",
      "Release",
      "net8.0",
      $"{platform}-{architecture}",
      OperatingSystem.IsWindows() ? "EarthTool.CLI.exe" : "EarthTool.CLI");
  }

  private static async Task<CliReportOperation> ReadOperationAsync(string reportPath)
  {
    using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = document.RootElement.GetProperty("operations")[0];
    var diagnostics = operation.GetProperty("diagnostics").EnumerateArray()
      .Select(item => new CliDiagnostic(
        item.GetProperty("code").GetString() ?? string.Empty,
        item.GetProperty("eventId").GetInt32(),
        ParseSeverity(item.GetProperty("severity").GetString())))
      .ToArray();
    Guid? lineageId = null;
    Guid? documentId = null;
    var identities = operation.GetProperty("identities");
    if (identities.GetProperty("baseline") is { ValueKind: JsonValueKind.Object } baseline)
    {
      lineageId = baseline.GetProperty("assetLineageId").GetGuid();
      documentId = baseline.GetProperty("documentId").GetGuid();
    }
    return new CliReportOperation(
      operation.GetProperty("status").GetString() == "succeeded",
      lineageId,
      documentId,
      diagnostics);
  }

  private static DiagnosticSeverity ParseSeverity(string? severity)
  {
    return severity switch
    {
      "information" => DiagnosticSeverity.Information,
      "warning" => DiagnosticSeverity.Warning,
      "error" => DiagnosticSeverity.Error,
      _ => throw new InvalidDataException("The CLI report contains an unsupported diagnostic severity.")
    };
  }

  private sealed record CliProcessResult(int ExitCode);

  private sealed record CliReportOperation(
    bool Succeeded,
    Guid? AssetLineageId,
    Guid? DocumentId,
    IReadOnlyList<CliDiagnostic> Diagnostics);
}

internal sealed record CliOracleResult(
  bool ExportSucceeded,
  bool ImportSucceeded,
  string? PackagePath,
  long PackageBytes,
  long ImportedMshBytes,
  IReadOnlyList<CliDiagnostic> ExportDiagnostics,
  IReadOnlyList<CliDiagnostic> ImportDiagnostics);

internal sealed record CliDiagnostic(
  string Code,
  int EventId,
  DiagnosticSeverity Severity);
