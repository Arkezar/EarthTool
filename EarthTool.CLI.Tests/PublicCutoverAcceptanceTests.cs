using AwesomeAssertions;
using EarthTool.CLI.Commands.MSH;
using EarthTool.Common.Enums;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EarthTool.CLI.Tests;

public sealed partial class PublicCutoverAcceptanceTests
{
  [Fact]
  public async Task CliProcessExposesOnlyTheGltfMshCommandTreeAndStableExitStatuses()
  {
    var rootHelp = await RunCliAsync("--help");
    var mshHelp = await RunCliAsync("msh", "--help");
    var importHelp = await RunCliAsync("msh", "import", "--help");
    var invalidImport = await RunCliAsync("msh", "import");
    var failedExport = await RunCliAsync(
      "msh",
      "export",
      Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.msh"));

    rootHelp.ExitCode.Should().Be(0);
    rootHelp.Output.Should().Contain("msh").And.NotContain("dae <InputFilePath>");
    mshHelp.ExitCode.Should().Be(0);
    mshHelp.Output.Should().Contain("export <INPUT>").And.Contain("import <INPUT>");
    mshHelp.Output.Should().NotContain("output-format");
    importHelp.ExitCode.Should().Be(0);
    importHelp.Output.Should().Contain("--plan").And.NotContain("edit <INPUT>").And.NotContain("new <INPUT>");
    invalidImport.ExitCode.Should().Be(CliExitCode.Usage);
    failedExport.ExitCode.Should().Be(CliExitCode.Failure);

    VerifyHelpApproval(rootHelp.Output, mshHelp.Output, importHelp.Output);
  }

  [Fact]
  public void ReleaseAssemblySurfaceContainsGltfWithoutLegacyConversionSurfaces()
  {
    var fileTypes = Enum.GetNames<FileType>();
    var mshTypes = typeof(MeshAsset).Assembly.ExportedTypes.ToArray();
    var cliReferences = typeof(InternalMshCommandHost).Assembly.GetReferencedAssemblies()
      .Select(reference => reference.Name);

    fileTypes.Should().Contain(["GLB", "GLTF"]).And.NotContain("DAE");
    mshTypes.Any(type => type.Namespace is "EarthTool.MSH.Enums"
      or "EarthTool.MSH.Interfaces"
      or "EarthTool.MSH.Models"
      or "EarthTool.MSH.Models.Collections"
      or "EarthTool.MSH.Models.Elements").Should().BeFalse();
    mshTypes.Any(type => type.Name is "EarthMeshReader"
      or "EarthMeshWriter"
      or "HierarchyBuilder").Should().BeFalse();
    cliReferences.Should().Contain("EarthTool.GLTF").And.NotContain("EarthTool.DAE");
  }

  [Fact]
  public void GltfPublicSurfaceExposesOnlyUnifiedMeshCreation()
  {
    var gltfTypes = typeof(GltfInterchange).Assembly.ExportedTypes
      .Where(type => type.Namespace == "EarthTool.GLTF")
      .ToArray();
    var interchangeMethods = typeof(GltfInterchange).GetMethods()
      .Where(method => method.DeclaringType == typeof(GltfInterchange))
      .ToArray();

    gltfTypes.Select(type => type.Name).Should().NotContain([
      "GltfExportReceipt",
      "GltfDynamicEditImportResult",
      "GltfEditImportOptions",
      "GltfEditImportResult",
      "GltfImportPlanKind",
      "GltfMeshCreationResult",
      "GltfMeshEditImportResult",
      "GltfMetadataConflictActions",
      "GltfMetadataConflictCatalog",
      "GltfMetadataConflictResolution",
      "GltfMetadataLineageDisposition",
      "GltfNewModelImportResult",
      "InterchangeBaseline",
      "NativeProjectionFingerprint",
      "PreservationChange",
      "PreservationDisposition",
      "PreservationReport"
    ]);
    interchangeMethods.Select(method => method.Name).Should()
      .Contain(["CreateMeshAsync", "CreateMeshFileAsync"])
      .And.NotContain(method => method.StartsWith("ImportEdit", StringComparison.Ordinal)
        || method.StartsWith("ImportNewModel", StringComparison.Ordinal));
    typeof(GltfExportOptions).GetProperties().Select(property => property.Name).Should().NotContain([
      "AssetLineageId",
      "DocumentId",
      "PreservedUnknownMetadata"
    ]);
    interchangeMethods
      .Where(method => method.Name.StartsWith("CreateMesh", StringComparison.Ordinal))
      .Should().OnlyContain(method => method.ReturnType == typeof(Task<OperationResult<MeshAsset>>));
    interchangeMethods
      .Where(method => method.Name.StartsWith("ExportGl", StringComparison.Ordinal))
      .Should().OnlyContain(method => method.ReturnType == typeof(Task<OperationResult>));
    typeof(GltfImportPlan).GetProperties().Select(property => property.Name).Should().NotContain([
      "EditOptions",
      "ExpectedBaseline",
      "Kind"
    ]);
    typeof(GltfImportPlan).GetMethods().Select(method => method.Name).Should().NotContain("CreateEdit");
    typeof(GltfCliReportOperation).GetProperties().Select(property => property.Name).Should().NotContain([
      "AppliedConflictActions",
      "Baseline",
      "ExpectedBaseline",
      "Fingerprint",
      "InterchangeAssetLineageId",
      "LineageDisposition",
      "NextBaseline",
      "PreservationChanges",
      "RestoredSerializedRepresentationPaths"
    ]);
    typeof(GltfCliReportOperation).GetMethods().Select(method => method.Name).Should().NotContain([
      "ForEditImport",
      "ForNewModelImport"
    ]);
    Enum.GetNames<GltfCliReportOperationKind>().Should().NotContain([
      "ImportEdit",
      "ImportNewModel"
    ]);
    typeof(GltfDiagnosticCodes).GetFields().Select(field => field.Name).Should().NotContain([
      "AssetLineageMismatch",
      "DocumentMismatch",
      "DuplicateScopeIdentity",
      "MissingExpectedScope",
      "StaleNativeProjection"
    ]);
  }

  [Fact]
  public void PublicDependencyInjectionRegistersTheMshAndGltfSurfaces()
  {
    using var services = new ServiceCollection()
      .AddMshServices()
      .AddGltfServices()
      .BuildServiceProvider();

    services.GetRequiredService<IMshReader>().Should().NotBeNull();
    services.GetRequiredService<IMshWriter>().Should().NotBeNull();
    services.GetRequiredService<IMshValidator>().Should().NotBeNull();
    services.GetRequiredService<GltfInterchange>().Should().NotBeNull();
    services.GetRequiredService<GltfImportPlanSerializer>().Should().NotBeNull();
    services.GetRequiredService<GltfCliReportSerializer>().Should().NotBeNull();
  }

  private static async Task<CliResult> RunCliAsync(params string[] arguments)
  {
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var executable = Environment.GetEnvironmentVariable("EARTHTOOL_CLI_EXECUTABLE");
    if (string.IsNullOrWhiteSpace(executable))
    {
      var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name
        ?? throw new InvalidOperationException("The test build configuration could not be resolved.");
      var platform = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
      var architecture = RuntimeInformation.OSArchitecture switch
      {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "arm64",
        var unsupported => throw new PlatformNotSupportedException($"Unsupported CLI test architecture: {unsupported}.")
      };
      executable = Path.Combine(
        root,
        "EarthTool.CLI",
        "bin",
        configuration,
        "net8.0",
        $"{platform}-{architecture}",
        OperatingSystem.IsWindows() ? "EarthTool.CLI.exe" : "EarthTool.CLI");
    }

    executable = Path.GetFullPath(executable, root);
    var startInfo = new ProcessStartInfo(executable)
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      WorkingDirectory = root
    };
    startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";
    foreach (var argument in arguments)
    {
      startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)
      ?? throw new InvalidOperationException("The built EarthTool CLI process could not be started.");
    var outputTask = process.StandardOutput.ReadToEndAsync();
    var errorTask = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    var output = await outputTask;
    var error = await errorTask;
    return new CliResult(process.ExitCode, AnsiEscapeSequence().Replace(output + error, string.Empty));
  }

  private static void VerifyHelpApproval(string rootHelp, string mshHelp, string importHelp)
  {
    var actual = $"# earthtool --help\n{NormalizeHelp(rootHelp)}\n\n"
      + $"# earthtool msh --help\n{NormalizeHelp(mshHelp)}\n\n"
      + $"# earthtool msh import --help\n{NormalizeHelp(importHelp)}\n";
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var approvedPath = Path.Combine(root, "EarthTool.CLI.Tests", "Approvals", "msh-cli-help.approved.txt");
    var receivedPath = Path.Combine(root, "EarthTool.CLI.Tests", "Approvals", "msh-cli-help.received.txt");
    var approved = File.ReadAllText(approvedPath).ReplaceLineEndings("\n");
    if (actual != approved)
    {
      File.WriteAllText(receivedPath, actual);
    }
    else
    {
      File.Delete(receivedPath);
    }

    actual.Should().Be(approved);
  }

  private static string NormalizeHelp(string help)
  {
    return string.Join("\n", help.Trim().ReplaceLineEndings("\n")
      .Split('\n').Select(line => line.TrimEnd()))
      .Replace("EarthTool.CLI.exe", "earthtool")
      .Replace("EarthTool.CLI.dll", "earthtool")
      .Replace("EarthTool.CLI", "earthtool");
  }

  [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]")]
  private static partial Regex AnsiEscapeSequence();

  private sealed record CliResult(int ExitCode, string Output);
}
