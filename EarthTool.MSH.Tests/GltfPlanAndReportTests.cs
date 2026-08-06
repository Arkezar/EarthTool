using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EarthTool.MSH.Tests;

public class GltfPlanAndReportTests
{
  private static readonly Guid _lineageId = new("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
  private static readonly Guid _documentId = new("11111111-2222-4333-8444-555555555555");

  [Fact]
  public async Task NewModelPlanRoundTripsExplicitDynamicMeshResourceBindings()
  {
    var plan = GltfImportPlan.CreateNewModel(
      GltfPackageKind.Glb,
      new string('a', 64),
      new GltfNewModelImportOptions(
        meshResourceBindings: new Dictionary<GltfNodeHandle, string>
        {
          [new GltfNodeHandle(3)] = "Objects\\effects\\scalable.msh",
        }
      )
    );
    var serializer = new GltfImportPlanSerializer();
    await using var serialized = new MemoryStream();
    var write = await serializer.SerializeAsync(plan, serialized);
    serialized.Position = 0;

    var read = await serializer.DeserializeAsync(serialized);

    write.Status.Should().Be(OperationStatus.Succeeded);
    read.Status.Should().Be(OperationStatus.Succeeded);
    read.Value!.NewModelOptions!.MeshResourceBindings.Should().ContainSingle()
      .Which.Should().Be(
        new KeyValuePair<GltfNodeHandle, string>(
          new GltfNodeHandle(3),
          "Objects\\effects\\scalable.msh"
        )
      );
  }

  [Fact]
  public async Task VersionTwoNewModelPlanRoundTripsEveryTypedAuthoringInput()
  {
    var plan = GltfImportPlan.CreateNewModel(
      GltfPackageKind.Glb,
      new string('a', 64),
      new GltfNewModelImportOptions(
        textureResourceBindings: new Dictionary<GltfMaterialHandle, string?>
        {
          [new GltfMaterialHandle(2)] = null,
          [new GltfMaterialHandle(1)] = "Textures\\authored\\hull.tex"
        },
        footprint: new GltfNewModelFootprint(
          3,
          Enumerable.Repeat(1.5f, 16),
          Enumerable.Range(0, 16).Select(index => (byte)(index % 4))),
        horizontalExtents: new GltfNewModelHorizontalExtents(2, 3, 4, 5),
        objectRoles: new Dictionary<GltfNodeHandle, GltfNewModelObjectRole>
        {
          [new GltfNodeHandle(2)] = new(
            GltfStaticObjectRoles.ViewerFaced | GltfStaticObjectRoles.Barrel,
            32)
        },
        staticLightOptions: new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>
        {
          [new GltfLightHandle(1)] = new(12.5f, 0.4f)
        }));
    var serializer = new GltfImportPlanSerializer();
    await using var first = new MemoryStream();
    await using var second = new MemoryStream();

    var firstWrite = await serializer.SerializeAsync(plan, first);
    var secondWrite = await serializer.SerializeAsync(plan, second);
    first.Position = 0;
    var read = await serializer.DeserializeAsync(first);

    firstWrite.Status.Should().Be(OperationStatus.Succeeded);
    secondWrite.Status.Should().Be(OperationStatus.Succeeded);
    first.ToArray().Should().Equal(second.ToArray());
    AssertJsonApproval("gltf-import-plan-v2", first.ToArray());
    read.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", read.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Path}:{diagnostic.Message}")));
    read.Value!.Format.Should().Be(GltfImportPlanFormat.Identifier);
    read.Value.Version.Should().Be(2);
    read.Value.Kind.Should().Be(GltfImportPlanKind.NewModel);
    read.Value.PackageKind.Should().Be(GltfPackageKind.Glb);
    read.Value.SourceSha256.Should().Be(new string('a', 64));
    read.Value.NewModelOptions!.TextureResourceBindings.Should().ContainKey(new GltfMaterialHandle(1));
    read.Value.NewModelOptions.ObjectRoles[new GltfNodeHandle(2)].BarrelMaximumAngle.Should().Be(32);
    read.Value.NewModelOptions.StaticLightOptions[new GltfLightHandle(1)].TargetDistance.Should().Be(12.5f);
    read.Value.EditOptions.Should().BeNull();
    GltfImportPlanFormat.SupportedVersions.Should().Equal(2);
    GltfCliReportFormat.SupportedVersions.Should().Equal(2);
  }

  [Theory]
  [InlineData("helperBindings", "semanticOverrides.helperBindings", "canonical authoring identifiers")]
  [InlineData("animationClasses", "semanticOverrides.animationClasses", "EarthTool A")]
  [InlineData("markerAttachment1", "semanticOverrides.objectRoles[].roles", "ET_Emitter_1")]
  public async Task VersionTwoPlanRejectsRemovedInputsWithMigrationDiagnostics(
    string removedInput,
    string expectedPath,
    string expectedMigration)
  {
    var plan = GltfImportPlan.CreateNewModel(
      GltfPackageKind.Glb,
      new string('a', 64),
      new GltfNewModelImportOptions(objectRoles:
        new Dictionary<GltfNodeHandle, GltfNewModelObjectRole>
        {
          [new GltfNodeHandle(1)] = new(GltfStaticObjectRoles.ViewerFaced)
        }));
    await using var serialized = new MemoryStream();
    (await new GltfImportPlanSerializer().SerializeAsync(plan, serialized)).Status.Should()
      .Be(OperationStatus.Succeeded);
    var root = JsonNode.Parse(serialized.ToArray())!.AsObject();
    root["version"] = 2;
    var overrides = root["semanticOverrides"]!.AsObject();
    if (removedInput is "helperBindings" or "animationClasses")
    {
      overrides[removedInput] = new JsonArray();
    }
    else
    {
      overrides["objectRoles"]![0]!["roles"] = new JsonArray(removedInput);
    }
    await using var source = new MemoryStream(Encoding.UTF8.GetBytes(root.ToJsonString()));

    var result = await new GltfImportPlanSerializer().DeserializeAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.RemovedImportPlanMember
      && diagnostic.EventId == 3005
      && diagnostic.Path == expectedPath
      && diagnostic.Message.Contains(expectedMigration, StringComparison.Ordinal));
  }

  [Fact]
  public async Task VersionOnePlanIsRejectedWithAnActionableUpgradeDiagnostic()
  {
    var plan = GltfImportPlan.CreateNewModel(GltfPackageKind.Glb, new string('a', 64));
    await using var serialized = new MemoryStream();
    (await new GltfImportPlanSerializer().SerializeAsync(plan, serialized)).Status.Should()
      .Be(OperationStatus.Succeeded);
    var root = JsonNode.Parse(serialized.ToArray())!.AsObject();
    root["version"] = 1;
    await using var source = new MemoryStream(Encoding.UTF8.GetBytes(root.ToJsonString()));

    var result = await new GltfImportPlanSerializer().DeserializeAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.UnsupportedImportPlanVersion
      && diagnostic.EventId == 3001
      && diagnostic.Path == "version"
      && diagnostic.Message.Contains("protocol version 2", StringComparison.Ordinal)
      && diagnostic.Data["actual"] == "1"
      && diagnostic.Data["supported"] == "2");
  }

  [Fact]
  public async Task VersionTwoEditPlanIsRejectedAsRemoved()
  {
    var options = new GltfEditImportOptions(
    [
      new GltfMetadataConflictResolution(
        "v1:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
        GltfMetadataConflictActions.MapScope,
        "nodes[2]"),
      new GltfMetadataConflictResolution(
        "v1:BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
        GltfMetadataConflictActions.AcceptDeletion)
    ]);
    var plan = GltfImportPlan.CreateEdit(
      GltfPackageKind.Gltf,
      new string('b', 64),
      new InterchangeBaseline(_lineageId, _documentId),
      options);
    var serializer = new GltfImportPlanSerializer();
    await using var stream = new MemoryStream();

    (await serializer.SerializeAsync(plan, stream)).Status.Should().Be(OperationStatus.Succeeded);
    stream.Position = 0;
    var read = await serializer.DeserializeAsync(stream);

    read.Status.Should().Be(OperationStatus.Failed);
    read.Value.Should().BeNull();
    read.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.RemovedImportPlanMember
      && diagnostic.EventId == 3005
      && diagnostic.Path == "mode");
  }

  [Theory]
  [InlineData("{", GltfDiagnosticCodes.MalformedImportPlan, 3000)]
  [InlineData("{\"format\":\"earthtool.msh.import-plan\",\"version\":3}", GltfDiagnosticCodes.UnsupportedImportPlanVersion, 3001)]
  [InlineData("{\"format\":\"earthtool.msh.import-plan\",\"version\":2,\"mode\":\"newModel\",\"package\":\"glb\",\"sourceSha256\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"rawMsh\":\"AA==\",\"semanticOverrides\":{}}", GltfDiagnosticCodes.MalformedImportPlan, 3000)]
  [InlineData("{\"format\":\"earthtool.msh.import-plan\",\"format\":\"earthtool.msh.import-plan\",\"version\":2}", GltfDiagnosticCodes.MalformedImportPlan, 3000)]
  public async Task InvalidPlansFailWithStableDiagnostics(string json, string code, int eventId)
  {
    await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

    var result = await new GltfImportPlanSerializer().DeserializeAsync(stream);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == code
      && diagnostic.EventId == eventId
      && diagnostic.Severity == DiagnosticSeverity.Error);
  }

  [Fact]
  public async Task ImportPlanLimitFailsBeforeAPlanIsMaterialized()
  {
    await using var stream = new MemoryStream(new byte[65]);
    var profile = new GltfOperationProfile(maxMetadataBytes: 64);

    var result = await new GltfImportPlanSerializer().DeserializeAsync(stream, profile);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.ImportPlanResourceLimitExceeded
      && diagnostic.EventId == 3002
      && diagnostic.Path == "$"
      && diagnostic.Data["maximum"] == "64");
  }

  [Fact]
  public async Task ImportPlanDepthLimitUsesTheStableResourceDiagnostic()
  {
    await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"a\":{\"b\":{\"c\":1}}}"));
    var profile = new GltfOperationProfile(maxJsonDepth: 2);

    var result = await new GltfImportPlanSerializer().DeserializeAsync(stream, profile);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.ImportPlanResourceLimitExceeded
      && diagnostic.EventId == 3002
      && diagnostic.Path == "$"
      && diagnostic.Data["maximum"] == "2");
  }

  [Fact]
  public async Task NewModelPlanRejectsAMismatchedSourceBeforeImport()
  {
    var source = await CreateMetadataFreeGlbAsync();
    var digest = Sha256(source);
    var plan = GltfImportPlan.CreateNewModel(
      GltfPackageKind.Glb,
      (digest[0] == '0' ? "1" : "0") + digest.Substring(1),
      new GltfNewModelImportOptions());
    await using var stream = new MemoryStream(source);

    var result = await new GltfInterchange().ImportNewModelGlbWithPlanAsync(stream, plan);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.ImportPlanMismatch
      && diagnostic.EventId == 3004
      && diagnostic.Path == "sourceSha256");
  }

  [Fact]
  public async Task DeserializedNewModelPlanReplaysEverySupportedTypedAuthoringInput()
  {
    var sourceAsset = await ReadAssetAsync(StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [2] = new(
          System.Numerics.Vector3.Zero,
          System.Numerics.Vector3.One,
          0,
          0,
          [0, 0, 0],
          0.2f,
          5,
          0.25f,
          4)
      },
      activeSpots: [2]));
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    (await interchange.ExportGlbWithReceiptAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(_lineageId, _documentId))).Status.Should().Be(OperationStatus.Succeeded);
    var source = RemoveMetadata(exported.ToArray());
    var plan = GltfImportPlan.CreateNewModel(
      GltfPackageKind.Glb,
      Sha256(source),
      new GltfNewModelImportOptions(
        textureResourceBindings: new Dictionary<GltfMaterialHandle, string?>
        {
          [new GltfMaterialHandle(1)] = "Textures\\authored\\hull.tex"
        },
        footprint: new GltfNewModelFootprint(
          5,
          Enumerable.Repeat(2f, 16),
          new byte[16]),
        horizontalExtents: new GltfNewModelHorizontalExtents(1, 2, 3, 4),
        objectRoles: new Dictionary<GltfNodeHandle, GltfNewModelObjectRole>
        {
          [new GltfNodeHandle(2)] = new(
            GltfStaticObjectRoles.ViewerFaced | GltfStaticObjectRoles.Barrel,
            32)
        },
        staticLightOptions: new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>
        {
          [new GltfLightHandle(1)] = new(targetDistance: 5, terrainLightAmplitude: 2.5f)
        }));
    await using var serialized = new MemoryStream();
    var serializer = new GltfImportPlanSerializer();
    (await serializer.SerializeAsync(plan, serialized)).Status.Should().Be(OperationStatus.Succeeded);
    serialized.Position = 0;
    var deserialized = await serializer.DeserializeAsync(serialized);
    deserialized.Status.Should().Be(OperationStatus.Succeeded);
    await using var stream = new MemoryStream(source);

    var result = await interchange.ImportNewModelGlbWithPlanAsync(stream, deserialized.Value!);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    result.Value!.Asset.CommonBaseHeader.BoxPresenceMask.Should().Be(5);
    result.Value.Asset.CommonBaseHeader.HorizontalExtents.Should().Equal(
      new byte[] { 0, 1, 0, 2, 0, 3, 0, 4 });
    var renderObject = result.Value.Asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    renderObject.TexturePathBytes.Should().Equal("Textures\\authored\\hull.tex"u8.ToArray());
    renderObject.KnownFlags.Should().Be(
      StaticRenderObjectFlags.ViewerFaced | StaticRenderObjectFlags.Barrel);
    renderObject.BarrelMaximumAngle.Should().Be(32);
    var spot = StaticLightMshFixture.GetSpot(
      result.Value.Asset.GetSerializedRepresentation().ToArray(),
      2);
    BinaryPrimitives.ReadSingleLittleEndian(spot.AsSpan(0x18)).Should().Be(5);
    BinaryPrimitives.ReadSingleLittleEndian(spot.AsSpan(0x2C)).Should().Be(2.5f);
  }

  [Fact]
  public async Task PublicPlanCannotBypassSerializedByteLimits()
  {
    var plan = GltfImportPlan.CreateNewModel(
      GltfPackageKind.Glb,
      new string('a', 64),
      new GltfNewModelImportOptions(new Dictionary<GltfMaterialHandle, string?>
      {
        [new GltfMaterialHandle(1)] = "Textures\\" + new string('x', 128) + ".tex"
      }));
    var profile = new GltfOperationProfile(maxMetadataBytes: 64);
    await using var source = new MemoryStream();

    var result = await new GltfInterchange().ImportNewModelGlbWithPlanAsync(source, plan, profile);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.ImportPlanResourceLimitExceeded
      && diagnostic.EventId == 3002
      && diagnostic.Path == "$"
      && diagnostic.Data["maximum"] == "64");
  }

  [Fact]
  public void EditPlanFactoryRejectsMalformedConflictKeys()
  {
    var create = () => GltfImportPlan.CreateEdit(
      GltfPackageKind.Glb,
      new string('a', 64),
      new InterchangeBaseline(_lineageId, _documentId),
      new GltfEditImportOptions(
      [
        new GltfMetadataConflictResolution("not-a-conflict-key", GltfMetadataConflictActions.AcceptDeletion)
      ]));

    create.Should().Throw<ArgumentException>();
  }

  [Fact]
  public async Task SeparateGltfPlanReplaysAgainstTheCapturedPackage()
  {
    var fixture = await CreateMetadataFreeSeparateGltfAsync();
    try
    {
      var plan = GltfImportPlan.CreateNewModel(
        GltfPackageKind.Gltf,
        fixture.SourceSha256);

      var result = await new GltfInterchange().ImportNewModelGltfFileWithPlanAsync(
        fixture.SourcePath,
        plan);

      result.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
      result.Value.Should().NotBeNull();
    }
    finally
    {
      Directory.Delete(fixture.Directory, true);
    }
  }

  [Fact]
  public async Task SeparateGltfPlanRejectsAChangedBufferSidecar()
  {
    var fixture = await CreateMetadataFreeSeparateGltfAsync();
    try
    {
      var binary = await File.ReadAllBytesAsync(fixture.BufferPath);
      binary[0] ^= 0x01;
      await File.WriteAllBytesAsync(fixture.BufferPath, binary);
      var plan = GltfImportPlan.CreateNewModel(
        GltfPackageKind.Gltf,
        fixture.SourceSha256);

      var result = await new GltfInterchange().ImportNewModelGltfFileWithPlanAsync(
        fixture.SourcePath,
        plan);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Value.Should().BeNull();
      result.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.ImportPlanMismatch
        && diagnostic.EventId == 3004
        && diagnostic.Path == "sourceSha256");
    }
    finally
    {
      Directory.Delete(fixture.Directory, true);
    }
  }

  [Fact]
  public async Task UnifiedCreationReplaysSeparateGltfPlanAndRejectsAChangedSidecar()
  {
    var fixture = await CreateMetadataFreeSeparateGltfAsync();
    try
    {
      var plan = GltfImportPlan.CreateNewModel(
        GltfPackageKind.Gltf,
        fixture.SourceSha256);
      var interchange = new GltfInterchange();

      var created = await interchange.CreateMeshFileWithPlanAsync(fixture.SourcePath, plan);

      created.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", created.Diagnostics.Select(diagnostic => diagnostic.Message)));
      created.Value.Should().BeOfType<StaticMeshAsset>();

      var binary = await File.ReadAllBytesAsync(fixture.BufferPath);
      binary[0] ^= 0x01;
      await File.WriteAllBytesAsync(fixture.BufferPath, binary);

      var changed = await interchange.CreateMeshFileWithPlanAsync(fixture.SourcePath, plan);

      changed.Status.Should().Be(OperationStatus.Failed);
      changed.Value.Should().BeNull();
      changed.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.ImportPlanMismatch
        && diagnostic.Path == "sourceSha256");
    }
    finally
    {
      Directory.Delete(fixture.Directory, true);
    }
  }

  [Fact]
  public async Task VersionTwoReportIsDeterministicAndContainsCompleteOperationEffects()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbWithReceiptAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(_lineageId, _documentId));
    var metadataFree = RemoveMetadata(exported.ToArray());
    await using var newSource = new MemoryStream(metadataFree);
    var imported = await interchange.CreateMeshAsync(newSource);
    var failed = new OperationResult(
      OperationStatus.Failed,
      [new OperationDiagnostic(
        GltfDiagnosticCodes.InvalidGeometry,
        1106,
        DiagnosticSeverity.Error,
        "meshes[0].primitives[0]",
        "Non-contractual prose.",
        24,
        new Dictionary<string, string>
        {
          ["zeta"] = "last",
          ["alpha"] = "first"
        })]);
    var report = new GltfCliReport(
    [
      GltfCliReportOperation.ForExport(
        "input.msh",
        "input.glb",
        GltfPackageKind.Glb,
        sourceAsset,
        export),
      GltfCliReportOperation.ForImport(
        "authored.glb",
        "authored.msh",
        GltfPackageKind.Glb,
        imported),
      GltfCliReportOperation.ForValidation(
        "invalid.glb",
        GltfPackageKind.Glb,
        failed)
    ]);
    var serializer = new GltfCliReportSerializer();
    await using var first = new MemoryStream();
    await using var second = new MemoryStream();

    var firstWrite = await serializer.SerializeAsync(report, first);
    var secondWrite = await serializer.SerializeAsync(report, second);

    firstWrite.Status.Should().Be(OperationStatus.Succeeded);
    secondWrite.Status.Should().Be(OperationStatus.Succeeded);
    first.ToArray().Should().Equal(second.ToArray());
    report.Status.Should().Be(OperationStatus.Failed);
    var normalized = NormalizeReportIdentities(first.ToArray());
    AssertJsonApproval("gltf-cli-report-v2", normalized);
    using var document = JsonDocument.Parse(first.ToArray());
    var operations = document.RootElement.GetProperty("operations");
    operations.GetArrayLength().Should().Be(3);
    operations[1].GetProperty("kind").GetString().Should().Be("import");
    operations[1].TryGetProperty("preservation", out _).Should().BeFalse();
    operations[1].GetProperty("identities").TryGetProperty("meshAssetLineageId", out _)
      .Should().BeFalse();
    operations[2].GetProperty("diagnostics")[0].GetProperty("data")
      .EnumerateObject().Select(property => property.Name).Should().Equal("alpha", "zeta");
  }

  private static async Task<StaticMeshAsset> ReadAssetAsync(byte[] bytes)
  {
    await using var stream = new MemoryStream(bytes);
    var result = await new MshReader().ReadAsync(stream);
    return result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
  }

  private static async Task<byte[]> CreateMetadataFreeGlbAsync()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var stream = new MemoryStream();
    var result = await new GltfInterchange().ExportGlbWithReceiptAsync(
      asset,
      stream,
      new GltfExportOptions(_lineageId, _documentId));
    result.Status.Should().Be(OperationStatus.Succeeded);
    return RemoveMetadata(stream.ToArray());
  }

  private static async Task<(string Directory, string SourcePath, string BufferPath, string SourceSha256)>
    CreateMetadataFreeSeparateGltfAsync()
  {
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var sourcePath = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var export = await new GltfInterchange().ExportGltfFileWithReceiptAsync(
      asset,
      sourcePath,
      new GltfExportOptions(_lineageId, _documentId));
    export.Status.Should().Be(OperationStatus.Succeeded);
    var root = JsonNode.Parse(await File.ReadAllTextAsync(sourcePath))!;
    RemoveMetadata(root);
    SetCanonicalStaticNames(root);
    await File.WriteAllTextAsync(sourcePath, root.ToJsonString());
    var bufferName = root["buffers"]![0]!["uri"]!.GetValue<string>();
    var digest = await new GltfImportPlanSerializer().ComputeGltfSourceSha256Async(sourcePath);
    digest.Status.Should().Be(OperationStatus.Succeeded);
    return (directory, sourcePath, Path.Combine(directory, bufferName), digest.Value!);
  }

  private static byte[] RemoveMetadata(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var root = JsonNode.Parse(Encoding.UTF8.GetString(glb, 20, jsonLength))!;
    RemoveMetadata(root);
    SetCanonicalStaticNames(root);
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

  private static byte[] NormalizeReportIdentities(byte[] json)
  {
    var root = JsonNode.Parse(json)!.AsObject();
    var operations = root["operations"]!.AsArray();
    var exportIdentities = operations[0]!["identities"]!.AsObject();
    exportIdentities["meshCreationGuid"] = _documentId.ToString("D");
    var importIdentities = operations[1]!["identities"]!.AsObject();
    importIdentities["meshCreationGuid"] = _documentId.ToString("D");
    return Encoding.UTF8.GetBytes(root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
  }

  private static void SetCanonicalStaticNames(JsonNode root)
  {
    var number = 1;
    foreach (var node in root["nodes"]!.AsArray().Where(node => node!["mesh"] is not null))
    {
      node!["name"] = $"ET_Static_{number++}";
    }
  }

  private static string Sha256(byte[] value)
  {
    return Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
  }

  private static void AssertJsonApproval(string name, byte[] value)
  {
    var actual = Encoding.UTF8.GetString(value).ReplaceLineEndings("\n");
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var approvedPath = Path.Combine(root, "EarthTool.MSH.Tests", "Approvals", $"{name}.approved.json");
    var receivedPath = Path.Combine(root, "EarthTool.MSH.Tests", "Approvals", $"{name}.received.json");
    var approved = File.Exists(approvedPath)
      ? File.ReadAllText(approvedPath).ReplaceLineEndings("\n")
      : string.Empty;
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
}
