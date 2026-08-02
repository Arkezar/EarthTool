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

public class MetadataGraphValidationTests
{
  private static readonly Guid _lineageId = new("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
  private static readonly Guid _documentId = new("11111111-2222-4333-8444-555555555555");

  [Fact]
  public void MetadataIdentitiesRequireVersionFourUuids()
  {
    var versionOne = Guid.Parse("11111111-2222-1333-8444-555555555555");

    var createOptions = () => new GltfExportOptions(versionOne, _documentId);
    var createBaseline = () => new InterchangeBaseline(_lineageId, versionOne);

    createOptions.Should().Throw<ArgumentException>();
    createBaseline.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void PreservedUnknownMetadataRequiresAnAdditivePointerAndValidRawJson()
  {
    var knownMember = () => new GltfExportOptions(
      _lineageId,
      _documentId,
      preservedUnknownMetadata: new Dictionary<string, string>
      {
        ["manifest:0:/format"] = "1"
      });
    var invalidJson = () => new GltfExportOptions(
      _lineageId,
      _documentId,
      preservedUnknownMetadata: new Dictionary<string, string>
      {
        ["manifest:0:/future"] = "{"
      });
    var noncanonicalScope = () => new GltfExportOptions(
      _lineageId,
      _documentId,
      preservedUnknownMetadata: new Dictionary<string, string>
      {
        ["object:01:/future"] = "1"
      });

    knownMember.Should().Throw<ArgumentException>();
    invalidJson.Should().Throw<ArgumentException>();
    noncanonicalScope.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void MetadataConflictCatalogMatchesSerializedApproval()
  {
    var actual = JsonSerializer.Serialize(
      GltfMetadataConflictCatalog.ActionsByCode,
      new JsonSerializerOptions { WriteIndented = true }) + "\n";
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var approved = File.ReadAllText(Path.Combine(
      root,
      "EarthTool.MSH.Tests",
      "Approvals",
      "gltf-metadata-conflicts.approved.json")).ReplaceLineEndings("\n");

    actual.Should().Be(approved);
  }

  [Fact]
  public void MetadataConflictCatalogCoversTheCompleteReservedRange()
  {
    GltfMetadataConflictCatalog.ActionsByCode.Keys.Should().Equal(
      Enumerable.Range(2000, 21).Select(eventId => $"ETG{eventId}"));
    GltfMetadataConflictCatalog.ActionsByCode.Values.Should().AllSatisfy(actions =>
    {
      actions.Should().NotBeEmpty();
      actions.Should().OnlyHaveUniqueItems();
    });
  }

  [Fact]
  public async Task CanonicalMetadataGraphMatchesSerializedApproval()
  {
    var (bytes, _) = await ExportFixtureAsync();
    var root = ReadJson(bytes);
    var graph = new List<JsonObject>
    {
      DescribeEnvelope("scenes[0]", root["scenes"]![0]!)
    };
    AddEnvelopeDescriptions(graph, root, "nodes");
    AddEnvelopeDescriptions(graph, root, "meshes");
    AddEnvelopeDescriptions(graph, root, "materials");
    var actual = JsonSerializer.Serialize(
      graph,
      new JsonSerializerOptions { WriteIndented = true }) + "\n";
    var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var approved = File.ReadAllText(Path.Combine(
      repositoryRoot,
      "EarthTool.MSH.Tests",
      "Approvals",
      "gltf-metadata-graph.approved.json")).ReplaceLineEndings("\n");

    actual.Should().Be(approved);
  }

  [Fact]
  public async Task ExportEmitsTheApprovedVersionOneEnvelopeAndInventory()
  {
    var (bytes, _) = await ExportFixtureAsync();
    var root = ReadJson(bytes);
    var manifest = ReadEnvelope(root["scenes"]![0]!);

    manifest["format"]!.GetValue<string>().Should().Be("earthtool.msh.gltf");
    manifest["version"]!.GetValue<int>().Should().Be(1);
    manifest["kind"]!.GetValue<string>().Should().Be("manifest");
    manifest["lineage"]!.GetValue<string>().Should().Be(_lineageId.ToString());
    manifest["document"]!.GetValue<string>().Should().Be(_documentId.ToString());
    manifest["id"]!.GetValue<int>().Should().Be(0);
    manifest["guards"].Should().BeOfType<JsonObject>();
    manifest["payload"]!["origin"]!["kind"]!.GetValue<string>().Should().Be("mshExport");
    manifest["payload"]!["asset"].Should().BeOfType<JsonObject>();
    manifest["payload"]!["asset"]!["sourceMsh"]!.GetValue<string>().Should().MatchRegex("^[A-Za-z0-9_-]+$");
    manifest["payload"]!["inventory"]!["object"].Should().BeOfType<JsonArray>();
    manifest["payload"]!["inventory"]!["mesh"].Should().BeOfType<JsonArray>();
    manifest["payload"]!["inventory"]!["material"].Should().BeOfType<JsonArray>();
    manifest["payload"]!["inventory"]!["light"].Should().BeOfType<JsonArray>();
  }

  [Theory]
  [InlineData("format", "foreign.format", GltfDiagnosticCodes.MalformedMetadata, 2003)]
  [InlineData("kind", "mesh", GltfDiagnosticCodes.KindCarrierMismatch, 2008)]
  [InlineData("id", "1", GltfDiagnosticCodes.MalformedMetadata, 2003)]
  public async Task MalformedManifestMembersProduceAssignedConflicts(
    string member,
    string replacement,
    string expectedCode,
    int expectedEventId)
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["scenes"]![0]!);
      metadata[member] = member == "id" ? int.Parse(replacement) : replacement;
      WriteEnvelope(root["scenes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, expectedCode, expectedEventId);
  }

  [Fact]
  public async Task NonStringReservedCarrierProducesCarrierConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
      root["scenes"]![0]!["extras"]!["earthtool"] = new JsonObject());

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.InvalidMetadataCarrier, 2002);
    result.Diagnostics[0].Data["actions"].Should().Be("abort,retryWithMetadata,discardLineage");
  }

  [Fact]
  public async Task DuplicateExtrasCarrierProducesCarrierConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteRawJson(bytes, root =>
    {
      var scene = root["scenes"]![0]!;
      var sceneJson = scene.ToJsonString();
      var duplicateScene = sceneJson.Insert(
        sceneJson.Length - 1,
        ",\"extras\":" + scene["extras"]!.ToJsonString());
      var json = root.ToJsonString();
      var offset = json.IndexOf(sceneJson, StringComparison.Ordinal);
      return json.Remove(offset, sceneJson.Length).Insert(offset, duplicateScene);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.InvalidMetadataCarrier, 2002);
  }

  [Fact]
  public async Task MissingManifestProducesTheAssignedConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root => root["scenes"]![0]!.AsObject().Remove("extras"));

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.MissingManifest, 2000);
  }

  [Fact]
  public async Task AdditionalSceneProducesTheAssignedSceneConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root => root["scenes"]!.AsArray().Add(new JsonObject
    {
      ["nodes"] = new JsonArray()
    }));

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.InvalidSceneContract, 2001);
  }

  [Fact]
  public async Task UnsupportedVersionRemainsOpaqueInsteadOfSalvagingIdentity()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var scene = root["scenes"]![0]!;
      WriteEnvelope(scene, "{\"version\":2,\"future\":1,\"future\":2}");
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.UnsupportedMetadataVersion, 2004);
    result.Diagnostics[0].Data.Should().NotContainKey("lineage");
    result.Diagnostics[0].Data["actions"].Should().Be("abort,retryWithMetadata,discardLineage");
  }

  [Fact]
  public async Task FractionalVersionIsMalformedRatherThanUnsupported()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var scene = root["scenes"]![0]!;
      var metadata = ReadEnvelope(scene);
      metadata["version"] = 1.5;
      WriteEnvelope(scene, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.MalformedMetadata, 2003);
  }

  [Fact]
  public async Task UnsupportedGuardDiagnosticIdentifiesItsCarrier()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["meshes"]![0]!);
      metadata["guards"]!["nativeProjection"]!["version"] = 0;
      WriteEnvelope(root["meshes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.UnsupportedGuard, 2015);
    result.Diagnostics[0].Path.Should().Be("meshes[0].guards.nativeProjection");
  }

  [Fact]
  public async Task MissingExpectedMeshEnvelopeProducesTheAssignedConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root => root["meshes"]![0]!.AsObject().Remove("extras"));

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.MissingExpectedScope, 2010);
    result.Diagnostics[0].Path.Should().Be("meshes[0]");
  }

  [Fact]
  public async Task PartitionReferenceToAnUnknownMaterialProducesTheAssignedConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["meshes"]![0]!);
      metadata["payload"]!["partitions"]![0]!["localId"] = int.MaxValue;
      WriteEnvelope(root["meshes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.DanglingMetadataReference, 2013);
    result.Diagnostics[0].Path.Should().Be("meshes[0].payload.partitions[0].localId");
  }

  [Fact]
  public async Task MissingReferencedMaterialReportsScopeAndDanglingReferenceConflicts()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root => root["materials"]![0]!.AsObject().Remove("extras"));

    var result = await ImportAsync(edited, baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Path)).Should().Equal(
      (GltfDiagnosticCodes.MissingExpectedScope, "materials[0]"),
      (GltfDiagnosticCodes.DanglingMetadataReference, "meshes[0].payload.partitions[0].localId"));
    result.Diagnostics[1].Data["nativePath"].Should().Be("materials");
  }

  [Fact]
  public async Task ObjectAndMeshIdentityMismatchProducesAmbiguousCorrespondence()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var nodeMetadata = ReadEnvelope(root["nodes"]![0]!);
      nodeMetadata["id"] = 2;
      WriteEnvelope(root["nodes"]![0]!, nodeMetadata);
      var manifest = ReadEnvelope(root["scenes"]![0]!);
      manifest["payload"]!["inventory"]!["object"]![0] = 2;
      WriteEnvelope(root["scenes"]![0]!, manifest);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.AmbiguousPartitionCorrespondence, 2012);
    result.Diagnostics[0].Path.Should().Be("nodes[0]");
  }

  [Fact]
  public async Task MissingRequiredNativeProjectionGuardProducesTheAssignedConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["meshes"]![0]!);
      metadata["guards"]!.AsObject().Remove("nativeProjection");
      WriteEnvelope(root["meshes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.MissingRequiredGuard, 2014);
    result.Diagnostics[0].Path.Should().Be("meshes[0].guards.nativeProjection");
  }

  [Fact]
  public async Task WrongGuardProjectionProducesTheAssignedConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["meshes"]![0]!);
      metadata["guards"]!["nativeProjection"]!["projection"] = "foreign-geometry";
      WriteEnvelope(root["meshes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.UnsupportedGuard, 2015);
    result.Diagnostics[0].Path.Should().Be("meshes[0].guards.nativeProjection");
  }

  [Fact]
  public async Task StaleGuardWithUnchangedNativeProjectionProducesTheAssignedConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["meshes"]![0]!);
      metadata["guards"]!["nativeProjection"]!["digest"] =
        Convert.ToBase64String(new byte[32]).TrimEnd('=').Replace('+', '-').Replace('/', '_');
      WriteEnvelope(root["meshes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.StaleNativeProjection, 2016);
    result.Diagnostics[0].Path.Should().Be("meshes[0].guards.nativeProjection");
  }

  [Fact]
  public async Task ArtistObjectGuardBindsTheEnvelopeScopeIdentity()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var cannonMetadata = ReadEnvelope(root["nodes"]![4]!);
      cannonMetadata["id"] = 55;
      WriteEnvelope(root["nodes"]![4]!, cannonMetadata);
      var manifest = ReadEnvelope(root["scenes"]![0]!);
      manifest["payload"]!["inventory"]!["object"]![4] = 55;
      manifest["payload"]!["nextIds"]!["object"] = 56;
      WriteEnvelope(root["scenes"]![0]!, manifest);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.StaleNativeProjection, 2016);
    result.Diagnostics[0].Path.Should().Be("nodes[4].guards.nativeProjection");
  }

  [Fact]
  public async Task GeometryFingerprintDoesNotApplyAGlobalEpsilonToBinary32Edits()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var binaryHeader = 20 + BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12));
    var positionOffset = binaryHeader + 8;
    var originalBits = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(positionOffset));
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(positionOffset), originalBits + 1);

    var result = await ImportAsync(bytes, baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    result.Value!.RestoredSerializedRepresentationPaths.Should().NotContain(
      "StaticRenderObjectSequence[0]");
    result.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath.StartsWith("StaticRenderObjectSequence[0].RenderVertices", StringComparison.Ordinal));
  }

  [Fact]
  public async Task ContradictorySourceProvenanceProducesTheAssignedConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var root = ReadJson(bytes);
    var manifest = ReadEnvelope(root["scenes"]![0]!);
    var sourceMsh = DecodeBase64Url(manifest["payload"]!["asset"]!["sourceMsh"]!.GetValue<string>());
    manifest["payload"]!["origin"]!["source"]!["byteLength"] = sourceMsh.Length + 1;
    WriteEnvelope(root["scenes"]![0]!, manifest);

    var result = await ImportAsync(PackJson(bytes, root.ToJsonString()), baseline);

    AssertConflict(result, GltfDiagnosticCodes.ProvenanceMismatch, 2017);
    result.Diagnostics[0].Path.Should().Be("scenes[0].payload.origin.source");
  }

  [Fact]
  public async Task ExportedSourceProvenanceMatchesThePreservedMsh()
  {
    var (bytes, _) = await ExportFixtureAsync();
    var manifest = ReadEnvelope(ReadJson(bytes)["scenes"]![0]!);
    var sourceMsh = DecodeBase64Url(manifest["payload"]!["asset"]!["sourceMsh"]!.GetValue<string>());
    var source = manifest["payload"]!["origin"]!["source"]!;

    source["byteLength"]!.GetValue<int>().Should().Be(sourceMsh.Length);
    DecodeBase64Url(source["sha256"]!.GetValue<string>()).Should().Equal(SHA256.HashData(sourceMsh));
  }

  [Fact]
  public async Task UnknownRequiredScopeKindProducesTheAssignedConflict()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["meshes"]![0]!);
      metadata["kind"] = "futureRequiredScope";
      WriteEnvelope(root["meshes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.UnknownRequiredSemantics, 2018);
  }

  [Fact]
  public async Task IndependentConflictsReturnOneStableInventoryBeforeReconciliation()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var (bytes, baseline) = await ExportAssetAsync(fixture.Data);
    var edited = RewriteJson(bytes, root =>
    {
      foreach (var mesh in root["meshes"]!.AsArray().Take(2))
      {
        var metadata = ReadEnvelope(mesh!);
        metadata["guards"]!.AsObject().Remove("nativeProjection");
        WriteEnvelope(mesh!, metadata);
      }
    });

    var result = await ImportAsync(edited, baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Path)).Should().Equal(
      (GltfDiagnosticCodes.MissingRequiredGuard, "meshes[0].guards.nativeProjection"),
      (GltfDiagnosticCodes.MissingRequiredGuard, "meshes[1].guards.nativeProjection"));
    result.Diagnostics.Should().AllSatisfy(diagnostic => diagnostic.Data["actions"].Should().Be(
      string.Join(",", GltfMetadataConflictCatalog.ActionsByCode[diagnostic.Code])));
  }

  [Fact]
  public async Task StructuralAndApplicabilityConflictsShareOneStableInventory()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var nodeMetadata = ReadEnvelope(root["nodes"]![0]!);
      nodeMetadata["lineage"] = Guid.NewGuid().ToString();
      WriteEnvelope(root["nodes"]![0]!, nodeMetadata);
      RemoveFirstMeshGuard(root);
    });

    var result = await ImportAsync(edited, baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Path)).Should().Equal(
      (GltfDiagnosticCodes.AssetLineageMismatch, "nodes[0]"),
      (GltfDiagnosticCodes.MissingRequiredGuard, "meshes[0].guards.nativeProjection"));
  }

  [Fact]
  public async Task ConflictInventoryLimitEndsWithTheAssignedTruncationConflict()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var (bytes, baseline) = await ExportAssetAsync(fixture.Data);
    var edited = RewriteJson(bytes, root =>
    {
      foreach (var mesh in root["meshes"]!.AsArray().Take(3))
      {
        var metadata = ReadEnvelope(mesh!);
        metadata["guards"]!.AsObject().Remove("nativeProjection");
        WriteEnvelope(mesh!, metadata);
      }
    });

    var result = await ImportAsync(edited, baseline, CreateProfile(maxMetadataConflicts: 2));

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Select(diagnostic => diagnostic.Code).Should().Equal(
      GltfDiagnosticCodes.MissingRequiredGuard,
      GltfDiagnosticCodes.TooManyMetadataConflicts);
    result.Diagnostics[^1].Data["actions"].Should().Be("abort,retryWithMetadata");
  }

  [Fact]
  public async Task GlbAndSeparateGltfReturnTheSameMetadataConflictInventory()
  {
    await using var source = new MemoryStream(OneTriangleMshFixture.Create());
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var glbExport = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(_lineageId, _documentId));
    var editedGlb = RewriteJson(glb.ToArray(), RemoveFirstMeshGuard);
    var directory = Directory.CreateTempSubdirectory("earthtool-metadata-");
    try
    {
      var path = Path.Combine(directory.FullName, "model.gltf");
      var separateExport = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(_lineageId, _documentId));
      separateExport.Status.Should().Be(OperationStatus.Succeeded);
      var separateRoot = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveFirstMeshGuard(separateRoot);
      await File.WriteAllTextAsync(path, separateRoot.ToJsonString());
      await using var editedInput = new MemoryStream(editedGlb);

      var glbResult = await interchange.ImportEditGlbAsync(editedInput, glbExport.Value!.Baseline);
      var separateResult = await interchange.ImportEditGltfFileAsync(path, separateExport.Value!.Baseline);

      glbResult.Value.Should().BeNull();
      separateResult.Value.Should().BeNull();
      separateResult.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Path)).Should().Equal(
        glbResult.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Path)));
    }
    finally
    {
      directory.Delete(true);
    }
  }

  [Fact]
  public async Task MetadataJsonDepthFailsBeforeEnvelopeMaterialization()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var scene = root["scenes"]![0]!;
      var envelope = scene["extras"]!["earthtool"]!.GetValue<string>();
      var deepValue = new string('[', 65) + "0" + new string(']', 65);
      WriteEnvelope(scene, envelope.Insert(envelope.Length - 1, ",\"deep\":" + deepValue));
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.MetadataResourceLimitExceeded, 2005);
  }

  [Fact]
  public async Task ForeignLocalEnvelopeCannotClaimPreservationState()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["nodes"]![0]!);
      metadata["lineage"] = Guid.NewGuid().ToString();
      WriteEnvelope(root["nodes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.AssetLineageMismatch, 2006);
  }

  [Fact]
  public async Task ForeignDocumentEnvelopeCannotClaimPreservationState()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["nodes"]![0]!);
      metadata["document"] = Guid.NewGuid().ToString();
      WriteEnvelope(root["nodes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.DocumentMismatch, 2007);
  }

  [Theory]
  [InlineData(true, GltfDiagnosticCodes.AssetLineageMismatch, 2006)]
  [InlineData(false, GltfDiagnosticCodes.DocumentMismatch, 2007)]
  public async Task ExpectedBaselineAuthorizesEditImport(
    bool replaceLineage,
    string expectedCode,
    int expectedEventId)
  {
    var (bytes, _) = await ExportFixtureAsync();
    var expected = new InterchangeBaseline(
      replaceLineage ? Guid.NewGuid() : _lineageId,
      replaceLineage ? _documentId : Guid.NewGuid());

    var result = await ImportAsync(bytes, expected);

    AssertConflict(result, expectedCode, expectedEventId);
  }

  [Fact]
  public async Task ReservedEnvelopeOnUnsupportedCarrierIsOrphaned()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root => root["asset"]!["extras"] = new JsonObject
    {
      ["earthtool"] = root["scenes"]![0]!["extras"]!["earthtool"]!.GetValue<string>()
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.OrphanEnvelope, 2011);
  }

  [Fact]
  public async Task DuplicateScopeIdentityIsRejectedBeforeReconciliation()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var duplicate = nodes[0]!.DeepClone();
      nodes.Add(duplicate);
      nodes[0]!["children"] = new JsonArray(nodes.Count - 1);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.DuplicateScopeIdentity, 2009);
    result.Diagnostics[0].Data["actions"].Should().Be("abort,mapScope,forkScope,discardAffectedState");
  }

  [Fact]
  public async Task InvalidManifestInventoryCannotAuthorizeScopes()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var scene = root["scenes"]![0]!;
      var metadata = ReadEnvelope(scene);
      metadata["payload"]!["inventory"]!["object"] = new JsonArray(1, 1);
      WriteEnvelope(scene, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    AssertConflict(result, GltfDiagnosticCodes.InvalidManifestInventory, 2020);
  }

  [Fact]
  public async Task ManifestIdentityHighWaterMarksSurviveTheNextBaseline()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var scene = root["scenes"]![0]!;
      var metadata = ReadEnvelope(scene);
      metadata["payload"]!["nextIds"]!["light"] = 100;
      WriteEnvelope(scene, metadata);
    });
    var import = await ImportAsync(edited, baseline);
    import.Status.Should().Be(OperationStatus.Succeeded);
    await using var rewritten = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      import.Value!.Asset,
      rewritten,
      import.Value.NextExportOptions);

    export.Status.Should().Be(OperationStatus.Succeeded);
    var manifest = ReadEnvelope(ReadJson(rewritten.ToArray())["scenes"]![0]!);
    manifest["payload"]!["nextIds"]!["light"]!.GetValue<int>().Should().Be(100);
  }

  [Fact]
  public async Task UnknownAdditiveMembersRetainRawTokensWithoutChangingIdentity()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    const string raw = "{\"z\":[1,2],\"a\":1}";
    var edited = RewriteJson(bytes, root =>
    {
      var scene = root["scenes"]![0]!;
      var metadata = ReadEnvelope(scene);
      metadata["futureMember"] = JsonNode.Parse(raw);
      metadata["future.member/name"] = JsonNode.Parse(raw);
      metadata["guards"]!["futureGuard"] = JsonNode.Parse(raw);
      metadata["payload"]!["origin"]!["source"]!["futureSourceMember"] = JsonNode.Parse(raw);
      metadata["payload"]!["asset"]!["futureAssetMember"] = JsonNode.Parse(raw);
      metadata["payload"]!["inventory"]!["futureInventoryMember"] = JsonNode.Parse(raw);
      WriteEnvelope(scene, metadata);
    });

    var result = await ImportAsync(edited, baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    result.Value!.PreservedUnknownMetadata["manifest:0:/futureMember"].Should().Be(raw);
    result.Value.PreservedUnknownMetadata["manifest:0:/future.member~1name"].Should().Be(raw);
    result.Value.PreservedUnknownMetadata["manifest:0:/guards/futureGuard"].Should().Be(raw);
    result.Value.PreservedUnknownMetadata["manifest:0:/payload/origin/source/futureSourceMember"]
      .Should().Be(raw);
    result.Value.PreservedUnknownMetadata["manifest:0:/payload/asset/futureAssetMember"].Should().Be(raw);
    result.Value.PreservedUnknownMetadata["manifest:0:/payload/inventory/futureInventoryMember"].Should().Be(raw);
    await using var rewritten = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      result.Value.Asset,
      rewritten,
      result.Value.NextExportOptions);

    export.Status.Should().Be(OperationStatus.Succeeded);
    var rewrittenRoot = ReadJson(rewritten.ToArray());
    rewrittenRoot["scenes"]![0]!["extras"]!["earthtool"]!.GetValue<string>().Should()
      .Contain("\"futureMember\":" + raw);
    ReadEnvelope(rewrittenRoot["scenes"]![0]!)["payload"]!["asset"]!["futureAssetMember"]!
      .ToJsonString().Should().Be(raw);
    ReadEnvelope(rewrittenRoot["scenes"]![0]!)["future.member/name"]!.ToJsonString().Should().Be(raw);
    ReadEnvelope(rewrittenRoot["scenes"]![0]!)["guards"]!["futureGuard"]!
      .ToJsonString().Should().Be(raw);
    ReadEnvelope(rewrittenRoot["scenes"]![0]!)["payload"]!["origin"]!["source"]!["futureSourceMember"]!
      .ToJsonString().Should().Be(raw);
    ReadEnvelope(rewrittenRoot["scenes"]![0]!)["payload"]!["inventory"]!["futureInventoryMember"]!
      .ToJsonString().Should().Be(raw);
  }

  [Fact]
  public async Task AggregateMetadataByteBudgetFailsBeforeReconciliation()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var profile = CreateProfile(maxTotalMetadataBytes: 1);

    var result = await ImportAsync(bytes, baseline, profile);

    AssertConflict(result, GltfDiagnosticCodes.MetadataResourceLimitExceeded, 2005);
  }

  [Theory]
  [InlineData("envelopes")]
  [InlineData("elements")]
  public async Task StructuralMetadataBudgetsFailBeforeReconciliation(string budget)
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var profile = budget == "envelopes"
      ? CreateProfile(maxMetadataEnvelopes: 1)
      : CreateProfile(maxMetadataElements: 1);

    var result = await ImportAsync(bytes, baseline, profile);

    AssertConflict(result, GltfDiagnosticCodes.MetadataResourceLimitExceeded, 2005);
  }

  [Fact]
  public async Task UnknownMemberBudgetFailsBeforeRetainingAllTokens()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var metadata = ReadEnvelope(root["scenes"]![0]!);
      metadata["futureOne"] = 1;
      metadata["futureTwo"] = 2;
      WriteEnvelope(root["scenes"]![0]!, metadata);
    });

    var result = await ImportAsync(edited, baseline, CreateProfile(maxUnknownMetadataMembers: 1));

    AssertConflict(result, GltfDiagnosticCodes.MetadataResourceLimitExceeded, 2005);
  }

  [Fact]
  public async Task ValidMetadataDoesNotConsumeTheUnknownMemberBudget()
  {
    var (bytes, baseline) = await ExportFixtureAsync();

    var result = await ImportAsync(bytes, baseline, CreateProfile(maxUnknownMetadataMembers: 1));

    result.Status.Should().Be(OperationStatus.Succeeded);
    result.Value!.PreservedUnknownMetadata.Should().BeEmpty();
  }

  [Fact]
  public async Task GuardBudgetFailsBeforeReconciliation()
  {
    var (bytes, baseline) = await ExportFixtureAsync();
    var edited = RewriteJson(bytes, root =>
    {
      var manifest = ReadEnvelope(root["scenes"]![0]!);
      var guard = ReadEnvelope(root["meshes"]![0]!)["guards"]!["nativeProjection"]!.DeepClone();
      manifest["guards"]!["futureOne"] = guard;
      manifest["guards"]!["futureTwo"] = guard.DeepClone();
      WriteEnvelope(root["scenes"]![0]!, manifest);
    });

    var result = await ImportAsync(edited, baseline, CreateProfile(maxMetadataGuards: 1));

    AssertConflict(result, GltfDiagnosticCodes.MetadataResourceLimitExceeded, 2005);
  }

  private static void AssertConflict(
    OperationResult<GltfEditImportResult> result,
    string code,
    int eventId)
  {
    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(code);
    diagnostic.EventId.Should().Be(eventId);
    diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
    diagnostic.Path.Should().NotBe("$");
    diagnostic.Data.Should().ContainKey("conflictKey");
    diagnostic.Data.Should().ContainKey("carrierType");
    diagnostic.Data["metadataPath"].Should().Be(diagnostic.Path);
    diagnostic.Data.Should().ContainKey("nativePath");
    diagnostic.Data.Should().ContainKey("affectedPayloadPaths");
    diagnostic.Data["actions"].Should().Be(string.Join(
      ",",
      GltfMetadataConflictCatalog.ActionsByCode[code]));
  }

  private static async Task<(byte[] Bytes, InterchangeBaseline Baseline)> ExportFixtureAsync()
  {
    return await ExportAssetAsync(OneTriangleMshFixture.Create());
  }

  private static async Task<(byte[] Bytes, InterchangeBaseline Baseline)> ExportAssetAsync(byte[] sourceBytes)
  {
    await using var source = new MemoryStream(sourceBytes);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    await using var destination = new MemoryStream();
    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      destination,
      new GltfExportOptions(_lineageId, _documentId));
    export.Status.Should().Be(OperationStatus.Succeeded);
    return (destination.ToArray(), export.Value!.Baseline);
  }

  private static async Task<OperationResult<GltfEditImportResult>> ImportAsync(
    byte[] bytes,
    InterchangeBaseline baseline,
    GltfOperationProfile? profile = null)
  {
    await using var source = new MemoryStream(bytes);
    return await new GltfInterchange().ImportEditGlbAsync(source, baseline, profile);
  }

  private static GltfOperationProfile CreateProfile(
    int maxTotalMetadataBytes = 32 * 1024 * 1024,
    int maxMetadataEnvelopes = 262144,
    int maxMetadataElements = 4194304,
    int maxUnknownMetadataMembers = 262144,
    int maxMetadataGuards = 64,
    int maxMetadataConflicts = 1024)
  {
    return new GltfOperationProfile(
      maxInputBytes: 32 * 1024 * 1024,
      maxOutputBytes: 32 * 1024 * 1024,
      maxMetadataBytes: 4 * 1024 * 1024,
      maxJsonDepth: 32,
      maxActiveRenderVertices: 65536,
      maxNodes: 4096,
      maxHierarchyDepth: 15,
      maxTextureBytes: 16 * 1024 * 1024,
      maxPreviewPixels: 16 * 1024 * 1024,
      maxTextureSearchRoots: 64,
      maxTextureDirectoryEntries: 65536,
      maxTotalMetadataBytes,
      maxMetadataEnvelopes,
      maxMetadataElements,
      maxUnknownMetadataMembers,
      maxMetadataGuards,
      maxMetadataConflicts);
  }

  private static byte[] DecodeBase64Url(string value)
  {
    return Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
      new string('=', (4 - value.Length % 4) % 4));
  }

  private static void RemoveFirstMeshGuard(JsonObject root)
  {
    var metadata = ReadEnvelope(root["meshes"]![0]!);
    metadata["guards"]!.AsObject().Remove("nativeProjection");
    WriteEnvelope(root["meshes"]![0]!, metadata);
  }

  private static void AddEnvelopeDescriptions(
    ICollection<JsonObject> graph,
    JsonObject root,
    string collectionName)
  {
    if (root[collectionName] is not JsonArray collection)
    {
      return;
    }
    for (var index = 0; index < collection.Count; index++)
    {
      var owner = collection[index]!;
      if (owner["extras"]?["earthtool"] is not null)
      {
        graph.Add(DescribeEnvelope($"{collectionName}[{index}]", owner));
      }
    }
  }

  private static JsonObject DescribeEnvelope(string path, JsonNode owner)
  {
    var envelope = ReadEnvelope(owner);
    var description = new JsonObject
    {
      ["path"] = path,
      ["kind"] = envelope["kind"]!.GetValue<string>(),
      ["id"] = envelope["id"]!.GetValue<int>(),
      ["guards"] = new JsonArray(envelope["guards"]!.AsObject().Select(guard => new JsonObject
      {
        ["name"] = guard.Key,
        ["projection"] = guard.Value!["projection"]!.GetValue<string>(),
        ["version"] = guard.Value["version"]!.GetValue<int>()
      }).ToArray()),
      ["payload"] = new JsonArray(envelope["payload"]!.AsObject().Select(member =>
        JsonValue.Create(member.Key)).ToArray())
    };
    if (envelope["kind"]!.GetValue<string>() == "manifest")
    {
      description["inventory"] = envelope["payload"]!["inventory"]!.DeepClone();
      description["nextIds"] = envelope["payload"]!["nextIds"]!.DeepClone();
    }
    else if (envelope["kind"]!.GetValue<string>() == "mesh")
    {
      description["references"] = new JsonArray(envelope["payload"]!["partitions"]!.AsArray()
        .Select(partition => JsonValue.Create(partition!["localId"]!.GetValue<int>())).ToArray());
    }
    else if (envelope["payload"]!["staticLightInstance"] is JsonNode staticLightInstance)
    {
      description["references"] = new JsonArray(
        staticLightInstance["definitionLocalId"]!.GetValue<int>());
    }
    return description;
  }

  private static JsonObject ReadJson(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return JsonNode.Parse(glb.AsSpan(20, jsonLength))!.AsObject();
  }

  private static JsonObject ReadEnvelope(JsonNode owner)
  {
    return JsonNode.Parse(owner["extras"]!["earthtool"]!.GetValue<string>())!.AsObject();
  }

  private static void WriteEnvelope(JsonNode owner, JsonObject envelope)
  {
    owner["extras"]!["earthtool"] = envelope.ToJsonString();
  }

  private static void WriteEnvelope(JsonNode owner, string envelope)
  {
    owner["extras"]!["earthtool"] = envelope;
  }

  private static byte[] RewriteJson(byte[] glb, Action<JsonObject> rewrite)
  {
    var root = ReadJson(glb);
    rewrite(root);
    return PackJson(glb, root.ToJsonString());
  }

  private static byte[] RewriteRawJson(byte[] glb, Func<JsonObject, string> rewrite)
  {
    return PackJson(glb, rewrite(ReadJson(glb)));
  }

  private static byte[] PackJson(byte[] glb, string text)
  {
    var json = Encoding.UTF8.GetBytes(text);
    var paddedJsonLength = (json.Length + 3) & ~3;
    var binaryHeader = 20 + BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(binaryHeader));
    var result = new byte[20 + paddedJsonLength + 8 + binaryLength];
    BinaryPrimitives.WriteUInt32LittleEndian(result, 0x46546C67);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), 2);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), checked((uint)result.Length));
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(12), checked((uint)paddedJsonLength));
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
    json.CopyTo(result.AsSpan(20));
    result.AsSpan(20 + json.Length, paddedJsonLength - json.Length).Fill(0x20);
    var resultBinaryHeader = 20 + paddedJsonLength;
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(resultBinaryHeader), checked((uint)binaryLength));
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(resultBinaryHeader + 4), 0x004E4942);
    glb.AsSpan(binaryHeader + 8, binaryLength).CopyTo(result.AsSpan(resultBinaryHeader + 8));
    return result;
  }
}
