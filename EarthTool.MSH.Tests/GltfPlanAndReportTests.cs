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
  public async Task VersionOneNewModelPlanRoundTripsEveryTypedSemanticOverride()
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
        helperBindings: new Dictionary<GltfNodeHandle, GltfNewModelHelperBinding>
        {
          [new GltfNodeHandle(3)] = new(GltfNewModelHelperKind.Attachment, 21),
          [new GltfNodeHandle(4)] = new(GltfNewModelHelperKind.Cannon, 2)
        },
        staticLightOptions: new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>
        {
          [new GltfLightHandle(1)] = new(12.5f, 0.4f)
        },
        animationClasses: new Dictionary<GltfAnimationHandle, GltfNewModelAnimationClass>
        {
          [new GltfAnimationHandle(1)] = GltfNewModelAnimationClass.A
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
    AssertJsonApproval("gltf-import-plan-v1", first.ToArray());
    read.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", read.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Path}:{diagnostic.Message}")));
    read.Value!.Format.Should().Be(GltfImportPlanFormat.Identifier);
    read.Value.Version.Should().Be(1);
    read.Value.Kind.Should().Be(GltfImportPlanKind.NewModel);
    read.Value.PackageKind.Should().Be(GltfPackageKind.Glb);
    read.Value.SourceSha256.Should().Be(new string('a', 64));
    read.Value.NewModelOptions!.TextureResourceBindings.Should().ContainKey(new GltfMaterialHandle(1));
    read.Value.NewModelOptions.ObjectRoles[new GltfNodeHandle(2)].BarrelMaximumAngle.Should().Be(32);
    read.Value.NewModelOptions.HelperBindings[new GltfNodeHandle(3)].PhysicalNumber.Should().Be(21);
    read.Value.NewModelOptions.HelperBindings[new GltfNodeHandle(4)].Kind.Should()
      .Be(GltfNewModelHelperKind.Cannon);
    read.Value.NewModelOptions.StaticLightOptions[new GltfLightHandle(1)].TargetDistance.Should().Be(12.5f);
    read.Value.NewModelOptions.AnimationClasses[new GltfAnimationHandle(1)].Should()
      .Be(GltfNewModelAnimationClass.A);
    read.Value.EditOptions.Should().BeNull();
    GltfImportPlanFormat.SupportedVersions.Should().Equal(1);
    GltfCliReportFormat.SupportedVersions.Should().Equal(1);
  }

  [Fact]
  public async Task NewModelPlanRejectsRemovedMarkerObjectRoleValues()
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
    root["semanticOverrides"]!["objectRoles"]![0]!["roles"] =
      new JsonArray("markerAttachment1");
    await using var source = new MemoryStream(Encoding.UTF8.GetBytes(root.ToJsonString()));

    var result = await new GltfImportPlanSerializer().DeserializeAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.MalformedImportPlan
      && diagnostic.Path == "semanticOverrides.objectRoles[].roles");
  }

  [Fact]
  public async Task VersionOneEditPlanRoundTripsOnlyTypedConflictActions()
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

    read.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", read.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Path}:{diagnostic.Message}")));
    read.Value!.Kind.Should().Be(GltfImportPlanKind.Edit);
    read.Value.ExpectedBaseline!.AssetLineageId.Should().Be(_lineageId);
    read.Value.EditOptions!.ConflictResolutions.Should().HaveCount(2);
    read.Value.EditOptions.ConflictResolutions[0].TargetNativePath.Should().Be("nodes[2]");
    read.Value.NewModelOptions.Should().BeNull();
  }

  [Theory]
  [InlineData("{", GltfDiagnosticCodes.MalformedImportPlan, 3000)]
  [InlineData("{\"format\":\"earthtool.msh.import-plan\",\"version\":2}", GltfDiagnosticCodes.UnsupportedImportPlanVersion, 3001)]
  [InlineData("{\"format\":\"earthtool.msh.import-plan\",\"version\":1,\"mode\":\"newModel\",\"package\":\"glb\",\"sourceSha256\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"rawMsh\":\"AA==\",\"semanticOverrides\":{}}", GltfDiagnosticCodes.MalformedImportPlan, 3000)]
  [InlineData("{\"format\":\"earthtool.msh.import-plan\",\"format\":\"earthtool.msh.import-plan\",\"version\":1}", GltfDiagnosticCodes.MalformedImportPlan, 3000)]
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
  public async Task NewModelPlanReplaysTypedSemanticOverrides()
  {
    var source = await CreateMetadataFreeGlbAsync();
    var plan = GltfImportPlan.CreateNewModel(
      GltfPackageKind.Glb,
      Sha256(source),
      new GltfNewModelImportOptions(
        footprint: new GltfNewModelFootprint(
          5,
          Enumerable.Repeat(2f, 16),
          new byte[16]),
        horizontalExtents: new GltfNewModelHorizontalExtents(1, 2, 3, 4)));
    await using var stream = new MemoryStream(source);

    var result = await new GltfInterchange().ImportNewModelGlbWithPlanAsync(stream, plan);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    result.Value!.Asset.CommonBaseHeader.BoxPresenceMask.Should().Be(5);
    result.Value.Asset.CommonBaseHeader.HorizontalExtents.Should().Equal(
      new byte[] { 0, 1, 0, 2, 0, 3, 0, 4 });
  }

  [Fact]
  public async Task EditPlanReplaysAMatchingConflictAction()
  {
    var (source, expected, conflictKey) = await CreateMissingManifestConflictAsync();
    var interchange = new GltfInterchange();
    var plan = GltfImportPlan.CreateEdit(
      GltfPackageKind.Glb,
      Sha256(source),
      expected,
      new GltfEditImportOptions(
      [
        new GltfMetadataConflictResolution(
          conflictKey,
          GltfMetadataConflictActions.DiscardLineage)
      ]));
    await using var replay = new MemoryStream(source);

    var applied = await interchange.ImportEditGlbWithPlanAsync(replay, expected, plan);

    applied.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", applied.Diagnostics.Select(diagnostic => diagnostic.Message)));
    applied.Value!.LineageDisposition.Should().Be(GltfMetadataLineageDisposition.Discarded);
    applied.Value.AppliedConflictResolutions.Should().ContainSingle().Subject.Action.Should()
      .Be(GltfMetadataConflictActions.DiscardLineage);
  }

  [Fact]
  public async Task EditPlanRejectsAStaleConflictAction()
  {
    var (source, expected, conflictKey) = await CreateMissingManifestConflictAsync();
    var interchange = new GltfInterchange();

    var staleKey = conflictKey.Substring(0, conflictKey.Length - 1)
      + (conflictKey[^1] == 'A' ? "B" : "A");
    var stalePlan = GltfImportPlan.CreateEdit(
      GltfPackageKind.Glb,
      Sha256(source),
      expected,
      new GltfEditImportOptions(
      [
        new GltfMetadataConflictResolution(staleKey, GltfMetadataConflictActions.DiscardLineage)
      ]));
    await using var staleReplay = new MemoryStream(source);

    var stale = await interchange.ImportEditGlbWithPlanAsync(staleReplay, expected, stalePlan);

    stale.Status.Should().Be(OperationStatus.Failed);
    stale.Value.Should().BeNull();
    stale.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.StaleImportPlan
      && diagnostic.EventId == 3003
      && diagnostic.Path == "conflictActions");
  }

  [Fact]
  public async Task PublicPlanCannotBypassConflictActionLimits()
  {
    var plan = GltfImportPlan.CreateEdit(
      GltfPackageKind.Glb,
      new string('a', 64),
      new InterchangeBaseline(_lineageId, _documentId),
      new GltfEditImportOptions(
      [
        new GltfMetadataConflictResolution(
          "v1:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
          GltfMetadataConflictActions.MapScope,
          "nodes[0]"),
        new GltfMetadataConflictResolution(
          "v1:BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
          GltfMetadataConflictActions.AcceptDeletion)
      ]));
    var profile = new GltfOperationProfile(
      1024,
      1024,
      1024,
      8,
      16,
      16,
      4,
      1024,
      1024,
      2,
      16,
      maxMetadataConflicts: 1);
    await using var source = new MemoryStream();

    var result = await new GltfInterchange().ImportEditGlbWithPlanAsync(
      source,
      plan.ExpectedBaseline!,
      plan,
      profile);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.ImportPlanResourceLimitExceeded
      && diagnostic.EventId == 3002
      && diagnostic.Path == "conflictActions");
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
  public async Task SeparateGltfEditPlanReplaysAMatchingConflictAction()
  {
    var fixture = await CreateMetadataFreeSeparateGltfAsync();
    try
    {
      var expected = new InterchangeBaseline(_lineageId, _documentId);
      var interchange = new GltfInterchange();
      var conflict = await interchange.ImportEditGltfFileAsync(fixture.SourcePath, expected);
      var conflictKey = conflict.Diagnostics.Should().ContainSingle().Subject.Data["conflictKey"];
      var plan = GltfImportPlan.CreateEdit(
        GltfPackageKind.Gltf,
        fixture.SourceSha256,
        expected,
        new GltfEditImportOptions(
        [
          new GltfMetadataConflictResolution(
            conflictKey,
            GltfMetadataConflictActions.DiscardLineage)
        ]));

      var result = await interchange.ImportEditGltfFileWithPlanAsync(
        fixture.SourcePath,
        expected,
        plan);

      result.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
      result.Value!.LineageDisposition.Should().Be(GltfMetadataLineageDisposition.Discarded);
      result.Value.AppliedConflictResolutions.Should().ContainSingle().Subject.Action.Should()
        .Be(GltfMetadataConflictActions.DiscardLineage);
    }
    finally
    {
      Directory.Delete(fixture.Directory, true);
    }
  }

  [Fact]
  public async Task VersionOneReportIsDeterministicAndContainsCompleteOperationEffects()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(_lineageId, _documentId));
    var metadataFree = RemoveMetadata(exported.ToArray());
    await using var newSource = new MemoryStream(metadataFree);
    var imported = await interchange.ImportNewModelGlbAsync(newSource);
    var expected = new InterchangeBaseline(_lineageId, _documentId);
    await using var conflictSource = new MemoryStream(metadataFree);
    var conflict = await interchange.ImportEditGlbAsync(conflictSource, expected);
    var conflictKey = conflict.Diagnostics.Should().ContainSingle().Subject.Data["conflictKey"];
    await using var editSource = new MemoryStream(metadataFree);
    var edited = await interchange.ImportEditGlbWithResolutionsAsync(
      editSource,
      expected,
      new GltfEditImportOptions(
      [
        new GltfMetadataConflictResolution(
          conflictKey,
          GltfMetadataConflictActions.DiscardLineage)
      ]));
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
      GltfCliReportOperation.ForNewModelImport(
        "authored.glb",
        "authored.msh",
        GltfPackageKind.Glb,
        imported),
      GltfCliReportOperation.ForEditImport(
        "edited.glb",
        "edited.msh",
        GltfPackageKind.Glb,
        expected,
        edited),
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
    AssertJsonApproval("gltf-cli-report-v1", normalized);
    using var document = JsonDocument.Parse(first.ToArray());
    var operations = document.RootElement.GetProperty("operations");
    operations.GetArrayLength().Should().Be(4);
    operations[1].GetProperty("preservation").GetProperty("changes").GetArrayLength()
      .Should().BeGreaterThan(0);
    operations[1].GetProperty("identities").GetProperty("meshAssetLineageId").GetString()
      .Should().Be(imported.Value!.Asset.LineageId.Value.ToString("D"));
    operations[1].GetProperty("identities").GetProperty("meshCreationGuid").GetString()
      .Should().Be(imported.Value.Asset.ArchiveFraming.CreationGuid!.Value.ToString("D"));
    operations[2].GetProperty("lineageDisposition").GetString().Should().Be("discarded");
    operations[2].GetProperty("appliedConflictActions").GetArrayLength().Should().Be(1);
    operations[2].GetProperty("preservation").GetProperty("changes").GetArrayLength()
      .Should().BeGreaterThan(0);
    operations[3].GetProperty("diagnostics")[0].GetProperty("data")
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
    var result = await new GltfInterchange().ExportGlbAsync(
      asset,
      stream,
      new GltfExportOptions(_lineageId, _documentId));
    result.Status.Should().Be(OperationStatus.Succeeded);
    return RemoveMetadata(stream.ToArray());
  }

  private static async Task<(byte[] Source, InterchangeBaseline Expected, string ConflictKey)>
    CreateMissingManifestConflictAsync()
  {
    var source = await CreateMetadataFreeGlbAsync();
    var expected = new InterchangeBaseline(_lineageId, _documentId);
    await using var firstAttempt = new MemoryStream(source);
    var conflict = await new GltfInterchange().ImportEditGlbAsync(firstAttempt, expected);
    var conflictKey = conflict.Diagnostics.Should().ContainSingle().Subject.Data["conflictKey"];
    return (source, expected, conflictKey);
  }

  private static async Task<(string Directory, string SourcePath, string BufferPath, string SourceSha256)>
    CreateMetadataFreeSeparateGltfAsync()
  {
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var sourcePath = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var export = await new GltfInterchange().ExportGltfFileAsync(
      asset,
      sourcePath,
      new GltfExportOptions(_lineageId, _documentId));
    export.Status.Should().Be(OperationStatus.Succeeded);
    var root = JsonNode.Parse(await File.ReadAllTextAsync(sourcePath))!;
    RemoveMetadata(root);
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

  private static byte[] NormalizeReportIdentities(byte[] json)
  {
    var root = JsonNode.Parse(json)!.AsObject();
    var operations = root["operations"]!.AsArray();
    var exportIdentities = operations[0]!["identities"]!.AsObject();
    exportIdentities["meshAssetLineageId"] = _lineageId.ToString("D");
    exportIdentities["meshCreationGuid"] = _documentId.ToString("D");
    var importIdentities = operations[1]!["identities"]!.AsObject();
    importIdentities["meshAssetLineageId"] = _lineageId.ToString("D");
    importIdentities["meshCreationGuid"] = _documentId.ToString("D");
    importIdentities["baseline"]!["assetLineageId"] = _lineageId.ToString("D");
    importIdentities["baseline"]!["documentId"] = _documentId.ToString("D");
    var editIdentities = operations[2]!["identities"]!.AsObject();
    editIdentities["meshAssetLineageId"] = _lineageId.ToString("D");
    editIdentities["meshCreationGuid"] = _documentId.ToString("D");
    editIdentities["nextBaseline"]!["assetLineageId"] = _lineageId.ToString("D");
    editIdentities["nextBaseline"]!["documentId"] = _documentId.ToString("D");
    return Encoding.UTF8.GetBytes(root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n");
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
