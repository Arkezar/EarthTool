using AwesomeAssertions;
using EarthTool.CLI.Commands.MSH;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Numerics;
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

    exitCode.Should().Be(CliExitCode.Success);
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
    operation.GetProperty("identities").GetProperty("baseline").ValueKind.Should()
      .Be(JsonValueKind.Object);
    operation.GetProperty("identities").GetProperty("fingerprint").ValueKind.Should()
      .Be(JsonValueKind.Object);
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
    File.Exists(Path.ChangeExtension(fixture.MshPath, ".gltf")).Should().BeTrue();
    File.Exists(fixture.GlbPath).Should().BeFalse();
  }

  [Fact]
  public async Task EditImportRequiresExpectedIdentitiesAndWritesTheNextBaseline()
  {
    using var fixture = await CliFixture.CreateAsync();
    var expected = await fixture.CreateEditGlbAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "edited");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "edit-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", "edit", fixture.GlbPath,
      "--expected-lineage", expected.AssetLineageId.ToString("D"),
      "--expected-document", expected.DocumentId.ToString("D"),
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    var mshPath = Path.Combine(outputDirectory, "model.msh");
    var read = await new MshReader().ReadFileAsync(mshPath);
    read.Status.Should().Be(OperationStatus.Succeeded);
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("kind").GetString().Should().Be("importEdit");
    operation.GetProperty("identities").GetProperty("expectedBaseline")
      .GetProperty("documentId").GetString().Should().Be(expected.DocumentId.ToString("D"));
    operation.GetProperty("identities").GetProperty("nextBaseline")
      .GetProperty("documentId").GetString().Should().NotBe(expected.DocumentId.ToString("D"));
  }

  [Fact]
  public async Task NewModelImportWritesCanonicalMshAndInitialBaseline()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateMetadataFreeGlbAsync();
    var outputDirectory = Path.Combine(fixture.Directory, "authored");
    System.IO.Directory.CreateDirectory(outputDirectory);
    var reportPath = Path.Combine(fixture.Directory, "new-report.json");

    var exitCode = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", "new", fixture.GlbPath,
      "--output", outputDirectory,
      "--report", reportPath
    ], TextWriter.Null);

    exitCode.Should().Be(CliExitCode.Success);
    var read = await new MshReader().ReadFileAsync(Path.Combine(outputDirectory, "model.msh"));
    read.Status.Should().Be(OperationStatus.Succeeded);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    asset.ArchiveFraming.CreationGuid.Should().NotBeNull();
    asset.StaticRenderObjectSequence.Should().ContainSingle();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("kind").GetString().Should().Be("importNewModel");
    operation.GetProperty("identities").GetProperty("baseline").ValueKind.Should()
      .Be(JsonValueKind.Object);
    operation.GetProperty("preservation").GetProperty("changes").GetArrayLength().Should()
      .BeGreaterThan(0);
  }

  [Fact]
  public async Task NewModelImportAppliesTypedPlanOptions()
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
      "msh", "import", "new", fixture.GlbPath,
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

    exitCode.Should().Be(CliExitCode.Failure);
    File.Exists(fixture.GlbPath).Should().BeFalse();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    var operation = report.RootElement.GetProperty("operations")[0];
    operation.GetProperty("kind").GetString().Should().Be("export");
    operation.GetProperty("status").GetString().Should().Be("failed");
    operation.GetProperty("diagnostics").GetArrayLength().Should().BeGreaterThan(0);
    operation.GetProperty("identities").GetProperty("meshAssetLineageId").ValueKind.Should()
      .Be(JsonValueKind.Null);
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
      "msh", "import", "new", fixture.GlbPath,
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
  public async Task InvalidCommandOptionsMapToUsageStatus()
  {
    using var fixture = await CliFixture.CreateAsync();
    await fixture.CreateEditGlbAsync();
    var expectedLineage = "aaaaaaaa-1111-4222-8333-bbbbbbbbbbbb";
    var expectedDocument = "cccccccc-4444-4555-8666-dddddddddddd";

    var missingIdentities = await InternalMshCommandHost.RunAsync(
      ["msh", "import", "edit", fixture.GlbPath],
      TextWriter.Null);
    var patternedEdit = await InternalMshCommandHost.RunAsync(
    [
      "msh", "import", "edit", Path.Combine(fixture.Directory, "*.glb"),
      "--expected-lineage", expectedLineage,
      "--expected-document", expectedDocument
    ], TextWriter.Null);
    var relativeTexRoot = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--tex-root", "relative"],
      TextWriter.Null);

    missingIdentities.Should().Be(CliExitCode.Usage);
    patternedEdit.Should().Be(CliExitCode.Usage);
    relativeTexRoot.Should().Be(CliExitCode.Usage);
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

    exitCode.Should().Be(CliExitCode.Cancellation);
    File.Exists(fixture.GlbPath).Should().BeFalse();
    using var report = JsonDocument.Parse(await File.ReadAllBytesAsync(reportPath));
    report.RootElement.GetProperty("status").GetString().Should().Be("cancelled");
  }

  [Fact]
  public async Task ReportWriteFailureIsIncludedInTheInvocationOutcome()
  {
    using var fixture = await CliFixture.CreateAsync();
    using var output = new StringWriter();

    var exitCode = await InternalMshCommandHost.RunAsync(
      ["msh", "export", fixture.MshPath, "--report", fixture.Directory],
      output);

    exitCode.Should().Be(CliExitCode.Failure);
    File.Exists(fixture.GlbPath).Should().BeTrue();
    output.ToString().Should()
      .Contain($"diagnostic code={GltfDiagnosticCodes.IoFailure}")
      .And.Contain("summary total=1 succeeded=0 failed=1 cancelled=0");
  }

  private sealed class CliFixture : IDisposable
  {
    public string Directory { get; }
    public string MshPath => Path.Combine(Directory, "model.msh");
    public string GlbPath => Path.Combine(Directory, "model.glb");

    private CliFixture(string directory)
    {
      Directory = directory;
    }

    public static async Task<CliFixture> CreateAsync()
    {
      var fixture = new CliFixture(Path.Combine(Path.GetTempPath(), $"earthtool-cli-{Guid.NewGuid():N}"));
      System.IO.Directory.CreateDirectory(fixture.Directory);
      var build = StaticMeshBuilder.Create(
          Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"),
          new MeshAssetLineageId(Guid.Parse("11111111-2222-4333-8444-555555555555")))
        .SetRenderObject(
        [
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY)
        ],
        [new CanonicalTriangle(0, 1, 2)])
        .Build();
      build.TryGetValue(out var asset).Should().BeTrue();
      var write = await new MshWriter().WriteFileAsync(asset!, fixture.MshPath);
      write.Succeeded.Should().BeTrue();
      return fixture;
    }

    public async Task<InterchangeBaseline> CreateEditGlbAsync()
    {
      var read = await new MshReader().ReadFileAsync(MshPath);
      var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
      var expected = new InterchangeBaseline(
        Guid.Parse("aaaaaaaa-1111-4222-8333-bbbbbbbbbbbb"),
        Guid.Parse("cccccccc-4444-4555-8666-dddddddddddd"));
      var export = await new GltfInterchange().ExportGlbFileAsync(
        asset,
        GlbPath,
        new GltfExportOptions(expected.AssetLineageId, expected.DocumentId));
      export.Status.Should().Be(OperationStatus.Succeeded);
      return expected;
    }

    public async Task CreateMetadataFreeGlbAsync()
    {
      await CreateEditGlbAsync();
      var glb = await File.ReadAllBytesAsync(GlbPath);
      await File.WriteAllBytesAsync(GlbPath, RemoveMetadata(glb));
    }

    public async Task<string> CreateNewModelPlanAsync(GltfNewModelImportOptions options)
    {
      var serializer = new GltfImportPlanSerializer();
      await using var source = File.OpenRead(GlbPath);
      var digest = await serializer.ComputeGlbSourceSha256Async(source);
      digest.Status.Should().Be(OperationStatus.Succeeded);
      var plan = GltfImportPlan.CreateNewModel(GltfPackageKind.Glb, digest.Value!, options);
      var planPath = Path.Combine(Directory, "import-plan.json");
      await using var destination = File.Create(planPath);
      var write = await serializer.SerializeAsync(plan, destination);
      write.Status.Should().Be(OperationStatus.Succeeded);
      return planPath;
    }

    private static byte[] RemoveMetadata(byte[] glb)
    {
      var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
      var root = JsonNode.Parse(Encoding.UTF8.GetString(glb, 20, jsonLength))!;
      RemoveMetadata(root);
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
      return result;
    }

    private static void RemoveMetadata(JsonNode node)
    {
      if (node is JsonObject value)
      {
        if (value["extras"] is JsonObject extras)
        {
          extras.Remove("earthtool");
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
