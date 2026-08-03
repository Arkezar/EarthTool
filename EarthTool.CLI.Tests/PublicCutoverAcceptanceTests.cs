using AwesomeAssertions;
using EarthTool.CLI.Commands.MSH;
using EarthTool.Common.Enums;
using EarthTool.GLTF;
using EarthTool.MSH;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Xunit;

namespace EarthTool.CLI.Tests;

public sealed class PublicCutoverAcceptanceTests
{
  [Fact]
  public async Task CliProcessExposesOnlyTheGltfMshCommandTreeAndStableExitStatuses()
  {
    var rootHelp = await RunCliAsync("--help");
    var mshHelp = await RunCliAsync("msh", "--help");
    var importHelp = await RunCliAsync("msh", "import", "--help");
    var invalidEdit = await RunCliAsync("msh", "import", "edit");
    var failedExport = await RunCliAsync(
      "msh",
      "export",
      Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.msh"));

    rootHelp.ExitCode.Should().Be(0);
    rootHelp.Output.Should().Contain("msh").And.NotContain("dae <InputFilePath>");
    mshHelp.ExitCode.Should().Be(0);
    mshHelp.Output.Should().Contain("export <INPUT>").And.Contain("import");
    mshHelp.Output.Should().NotContain("output-format");
    importHelp.ExitCode.Should().Be(0);
    importHelp.Output.Should().Contain("edit <INPUT>").And.Contain("new <INPUT>");
    invalidEdit.ExitCode.Should().Be(CliExitCode.Usage);
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
    var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name
      ?? throw new InvalidOperationException("The test build configuration could not be resolved.");
    var platform = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
    var architecture = RuntimeInformation.OSArchitecture switch
    {
      Architecture.X64 => "x64",
      Architecture.Arm64 => "arm64",
      var unsupported => throw new PlatformNotSupportedException($"Unsupported CLI test architecture: {unsupported}.")
    };
    var executable = Path.Combine(
      root,
      "EarthTool.CLI",
      "bin",
      configuration,
      "net8.0",
      $"{platform}-{architecture}",
      OperatingSystem.IsWindows() ? "EarthTool.CLI.exe" : "EarthTool.CLI");
    var startInfo = new ProcessStartInfo(executable)
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      WorkingDirectory = root
    };
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
    return new CliResult(process.ExitCode, output + error);
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

  private sealed record CliResult(int ExitCode, string Output);
}
