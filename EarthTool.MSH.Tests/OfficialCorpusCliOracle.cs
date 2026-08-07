using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace EarthTool.MSH.Tests;

internal static class OfficialCorpusCliOracle
{
  private const string ExecutableEnvironmentVariable = "EARTHTOOL_OFFICIAL_CLI_EXECUTABLE";

  internal static async Task<CliOracleResult> RunAsync(
    byte[] canonicalMsh,
    GltfPackageKind packageKind,
    string workingDirectory,
    string textureRoot)
  {
    var package = packageKind == GltfPackageKind.Glb ? "glb" : "gltf";
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var directory = Path.Combine(workingDirectory, "cli-" + package);
    var exportDirectory = Path.Combine(directory, "export");
    var importDirectory = Path.Combine(directory, "import");
    Directory.CreateDirectory(exportDirectory);
    Directory.CreateDirectory(importDirectory);
    var inputPath = Path.Combine(directory, "source.msh");
    var exportReport = Path.Combine(directory, "export-report.json");
    var importReport = Path.Combine(directory, "import-report.json");
    var temporaryIoDuration = TimeSpan.Zero;
    var exportDuration = TimeSpan.Zero;
    var importDuration = TimeSpan.Zero;
    var ioStarted = Stopwatch.GetTimestamp();
    await File.WriteAllBytesAsync(inputPath, canonicalMsh);
    temporaryIoDuration += Stopwatch.GetElapsedTime(ioStarted);

    try
    {
      var packagePath = Path.Combine(exportDirectory, "source." + package);
      var exportStarted = Stopwatch.GetTimestamp();
      var export = await RunProcessAsync(root, [
        "msh", "export", inputPath,
        "--format", packageKind == GltfPackageKind.Glb ? "Glb" : "Gltf",
        "--tex-root", textureRoot,
        "--msh-root", textureRoot,
        "--output", exportDirectory,
        "--report", exportReport
      ]);
      var exportOperation = await ReadOperationAsync(exportReport, "export", package, packagePath);
      exportDuration = Stopwatch.GetElapsedTime(exportStarted);
      if (export.ExitCode != 0 || !exportOperation.Succeeded)
      {
        return new CliOracleResult(
          false,
          false,
          null,
          null,
          0,
          0,
          exportOperation.Diagnostics,
          [],
          exportDuration,
          importDuration,
          temporaryIoDuration);
      }

      ioStarted = Stopwatch.GetTimestamp();
      var packageBytes = Directory.EnumerateFiles(exportDirectory, "*", SearchOption.AllDirectories)
        .Sum(path => new FileInfo(path).Length);
      temporaryIoDuration += Stopwatch.GetElapsedTime(ioStarted);
      var importStarted = Stopwatch.GetTimestamp();
      var outputPath = Path.Combine(importDirectory, "source.msh");
      await using var canonicalSource = new MemoryStream(canonicalMsh, writable: false);
      var canonicalAsset = await new MshReader().ReadAsync(canonicalSource);
      if (!canonicalAsset.Succeeded)
      {
        throw new InvalidDataException("The canonical qualification MSH could not be read.");
      }
      var importOptions = CreateImportOptions(canonicalAsset.Value!);
      var planPath = Path.Combine(directory, "import-plan.json");
      await WritePlanAsync(packagePath, packageKind, importOptions, planPath);
      var import = await RunProcessAsync(root, [
        "msh", "import", packagePath,
        "--plan", planPath,
        "--output", importDirectory,
        "--report", importReport
      ]);
      var importOperation = await ReadOperationAsync(importReport, "import", package, outputPath);
      importDuration = Stopwatch.GetElapsedTime(importStarted);
      ioStarted = Stopwatch.GetTimestamp();
      var importedBytes = File.Exists(outputPath)
        ? await File.ReadAllBytesAsync(outputPath)
        : [];
      temporaryIoDuration += Stopwatch.GetElapsedTime(ioStarted);
      OperationResult<MeshAsset> libraryImport;
      if (packageKind == GltfPackageKind.Glb)
      {
        await using var packageSource = File.OpenRead(packagePath);
        libraryImport = await new GltfInterchange().CreateMeshAsync(packageSource, importOptions);
      }
      else
      {
        libraryImport = await new GltfInterchange().CreateMeshFileAsync(packagePath, importOptions);
      }
      var semanticMatch = false;
      if (libraryImport.Succeeded)
      {
        var cliAsset = await new MshReader().ReadAsync(new MemoryStream(importedBytes));
        semanticMatch = cliAsset.Succeeded
          && OfficialCorpusQualification.ComputeSemanticDigest(
            libraryImport.Value!,
            includeCreationGuid: false)
            == OfficialCorpusQualification.ComputeSemanticDigest(
            cliAsset.Value!,
            includeCreationGuid: false);
      }
      var importedMatches = import.ExitCode == 0
        && importOperation.Succeeded
        && importOperation.ReportValid
        && importOperation.AssetKind == exportOperation.AssetKind
        && semanticMatch;
      return new CliOracleResult(
        exportOperation.ReportValid,
        importedMatches,
        packagePath,
        exportOperation.AssetKind,
        packageBytes,
        importedBytes.LongLength,
        exportOperation.Diagnostics,
        importOperation.Diagnostics,
        exportDuration,
        importDuration,
        temporaryIoDuration);
    }
    catch
    {
      return new CliOracleResult(
        false,
        false,
        null,
        null,
        0,
        0,
        [],
        [],
        exportDuration,
        importDuration,
        temporaryIoDuration);
    }
  }

  internal static async Task<CliBatchOracleResult> RunExportAllMeshesAsync(
    string corpusRoot,
    string workingDirectory)
  {
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var inputDirectory = Directory.Exists(Path.Combine(corpusRoot, "meshes"))
      ? Path.Combine(corpusRoot, "meshes")
      : corpusRoot;
    var directory = Path.Combine(workingDirectory, "export-all-meshes");
    var outputDirectory = Path.Combine(directory, "output");
    var reportPath = Path.Combine(directory, "report.json");
    Directory.CreateDirectory(outputDirectory);
    try
    {
      var process = await RunProcessAsync(root, [
        "msh", "export",
        "--tex-root", corpusRoot,
        "--msh-root", corpusRoot,
        "--output", outputDirectory,
        "--report", reportPath,
        Path.Combine(inputDirectory, "*.msh")
      ]);
      using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
      var report = document.RootElement;
      var operations = report.GetProperty("operations").EnumerateArray().ToArray();
      var staticAssets = operations.Count(operation =>
        operation.GetProperty("assetKind").GetString() == "static");
      var dynamicAssets = operations.Count(operation =>
        operation.GetProperty("assetKind").GetString() == "dynamic");
      var succeeded = operations.Count(operation =>
        operation.GetProperty("status").GetString() == "succeeded");
      var failed = operations.Count(operation =>
        operation.GetProperty("status").GetString() == "failed");
      var cancelled = operations.Count(operation =>
        operation.GetProperty("status").GetString() == "cancelled");
      var unsupported = operations
        .SelectMany(operation => operation.GetProperty("diagnostics").EnumerateArray())
        .Count(diagnostic => diagnostic.GetProperty("code").GetString()
          == GltfDiagnosticCodes.UnsupportedDomain);
      var outputFiles = Directory.EnumerateFiles(outputDirectory, "*.glb", SearchOption.TopDirectoryOnly)
        .Count();
      var reportValid = report.GetProperty("format").GetString() == GltfCliReportFormat.Identifier
        && report.GetProperty("version").GetInt32() == GltfCliReportFormat.Version
        && report.GetProperty("status").GetString() == "succeeded"
        && operations.All(operation => operation.GetProperty("kind").GetString() == "export"
          && operation.GetProperty("package").GetString() == "glb");
      return new CliBatchOracleResult(
        process.ExitCode == 0 && reportValid,
        operations.Length,
        staticAssets,
        dynamicAssets,
        succeeded,
        failed,
        cancelled,
        unsupported,
        outputFiles);
    }
    catch
    {
      return new CliBatchOracleResult(false, 0, 0, 0, 0, 1, 0, 0, 0);
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

  internal static GltfNewModelImportOptions CreateImportOptions(MeshAsset asset)
  {
    var textureBindings = new Dictionary<GltfMaterialHandle, string?>();
    var meshBindings = new Dictionary<GltfNodeHandle, string>();
    if (asset is StaticMeshAsset staticAsset)
    {
      for (var index = 0; index < staticAsset.StaticRenderObjectSequence.Count; index++)
      {
        var binding = staticAsset.StaticRenderObjectSequence[index].TexturePathBytes;
        if (binding.Count != 0)
        {
          textureBindings[new GltfMaterialHandle(index + 1)] = Encoding.ASCII.GetString(
            binding.ToArray());
        }
      }
    }
    else if (asset is DynamicMeshAsset dynamicAsset)
    {
      var objects = Flatten(dynamicAsset.RootDynamicObject).ToArray();
      var material = 1;
      for (var index = 0; index < objects.Length; index++)
      {
        var extension = objects[index].Extension;
        if (DynamicGltfDocument.HasNativePreview(extension.KnownEffectType))
        {
          if (extension.TexturePathBytes.Count != 0)
          {
            textureBindings[new GltfMaterialHandle(material)] = Encoding.ASCII.GetString(
              extension.TexturePathBytes.ToArray());
          }
          material++;
        }
        if (extension.MeshNameBytes.Count != 0)
        {
          meshBindings[new GltfNodeHandle(index + 2)] = Encoding.ASCII.GetString(
            extension.MeshNameBytes.ToArray());
        }
      }
    }
    return new GltfNewModelImportOptions(
      textureResourceBindings: textureBindings,
      meshResourceBindings: meshBindings);
  }

  private static IEnumerable<DynamicObject> Flatten(DynamicObject root)
  {
    yield return root;
    foreach (var child in root.Children)
    {
      foreach (var descendant in Flatten(child))
      {
        yield return descendant;
      }
    }
  }

  private static async Task WritePlanAsync(
    string packagePath,
    GltfPackageKind packageKind,
    GltfNewModelImportOptions options,
    string planPath)
  {
    var serializer = new GltfImportPlanSerializer();
    OperationResult<string> digest;
    if (packageKind == GltfPackageKind.Glb)
    {
      await using var source = File.OpenRead(packagePath);
      digest = await serializer.ComputeGlbSourceSha256Async(source);
    }
    else
    {
      digest = await serializer.ComputeGltfSourceSha256Async(packagePath);
    }
    if (!digest.Succeeded)
    {
      throw new InvalidDataException("The qualification package digest could not be created.");
    }
    await using var destination = File.Create(planPath);
    var write = await serializer.SerializeAsync(
      GltfImportPlan.CreateNewModel(packageKind, digest.Value!, options),
      destination);
    if (!write.Succeeded)
    {
      throw new InvalidDataException("The qualification import plan could not be written.");
    }
  }

  private static string ResolveExecutable(string root)
  {
    var packagedExecutable = Environment.GetEnvironmentVariable(ExecutableEnvironmentVariable);
    if (!string.IsNullOrWhiteSpace(packagedExecutable))
    {
      var fullPath = Path.GetFullPath(packagedExecutable);
      if (!File.Exists(fullPath))
      {
        throw new FileNotFoundException("The packaged EarthTool CLI executable was not found.", fullPath);
      }
      return fullPath;
    }
    var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name
      ?? throw new InvalidOperationException("The test build configuration could not be resolved.");
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
      configuration,
      "net8.0",
      $"{platform}-{architecture}",
      OperatingSystem.IsWindows() ? "EarthTool.CLI.exe" : "EarthTool.CLI");
  }

  private static async Task<CliReportOperation> ReadOperationAsync(
    string reportPath,
    string expectedKind,
    string expectedPackage,
    string expectedDestination)
  {
    using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var root = document.RootElement;
    var operations = root.GetProperty("operations");
    if (root.GetProperty("format").GetString() != GltfCliReportFormat.Identifier
      || root.GetProperty("version").GetInt32() != GltfCliReportFormat.Version
      || operations.GetArrayLength() != 1)
    {
      throw new InvalidDataException("The CLI report contract does not match the qualification oracle.");
    }
    var operation = operations[0];
    var diagnostics = operation.GetProperty("diagnostics").EnumerateArray()
      .Select(item => new CliDiagnostic(
        item.GetProperty("code").GetString() ?? string.Empty,
        item.GetProperty("eventId").GetInt32(),
        ParseSeverity(item.GetProperty("severity").GetString())))
      .ToArray();
    var identities = operation.GetProperty("identities");
    var succeeded = operation.GetProperty("status").GetString() == "succeeded";
    var assetKind = operation.GetProperty("assetKind").ValueKind == JsonValueKind.String
      ? operation.GetProperty("assetKind").GetString()
      : null;
    var successfulContract = !succeeded
      || assetKind is not null
        && identities.EnumerateObject().Select(property => property.Name)
          .SequenceEqual(["meshCreationGuid"]);
    var reportValid = operation.GetProperty("kind").GetString() == expectedKind
      && operation.GetProperty("package").GetString() == expectedPackage
      && root.GetProperty("status").GetString() == (succeeded ? "succeeded" : "failed")
      && operation.GetProperty("index").GetInt32() == 0
      && Path.GetFullPath(operation.GetProperty("destination").GetString() ?? string.Empty)
        == Path.GetFullPath(expectedDestination)
      && successfulContract;
    return new CliReportOperation(
      succeeded,
      reportValid,
      assetKind,
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
    bool ReportValid,
    string? AssetKind,
    IReadOnlyList<CliDiagnostic> Diagnostics);
}

internal sealed record CliOracleResult(
  bool ExportSucceeded,
  bool ImportSucceeded,
  string? PackagePath,
  string? AssetKind,
  long PackageBytes,
  long ImportedMshBytes,
  IReadOnlyList<CliDiagnostic> ExportDiagnostics,
  IReadOnlyList<CliDiagnostic> ImportDiagnostics,
  TimeSpan ExportDuration,
  TimeSpan ImportDuration,
  TimeSpan TemporaryIoDuration);

internal sealed record CliBatchOracleResult(
  bool Succeeded,
  int Assets,
  int StaticAssets,
  int DynamicAssets,
  int SuccessfulOperations,
  int FailedOperations,
  int CancelledOperations,
  int UnsupportedDomainDiagnostics,
  int OutputFiles);

internal sealed record CliDiagnostic(
  string Code,
  int EventId,
  DiagnosticSeverity Severity);
