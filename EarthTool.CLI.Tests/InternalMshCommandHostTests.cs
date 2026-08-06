using AwesomeAssertions;
using EarthTool.CLI.Commands.MSH;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace EarthTool.CLI.Tests;

public sealed class InternalMshCommandHostTests
{
  [Fact]
  public async Task ExportDefaultsToGlbAndReportsTheCompleteOutcome()
  {
    using var fixture = await CliFixture.CreateAsync();
    using var output = new StringWriter();
    var reportPath = Path.Combine(fixture.Directory, "export-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--report", reportPath],
      output);

    exitCode.Should().Be(0);
    File.Exists(fixture.GlbPath).Should().BeTrue();
    File.Exists(Path.ChangeExtension(fixture.MshPath, ".gltf")).Should().BeFalse();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("format").GetString().Should()
      .Be(GltfCliReportFormat.Identifier);
    report.RootElement.GetProperty("version").GetInt32().Should()
      .Be(GltfCliReportFormat.Version);
    report.RootElement.GetProperty("status").GetString().Should().Be("succeeded");
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("kind").GetString().Should().Be("export");
    operation.GetProperty("package").GetString().Should().Be("glb");
    operation.GetProperty("status").GetString().Should().Be("succeeded");
    operation.GetProperty("identities").GetProperty("meshCreationGuid").ValueKind.Should()
      .Be(JsonValueKind.String);
    operation.TryGetProperty("preservation", out _).Should().BeFalse();
    output.ToString().Should().Contain("summary total=1 succeeded=1 failed=0 cancelled=0");
  }

  [Fact]
  public async Task ExportSelectsSeparateGltfExplicitly()
  {
    using var fixture = await CliFixture.CreateAsync();

    var exitCode = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--format", "gltf"],
      TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    var gltfPath = Path.ChangeExtension(fixture.MshPath, ".gltf");
    File.Exists(gltfPath).Should().BeTrue();
    File.Exists(fixture.GlbPath).Should().BeFalse();
    using var gltf = JsonDocument.Parse(await File.ReadAllBytesAsync(gltfPath));
    var sceneRootIndex = gltf.RootElement.GetProperty("scenes")[0]
      .GetProperty("nodes")[0].GetInt32();
    var nodes = gltf.RootElement.GetProperty("nodes");
    nodes[sceneRootIndex].GetProperty("name").GetString().Should().Be("model");
    nodes[nodes[sceneRootIndex].GetProperty("children")[0].GetInt32()]
      .GetProperty("name").GetString().Should().Be("ET_Static_1");
  }

  [Fact]
  public async Task ExportAcceptsSupportedDynamicEffectsAndReportsItsAssetKind()
  {
    using var fixture = await CliFixture.CreateDynamicAsync();
    var reportPath = Path.Combine(fixture.Directory, "dynamic-export-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--report", reportPath],
      TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    File.Exists(fixture.GlbPath).Should().BeTrue();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("assetKind").GetString().Should().Be("dynamic");
    operation.GetProperty("package").GetString().Should().Be("glb");
    operation.GetProperty("status").GetString().Should().Be("succeeded");
    operation.GetProperty("identities").GetProperty("meshCreationGuid")
      .ValueKind.Should().Be(JsonValueKind.String);
    operation.GetProperty("identities").TryGetProperty("fingerprint", out _).Should().BeFalse();
  }

  [Fact]
  public async Task BatchExportsAllSupportedDynamicEffectsAsSeparateGltf()
  {
    using var fixture = await CliFixture.CreateDynamicAsync();
    var inputDirectory = Path.Combine(fixture.Directory, "dynamic-inputs");
    var outputDirectory = Path.Combine(fixture.Directory, "dynamic-exports");
    System.IO.Directory.CreateDirectory(inputDirectory);
    System.IO.Directory.CreateDirectory(outputDirectory);
    File.Copy(fixture.MshPath, Path.Combine(inputDirectory, "alpha.msh"));
    File.Copy(fixture.MshPath, Path.Combine(inputDirectory, "zeta.msh"));
    var reportPath = Path.Combine(fixture.Directory, "dynamic-batch-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", Path.Combine(inputDirectory, "*.msh"),
      "--format", "gltf",
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    File.Exists(Path.Combine(outputDirectory, "alpha.gltf")).Should().BeTrue();
    File.Exists(Path.Combine(outputDirectory, "zeta.gltf")).Should().BeTrue();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operations = report.RootElement.GetProperty("operations").EnumerateArray().ToArray();
    operations.Should().HaveCount(2);
    operations.Select(item => item.GetProperty("assetKind").GetString()).Should()
      .OnlyContain(item => item == "dynamic");
    operations.Select(item => item.GetProperty("package").GetString()).Should()
      .OnlyContain(item => item == "gltf");
    operations.SelectMany(item => item.GetProperty("diagnostics").EnumerateArray())
      .Select(item => item.GetProperty("code").GetString()).Should()
      .NotContain(GltfDiagnosticCodes.UnsupportedDomain);
  }

  [Fact]
  public async Task ExportAllMeshesBatchExportsStaticAndDynamicAssetsWithoutUnsupportedDomainFailures()
  {
    using var staticFixture = await CliFixture.CreateAsync();
    using var dynamicFixture = await CliFixture.CreateDynamicAsync();
    var inputDirectory = Path.Combine(staticFixture.Directory, "meshes");
    var outputDirectory = Path.Combine(staticFixture.Directory, "exports");
    System.IO.Directory.CreateDirectory(inputDirectory);
    System.IO.Directory.CreateDirectory(outputDirectory);
    File.Copy(staticFixture.MshPath, Path.Combine(inputDirectory, "static.msh"));
    File.Copy(dynamicFixture.MshPath, Path.Combine(inputDirectory, "dynamic.msh"));
    var reportPath = Path.Combine(staticFixture.Directory, "export-all-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export",
      "--tex-root", staticFixture.Directory,
      "--msh-root", staticFixture.Directory,
      "--output", outputDirectory,
      "--report", reportPath,
      Path.Combine(inputDirectory, "*.msh")
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    File.Exists(Path.Combine(outputDirectory, "static.glb")).Should().BeTrue();
    File.Exists(Path.Combine(outputDirectory, "dynamic.glb")).Should().BeTrue();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("status").GetString().Should().Be("succeeded");
    var operations = report.RootElement.GetProperty("operations").EnumerateArray().ToArray();
    operations.Select(operation => operation.GetProperty("assetKind").GetString()).Should()
      .Equal("dynamic", "static");
    operations.SelectMany(operation => operation.GetProperty("diagnostics").EnumerateArray())
      .Select(diagnostic => diagnostic.GetProperty("code").GetString()).Should()
      .NotContain(GltfDiagnosticCodes.UnsupportedDomain);
  }

  [Fact]
  public async Task ExportRetainsRepeatedTexRootArgumentOrder()
  {
    using var fixture = await CliFixture.CreateAsync("Textures\\preview.tex");
    var firstRoot = Path.Combine(fixture.Directory, "first-root");
    var secondRoot = Path.Combine(fixture.Directory, "second-root");
    var firstPixels = new byte[] { 0xFF, 0x00, 0x00, 0xFF };
    var secondPixels = new byte[] { 0x00, 0x00, 0xFF, 0xFF };
    System.IO.Directory.CreateDirectory(Path.Combine(firstRoot, "Textures"));
    System.IO.Directory.CreateDirectory(Path.Combine(secondRoot, "Textures"));
    await File.WriteAllBytesAsync(
      Path.Combine(firstRoot, "Textures", "preview.tex"),
      CliFixture.CreateRgbaTex(firstPixels));
    await File.WriteAllBytesAsync(
      Path.Combine(secondRoot, "Textures", "preview.tex"),
      CliFixture.CreateRgbaTex(secondPixels));

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", fixture.MshPath,
      "--format", "gltf",
      "--tex-root", firstRoot,
      "--tex-root", secondRoot
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    using var package = JsonDocument.Parse(
      await File.ReadAllBytesAsync(Path.ChangeExtension(fixture.MshPath, ".gltf")));
    package.RootElement.GetProperty("images")[0].GetProperty("uri").GetString().Should()
      .Be(CliFixture.GetPreviewContentAddress(firstPixels) + ".png");
  }

  [Fact]
  public async Task DynamicExportRetainsRepeatedMshRootOrderAndReportsShadowing()
  {
    using var fixture = await CliFixture.CreateDynamicAsync();
    var firstRoot = Path.Combine(fixture.Directory, "first-msh-root");
    var secondRoot = Path.Combine(fixture.Directory, "second-msh-root");
    await CliFixture.CreateReferencedMshAsync(firstRoot, "preview.msh");
    await CliFixture.CreateReferencedMshAsync(secondRoot, "PREVIEW.MSH");
    var reportPath = Path.Combine(fixture.Directory, "scalable-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", fixture.MshPath,
      "--msh-root", firstRoot,
      "--msh-root", secondRoot,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var diagnostics = report.RootElement.GetProperty("operations")[0]
      .GetProperty("diagnostics").EnumerateArray()
      .Select(item => item.GetProperty("code").GetString()).ToArray();
    diagnostics.Should().Contain(GltfDiagnosticCodes.MeshResourceShadowed);
    diagnostics.Should().NotContain(GltfDiagnosticCodes.UnsupportedDomain);
  }

  [Fact]
  public async Task ExportExpandsPatternsDeterministicallyIntoOneInvocationReport()
  {
    using var fixture = await CliFixture.CreateAsync();
    var inputDirectory = Path.Combine(fixture.Directory, "inputs");
    var outputDirectory = Path.Combine(fixture.Directory, "exports");
    System.IO.Directory.CreateDirectory(inputDirectory);
    System.IO.Directory.CreateDirectory(outputDirectory);
    var alpha = Path.Combine(inputDirectory, "alpha.msh");
    var zeta = Path.Combine(inputDirectory, "zeta.msh");
    File.Copy(fixture.MshPath, zeta);
    File.Copy(fixture.MshPath, alpha);
    var reportPath = Path.Combine(fixture.Directory, "batch-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", Path.Combine(inputDirectory, "*.msh"),
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    File.Exists(Path.Combine(outputDirectory, "alpha.glb")).Should().BeTrue();
    File.Exists(Path.Combine(outputDirectory, "zeta.glb")).Should().BeTrue();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("operations").EnumerateArray()
      .Select(operation => operation.GetProperty("input").GetString()).Should()
      .Equal(Path.GetFullPath(alpha), Path.GetFullPath(zeta));
  }

  [Fact]
  public async Task ExportCreatesTheRequestedOutputDirectory()
  {
    using var fixture = await CliFixture.CreateAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "new", "exports");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", fixture.MshPath,
      "--output", outputDirectory
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    File.Exists(Path.Combine(outputDirectory, "model.glb")).Should().BeTrue();
  }

  [Fact]
  public async Task ExportFailsPreflightWhenTheOutputDirectoryCannotBeCreated()
  {
    using var fixture = await CliFixture.CreateAsync();
    var blockingFile = Path.Combine(fixture.Directory, "blocked");
    await File.WriteAllTextAsync(blockingFile, "not a directory");
    using var output = new StringWriter();

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", fixture.MshPath,
      "--output", Path.Combine(blockingFile, "exports")
    ], output);

    exitCode.Should().Be(CliExitCode.Failure);
    File.Exists(fixture.GlbPath).Should().BeFalse();
    output.ToString().Should().Contain("Output directory could not be created");
  }

  [Fact]
  public async Task BatchDestinationCollisionsFailBeforeAnyOutputIsWritten()
  {
    using var fixture = await CliFixture.CreateAsync();
    var firstDirectory = Path.Combine(fixture.Directory, "first");
    var secondDirectory = Path.Combine(fixture.Directory, "second");
    var outputDirectory = Path.Combine(fixture.Directory, "exports");
    System.IO.Directory.CreateDirectory(firstDirectory);
    System.IO.Directory.CreateDirectory(secondDirectory);
    System.IO.Directory.CreateDirectory(outputDirectory);
    var first = Path.Combine(firstDirectory, "shared.msh");
    var second = Path.Combine(secondDirectory, "shared.msh");
    File.Copy(fixture.MshPath, first);
    File.Copy(fixture.MshPath, second);

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", first, second,
      "--output", outputDirectory
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Failure);
    System.IO.Directory.EnumerateFileSystemEntries(outputDirectory).Should().BeEmpty();
  }

  [Fact]
  public async Task SeparateGltfReportCannotCollideWithAContentAddressedSidecar()
  {
    using var fixture = await CliFixture.CreateAsync();
    var probeDirectory = Path.Combine(fixture.Directory, "probe");
    var outputDirectory = Path.Combine(fixture.Directory, "exports");
    System.IO.Directory.CreateDirectory(probeDirectory);
    System.IO.Directory.CreateDirectory(outputDirectory);
    var probeExitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", fixture.MshPath,
      "--format", "gltf",
      "--output", probeDirectory
    ], TextWriter.Null);
    probeExitCode.Should().Be(0);
    using var probe = JsonDocument.Parse(
      await File.ReadAllBytesAsync(Path.Combine(probeDirectory, "model.gltf")));
    var bufferFileName = probe.RootElement.GetProperty("buffers")[0]
      .GetProperty("uri").GetString()!;

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", fixture.MshPath,
      "--format", "gltf",
      "--output", outputDirectory,
      "--report", Path.Combine(outputDirectory, bufferFileName)
    ], TextWriter.Null);

    exitCode.Should().Be(1);
    System.IO.Directory.EnumerateFileSystemEntries(outputDirectory).Should().BeEmpty();
  }

  [Fact]
  public async Task BatchContinuesAfterAFileFailureAndReportsEveryAttempt()
  {
    using var fixture = await CliFixture.CreateAsync();
    var invalid = Path.Combine(fixture.Directory, "invalid.msh");
    var valid = Path.Combine(fixture.Directory, "valid.msh");
    await File.WriteAllBytesAsync(invalid, [0x00, 0x01, 0x02]);
    File.Copy(fixture.MshPath, valid);
    var outputDirectory = Path.Combine(fixture.Directory, "exports");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "batch-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", invalid, valid,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Failure);
    File.Exists(Path.Combine(outputDirectory, "invalid.glb")).Should().BeFalse();
    File.Exists(Path.Combine(outputDirectory, "valid.glb")).Should().BeTrue();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("operations").EnumerateArray()
      .Select(operation => operation.GetProperty("status").GetString()).Should()
      .Equal("failed", "succeeded");
  }

  [Fact]
  public async Task EmptyPatternFailsPreflightAndPreservesTheExistingReport()
  {
    using var fixture = await CliFixture.CreateAsync();
    var reportPath = Path.Combine(fixture.Directory, "report.json");
    var originalReport = "existing report"u8.ToArray();
    await File.WriteAllBytesAsync(reportPath, originalReport);

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "export", Path.Combine(fixture.Directory, "missing-*.msh"),
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Failure);
    (await File.ReadAllBytesAsync(reportPath)).Should().Equal(originalReport);
  }

  [Fact]
  public async Task ImportCreatesCanonicalAssetWithoutReceiptData()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateEditGlbAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "edited");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "edit-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    var mshPath = Path.Combine(outputDirectory, "model.msh");
    var read = await new MshReader().ReadFileAsync(mshPath);
    read.Status.Should().Be(OperationStatus.Succeeded);
    var source = await new MshReader().ReadFileAsync(fixture.MshPath);
    read.Value!.ArchiveFraming.CreationGuid!.Value.Should().NotBe(
      source.Value!.ArchiveFraming.CreationGuid!.Value
    );
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("kind").GetString().Should().Be("import");
    var identities = operation.GetProperty("identities");
    identities.GetProperty("meshCreationGuid").ValueKind.Should().Be(JsonValueKind.String);
    identities.TryGetProperty("meshAssetLineageId", out _).Should().BeFalse();
    operation.TryGetProperty("preservation", out _).Should().BeFalse();
  }

  [Fact]
  public async Task ImportCanonicalizesEmitterCompatibilityAnomalyWithoutPreservationReport()
  {
    using var fixture = await CliFixture.CreateEmitterCompatibilityAnomalyAsync();
    await fixture.CreateEditGlbAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "compatibility-edited");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "compatibility-edit-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    var read = await new MshReader().ReadFileAsync(Path.Combine(outputDirectory, "model.msh"));
    var source = await new MshReader().ReadFileAsync(fixture.MshPath);
    read.Value!.ArchiveFraming.CreationGuid!.Value.Should().NotBe(
      source.Value!.ArchiveFraming.CreationGuid!.Value
    );
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.TryGetProperty("preservation", out _).Should().BeFalse();
  }

  [Fact]
  public async Task MetadataBackedAndMetadataFreeImportsInferEmitterOwnershipWithoutAPlan()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateEditGlbAsync();
    await fixture.AddEmitterHelpersAsync(1);
    var editOutputDirectory = Path.Combine(fixture.Directory, "inferred-edit");
    var newOutputDirectory = Path.Combine(fixture.Directory, "inferred-new");
    System.IO.Directory.CreateDirectory(editOutputDirectory);
    System.IO.Directory.CreateDirectory(newOutputDirectory);
    var editReportPath = Path.Combine(fixture.Directory, "inferred-edit-report.json");
    var newReportPath = Path.Combine(fixture.Directory, "inferred-new-report.json");

    var editExitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--output", editOutputDirectory,
      "--report", editReportPath
    ], TextWriter.Null);
    await fixture.RemoveMetadataFromGlbAsync();
    var newExitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--output", newOutputDirectory,
      "--report", newReportPath
    ], TextWriter.Null);

    editExitCode.Should().Be(
      CliExitCode.Success,
      await File.ReadAllTextAsync(editReportPath));
    newExitCode.Should().Be(CliExitCode.Success);
    foreach (var path in new[]
    {
      Path.Combine(editOutputDirectory, "model.msh"),
      Path.Combine(newOutputDirectory, "model.msh")
    })
    {
      var read = await new MshReader().ReadFileAsync(path);
      read.Value.Should().BeOfType<StaticMeshAsset>().Subject
        .StaticRenderObjectSequence.Should().ContainSingle().Subject
        .KnownFlags.Should().HaveFlag(StaticRenderObjectFlags.MarkerAttachment1);
    }
    using var editReport = JsonDocument.Parse(await File.ReadAllBytesAsync(editReportPath));
    editReport.RootElement.GetProperty("operations")[0].TryGetProperty("preservation", out _)
      .Should().BeFalse();
    using var newReport = JsonDocument.Parse(await File.ReadAllBytesAsync(newReportPath));
    newReport.RootElement.GetProperty("operations")[0].GetProperty("status").GetString()
      .Should().Be("succeeded");
  }

  [Fact]
  public async Task MetadataBackedAndMetadataFreeImportsRejectDuplicateEmitterOwnershipWithoutAPlan()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateEditGlbAsync();
    await fixture.AddEmitterHelpersAsync(2);
    var editOutputDirectory = Path.Combine(fixture.Directory, "ambiguous-edit");
    var newOutputDirectory = Path.Combine(fixture.Directory, "ambiguous-new");
    System.IO.Directory.CreateDirectory(editOutputDirectory);
    System.IO.Directory.CreateDirectory(newOutputDirectory);
    var editReportPath = Path.Combine(fixture.Directory, "ambiguous-edit-report.json");
    var newReportPath = Path.Combine(fixture.Directory, "ambiguous-new-report.json");

    var editExitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--output", editOutputDirectory,
      "--report", editReportPath
    ], TextWriter.Null);
    await fixture.RemoveMetadataFromGlbAsync();
    var newExitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--output", newOutputDirectory,
      "--report", newReportPath
    ], TextWriter.Null);

    editExitCode.Should().Be(CliExitCode.Failure);
    newExitCode.Should().Be(CliExitCode.Failure);
    File.Exists(Path.Combine(editOutputDirectory, "model.msh")).Should().BeFalse();
    File.Exists(Path.Combine(newOutputDirectory, "model.msh")).Should().BeFalse();
    foreach (var reportPath in new[]
    {
      editReportPath,
      newReportPath
    })
    {
      using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
      var diagnostics = report.RootElement.GetProperty("operations")[0]
        .GetProperty("diagnostics").EnumerateArray().ToArray();
      var diagnostic = diagnostics.Should().ContainSingle(diagnostic =>
        diagnostic.GetProperty("code").GetString() == GltfDiagnosticCodes.DuplicateAuthoringOwner)
        .Subject;
      diagnostic.GetProperty("severity").GetString().Should().Be("error");
    }
  }

  [Fact]
  public async Task ImportCreatesCanonicalDynamicAsset()
  {
    using var fixture = await CliFixture.CreateDynamicGroupAsync();
    await fixture.CreateEditDynamicGlbAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "dynamic-edited");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "dynamic-edit-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success, await File.ReadAllTextAsync(reportPath));
    var restored = await new MshReader().ReadFileAsync(Path.Combine(outputDirectory, "model.msh"));
    restored.Value.Should().BeOfType<DynamicMeshAsset>();
    var source = await new MshReader().ReadFileAsync(fixture.MshPath);
    restored.Value!.ArchiveFraming.CreationGuid!.Value.Should().NotBe(
      source.Value!.ArchiveFraming.CreationGuid!.Value
    );
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("assetKind").GetString().Should().Be("dynamic");
    operation.GetProperty("status").GetString().Should().Be("succeeded");
  }

  [Fact]
  public async Task ImportCreatesVisibleDynamicAssetFromCanonicalExportAndPlan()
  {
    const string textureKey = "Textures\\effects\\track.tex";
    using var fixture = await CliFixture.CreateDynamicTrackAsync(textureKey);
    await fixture.ExportDynamicGlbAsync();
    var planPath = await fixture.CreateNewModelPlanAsync(
      new GltfNewModelImportOptions(
        textureResourceBindings: new Dictionary<GltfMaterialHandle, string?>
        {
          [new GltfMaterialHandle(1)] = textureKey,
        }));
    var outputDirectory = Path.Combine(fixture.Directory, "dynamic-visible");
    System.IO.Directory.CreateDirectory(outputDirectory);

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--plan", planPath,
      "--output", outputDirectory
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    var read = await new MshReader().ReadFileAsync(Path.Combine(outputDirectory, "model.msh"));
    var asset = read.Value.Should().BeOfType<DynamicMeshAsset>().Subject;
    asset.RootDynamicObject.Extension.KnownEffectType.Should().Be(DynamicEffectType.Track);
    asset.RootDynamicObject.Extension.FrameCount.Should().Be(2);
    asset.RootDynamicObject.Extension.EndAlpha.Should().Be(0.25f);
  }

  [Fact]
  public async Task ImportCreatesCanonicalMshAndReportsDefaultedAuthoringValues()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateMetadataFreeGlbAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "authored");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "new-report.json");

    using var output = new StringWriter();
    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--output", outputDirectory,
      "--report", reportPath
    ], output);

    exitCode.Should().Be(CliExitCode.Success);
    var read = await new MshReader().ReadFileAsync(Path.Combine(outputDirectory, "model.msh"));
    read.Status.Should().Be(OperationStatus.Succeeded);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    asset.ArchiveFraming.CreationGuid.Should().NotBeNull();
    asset.StaticRenderObjectSequence.Should().ContainSingle();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("kind").GetString().Should().Be("import");
    operation.GetProperty("diagnostics").EnumerateArray()
      .Select(diagnostic => diagnostic.GetProperty("code").GetString()).Should()
      .Contain(GltfDiagnosticCodes.AuthoringValueDefaulted);
    output.ToString().Should().Contain(
      $"diagnostic code={GltfDiagnosticCodes.AuthoringValueDefaulted}"
    );
    operation.TryGetProperty("preservation", out _).Should().BeFalse();
  }

  [Fact]
  public async Task ImportCreatesCanonicalAssetFromSeparateGltf()
  {
    using var fixture = await CliFixture.CreateAsync();
    var sourcePath = await fixture.CreateCanonicalStaticGltfAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "separate-authored");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "separate-import-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", sourcePath,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success, await File.ReadAllTextAsync(reportPath));
    var read = await new MshReader().ReadFileAsync(Path.Combine(outputDirectory, "model.msh"));
    read.Value.Should().BeOfType<StaticMeshAsset>();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("package").GetString().Should().Be("gltf");
    operation.GetProperty("status").GetString().Should().Be("succeeded");
    operation.TryGetProperty("preservation", out _).Should().BeFalse();
  }

  [Fact]
  public async Task ImportCreatesCanonicalDynamicAssetFromSeparateGltf()
  {
    using var fixture = await CliFixture.CreateDynamicGroupAsync();
    var sourcePath = await fixture.CreateCanonicalDynamicGltfAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "separate-dynamic");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "separate-dynamic-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", sourcePath,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success, await File.ReadAllTextAsync(reportPath));
    var read = await new MshReader().ReadFileAsync(Path.Combine(outputDirectory, "model.msh"));
    read.Value.Should().BeOfType<DynamicMeshAsset>();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("package").GetString().Should().Be("gltf");
    operation.GetProperty("assetKind").GetString().Should().Be("dynamic");
    operation.GetProperty("status").GetString().Should().Be("succeeded");
  }

  [Fact]
  public async Task ImportBatchesPatternMatchesIntoOneReport()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateMetadataFreeGlbAsync();
    var inputDirectory = Path.Combine(fixture.Directory, "new-models");
    var outputDirectory = Path.Combine(fixture.Directory, "authored");
    System.IO.Directory.CreateDirectory(inputDirectory);
    System.IO.Directory.CreateDirectory(outputDirectory);
    File.Copy(fixture.GlbPath, Path.Combine(inputDirectory, "alpha.glb"));
    File.Copy(fixture.GlbPath, Path.Combine(inputDirectory, "zeta.glb"));
    var reportPath = Path.Combine(fixture.Directory, "new-batch-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", Path.Combine(inputDirectory, "*.glb"),
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    File.Exists(Path.Combine(outputDirectory, "alpha.msh")).Should().BeTrue();
    File.Exists(Path.Combine(outputDirectory, "zeta.msh")).Should().BeTrue();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("operations").EnumerateArray()
      .Select(operation => operation.GetProperty("input").GetString()).Should()
      .Equal(
        Path.Combine(inputDirectory, "alpha.glb"),
        Path.Combine(inputDirectory, "zeta.glb"));
  }

  [Fact]
  public async Task ImportRejectsDestinationCollisionsBeforeWriting()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateMetadataFreeGlbAsync();
    var firstDirectory = Path.Combine(fixture.Directory, "first-new");
    var secondDirectory = Path.Combine(fixture.Directory, "second-new");
    var outputDirectory = Path.Combine(fixture.Directory, "authored");
    System.IO.Directory.CreateDirectory(firstDirectory);
    System.IO.Directory.CreateDirectory(secondDirectory);
    System.IO.Directory.CreateDirectory(outputDirectory);
    var first = Path.Combine(firstDirectory, "shared.glb");
    var second = Path.Combine(secondDirectory, "shared.glb");
    File.Copy(fixture.GlbPath, first);
    File.Copy(fixture.GlbPath, second);

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", first, second,
      "--output", outputDirectory
    ], TextWriter.Null);

    exitCode.Should().Be(1);
    System.IO.Directory.EnumerateFileSystemEntries(outputDirectory).Should().BeEmpty();
  }

  [Fact]
  public async Task ImportContinuesAfterAFileFailure()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateMetadataFreeGlbAsync();
    var invalid = Path.Combine(fixture.Directory, "invalid.glb");
    var valid = Path.Combine(fixture.Directory, "valid.glb");
    await File.WriteAllBytesAsync(invalid, [0x00, 0x01, 0x02]);
    File.Copy(fixture.GlbPath, valid);
    var outputDirectory = Path.Combine(fixture.Directory, "authored");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "new-continuation-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", invalid, valid,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(1);
    File.Exists(Path.Combine(outputDirectory, "invalid.msh")).Should().BeFalse();
    File.Exists(Path.Combine(outputDirectory, "valid.msh")).Should().BeTrue();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("operations").EnumerateArray()
      .Select(operation => operation.GetProperty("status").GetString()).Should()
      .Equal("failed", "succeeded");
  }

  [Fact]
  public async Task ImportAppliesTypedPlanOptions()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateMetadataFreeGlbAsync();
    var planPath = await fixture.CreateNewModelPlanAsync(
      new GltfNewModelImportOptions(
        horizontalExtents: new GltfNewModelHorizontalExtents(2, 3, 4, 5)));
    var outputDirectory = Path.Combine(fixture.Directory, "planned");
    System.IO.Directory.CreateDirectory(outputDirectory);

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--plan", planPath,
      "--output", outputDirectory
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    var read = await new MshReader().ReadFileAsync(Path.Combine(outputDirectory, "model.msh"));
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    asset.CommonBaseHeader.HorizontalExtents.Should().Equal(
      0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05);
  }

  [Fact]
  public async Task FailedExportReportsStructuredDiagnosticsWithoutSourceIdentities()
  {
    using var fixture = await CliFixture.CreateAsync();
    await File.WriteAllBytesAsync(fixture.MshPath, [0x00, 0x01, 0x02]);
    var reportPath = Path.Combine(fixture.Directory, "failed-export-report.json");
    using var output = new StringWriter();

    var exitCode = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--report", reportPath],
      output);

    exitCode.Should().Be(1);
    File.Exists(fixture.GlbPath).Should().BeFalse();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("kind").GetString().Should().Be("export");
    operation.GetProperty("status").GetString().Should().Be("failed");
    operation.GetProperty("diagnostics").GetArrayLength().Should().BeGreaterThan(0);
    operation.GetProperty("identities").GetProperty("meshCreationGuid").ValueKind.Should()
      .Be(JsonValueKind.Null);
    operation.GetProperty("identities").TryGetProperty("meshAssetLineageId", out _)
      .Should().BeFalse();
    output.ToString().Should().Contain("diagnostic code=").And.Contain("eventId=");
  }

  [Fact]
  public async Task StaleImportPlanFailsWithoutReplacingTheDestination()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateMetadataFreeGlbAsync();
    var planPath = await fixture.CreateNewModelPlanAsync(new GltfNewModelImportOptions());
    await File.AppendAllTextAsync(fixture.GlbPath, "stale");
    var outputDirectory = Path.Combine(fixture.Directory, "stale-plan");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var destination = Path.Combine(outputDirectory, "model.msh");
    var original = new byte[] { 9, 8, 7 };
    await File.WriteAllBytesAsync(destination, original);
    var reportPath = Path.Combine(fixture.Directory, "stale-plan-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--plan", planPath,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Failure);
    (await File.ReadAllBytesAsync(destination)).Should().Equal(original);
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("operations")[0].GetProperty("diagnostics")
      .EnumerateArray().Select(item => item.GetProperty("code").GetString()).Should()
      .Contain(GltfDiagnosticCodes.ImportPlanMismatch);
  }

  [Fact]
  public async Task ImportRejectsEditModeAndWrongPackagePlans()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateMetadataFreeGlbAsync();
    var editPlanPath = await fixture.CreateEditPlanAsync("edit-plan.json");
    var packagePlanPath = await fixture.CreateNewModelPlanAsync(
      new GltfNewModelImportOptions(),
      GltfPackageKind.Gltf,
      "package-plan.json");

    foreach (var (planPath, expectedCode, dimension) in new[]
    {
      (editPlanPath, GltfDiagnosticCodes.RemovedImportPlanMember, "mode"),
      (packagePlanPath, GltfDiagnosticCodes.ImportPlanMismatch, "package")
    })
    {
      var reportPath = Path.Combine(fixture.Directory, $"{dimension}-mismatch-report.json");
      var exitCode = await InternalMshCommandHost.RunAsync(
      [
        "msh", "import", fixture.GlbPath,
        "--plan", planPath,
        "--report", reportPath
      ], TextWriter.Null);

      exitCode.Should().Be(CliExitCode.Failure);
      using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
      var diagnostic = report.RootElement.GetProperty("operations")[0]
        .GetProperty("diagnostics").EnumerateArray().Should().ContainSingle().Subject;
      diagnostic.GetProperty("code").GetString().Should().Be(expectedCode);
      diagnostic.GetProperty("path").GetString().Should().Be(dimension);
      if (expectedCode == GltfDiagnosticCodes.ImportPlanMismatch)
      {
        diagnostic.GetProperty("data").GetProperty("dimension").GetString().Should().Be(dimension);
      }
    }
  }

  [Fact]
  public async Task InvalidExportOptionsMapToUsageStatus()
  {
    using var fixture = await CliFixture.CreateAsync();
    var relativeTexRoot = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--tex-root", "relative"],
      TextWriter.Null);
    var relativeMshRoot = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--msh-root", "relative"],
      TextWriter.Null);
    relativeTexRoot.Should().Be(2);
    relativeMshRoot.Should().Be(2);
  }

  [Fact]
  public async Task CancellationMapsToStableStatusAndDoesNotCreateOutput()
  {
    using var fixture = await CliFixture.CreateAsync();
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    var reportPath = Path.Combine(fixture.Directory, "cancelled-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--report", reportPath],
      TextWriter.Null,
      cancellation.Token);

    exitCode.Should().Be(130);
    File.Exists(fixture.GlbPath).Should().BeFalse();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("status").GetString().Should().Be("cancelled");
  }

  [Fact]
  public async Task ReportWriteFailureIsIncludedInTheInvocationOutcome()
  {
    using var fixture = await CliFixture.CreateAsync();
    using var output = new StringWriter();
    var reportDirectory = Path.Combine(fixture.Directory, "report-target");
    System.IO.Directory.CreateDirectory(reportDirectory);

    var exitCode = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--report", reportDirectory],
      output);

    exitCode.Should().Be(CliExitCode.Failure);
    File.Exists(fixture.GlbPath).Should().BeTrue();
    System.IO.Directory.EnumerateFiles(fixture.Directory, ".report-target.*.tmp").Should().BeEmpty();
    output.ToString().Should()
      .Contain($"diagnostic code={GltfDiagnosticCodes.IoFailure}")
      .And.Contain("summary total=1 succeeded=0 failed=1 cancelled=0");
  }

  [Fact]
  public async Task InjectedReportCommitFailurePreservesTheExistingReport()
  {
    using var fixture = await CliFixture.CreateAsync();
    var reportPath = Path.Combine(fixture.Directory, "report.json");
    var original = "existing report"u8.ToArray();
    await File.WriteAllBytesAsync(reportPath, original);
    var fileSystem = new CommitFailingReportFileSystem();

    var exitCode = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--report", reportPath],
      TextWriter.Null,
      configureServices: services => services.AddSingleton<ICliReportFileSystem>(fileSystem));

    exitCode.Should().Be(1);
    File.Exists(fixture.GlbPath).Should().BeTrue();
    (await File.ReadAllBytesAsync(reportPath)).Should().Equal(original);
    fileSystem.DeleteAttempted.Should().BeTrue();
  }

  [Fact]
  public async Task InjectedMshWriteFailurePreservesTheDestinationAndIsReported()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateMetadataFreeGlbAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "authored");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var destination = Path.Combine(outputDirectory, "model.msh");
    var original = new byte[] { 9, 8, 7 };
    await File.WriteAllBytesAsync(destination, original);
    var reportPath = Path.Combine(fixture.Directory, "write-failure-report.json");
    var writer = new FailingMshWriter();

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", fixture.GlbPath,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null, configureServices: services => services.AddSingleton<IMshWriter>(writer));

    exitCode.Should().Be(1);
    writer.WriteAttempted.Should().BeTrue();
    (await File.ReadAllBytesAsync(destination)).Should().Equal(original);
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("operations")[0].GetProperty("diagnostics")
      .EnumerateArray().Select(diagnostic => diagnostic.GetProperty("code").GetString()).Should()
      .Contain(MshDiagnosticCodes.IoFailure);
  }

  private sealed class CommitFailingReportFileSystem : ICliReportFileSystem
  {
    public bool DeleteAttempted { get; private set; }

    public string GetTemporaryPath(string destinationPath)
    {
      return "injected-report.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new MemoryStream();
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      throw new IOException("Injected report commit failure.");
    }

    public void TryDelete(string temporaryPath)
    {
      DeleteAttempted = true;
    }
  }

  private sealed class FailingMshWriter : IMshWriter
  {
    public bool WriteAttempted { get; private set; }

    public Task<OperationResult> WriteAsync(
      MeshAsset asset,
      Stream destination,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      return Task.FromResult(Failure());
    }

    public Task<OperationResult> WriteFileAsync(
      MeshAsset asset,
      string destinationPath,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      WriteAttempted = true;
      return Task.FromResult(Failure());
    }

    private static OperationResult Failure()
    {
      return new OperationResult(OperationStatus.Failed,
      [
        new OperationDiagnostic(
          MshDiagnosticCodes.IoFailure,
          1007,
          DiagnosticSeverity.Error,
          "$",
          "Injected MSH write failure.")
      ]);
    }
  }

  private sealed class CliFixture : IDisposable
  {
    private const int AttachmentTableOffset = 0x1D8;

    public string Directory { get; }
    public string MshPath => Path.Combine(Directory, "model.msh");
    public string GlbPath => Path.Combine(Directory, "model.glb");

    private CliFixture(string directory)
    {
      Directory = directory;
    }

    public static async Task<CliFixture> CreateAsync(string? textureResource = null)
    {
      var fixture = new CliFixture(Path.Combine(Path.GetTempPath(), $"earthtool-cli-{Guid.NewGuid():N}"));
      System.IO.Directory.CreateDirectory(fixture.Directory);
      var builder = StaticMeshBuilder.Create(
        Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"));
      var vertices = new[]
      {
        new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
        new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
        new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY)
      };
      var triangles = new[] { new CanonicalTriangle(0, 1, 2) };
      var build = textureResource is null
        ? builder.SetRenderObject(vertices, triangles).Build()
        : builder.SetRootSourceObject(new CanonicalStaticSourceObject(
        [
          new CanonicalStaticRenderObject(vertices, triangles, textureResource)
        ])).Build();
      build.TryGetValue(out var asset).Should().BeTrue();
      var write = await new MshWriter().WriteFileAsync(asset!, fixture.MshPath);
      write.Succeeded.Should().BeTrue();
      return fixture;
    }

    public static async Task<CliFixture> CreateEmitterCompatibilityAnomalyAsync()
    {
      var fixture = new CliFixture(Path.Combine(Path.GetTempPath(), $"earthtool-cli-{Guid.NewGuid():N}"));
      System.IO.Directory.CreateDirectory(fixture.Directory);
      var vertices = new[]
      {
        new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
        new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
        new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY)
      };
      var renderObject = new CanonicalStaticRenderObject(
        vertices,
        [new CanonicalTriangle(0, 1, 2)]);
      var markerRole = new CanonicalStaticObjectRole(StaticRenderObjectFlags.MarkerAttachment1);
      var build = StaticMeshBuilder.Create(
          Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"))
        .SetRootSourceObject(new CanonicalStaticSourceObject(
          [renderObject],
          [new CanonicalStaticSourceObject([renderObject], role: markerRole)],
          markerRole))
        .Build();
      build.TryGetValue(out var asset).Should().BeTrue();
      var write = await new MshWriter().WriteFileAsync(asset!, fixture.MshPath);
      write.Succeeded.Should().BeTrue();
      var bytes = await File.ReadAllBytesAsync(fixture.MshPath);
      // Activate emitter slot 1 so the two marker owners form a preserved legacy anomaly.
      var attachmentOffset = 0x14 + AttachmentTableOffset + (4 * 8);
      BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(attachmentOffset), 256);
      BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(attachmentOffset + 2), -512);
      BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(attachmentOffset + 4), 768);
      bytes[attachmentOffset + 7] = 0x80;
      await File.WriteAllBytesAsync(fixture.MshPath, bytes);
      return fixture;
    }

    public static async Task<CliFixture> CreateDynamicAsync()
    {
      var fixture = new CliFixture(Path.Combine(Path.GetTempPath(), $"earthtool-cli-{Guid.NewGuid():N}"));
      System.IO.Directory.CreateDirectory(fixture.Directory);
      var frames = new CanonicalDynamicFrameSequence(0, 1, 1);
      var sprite = new CanonicalDynamicSpriteSheet(frames, 1, 1);
      var shape = new CanonicalDynamicEffectShape(
        new EffectRectangle(-1, 1, 1, -1),
        new EffectRectangle(-2, 2, 2, -2),
        0.25f);
      var alpha = new CanonicalDynamicAlpha(1, 0, DynamicAlphaTiming.LifetimeProgress);
      var light = new CanonicalDynamicTerrainLight(DynamicLightType.Pyramid, Vector3.One);
      var smoke = DynamicEffectRecipes.Smoke(
        sprite,
        shape,
        "Textures\\fx\\smoke.tex",
        new Vector3(1, 0.5f, 0.25f),
        1,
        alpha,
        false);
      var track = DynamicEffectRecipes.Track(
        frames,
        shape.StartEffectRectangle,
        shape.EndEffectRectangle,
        "Textures\\fx\\track.tex",
        alpha,
        false,
        [smoke]);
      var flat = DynamicEffectRecipes.FlatExplosion(
        sprite,
        shape,
        "Textures\\fx\\flat.tex",
        new Vector3(0.25f, 0.5f, 1),
        alpha,
        false,
        light);
      var mapped = DynamicEffectRecipes.MappedExplosion(
        frames,
        shape.StartEffectRectangle,
        shape.EndEffectRectangle,
        "Textures\\fx\\mapped.tex",
        new Vector3(0.5f, 1, 0.25f),
        alpha,
        true,
        light,
        [flat]);
      var electrical = DynamicEffectRecipes.ElectricalCannon(
        sprite,
        -0.25f,
        "Textures\\fx\\electrical.tex",
        new Vector3(0.25f, 0.5f, 1),
        alpha,
        true);
      var laser = DynamicEffectRecipes.Laser(
        sprite,
        0.5f,
        "Textures\\fx\\laser.tex",
        new Vector3(1, 0.5f, 0.25f),
        alpha,
        false,
        light,
        [electrical]);
      var lightning = DynamicEffectRecipes.Lightning(
        sprite,
        -0.75f,
        "Textures\\fx\\lightning.tex",
        new Vector3(0.5f, 1, 0.25f),
        alpha,
        true,
        light);
      var laserWall = DynamicEffectRecipes.LaserWall(
        sprite,
        1,
        "Textures\\fx\\laser-wall.tex",
        new Vector3(0.25f, 0.5f, 1),
        alpha,
        false,
        Vector3.One,
        [lightning]);
      var line = DynamicEffectRecipes.Line(
        sprite,
        shape,
        "Textures\\fx\\line.tex",
        new Vector3(0.2f, 0.3f, 0.4f),
        0.5f,
        0.8f,
        0.2f,
        true);
      var shockwave = DynamicEffectRecipes.Shockwave(
        sprite,
        shape,
        "Textures\\fx\\shockwave.tex",
        new Vector3(0.4f, 0.5f, 0.6f),
        0.5f,
        0.8f,
        0.2f,
        false,
        [line]);
      var keelwater = DynamicEffectRecipes.Keelwater(
        sprite,
        shape,
        "Textures\\fx\\keelwater.tex",
        0.8f,
        0.2f,
        false);
      var sphere = DynamicEffectRecipes.Sphere(
        "Textures\\fx\\sphere.tex",
        new Vector3(0.7f, 0.8f, 0.9f),
        true,
        [keelwater]);
      var scalable = DynamicEffectRecipes.ScalableObject(
        frames,
        "preview",
        "Textures\\fx\\scalable.tex",
        1,
        2,
        new Vector3(0.6f, 0.7f, 0.8f),
        alpha,
        false,
        light);
      var build = DynamicMeshBuilder.Create(
          Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"))
        .SetRoot(DynamicEffectRecipes.Group(
          [track, mapped, laser, laserWall, shockwave, sphere, scalable]))
        .Build();
      build.TryGetValue(out var asset).Should().BeTrue();
      var write = await new MshWriter().WriteFileAsync(asset!, fixture.MshPath);
      write.Succeeded.Should().BeTrue();
      return fixture;
    }

    public static async Task<CliFixture> CreateDynamicGroupAsync()
    {
      var fixture = new CliFixture(Path.Combine(Path.GetTempPath(), $"earthtool-cli-{Guid.NewGuid():N}"));
      System.IO.Directory.CreateDirectory(fixture.Directory);
      var build = DynamicMeshBuilder.Create(
          Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee")
        )
        .SetRoot(DynamicEffectRecipes.Group())
        .Build();
      build.TryGetValue(out var asset).Should().BeTrue();
      var write = await new MshWriter().WriteFileAsync(asset!, fixture.MshPath);
      write.Succeeded.Should().BeTrue();
      return fixture;
    }

    public static async Task<CliFixture> CreateDynamicTrackAsync(string textureKey)
    {
      var fixture = new CliFixture(
        Path.Combine(Path.GetTempPath(), $"earthtool-cli-{Guid.NewGuid():N}"));
      System.IO.Directory.CreateDirectory(fixture.Directory);
      var build = DynamicMeshBuilder.Create()
        .SetRoot(
          DynamicEffectRecipes.Track(
            new CanonicalDynamicFrameSequence(0, 2, 3),
            new EffectRectangle(-1, 1, 1, -1),
            new EffectRectangle(-2, 2, 2, -2),
            textureKey,
            new CanonicalDynamicAlpha(0.75f, 0.25f, DynamicAlphaTiming.LifetimeProgress),
            true))
        .Build();
      build.TryGetValue(out var asset).Should().BeTrue();
      var write = await new MshWriter().WriteFileAsync(asset!, fixture.MshPath);
      write.Succeeded.Should().BeTrue();
      return fixture;
    }

    public static async Task CreateReferencedMshAsync(string root, string fileName)
    {
      var meshes = Path.Combine(root, "Meshes");
      System.IO.Directory.CreateDirectory(meshes);
      var vertices = new[]
      {
        new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
        new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
        new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY)
      };
      var build = StaticMeshBuilder.Create()
        .SetRenderObject(vertices, [new CanonicalTriangle(0, 1, 2)])
        .Build();
      build.TryGetValue(out var asset).Should().BeTrue();
      var write = await new MshWriter().WriteFileAsync(asset!, Path.Combine(meshes, fileName));
      write.Succeeded.Should().BeTrue();
    }

    public async Task CreateEditGlbAsync()
    {
      var read = await new MshReader().ReadFileAsync(MshPath);
      var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
      var export = await new GltfInterchange().ExportGlbFileAsync(
        asset,
        GlbPath);
      export.Status.Should().Be(OperationStatus.Succeeded);
      await RemoveMetadataFromGlbAsync();
    }

    public async Task CreateEditDynamicGlbAsync()
    {
      var read = await new MshReader().ReadFileAsync(MshPath);
      var asset = read.Value.Should().BeOfType<DynamicMeshAsset>().Subject;
      var export = await new GltfInterchange().ExportGlbFileAsync(
        asset,
        GlbPath);
      export.Status.Should().Be(OperationStatus.Succeeded);
      await RewriteGlbAsync(RemoveMetadata);
    }

    public async Task ExportDynamicGlbAsync()
    {
      var read = await new MshReader().ReadFileAsync(MshPath);
      var asset = read.Value.Should().BeOfType<DynamicMeshAsset>().Subject;
      var export = await new GltfInterchange().ExportGlbFileAsync(asset, GlbPath);
      export.Status.Should().Be(OperationStatus.Succeeded);
    }

    public async Task CreateMetadataFreeGlbAsync()
    {
      await CreateEditGlbAsync();
    }

    public async Task<string> CreateCanonicalStaticGltfAsync()
    {
      var path = Path.ChangeExtension(MshPath, ".gltf");
      var read = await new MshReader().ReadFileAsync(MshPath);
      var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
      var export = await new GltfInterchange().ExportGltfFileAsync(asset, path);
      export.Status.Should().Be(OperationStatus.Succeeded);
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
      RemoveMetadata(root);
      await File.WriteAllTextAsync(path, root.ToJsonString());
      return path;
    }

    public async Task<string> CreateCanonicalDynamicGltfAsync()
    {
      var path = Path.ChangeExtension(MshPath, ".gltf");
      var read = await new MshReader().ReadFileAsync(MshPath);
      var asset = read.Value.Should().BeOfType<DynamicMeshAsset>().Subject;
      var export = await new GltfInterchange().ExportGltfFileAsync(asset, path);
      export.Status.Should().Be(OperationStatus.Succeeded);
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
      RemoveMetadata(root);
      await File.WriteAllTextAsync(path, root.ToJsonString());
      return path;
    }

    public async Task AddEmitterHelpersAsync(int count)
    {
      await RewriteGlbAsync(root =>
      {
        var nodes = root["nodes"]!.AsArray();
        var sourceIndex = nodes.Select((node, index) => (node, index)).Single(item =>
          item.node!.AsObject().ContainsKey("mesh")).index;
        var source = nodes[sourceIndex]!.AsObject();
        var children = source["children"] as JsonArray ?? new JsonArray();
        source["children"] = children;
        for (var index = 0; index < count; index++)
        {
          var emitterIndex = nodes.Count;
          nodes.Add(new JsonObject
          {
            ["name"] = "ET_Emitter_1",
            ["translation"] = new JsonArray(index + 1, 2, 3),
            ["rotation"] = new JsonArray(0, 0, -0.70710677f, 0.70710677f)
          });
          children.Add(emitterIndex);
        }
      });
    }

    public Task RemoveMetadataFromGlbAsync()
    {
      return RewriteGlbAsync(root =>
      {
        RemoveMetadata(root);
        var number = 1;
        foreach (var node in root["nodes"]!.AsArray().Where(node => node!["mesh"] is not null))
        {
          node!["name"] = $"ET_Static_{number++}";
        }
      });
    }

    public async Task<string> CreateNewModelPlanAsync(
      GltfNewModelImportOptions options,
      GltfPackageKind packageKind = GltfPackageKind.Glb,
      string fileName = "import-plan.json")
    {
      var serializer = new GltfImportPlanSerializer();
      await using var source = File.OpenRead(GlbPath);
      var digest = await serializer.ComputeGlbSourceSha256Async(source);
      digest.Status.Should().Be(OperationStatus.Succeeded);
      var plan = GltfImportPlan.CreateNewModel(packageKind, digest.Value!, options);
      var planPath = Path.Combine(Directory, fileName);
      await WritePlanAsync(plan, planPath);
      return planPath;
    }

    public async Task<string> CreateEditPlanAsync(string fileName)
    {
      var serializer = new GltfImportPlanSerializer();
      await using var source = File.OpenRead(GlbPath);
      var digest = await serializer.ComputeGlbSourceSha256Async(source);
      digest.Status.Should().Be(OperationStatus.Succeeded);
      var planPath = Path.Combine(Directory, fileName);
      await File.WriteAllTextAsync(planPath, $$"""
        {
          "format": "earthtool.msh.import-plan",
          "version": 2,
          "mode": "edit",
          "package": "glb",
          "sourceSha256": "{{digest.Value}}",
          "expectedBaseline": {
            "assetLineageId": "aaaaaaaa-1111-4222-8333-bbbbbbbbbbbb",
            "documentId": "cccccccc-4444-4555-8666-dddddddddddd"
          },
          "conflictActions": []
        }
        """);
      return planPath;
    }

    private static async Task WritePlanAsync(GltfImportPlan plan, string planPath)
    {
      await using var destination = File.Create(planPath);
      var write = await new GltfImportPlanSerializer().SerializeAsync(plan, destination);
      write.Status.Should().Be(OperationStatus.Succeeded);
    }

    private async Task RewriteGlbAsync(Action<JsonObject> rewrite)
    {
      var glb = await File.ReadAllBytesAsync(GlbPath);
      var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
      var root = JsonNode.Parse(Encoding.UTF8.GetString(glb, 20, jsonLength))!.AsObject();
      rewrite(root);
      var json = Encoding.UTF8.GetBytes(root.ToJsonString());
      var paddedLength = (json.Length + 3) & ~3;
      var oldBinaryOffset = 20 + jsonLength;
      var binaryLength = glb.Length - oldBinaryOffset;
      var result = new byte[20 + paddedLength + binaryLength];
      glb.AsSpan(0, 8).CopyTo(result);
      BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length);
      BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), paddedLength);
      BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
      json.CopyTo(result.AsSpan(20));
      result.AsSpan(20 + json.Length, paddedLength - json.Length).Fill(0x20);
      glb.AsSpan(oldBinaryOffset, binaryLength).CopyTo(result.AsSpan(20 + paddedLength));
      await File.WriteAllBytesAsync(GlbPath, result);
    }

    public static byte[] CreateRgbaTex(byte[] pixels)
    {
      var result = new byte[24 + pixels.Length];
      "TEX\0\x01\0\0\0"u8.CopyTo(result);
      BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), 0x03000012);
      BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), 0x8888);
      BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(16), 1);
      BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(20), 1);
      pixels.CopyTo(result, 24);
      return result;
    }

    public static string GetPreviewContentAddress(byte[] pixels)
    {
      var preimage = new byte[sizeof(int) * 2 + pixels.Length];
      BinaryPrimitives.WriteInt32LittleEndian(preimage, 1);
      BinaryPrimitives.WriteInt32LittleEndian(preimage.AsSpan(sizeof(int)), 1);
      pixels.CopyTo(preimage, sizeof(int) * 2);
      return Convert.ToHexString(SHA256.HashData(preimage)).ToLowerInvariant();
    }

    private static void RemoveMetadata(JsonNode node)
    {
      if (node is JsonObject value)
      {
        if (value["extras"] is JsonObject extras)
        {
          extras.Remove("earthtool");
          extras.Remove("earthtoolAuthoring");
          if (extras.Count == 0)
          {
            value.Remove("extras");
          }
        }
        foreach (var child in value.Select(item => item.Value).Where(child => child is not null).ToArray())
        {
          RemoveMetadata(child!);
        }
      }
      else if (node is JsonArray array)
      {
        foreach (var child in array.Where(child => child is not null))
        {
          RemoveMetadata(child!);
        }
      }
    }

    public void Dispose()
    {
      System.IO.Directory.Delete(Directory, true);
    }
  }
}
