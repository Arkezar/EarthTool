using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Expert;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EarthTool.MSH.Tests;

public class DynamicGltfInterchangeTests
{
  private static readonly Guid _lineageId = new("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
  private static readonly Guid _documentId = new("11111111-2222-4333-8444-555555555555");

  [Fact]
  public async Task GroupAndExplosionExportAsAnOrderedNativePreview()
  {
    var asset = CreateAsset();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset,
      destination,
      new GltfExportOptions(_lineageId, _documentId, null, null, null, "EDBBPP")
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result.Value!.Fingerprint.Name.Should().Be("dynamic-group-explosion-preview");
    result.Value.Fingerprint.Version.Should().Be(1);
    using var json = ReadGlbJson(destination.ToArray());
    var nodes = json.RootElement.GetProperty("nodes");
    nodes.GetArrayLength().Should().Be(4);
    nodes[0]
      .GetProperty("children")
      .EnumerateArray()
      .Select(item => item.GetInt32())
      .Should()
      .Equal(1);
    nodes[0].TryGetProperty("mesh", out _).Should().BeFalse();
    nodes[0].GetProperty("name").GetString().Should().Be("EDBBPP");
    nodes[0]
      .GetProperty("extras")
      .GetProperty("earthtoolPlacementRoot")
      .GetBoolean()
      .Should()
      .BeTrue();
    nodes[1].GetProperty("name").GetString().Should().Be("EDBBPP_1_Group");
    nodes[1]
      .GetProperty("children")
      .EnumerateArray()
      .Select(item => item.GetInt32())
      .Should()
      .Equal(2, 3);
    nodes[2].GetProperty("name").GetString().Should().Be("EDBBPP_2_Explosion");
    nodes[3].GetProperty("name").GetString().Should().Be("EDBBPP_3_Explosion");
    nodes[2].GetProperty("mesh").GetInt32().Should().Be(0);
    nodes[3].GetProperty("mesh").GetInt32().Should().Be(1);
    json.RootElement.GetProperty("meshes").GetArrayLength().Should().Be(2);
    json.RootElement.GetProperty("meshes")[0]
      .GetProperty("name")
      .GetString()
      .Should()
      .Be("EDBBPP_2_Explosion_Mesh");
    json.RootElement.GetProperty("meshes")[1]
      .GetProperty("name")
      .GetString()
      .Should()
      .Be("EDBBPP_3_Explosion_Mesh");
    json.RootElement.GetProperty("images").GetArrayLength().Should().Be(1);
    json.RootElement.GetProperty("images")[0]
      .GetProperty("mimeType")
      .GetString()
      .Should()
      .Be("image/png");
    json.RootElement.GetProperty("images")[0]
      .GetProperty("bufferView")
      .GetInt32()
      .Should()
      .BeGreaterThan(0);
    result
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.TextureResourceMissing)
      .And.Contain(GltfDiagnosticCodes.TextureDiagnosticPreviewUsed);
  }

  [Fact]
  public async Task UnchangedGlbImportRestoresTheExactDynamicMsh()
  {
    var asset = CreateAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    package.Position = 0;

    var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported
      .Value!.Asset.GetSerializedRepresentation()
      .Should()
      .Equal(asset.GetSerializedRepresentation());
    imported.Value.NextExportOptions.AssetLineageId.Should().Be(_lineageId);
    imported.Value.NextBaseline.AssetLineageId.Should().Be(_lineageId);
    imported.Value.NextBaseline.DocumentId.Should().NotBe(_documentId);
    imported.Value.RestoredSerializedRepresentationPaths.Should().Contain("RootDynamicObject");
  }

  [Fact]
  public async Task UnifiedCreationCreatesMetadataBackedDynamicAssetFromStream()
  {
    var asset = CreateAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    export.Value!.Baseline.AssetLineageId.Should().Be(_lineageId);
    package.Position = 0;

    var created = await interchange.CreateMeshAsync(package);

    created.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(created.Diagnostics));
    var dynamicAsset = created.Value!.Asset.Should().BeOfType<DynamicMeshAsset>().Subject;
    created
      .Value.Asset.GetSerializedRepresentation()
      .Should()
      .Equal(asset.GetSerializedRepresentation());
    created.Value.Preservation.Changes.Should().NotBeEmpty();
  }

  [Fact]
  public async Task DynamicPlacementRootTransformAndAnimationRemainSceneOnlyOnEditImport()
  {
    var asset = CreateAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var edited = RewriteGlbExpanded(
      package.ToArray(),
      (root, binary) =>
      {
        root["nodes"]![0]!["translation"] = new JsonArray(10, 20, 30);
        var input = AppendFloatAccessor(root, binary, [0, 1], "SCALAR", 2, 0, 1);
        var output = AppendFloatAccessor(root, binary, [0, 0, 0, 1, 2, 3], "VEC3", 2);
        root["animations"] = new JsonArray(
          new JsonObject
          {
            ["samplers"] = new JsonArray(
              new JsonObject
              {
                ["input"] = input,
                ["output"] = output,
                ["interpolation"] = "LINEAR",
              }
            ),
            ["channels"] = new JsonArray(
              new JsonObject
              {
                ["sampler"] = 0,
                ["target"] = new JsonObject { ["node"] = 0, ["path"] = "translation" },
              }
            ),
          }
        );
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported
      .Value!.Asset.GetSerializedRepresentation()
      .Should()
      .Equal(asset.GetSerializedRepresentation());
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InertDataIgnored && diagnostic.Path == "nodes[0]"
      );
  }

  [Fact]
  public async Task RemovedPlacementRootImportsAsALegacyDirectRoot()
  {
    var asset = CreateAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var legacy = RewriteGlb(
      package.ToArray(),
      (root, _) =>
      {
        root["nodes"]!.AsArray().RemoveAt(0);
        root["scenes"]![0]!["nodes"]![0] = 0;
        foreach (var node in root["nodes"]!.AsArray())
        {
          if (node?["children"] is not JsonArray children)
          {
            continue;
          }
          for (var index = 0; index < children.Count; index++)
          {
            children[index] = children[index]!.GetValue<int>() - 1;
          }
        }
      }
    );
    await using var legacyStream = new MemoryStream(legacy);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      legacyStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported
      .Value!.Asset.GetSerializedRepresentation()
      .Should()
      .Equal(asset.GetSerializedRepresentation());
    imported
      .Diagnostics.Should()
      .NotContain(diagnostic => diagnostic.Code == GltfDiagnosticCodes.InertDataIgnored);
  }

  [Fact]
  public async Task UnmarkedDynamicPlacementRootIsRejected()
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, _) => root["nodes"]![0]!.AsObject().Remove("extras")
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
  }

  [Theory]
  [InlineData("malformed")]
  [InlineData("duplicate")]
  [InlineData("misplaced")]
  public async Task InvalidDynamicPlacementMarkersAreRejected(string mutation)
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, _) =>
      {
        var rootExtras = root["nodes"]![0]!["extras"]!.AsObject();
        var objectExtras = root["nodes"]![1]!["extras"]!.AsObject();
        if (mutation == "malformed")
        {
          rootExtras[GlbDocument.PlacementRootMarker] = false;
        }
        else
        {
          objectExtras[GlbDocument.PlacementRootMarker] = true;
          if (mutation == "misplaced")
          {
            rootExtras.Remove(GlbDocument.PlacementRootMarker);
          }
        }
      }
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
  }

  [Fact]
  public async Task GroupOnlyExportHasNoSyntheticEffectGeometry()
  {
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([DynamicEffectRecipes.Group()]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset!,
      destination,
      new GltfExportOptions(_lineageId, _documentId)
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    using var json = ReadGlbJson(destination.ToArray());
    json.RootElement.GetProperty("nodes").GetArrayLength().Should().Be(3);
    json.RootElement.TryGetProperty("meshes", out _).Should().BeFalse();
    json.RootElement.TryGetProperty("materials", out _).Should().BeFalse();
  }

  [Theory]
  [InlineData(3, 15)]
  [InlineData(4096, 2)]
  public async Task DynamicExportLimitsIncludeThePlacementRoot(int maxNodes, int maxHierarchyDepth)
  {
    await using var destination = new MemoryStream();
    var profile = new GltfOperationProfile(
      32 * 1024 * 1024,
      32 * 1024 * 1024,
      4 * 1024 * 1024,
      32,
      65536,
      maxNodes,
      maxHierarchyDepth
    );

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateAsset(),
      destination,
      profile: profile
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Should()
      .ContainSingle(diagnostic => diagnostic.Code == GltfDiagnosticCodes.ResourceLimitExceeded);
  }

  [Fact]
  public async Task DuplicateDynamicScopeFailsWithoutAnAsset()
  {
    var asset = CreateAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, _) =>
      {
        var metadataText = root["nodes"]![3]!["extras"]!["earthtool"]!.GetValue<string>();
        var metadata = JsonNode.Parse(metadataText)!;
        metadata["scope"]!["localId"] = 2;
        root["nodes"]![3]!["extras"]!["earthtool"] = metadata.ToJsonString();
      }
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.DuplicateScopeIdentity);
  }

  [Fact]
  public async Task SharedExplosionPreviewOwnershipFailsTransactionally()
  {
    var asset = CreateAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, _) => root["nodes"]![3]!["mesh"] = root["nodes"]![2]!["mesh"]!.GetValue<int>()
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
  }

  [Fact]
  public async Task SharedExplosionPositionAccessorFailsTransactionally()
  {
    var asset = CreateAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, _) =>
        root["meshes"]![1]!["primitives"]![0]!["attributes"]!["POSITION"] = root["meshes"]![0]![
          "primitives"
        ]![0]!["attributes"]!["POSITION"]!.GetValue<int>()
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
  }

  [Fact]
  public async Task MissingDynamicGuardUsesItsStableDiagnostic()
  {
    var asset = CreateAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, _) =>
      {
        var metadataText = root["nodes"]![2]!["extras"]!["earthtool"]!.GetValue<string>();
        var metadata = JsonNode.Parse(metadataText)!;
        metadata["guards"]!.AsObject().Remove("orderedChildren");
        root["nodes"]![2]!["extras"]!["earthtool"] = metadata.ToJsonString();
      }
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.MissingRequiredGuard);
  }

  [Fact]
  public async Task UnknownLightAndNoncanonicalAdditiveValuesRoundTripExactly()
  {
    var canonical = CreateAsset();
    var bytes = canonical.GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x36C), 99);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x3B8), 7);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var asset).Should().BeTrue();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset!,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    package.Position = 0;

    var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported.Value!.Asset.GetSerializedRepresentation().Should().Equal(bytes);
    var extension = imported.Value.Asset.RootDynamicObject.Children[0].Extension;
    extension.LightType.Should().Be(99);
    extension.KnownLightType.Should().BeNull();
    extension.AdditiveFlag.Should().Be(7);
  }

  [Fact]
  public async Task SpriteEffectsExportThroughThePublicGlbSeam()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateSpriteEffectsAsset(),
      destination,
      new GltfExportOptions(_lineageId, _documentId)
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result
      .Diagnostics.Should()
      .NotContain(item => item.Code == GltfDiagnosticCodes.UnsupportedDomain);
    result
      .Diagnostics.Count(item => item.Code == GltfDiagnosticCodes.TextureResourceMissing)
      .Should()
      .Be(4);
    using var json = ReadGlbJson(destination.ToArray());
    json.RootElement.GetProperty("nodes").GetArrayLength().Should().Be(6);
    json.RootElement.GetProperty("meshes").GetArrayLength().Should().Be(4);
  }

  [Fact]
  public async Task RibbonEffectsExportThroughThePublicGlbSeam()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      destination,
      new GltfExportOptions(_lineageId, _documentId)
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result
      .Diagnostics.Should()
      .NotContain(item => item.Code == GltfDiagnosticCodes.UnsupportedDomain);
    result
      .Diagnostics.Count(item => item.Code == GltfDiagnosticCodes.TextureResourceMissing)
      .Should()
      .Be(4);
    using var json = ReadGlbJson(destination.ToArray());
    json.RootElement.GetProperty("nodes").GetArrayLength().Should().Be(6);
    json.RootElement.GetProperty("meshes").GetArrayLength().Should().Be(4);
  }

  [Fact]
  public async Task AttachedAndProceduralEffectsExportWithExplicitPreviewContexts()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateAttachedAndProceduralEffectsAsset(),
      destination,
      new GltfExportOptions(_lineageId, _documentId)
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result
      .Diagnostics.Should()
      .NotContain(item => item.Code == GltfDiagnosticCodes.UnsupportedDomain);
    result
      .Diagnostics.Count(item => item.Code == GltfDiagnosticCodes.TextureResourceMissing)
      .Should()
      .Be(4);
    using var json = ReadGlbJson(destination.ToArray());
    var root = json.RootElement;
    root.GetProperty("nodes").GetArrayLength().Should().Be(6);
    root.GetProperty("meshes").GetArrayLength().Should().Be(4);
    root.GetProperty("accessors")[8].GetProperty("count").GetInt32().Should().BeGreaterThan(4);
    ReadDynamicObjectMetadata(root.GetProperty("nodes")[2])
      .GetProperty("payload")
      .GetProperty("previewContext")
      .GetString()
      .Should()
      .Be("attachedParticle");
    var spherePayload = ReadDynamicObjectMetadata(root.GetProperty("nodes")[4])
      .GetProperty("payload");
    spherePayload.GetProperty("previewContext").GetString().Should().Be("primary");
    spherePayload.GetProperty("previewFrameDomain").GetString().Should().Be("builtIn16");
    spherePayload.GetProperty("previewSourceFrame").GetInt32().Should().Be(0);
    root.GetProperty("nodes")[3]
      .GetProperty("translation")
      .EnumerateArray()
      .Select(item => item.GetSingle())
      .Should()
      .Equal(2, 4, -3);
  }

  [Fact]
  public async Task RibbonPreviewRetainsHalfWidthSignTextureSideAndWinding()
  {
    await using var destination = new MemoryStream();
    var result = await new GltfInterchange().ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      destination,
      new GltfExportOptions(_lineageId, _documentId)
    );
    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var glb = destination.ToArray();
    using var json = ReadGlbJson(glb);
    var binary = ReadGlbBinary(glb);
    var expected = new[]
    {
      (RibbonHalfWidth: 0.5f, Vertices: 4),
      (RibbonHalfWidth: -0.25f, Vertices: 42),
      (RibbonHalfWidth: 1f, Vertices: 4),
      (RibbonHalfWidth: -0.75f, Vertices: 62),
    };

    for (var meshIndex = 0; meshIndex < expected.Length; meshIndex++)
    {
      var primitive = json.RootElement.GetProperty("meshes")[meshIndex].GetProperty("primitives")[
        0
      ];
      var positions = ReadVector3Accessor(
        json.RootElement,
        binary,
        primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32()
      );
      var textureCoordinates = ReadVector2Accessor(
        json.RootElement,
        binary,
        primitive.GetProperty("attributes").GetProperty("TEXCOORD_0").GetInt32()
      );
      var indices = ReadUInt16Accessor(
        json.RootElement,
        binary,
        primitive.GetProperty("indices").GetInt32()
      );
      positions.Should().HaveCount(expected[meshIndex].Vertices);
      Vector3
        .Distance(positions[0], positions[1])
        .Should()
        .BeApproximately(Math.Abs(expected[meshIndex].RibbonHalfWidth) * 2, 0.0001f);
      textureCoordinates[0].X.Should().BeLessThan(textureCoordinates[1].X);
      indices.Take(6).Should().Equal(0, 2, 1, 1, 2, 3);
      var winding = Vector3
        .Cross(
          positions[indices[1]] - positions[indices[0]],
          positions[indices[2]] - positions[indices[0]]
        )
        .Z;
      Math.Sign(winding).Should().Be(-Math.Sign(expected[meshIndex].RibbonHalfWidth));
    }
  }

  [Fact]
  public async Task UnchangedRibbonGlbImportRestoresExactDynamicMsh()
  {
    var asset = CreateRibbonEffectsAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    package.Position = 0;

    var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported
      .Value!.Asset.GetSerializedRepresentation()
      .Should()
      .Equal(asset.GetSerializedRepresentation());
  }

  [Fact]
  public async Task UnchangedAttachedAndProceduralGlbImportRestoresExactDynamicMsh()
  {
    var asset = CreateAttachedAndProceduralEffectsAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    package.Position = 0;

    var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported
      .Value!.Asset.GetSerializedRepresentation()
      .Should()
      .Equal(asset.GetSerializedRepresentation());
  }

  [Fact]
  public async Task AttachedAndSphereEditsRegenerateOnlyOwnedRepresentations()
  {
    var asset = CreateAttachedAndProceduralEffectsAsset();
    var originalShockwave = asset.RootDynamicObject.Children[0].Extension;
    var originalLine = asset.RootDynamicObject.Children[0].Children[0].Extension;
    var originalSphere = asset.RootDynamicObject.Children[1].Extension;
    var originalKeelwater = asset.RootDynamicObject.Children[1].Children[0].Extension;
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    var edited = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        root["materials"]![0]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          0.15f,
          0.2f,
          0.25f,
          0.7f
        );
        root["materials"]![2]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          0.1f,
          0.2f,
          0.3f,
          1
        );
        root["materials"]![1]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          0.05f,
          0.1f,
          0.15f,
          0.6f
        );
        root["materials"]![3]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          1,
          0,
          0,
          0.65f
        );
        var quadAccessor = root["accessors"]![0]!;
        var quadView = root["bufferViews"]![quadAccessor["bufferView"]!.GetValue<int>()]!;
        var quadOffset = quadView["byteOffset"]!.GetValue<int>();
        var positions = new[]
        {
          new Vector3(-2, -5, 1),
          new Vector3(4, -5, 1),
          new Vector3(4, 3, 1),
          new Vector3(-2, 3, 1),
        };
        for (var index = 0; index < positions.Length; index++)
        {
          WriteVector3(binary, quadOffset + index * 12, positions[index]);
        }
        quadAccessor["min"] = new JsonArray(-2, -5, 1);
        quadAccessor["max"] = new JsonArray(4, 3, 1);
        RewriteQuad(root, binary, 4, new EffectRectangle(-3, 4, 5, -6), 1.5f);
        RewriteQuad(root, binary, 12, new EffectRectangle(-4, 5, 6, -7), 2);
        var sphereAccessor = root["accessors"]![8]!;
        var sphereView = root["bufferViews"]![sphereAccessor["bufferView"]!.GetValue<int>()]!;
        var sphereOffset = sphereView["byteOffset"]!.GetValue<int>();
        WriteVector3(binary, sphereOffset, new Vector3(0, 1.25f, 0));
        sphereAccessor["max"] = new JsonArray(1, 1.25f, 1);
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    var shockwave = imported.Value!.Asset.RootDynamicObject.Children[0].Extension;
    shockwave.StartEffectRectangle.Should().Be(new EffectRectangle(-2, 3, 4, -5));
    shockwave.EffectDepthOffset.Should().Be(1);
    shockwave.VisibleEffectColor.Should().Be(new Vector3(0.3f, 0.4f, 0.5f));
    shockwave.StartAlpha.Should().BeApproximately(0.7f, 0.0001f);
    shockwave.FrameCount.Should().Be(originalShockwave.FrameCount);
    shockwave.ChildEndTranslation.Should().Be(originalShockwave.ChildEndTranslation);
    var line = imported.Value.Asset.RootDynamicObject.Children[0].Children[0].Extension;
    line.StartEffectRectangle.Should().Be(new EffectRectangle(-3, 4, 5, -6));
    line.EffectDepthOffset.Should().Be(1.5f);
    line.VisibleEffectColor.Should().Be(new Vector3(0.1f, 0.2f, 0.3f));
    line.StartAlpha.Should().BeApproximately(0.6f, 0.0001f);
    line.EndAlpha.Should().Be(originalLine.EndAlpha);
    var sphere = imported.Value.Asset.RootDynamicObject.Children[1].Extension;
    sphere.VisibleEffectColor.Should().Be(new Vector3(0.1f, 0.2f, 0.3f));
    sphere.FirstSourceFrame.Should().Be(originalSphere.FirstSourceFrame);
    sphere.FrameCount.Should().Be(originalSphere.FrameCount);
    sphere.StartEffectRectangle.Should().Be(originalSphere.StartEffectRectangle);
    var keelwater = imported.Value.Asset.RootDynamicObject.Children[1].Children[0].Extension;
    keelwater.StartEffectRectangle.Should().Be(new EffectRectangle(-4, 5, 6, -7));
    keelwater.EffectDepthOffset.Should().Be(2);
    keelwater.VisibleEffectColor.Should().Be(originalKeelwater.VisibleEffectColor);
    keelwater.StartAlpha.Should().BeApproximately(0.65f, 0.0001f);
    keelwater.EndAlpha.Should().Be(originalKeelwater.EndAlpha);
    imported
      .Value.Preservation.Changes.Should()
      .Contain(change =>
        change.FieldPath == "DynamicObjectScopes[4].PreviewShape"
        && change.Disposition == PreservationDisposition.Retained
        && change.Reason == "dynamic-runtime-preview-input"
      );
  }

  [Fact]
  public async Task SphereNormalEditRemainsReportedPreviewState()
  {
    var asset = CreateAttachedAndProceduralEffectsAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var edited = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        var accessor = root["accessors"]![9]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        WriteVector3(binary, view["byteOffset"]!.GetValue<int>(), Vector3.UnitX);
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported
      .Value!.Asset.GetSerializedRepresentation()
      .Should()
      .Equal(asset.GetSerializedRepresentation());
    imported
      .Value.Preservation.Changes.Should()
      .ContainSingle(change =>
        change.FieldPath == "DynamicObjectScopes[4].PreviewShape"
        && change.Disposition == PreservationDisposition.Retained
        && change.Reason == "dynamic-runtime-preview-input"
      );
  }

  [Fact]
  public async Task RibbonEffectsPreserveNoncanonicalAndInactiveStateExactly()
  {
    var canonical = CreateRibbonEffectsAsset();
    var bytes = canonical.GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    WriteSingle(bytes, firstChildOffset + 0x384, 0.125f);
    WriteSingle(bytes, firstChildOffset + 0x388, 0.2f);
    WriteSingle(bytes, firstChildOffset + 0x38C, 17);
    WriteSingle(bytes, firstChildOffset + 0x3AC, -9);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x3B4), 0xAABBCCDD);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x3B8), 7);
    WriteSingle(bytes, firstChildOffset + 0x3D4, -3);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x3D8), 9);
    WriteSingle(bytes, firstChildOffset + 0x3E4, 23);
    WriteSingle(bytes, firstChildOffset + 0x3E8, -29);
    WriteSingle(bytes, firstChildOffset + 0x3F8, 31);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var asset).Should().BeTrue();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset!,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    package.Position = 0;

    var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported.Value!.Asset.GetSerializedRepresentation().Should().Equal(bytes);
  }

  [Fact]
  public async Task RibbonHalfWidthAndMaterialEditsRegenerateOnlyOwnedRepresentations()
  {
    var asset = CreateRibbonEffectsAsset();
    var original = asset.RootDynamicObject.Children[0].Extension;
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var edited = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        root["materials"]![0]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          0.9f,
          0.8f,
          0.7f,
          0.6f
        );
        var accessor = root["accessors"]![0]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        var offset = view["byteOffset"]!.GetValue<int>();
        WriteVector3(binary, offset, new Vector3(0, -1, 0));
        WriteVector3(binary, offset + 12, new Vector3(0, 1, 0));
        WriteVector3(binary, offset + 24, new Vector3(8, -1, 0));
        WriteVector3(binary, offset + 36, new Vector3(8, 1, 0));
        accessor["min"] = new JsonArray(0, -1, 0);
        accessor["max"] = new JsonArray(8, 1, 0);
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var created = await interchange.CreateMeshAsync(editedStream);

    created.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(created.Diagnostics));
    var createdAsset = created.Value!.Asset.Should().BeOfType<DynamicMeshAsset>().Subject;
    export.Value!.Baseline.AssetLineageId.Should().Be(_lineageId);
    var extension = createdAsset.RootDynamicObject.Children[0].Extension;
    extension.RibbonHalfWidth.Should().BeApproximately(-1, 0.0001f);
    extension.VisibleEffectColor.Should().Be(new Vector3(0.9f, 0.8f, 0.7f));
    extension.StartAlpha.Should().BeApproximately(0.6f, 0.0001f);
    extension.FrameCount.Should().Be(original.FrameCount);
    extension.ReciprocalColumnCount.Should().Be(original.ReciprocalColumnCount);
    extension.AdditiveFlag.Should().Be(original.AdditiveFlag);
    extension.LightType.Should().Be(original.LightType);
    extension.TerrainLightColor.Should().Be(original.TerrainLightColor);
    extension.ReservedWord.Should().Be(original.ReservedWord);
    extension.TexturePathBytes.Should().Equal(original.TexturePathBytes);
  }

  [Fact]
  public async Task RibbonHalfWidthEditsCoverEveryEffect()
  {
    var expectedRibbonHalfWidths = new[] { 1.25f, -1.5f, 1.75f, -2f };
    for (var meshIndex = 0; meshIndex < expectedRibbonHalfWidths.Length; meshIndex++)
    {
      var asset = CreateRibbonEffectsAsset();
      var originals = GetRibbonExtensions(asset);
      await using var package = new MemoryStream();
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGlbAsync(
        asset,
        package,
        new GltfExportOptions(_lineageId, _documentId)
      );
      var edited = RewriteGlb(
        package.ToArray(),
        (root, binary) =>
        {
          var accessor = root["accessors"]![meshIndex * 4]!;
          var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
          var offset = view["byteOffset"]!.GetValue<int>();
          var positions = new List<Vector3>();
          for (var vertex = 0; vertex < accessor["count"]!.GetValue<int>(); vertex += 2)
          {
            var left = ReadVector3(binary, offset + vertex * 12);
            var right = ReadVector3(binary, offset + (vertex + 1) * 12);
            var center = (left + right) * 0.5f;
            var side =
              Vector3.Normalize(left - right) * Math.Abs(expectedRibbonHalfWidths[meshIndex]);
            positions.Add(center + side);
            positions.Add(center - side);
          }
          for (var vertex = 0; vertex < positions.Count; vertex++)
          {
            WriteVector3(binary, offset + vertex * 12, positions[vertex]);
          }
          accessor["min"] = new JsonArray(
            positions.Min(item => item.X),
            positions.Min(item => item.Y),
            positions.Min(item => item.Z)
          );
          accessor["max"] = new JsonArray(
            positions.Max(item => item.X),
            positions.Max(item => item.Y),
            positions.Max(item => item.Z)
          );
        }
      );
      await using var editedStream = new MemoryStream(edited);

      var imported = await interchange.ImportEditDynamicGlbAsync(
        editedStream,
        export.Value!.Baseline
      );

      imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
      var actual = GetRibbonExtensions(imported.Value!.Asset);
      actual[meshIndex]
        .RibbonHalfWidth.Should()
        .BeApproximately(expectedRibbonHalfWidths[meshIndex], 0.0001f);
      actual[meshIndex]
        .SerializedRepresentation.Take(0x48)
        .Should()
        .Equal(originals[meshIndex].SerializedRepresentation.Take(0x48));
      actual[meshIndex]
        .SerializedRepresentation.Skip(0x4C)
        .Should()
        .Equal(originals[meshIndex].SerializedRepresentation.Skip(0x4C));
      for (var other = 0; other < actual.Count; other++)
      {
        if (other != meshIndex)
        {
          actual[other]
            .SerializedRepresentation.Should()
            .Equal(originals[other].SerializedRepresentation);
        }
      }
    }
  }

  [Fact]
  public async Task RibbonPathAndOrientationEditsRemainPreviewOnly()
  {
    var asset = CreateRibbonEffectsAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var edited = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        var accessor = root["accessors"]![0]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        var offset = view["byteOffset"]!.GetValue<int>();
        WriteVector3(binary, offset, new Vector3(-0.5f, 0, 0));
        WriteVector3(binary, offset + 12, new Vector3(0.5f, 0, 0));
        WriteVector3(binary, offset + 24, new Vector3(-0.5f, 8, 0));
        WriteVector3(binary, offset + 36, new Vector3(0.5f, 8, 0));
        accessor["min"] = new JsonArray(-0.5f, 0, 0);
        accessor["max"] = new JsonArray(0.5f, 8, 0);
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported
      .Value!.Asset.GetSerializedRepresentation()
      .Should()
      .Equal(asset.GetSerializedRepresentation());
    imported
      .Value.Preservation.Changes.Should()
      .Contain(change =>
        change.FieldPath == "DynamicObjectScopes[2].PreviewPath"
        && change.Disposition == PreservationDisposition.Retained
        && change.Reason == "dynamic-runtime-preview-input"
      );
  }

  [Fact]
  public async Task DegenerateRibbonEditFailsWithScopedDiagnostic()
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var edited = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        var accessor = root["accessors"]![0]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        var offset = view["byteOffset"]!.GetValue<int>();
        WriteVector3(binary, offset + 24, new Vector3(0, 0.5f, 0));
        WriteVector3(binary, offset + 36, new Vector3(0, -0.5f, 0));
        accessor["min"] = new JsonArray(0, -0.5f, 0);
        accessor["max"] = new JsonArray(0, 0.5f, 0);
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "DynamicObjectScopes[2].RibbonPreview"
      );
  }

  [Fact]
  public async Task SharedRibbonTextureCoordinateAccessorFailsTransactionally()
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, _) =>
        root["meshes"]![2]!["primitives"]![0]!["attributes"]!["TEXCOORD_0"] = root["meshes"]![0]![
          "primitives"
        ]![0]!["attributes"]!["TEXCOORD_0"]!.GetValue<int>()
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
  }

  [Fact]
  public async Task EditedRibbonWindingFailsWithScopedDiagnostic()
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        var accessor = root["accessors"]![3]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        var offset = view["byteOffset"]!.GetValue<int>();
        BinaryPrimitives.WriteUInt16LittleEndian(binary.AsSpan(offset), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(binary.AsSpan(offset + 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(binary.AsSpan(offset + 4), 2);
      }
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "DynamicObjectScopes[2].RibbonPreview"
      );
  }

  [Fact]
  public async Task EditedRibbonNormalCannotChangeSerializedHalfWidthSign()
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        var accessor = root["accessors"]![1]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        var offset = view["byteOffset"]!.GetValue<int>();
        for (var index = 0; index < accessor["count"]!.GetValue<int>(); index++)
        {
          WriteVector3(binary, offset + index * 12, -Vector3.UnitZ);
        }
        accessor["min"] = new JsonArray(0, 0, -1);
        accessor["max"] = new JsonArray(0, 0, -1);
      }
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "DynamicObjectScopes[2].RibbonPreview"
      );
  }

  [Fact]
  public async Task JaggedRibbonCannotMixSegmentWinding()
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        var accessor = root["accessors"]![4]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        var offset = view["byteOffset"]!.GetValue<int>();
        WriteVector3(binary, offset + 48, new Vector3(-1, -0.25f, 0));
        WriteVector3(binary, offset + 60, new Vector3(-1, 0.25f, 0));
        accessor["min"] = new JsonArray(-1, -0.57f, 0);
      }
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "DynamicObjectScopes[3].RibbonPreview"
      );
  }

  [Theory]
  [InlineData(0)]
  [InlineData(float.NaN)]
  [InlineData(float.PositiveInfinity)]
  public async Task InvalidSerializedRibbonHalfWidthsFailWithoutPartialOutput(float ribbonHalfWidth)
  {
    var source = CreateRibbonEffectsAsset();
    var bytes = source.GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    WriteSingle(bytes, firstChildOffset + 0x3B0, ribbonHalfWidth);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var asset).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(asset!, destination);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "DynamicObjectScopes[2].Extension.RibbonHalfWidth"
      );
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task RibbonPreviewVertexLimitFailsWithoutPartialOutput()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      destination,
      profile: new GltfOperationProfile(maxActiveRenderVertices: 32)
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task SeparateGltfRoundTripsExactRibbonEffects()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-ribbon-gltf-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
      var path = Path.Combine(directory, "ribbons.gltf");
      var asset = CreateRibbonEffectsAsset();
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(_lineageId, _documentId, null, null, null, "EDBBPP")
      );
      export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
      using (var json = JsonDocument.Parse(await File.ReadAllBytesAsync(path)))
      {
        var sceneRootIndex = json
          .RootElement.GetProperty("scenes")[0]
          .GetProperty("nodes")[0]
          .GetInt32();
        json.RootElement.GetProperty("nodes")[sceneRootIndex]
          .GetProperty("name")
          .GetString()
          .Should()
          .Be("EDBBPP");
        json.RootElement.GetProperty("nodes")[sceneRootIndex]
          .GetProperty("extras")
          .GetProperty("earthtoolPlacementRoot")
          .GetBoolean()
          .Should()
          .BeTrue();
      }

      var created = await interchange.CreateMeshFileAsync(path);

      created.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(created.Diagnostics));
      var createdAsset = created.Value!.Asset.Should().BeOfType<DynamicMeshAsset>().Subject;
      export.Value!.Baseline.AssetLineageId.Should().Be(_lineageId);
      createdAsset
        .GetSerializedRepresentation()
        .Should()
        .Equal(asset.GetSerializedRepresentation());

      var gltfPath = Path.Combine(directory, "scalable.gltf");
      var separateExport = await interchange.ExportGltfFileAsync(
        asset,
        gltfPath,
        new GltfExportOptions(
          _lineageId,
          _documentId,
          textureSearchRoots: null,
          preservedUnknownMetadata: null,
          meshResourceSearchRoots: [directory]
        )
      );
      separateExport
        .Status.Should()
        .Be(OperationStatus.Succeeded, Diagnostics(separateExport.Diagnostics));

      var separateImport = await interchange.ImportEditDynamicGltfFileAsync(
        gltfPath,
        separateExport.Value!.Baseline
      );

      separateImport
        .Status.Should()
        .Be(OperationStatus.Succeeded, Diagnostics(separateImport.Diagnostics));
      separateImport
        .Value!.Asset.GetSerializedRepresentation()
        .Should()
        .Equal(asset.GetSerializedRepresentation());
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateGltfRoundTripsExactAttachedAndProceduralEffects()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-attached-gltf-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
      var path = Path.Combine(directory, "attached.gltf");
      var asset = CreateAttachedAndProceduralEffectsAsset();
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(_lineageId, _documentId)
      );
      export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));

      var imported = await interchange.ImportEditDynamicGltfFileAsync(path, export.Value!.Baseline);

      imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
      imported
        .Value!.Asset.GetSerializedRepresentation()
        .Should()
        .Equal(asset.GetSerializedRepresentation());
      imported.Value.NextExportOptions.DynamicObjectIds.Should().Equal(1, 2, 3, 4, 5);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task InvalidSpritePreviewDomainsFailWithoutPartialOutput()
  {
    var frames = new CanonicalDynamicFrameSequence(0, 1, 1);
    var sprite = new CanonicalDynamicSpriteSheet(frames, 1, 1);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 1, 1, -1),
      new EffectRectangle(-2, 2, 2, -2),
      0.25f
    );
    var alpha = new CanonicalDynamicAlpha(1, 0, DynamicAlphaTiming.FramePhase);
    var light = new CanonicalDynamicTerrainLight(DynamicLightType.Constant, Vector3.One);
    var cases = new (DynamicMeshAsset Asset, int Offset, int Value)[]
    {
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.Track(
            frames,
            shape.StartEffectRectangle,
            shape.EndEffectRectangle,
            "Textures\\fx\\track.tex",
            alpha,
            false
          )
        ),
        0x370,
        -1
      ),
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.MappedExplosion(
            frames,
            shape.StartEffectRectangle,
            shape.EndEffectRectangle,
            "Textures\\fx\\mapped.tex",
            Vector3.One,
            alpha,
            false,
            light
          )
        ),
        0x370,
        int.MaxValue
      ),
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.FlatExplosion(
            sprite,
            shape,
            "Textures\\fx\\flat.tex",
            Vector3.One,
            alpha,
            false,
            light
          )
        ),
        0x378,
        0
      ),
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.FlatExplosion(
            sprite,
            shape,
            "Textures\\fx\\flat.tex",
            Vector3.One,
            alpha,
            false,
            light
          )
        ),
        0x37C,
        0
      ),
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.Smoke(
            sprite,
            shape,
            "Textures\\fx\\smoke.tex",
            Vector3.One,
            1,
            alpha,
            false
          )
        ),
        0x38C,
        BitConverter.SingleToInt32Bits(float.NaN)
      ),
    };

    foreach (var item in cases)
    {
      var bytes = item.Asset.GetSerializedRepresentation();
      const int firstChildOffset = 0x18 + 0x410;
      BinaryPrimitives.WriteInt32LittleEndian(
        bytes.AsSpan(firstChildOffset + item.Offset),
        item.Value
      );
      var expert = MshExpert.CreateDynamic(bytes);
      expert.TryGetValue(out var malformed).Should().BeTrue();
      await using var destination = new MemoryStream();

      var result = await new GltfInterchange().ExportGlbAsync(malformed!, destination);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Value.Should().BeNull();
      result
        .Diagnostics.Should()
        .ContainSingle(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
          && diagnostic.Path.StartsWith(
            "DynamicObjectScopes[2].Extension",
            StringComparison.Ordinal
          )
        );
      destination.Length.Should().Be(0);
    }
  }

  [Theory]
  [InlineData(1, "previewTotalLifetimeTicks", 0)]
  [InlineData(2, "previewTotalLifetimeTicks", 0)]
  [InlineData(3, "previewTotalLifetimeTicks", 0)]
  [InlineData(4, "previewTotalLifetimeTicks", 0)]
  [InlineData(1, "previewParentPhase", 1)]
  [InlineData(2, "previewParentPhase", 1)]
  [InlineData(3, "previewParentPhase", 1)]
  [InlineData(4, "previewParentPhase", 1)]
  public async Task InvalidPreviewInputsFailTransactionally(int nodeIndex, string field, int value)
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateAttachedAndProceduralEffectsAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, _) =>
      {
        var nativeNodeIndex = nodeIndex + 1;
        var metadata = JsonNode.Parse(
          root["nodes"]![nativeNodeIndex]!["extras"]!["earthtool"]!.GetValue<string>()
        )!;
        metadata["payload"]![field] = value;
        root["nodes"]![nativeNodeIndex]!["extras"]!["earthtool"] = metadata.ToJsonString();
      }
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.StaleNativeProjection
        && diagnostic.Path == $"nodes[{nodeIndex + 1}].extras.earthtool"
      );
  }

  [Fact]
  public async Task MissingAttachedPreviewGuardUsesItsStableDiagnostic()
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateAttachedAndProceduralEffectsAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, _) =>
      {
        var metadata = JsonNode.Parse(
          root["nodes"]![2]!["extras"]!["earthtool"]!.GetValue<string>()
        )!;
        metadata["guards"]!.AsObject().Remove("effectPreview");
        root["nodes"]![2]!["extras"]!["earthtool"] = metadata.ToJsonString();
      }
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.MissingRequiredGuard
        && diagnostic.Path == "nodes[2].extras.earthtool"
      );
  }

  [Fact]
  public async Task InvalidAttachedFrameAndFiniteDomainsFailWithoutPartialOutput()
  {
    var cases = new (int Offset, int Value)[]
    {
      (0x374, 0),
      (0x3D4, BitConverter.SingleToInt32Bits(float.NaN)),
    };
    foreach (var item in cases)
    {
      var source = CreateAttachedAndProceduralEffectsAsset();
      var bytes = source.GetSerializedRepresentation();
      const int firstChildOffset = 0x18 + 0x410;
      BinaryPrimitives.WriteInt32LittleEndian(
        bytes.AsSpan(firstChildOffset + item.Offset),
        item.Value
      );
      var expert = MshExpert.CreateDynamic(bytes);
      expert.TryGetValue(out var malformed).Should().BeTrue();
      await using var destination = new MemoryStream();

      var result = await new GltfInterchange().ExportGlbAsync(malformed!, destination);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Value.Should().BeNull();
      result
        .Diagnostics.Should()
        .ContainSingle(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
          && diagnostic.Path.StartsWith(
            "DynamicObjectScopes[2].Extension",
            StringComparison.Ordinal
          )
        );
      destination.Length.Should().Be(0);
    }
  }

  [Theory]
  [InlineData(DynamicEffectType.Shockwave)]
  [InlineData(DynamicEffectType.Line)]
  [InlineData(DynamicEffectType.Keelwater)]
  public async Task EveryAttachedEffectRejectsInvalidFrameDomains(DynamicEffectType effectType)
  {
    var source = CreateSingleAttachedEffectAsset(effectType);
    var bytes = source.GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x374), 0);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var malformed).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(malformed!, destination);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "DynamicObjectScopes[2].Extension.Frames"
      );
    destination.Length.Should().Be(0);
  }

  [Theory]
  [InlineData(DynamicEffectType.Shockwave, 0x3D4)]
  [InlineData(DynamicEffectType.Line, 0x3D4)]
  [InlineData(DynamicEffectType.Sphere, 0x3C8)]
  [InlineData(DynamicEffectType.Keelwater, 0x3E0)]
  public async Task EveryNewEffectRejectsNonFiniteActivePreviewValues(
    DynamicEffectType effectType,
    int fieldOffset
  )
  {
    var source =
      effectType == DynamicEffectType.Sphere
        ? CreateSingleEffectAsset(
          DynamicEffectRecipes.Sphere(
            "Textures\\fx\\sphere.tex",
            new Vector3(0.4f, 0.5f, 0.6f),
            true
          )
        )
        : CreateSingleAttachedEffectAsset(effectType);
    var bytes = source.GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    WriteSingle(bytes, firstChildOffset + fieldOffset, float.NaN);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var malformed).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(malformed!, destination);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path.StartsWith("DynamicObjectScopes[2].Extension", StringComparison.Ordinal)
      );
    destination.Length.Should().Be(0);
  }

  [Theory]
  [InlineData(DynamicEffectType.Shockwave)]
  [InlineData(DynamicEffectType.Line)]
  [InlineData(DynamicEffectType.Sphere)]
  [InlineData(DynamicEffectType.Keelwater)]
  public async Task EveryNewEffectHonorsTransactionalOutputLimits(DynamicEffectType effectType)
  {
    var asset =
      effectType == DynamicEffectType.Sphere
        ? CreateSingleEffectAsset(
          DynamicEffectRecipes.Sphere(
            "Textures\\fx\\sphere.tex",
            new Vector3(0.4f, 0.5f, 0.6f),
            true
          )
        )
        : CreateSingleAttachedEffectAsset(effectType);
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset,
      destination,
      profile: new GltfOperationProfile(maxOutputBytes: 256)
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task AttachedAlphaEditsIgnoreTheInactiveSerializedTimingMode()
  {
    var source = CreateSingleAttachedEffectAsset(DynamicEffectType.Line);
    var bytes = source.GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x380), 0);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x3D8), 7);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var asset).Should().BeTrue();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(asset!, package);
    var edited = RewriteGlb(
      package.ToArray(),
      (root, _) => root["materials"]![0]!["pbrMetallicRoughness"]!["baseColorFactor"]![3] = 0.7f
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported.Value!.Asset.RootDynamicObject.Children[0].Extension.AlphaTimingMode.Should().Be(7);
    await using var reexported = new MemoryStream();
    var reexport = await interchange.ExportGlbAsync(
      imported.Value.Asset,
      reexported,
      imported.Value.NextExportOptions
    );
    reexport.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(reexport.Diagnostics));
    using var json = ReadGlbJson(reexported.ToArray());
    json.RootElement.GetProperty("materials")[0]
      .GetProperty("pbrMetallicRoughness")
      .GetProperty("baseColorFactor")[3]
      .GetSingle()
      .Should()
      .BeApproximately(0.7f, 0.0001f);
  }

  [Fact]
  public async Task AttachedAndProceduralEffectsPreserveNoncanonicalStateExactly()
  {
    foreach (
      var effectType in new[]
      {
        DynamicEffectType.Shockwave,
        DynamicEffectType.Line,
        DynamicEffectType.Sphere,
        DynamicEffectType.Keelwater,
      }
    )
    {
      var source =
        effectType == DynamicEffectType.Sphere
          ? CreateSingleEffectAsset(
            DynamicEffectRecipes.Sphere(
              "Textures\\fx\\sphere.tex",
              new Vector3(0.4f, 0.5f, 0.6f),
              true
            )
          )
          : CreateSingleAttachedEffectAsset(effectType);
      var bytes = source.GetSerializedRepresentation();
      const int firstChildOffset = 0x18 + 0x410;
      BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x36C), 99);
      BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x3B4), 0xAABBCCDD);
      BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x3B8), 7);
      WriteSingle(bytes, firstChildOffset + 0x3F8, 17);
      if (effectType == DynamicEffectType.Sphere)
      {
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x370), 23);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x374), -7);
      }
      else
      {
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x3D8), 9);
      }
      if (effectType == DynamicEffectType.Keelwater)
      {
        WriteSingle(bytes, firstChildOffset + 0x3C8, -3);
      }
      var expert = MshExpert.CreateDynamic(bytes);
      expert.TryGetValue(out var asset).Should().BeTrue();
      await using var package = new MemoryStream();
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGlbAsync(asset!, package);
      export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
      package.Position = 0;

      var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

      imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
      imported.Value!.Asset.GetSerializedRepresentation().Should().Equal(bytes);
    }
  }

  [Fact]
  public async Task AttachedAndProceduralEffectsBindRealTexPreviews()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-attached-tex-{Guid.NewGuid():N}");
    var textureDirectory = Path.Combine(directory, "Textures", "fx");
    Directory.CreateDirectory(textureDirectory);
    try
    {
      var tex = CreateRgbaTex([0x11, 0x22, 0x33, 0xFF]);
      foreach (var name in new[] { "shockwave.tex", "line.tex", "sphere.tex", "keelwater.tex" })
      {
        await File.WriteAllBytesAsync(Path.Combine(textureDirectory, name), tex);
      }
      await using var destination = new MemoryStream();

      var result = await new GltfInterchange().ExportGlbAsync(
        CreateAttachedAndProceduralEffectsAsset(),
        destination,
        new GltfExportOptions(textureSearchRoots: [directory])
      );

      result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
      result
        .Diagnostics.Should()
        .NotContain(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing
          || diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable
        );
      using var json = ReadGlbJson(destination.ToArray());
      json.RootElement.GetProperty("images").GetArrayLength().Should().Be(1);
      json.RootElement.GetProperty("materials")
        .EnumerateArray()
        .Should()
        .AllSatisfy(material =>
          material
            .GetProperty("pbrMetallicRoughness")
            .GetProperty("baseColorTexture")
            .GetProperty("index")
            .GetInt32()
            .Should()
            .Be(0)
        );
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SpherePreservesInactiveOrdinaryFrameDeclarationsExactly()
  {
    var source = CreateSingleEffectAsset(
      DynamicEffectRecipes.Sphere("Textures\\fx\\sphere.tex", new Vector3(0.4f, 0.5f, 0.6f), true)
    );
    var bytes = source.GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x370), 23);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x374), -7);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x378), 0);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var asset).Should().BeTrue();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(asset!, package);
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    package.Position = 0;

    var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported.Value!.Asset.GetSerializedRepresentation().Should().Equal(bytes);
  }

  [Fact]
  public async Task InvalidSphereTopologyFailsTransactionally()
  {
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      CreateAttachedAndProceduralEffectsAsset(),
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var malformed = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        var accessor = root["accessors"]![10]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        WriteSingle(binary, view["byteOffset"]!.GetValue<int>(), 0.25f);
      }
    );
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "DynamicObjectScopes[4].SpherePreview"
      );
  }

  [Fact]
  public async Task SpherePreviewVertexLimitFailsWithoutPartialOutput()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateAttachedAndProceduralEffectsAsset(),
      destination,
      profile: new GltfOperationProfile(maxActiveRenderVertices: 100)
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task FiniteUnrepresentableSmokeMaterialValuesRemainExact()
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(0, 1, 1), 1, 1);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 1, 1, -1),
      new EffectRectangle(-2, 2, 2, -2),
      0.25f
    );
    var source = CreateSingleEffectAsset(
      DynamicEffectRecipes.Smoke(
        sprite,
        shape,
        "Textures\\fx\\smoke.tex",
        Vector3.One,
        1,
        new CanonicalDynamicAlpha(1, 0, DynamicAlphaTiming.FramePhase),
        false
      )
    );
    var bytes = source.GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    WriteSingle(bytes, firstChildOffset + 0x384, 0);
    WriteSingle(bytes, firstChildOffset + 0x388, -1);
    WriteSingle(bytes, firstChildOffset + 0x3C8, 2);
    WriteSingle(bytes, firstChildOffset + 0x3CC, -1);
    WriteSingle(bytes, firstChildOffset + 0x3D4, -2);
    WriteSingle(bytes, firstChildOffset + 0x3E0, 2);
    WriteSingle(bytes, firstChildOffset + 0x3F8, float.NaN);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var asset).Should().BeTrue();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(asset!, package);
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    package.Position = 0;

    var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported.Value!.Asset.GetSerializedRepresentation().Should().Equal(bytes);
  }

  [Fact]
  public async Task UnsupportedEffectAndObjectLimitFailWithoutOutput()
  {
    var limitedResult = await new GltfInterchange().ExportGlbAsync(
      CreateSpriteEffectsAsset(),
      new MemoryStream(),
      profile: new GltfOperationProfile(maxOutputBytes: 1024)
    );

    limitedResult.Status.Should().Be(OperationStatus.Failed);
    limitedResult
      .Diagnostics.Select(item => item.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
  }

  [Fact]
  public async Task ScalableObjectUsesAReferencedStaticMeshPreviewAndRoundTripsExactly()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-{Guid.NewGuid():N}");
    var meshes = Path.Combine(directory, "mEsHeS", "EfFeCtS");
    Directory.CreateDirectory(meshes);
    try
    {
      var referenced = CreateReferencedStaticAsset();
      await File.WriteAllBytesAsync(
        Path.Combine(meshes, "PrEvIeW.MsH"),
        referenced.GetSerializedRepresentation()
      );
      var asset = CreateScalableAsset("effects\\preview", 2, 5);
      await using var package = new MemoryStream();
      var interchange = new GltfInterchange();

      var export = await interchange.ExportGlbAsync(
        asset,
        package,
        new GltfExportOptions(
          _lineageId,
          _documentId,
          textureSearchRoots: null,
          preservedUnknownMetadata: null,
          meshResourceSearchRoots: [directory]
        )
      );

      export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
      export
        .Diagnostics.Should()
        .NotContain(item => item.Code == GltfDiagnosticCodes.UnsupportedDomain);
      using var json = ReadGlbJson(package.ToArray());
      var scalableNode = json.RootElement.GetProperty("nodes")[2];
      scalableNode
        .GetProperty("scale")
        .EnumerateArray()
        .Select(item => item.GetSingle())
        .Should()
        .OnlyContain(item => Math.Abs(item - 2.03f) < 0.0001f);
      var primitive = json.RootElement.GetProperty("meshes")[0].GetProperty("primitives")[0];
      var positionAccessor = primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32();
      json.RootElement.GetProperty("accessors")[positionAccessor]
        .GetProperty("count")
        .GetInt32()
        .Should()
        .Be(3);
      package.Position = 0;

      var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

      imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
      imported
        .Value!.Asset.GetSerializedRepresentation()
        .Should()
        .Equal(asset.GetSerializedRepresentation());
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task MissingAndShadowedScalableResourcesKeepTheirExactBinding()
  {
    var firstRoot = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-scalable-first-{Guid.NewGuid():N}"
    );
    var secondRoot = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-scalable-second-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(Path.Combine(firstRoot, "Meshes"));
    Directory.CreateDirectory(Path.Combine(secondRoot, "Meshes"));
    try
    {
      var referenced = CreateReferencedStaticAsset().GetSerializedRepresentation();
      await File.WriteAllBytesAsync(Path.Combine(firstRoot, "Meshes", "shared.msh"), referenced);
      await File.WriteAllBytesAsync(Path.Combine(secondRoot, "Meshes", "SHARED.MSH"), referenced);
      var shadowed = CreateScalableAsset("shared", 1, 2);

      var shadowedResult = await new GltfInterchange().ExportGlbAsync(
        shadowed,
        new MemoryStream(),
        new GltfExportOptions(
          _lineageId,
          _documentId,
          textureSearchRoots: null,
          preservedUnknownMetadata: null,
          meshResourceSearchRoots: [firstRoot, secondRoot]
        )
      );
      var missing = CreateScalableAsset("..\\outside", 1, 2);
      var missingResult = await new GltfInterchange().ExportGlbAsync(
        missing,
        new MemoryStream(),
        new GltfExportOptions(
          _lineageId,
          _documentId,
          textureSearchRoots: null,
          preservedUnknownMetadata: null,
          meshResourceSearchRoots: [firstRoot]
        )
      );

      shadowedResult
        .Status.Should()
        .Be(OperationStatus.Succeeded, Diagnostics(shadowedResult.Diagnostics));
      shadowedResult
        .Diagnostics.Should()
        .ContainSingle(item => item.Code == GltfDiagnosticCodes.MeshResourceShadowed);
      missingResult
        .Status.Should()
        .Be(OperationStatus.Succeeded, Diagnostics(missingResult.Diagnostics));
      missingResult
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.MeshPreviewUnavailable)
        .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
      missing
        .RootDynamicObject.Children[0]
        .Extension.MeshNameBytes.Should()
        .Equal(Encoding.ASCII.GetBytes("..\\outside"));
    }
    finally
    {
      Directory.Delete(firstRoot, true);
      Directory.Delete(secondRoot, true);
    }
  }

  [Fact]
  public async Task AmbiguousAndDynamicScalableResourcesUseDeterministicPlaceholders()
  {
    var root = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-hazards-{Guid.NewGuid():N}");
    var meshes = Path.Combine(root, "Meshes");
    Directory.CreateDirectory(meshes);
    try
    {
      var staticBytes = CreateReferencedStaticAsset().GetSerializedRepresentation();
      await File.WriteAllBytesAsync(Path.Combine(meshes, "ambiguous.msh"), staticBytes);
      await File.WriteAllBytesAsync(Path.Combine(meshes, "AMBIGUOUS.MSH"), staticBytes);
      var supportsCaseDistinctFiles =
        Directory
          .EnumerateFiles(meshes)
          .Count(path =>
            string.Equals(
              Path.GetFileName(path),
              "ambiguous.msh",
              StringComparison.OrdinalIgnoreCase
            )
          ) == 2;
      await File.WriteAllBytesAsync(
        Path.Combine(meshes, "dynamic.msh"),
        CreateAsset().GetSerializedRepresentation()
      );
      var interchange = new GltfInterchange();

      var ambiguous = await interchange.ExportGlbAsync(
        CreateScalableAsset("ambiguous", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(null, null, null, null, [root])
      );
      var dynamic = await interchange.ExportGlbAsync(
        CreateScalableAsset("dynamic", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(null, null, null, null, [root])
      );

      ambiguous.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(ambiguous.Diagnostics));
      if (supportsCaseDistinctFiles)
      {
        ambiguous
          .Diagnostics.Select(item => item.Code)
          .Should()
          .Contain(GltfDiagnosticCodes.AmbiguousMeshResource)
          .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
      }
      else
      {
        ambiguous
          .Diagnostics.Select(item => item.Code)
          .Should()
          .NotContain(GltfDiagnosticCodes.AmbiguousMeshResource)
          .And.NotContain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
      }
      dynamic.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(dynamic.Diagnostics));
      dynamic
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.UnsupportedMeshResource)
        .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
    }
    finally
    {
      Directory.Delete(root, true);
    }
  }

  [Fact]
  public async Task CyclicScalableResourceChainsAreBoundedAndDiagnosed()
  {
    var root = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-cycle-{Guid.NewGuid():N}");
    var meshes = Path.Combine(root, "Meshes");
    Directory.CreateDirectory(meshes);
    try
    {
      await File.WriteAllBytesAsync(
        Path.Combine(meshes, "first.msh"),
        CreateScalableAsset("second", 1, 2).GetSerializedRepresentation()
      );
      await File.WriteAllBytesAsync(
        Path.Combine(meshes, "second.msh"),
        CreateScalableAsset("first", 1, 2).GetSerializedRepresentation()
      );

      var cyclic = await new GltfInterchange().ExportGlbAsync(
        CreateScalableAsset("first", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(null, null, null, null, [root])
      );
      await using var limitedOutput = new MemoryStream();
      var limited = await new GltfInterchange().ExportGlbAsync(
        CreateScalableAsset("first", 1, 2),
        limitedOutput,
        new GltfExportOptions(null, null, null, null, [root]),
        new GltfOperationProfile(new GltfMeshResourceLimits(maxDepth: 1))
      );

      cyclic.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(cyclic.Diagnostics));
      cyclic
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.MeshResourceCycle)
        .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
      limited.Status.Should().Be(OperationStatus.Failed);
      limited
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
      limitedOutput.Length.Should().Be(0);
    }
    finally
    {
      Directory.Delete(root, true);
    }
  }

  [Fact]
  public async Task ScalableResourceLimitsFailWithoutPartialOutput()
  {
    var roots = new[]
    {
      Path.GetFullPath(Path.Combine(Path.GetTempPath(), "earthtool-root-a")),
      Path.GetFullPath(Path.Combine(Path.GetTempPath(), "earthtool-root-b")),
    };
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateScalableAsset("preview", 1, 2),
      destination,
      new GltfExportOptions(null, null, null, null, roots),
      new GltfOperationProfile(new GltfMeshResourceLimits(maxSearchRoots: 1))
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Select(item => item.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task AggregateScalablePreviewVertexLimitCountsEveryEmittedScope()
  {
    var root = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-vertices-{Guid.NewGuid():N}");
    Directory.CreateDirectory(Path.Combine(root, "Meshes"));
    try
    {
      await File.WriteAllBytesAsync(
        Path.Combine(root, "Meshes", "preview.msh"),
        CreateReferencedStaticAsset().GetSerializedRepresentation()
      );
      var build = DynamicMeshBuilder
        .Create()
        .SetRoot(
          DynamicEffectRecipes.Group([
            CreateScalableRecipe("preview", 1, 2),
            CreateScalableRecipe("preview", 1, 2),
          ])
        )
        .Build();
      build.TryGetValue(out var asset).Should().BeTrue();
      await using var destination = new MemoryStream();

      var result = await new GltfInterchange().ExportGlbAsync(
        asset!,
        destination,
        new GltfExportOptions(null, null, null, null, [root]),
        new GltfOperationProfile(new GltfMeshResourceLimits(maxPreviewVertices: 5))
      );

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
      destination.Length.Should().Be(0);
    }
    finally
    {
      Directory.Delete(root, true);
    }
  }

  [Fact]
  public async Task ScalableLookupRejectsRelativeRootsAndLinkedComponents()
  {
    var createRelativeOptions = () => new GltfExportOptions(null, null, null, null, ["relative"]);
    createRelativeOptions.Should().Throw<ArgumentException>();

    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-link-{Guid.NewGuid():N}");
    var outside = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-scalable-outside-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(directory);
    Directory.CreateDirectory(Path.Combine(outside, "Meshes"));
    try
    {
      await File.WriteAllBytesAsync(
        Path.Combine(outside, "Meshes", "preview.msh"),
        CreateReferencedStaticAsset().GetSerializedRepresentation()
      );
      Directory.CreateSymbolicLink(
        Path.Combine(directory, "Meshes"),
        Path.Combine(outside, "Meshes")
      );

      var result = await new GltfInterchange().ExportGlbAsync(
        CreateScalableAsset("preview", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(null, null, null, null, [directory])
      );

      result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
      result
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.MeshResourceMissing)
        .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
    }
    finally
    {
      Directory.Delete(directory, true);
      Directory.Delete(outside, true);
    }
  }

  [Fact]
  public async Task ScalableObjectExportsLifetimeTranslationAndScaleAnimation()
  {
    var asset = CreateSingleEffectAsset(
      CreateScalableRecipe("preview", 2, 5)
        .SetChildTranslation(new Vector3(1, 2, 3), new Vector3(4, 5, 6))
    );
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset,
      destination,
      new GltfExportOptions(_lineageId, _documentId)
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var glb = destination.ToArray();
    using var json = ReadGlbJson(glb);
    var root = json.RootElement;
    var animation = root.GetProperty("animations").EnumerateArray().Single();
    animation.GetProperty("name").GetString().Should().Be("EarthTool Dynamic Preview");
    var channels = animation.GetProperty("channels").EnumerateArray().ToArray();
    channels
      .Select(channel => channel.GetProperty("target").GetProperty("path").GetString())
      .Should()
      .BeEquivalentTo(["translation", "scale"]);
    channels
      .Select(channel => channel.GetProperty("target").GetProperty("node").GetInt32())
      .Should()
      .OnlyContain(node => node == 2);
    var samplers = animation.GetProperty("samplers");
    var inputAccessor = samplers[0].GetProperty("input").GetInt32();
    ReadFloatAccessor(root, ReadGlbBinary(glb), inputAccessor).Should().Equal(0, 5);
    var node = root.GetProperty("nodes")[2];
    var restTranslation = node.GetProperty("translation")
      .EnumerateArray()
      .Select(value => value.GetSingle())
      .ToArray();
    var restScale = node.GetProperty("scale")
      .EnumerateArray()
      .Select(value => value.GetSingle())
      .ToArray();
    foreach (var channel in channels)
    {
      var path = channel.GetProperty("target").GetProperty("path").GetString();
      var sampler = samplers[channel.GetProperty("sampler").GetInt32()];
      sampler.GetProperty("input").GetInt32().Should().Be(inputAccessor);
      sampler.GetProperty("interpolation").GetString().Should().Be("LINEAR");
      var values = ReadVector3Accessor(
        root,
        ReadGlbBinary(glb),
        sampler.GetProperty("output").GetInt32()
      );
      if (path == "translation")
      {
        values.Should().Equal(new Vector3(1, 3, -2), new Vector3(4, 6, -5));
        values[0]
          .Should()
          .Be(new Vector3(restTranslation[0], restTranslation[1], restTranslation[2]));
      }
      else
      {
        values[0].Should().Be(new Vector3(restScale[0], restScale[1], restScale[2]));
        values[1].Should().Be(new Vector3(5));
      }
    }
  }

  [Fact]
  public async Task ScalableScaleAndBindingEditsRegenerateOnlyTheDynamicRecord()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-edit-{Guid.NewGuid():N}");
    Directory.CreateDirectory(Path.Combine(directory, "Meshes"));
    try
    {
      var resourcePath = Path.Combine(directory, "Meshes", "preview.msh");
      await File.WriteAllBytesAsync(
        resourcePath,
        CreateReferencedStaticAsset().GetSerializedRepresentation()
      );
      var resourceBefore = await File.ReadAllBytesAsync(resourcePath);
      var asset = CreateScalableAsset("preview", 2, 5);
      await using var package = new MemoryStream();
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGlbAsync(
        asset,
        package,
        new GltfExportOptions(
          _lineageId,
          _documentId,
          textureSearchRoots: null,
          preservedUnknownMetadata: null,
          meshResourceSearchRoots: [directory]
        )
      );
      var edited = RewriteGlb(
        package.ToArray(),
        (root, binary) =>
        {
          root["nodes"]![2]!["scale"] = new JsonArray(3.02f, 3.02f, 3.02f);
          var metadata = JsonNode.Parse(
            root["nodes"]![2]!["extras"]!["earthtool"]!.GetValue<string>()
          )!;
          metadata["payload"]!["meshName"] = Convert
            .ToBase64String(Encoding.ASCII.GetBytes("renamed"))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
          root["nodes"]![2]!["extras"]!["earthtool"] = metadata.ToJsonString();
          var accessor = root["accessors"]![0]!;
          var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
          WriteSingle(binary, view["byteOffset"]!.GetValue<int>(), 42);
          accessor["min"] = new JsonArray(0, 0, -1);
          accessor["max"] = new JsonArray(42, 0, 0);
        }
      );
      await using var editedStream = new MemoryStream(edited);

      var imported = await interchange.ImportEditDynamicGlbAsync(
        editedStream,
        export.Value!.Baseline
      );

      imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
      var scalable = imported.Value!.Asset.RootDynamicObject.Children[0].Extension;
      scalable.StartModelScale.Should().BeApproximately(3, 0.0001f);
      scalable.EndModelScale.Should().Be(5);
      scalable.MeshNameBytes.Should().Equal(Encoding.ASCII.GetBytes("renamed"));
      (await File.ReadAllBytesAsync(resourcePath)).Should().Equal(resourceBefore);
      imported
        .Value.Preservation.Changes.Should()
        .Contain(item =>
          item.FieldPath.EndsWith("StartModelScale", StringComparison.Ordinal)
          && item.Disposition == PreservationDisposition.Regenerated
        );
      imported
        .Value.Preservation.Changes.Should()
        .Contain(item =>
          item.FieldPath.EndsWith("MeshNameBytes", StringComparison.Ordinal)
          && item.Disposition == PreservationDisposition.Regenerated
        );
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateManifestFailureRemovesNewDynamicSidecar()
  {
    var directory = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-dynamic-transaction-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(directory);
    try
    {
      var destination = Path.Combine(directory, "effect.gltf");
      var interchange = new GltfInterchange(new ManifestFailingFileSystem());

      var result = await interchange.ExportGltfFileAsync(CreateAsset(), destination);

      result.Status.Should().Be(OperationStatus.Failed);
      Directory.EnumerateFileSystemEntries(directory).Should().BeEmpty();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateGltfRoundTripsTheExactDynamicMsh()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-dynamic-gltf-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
      var path = Path.Combine(directory, "effect.gltf");
      var asset = CreateSpriteEffectsAsset();
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(_lineageId, _documentId)
      );
      export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));

      var imported = await interchange.ImportEditDynamicGltfFileAsync(path, export.Value!.Baseline);

      imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
      imported
        .Value!.Asset.GetSerializedRepresentation()
        .Should()
        .Equal(asset.GetSerializedRepresentation());
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task NestedSpriteEffectsRoundTripExactlyThroughBothPackageForms()
  {
    var asset = CreateSpriteEffectsAsset();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var glbExport = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(_lineageId, _documentId)
    );
    glbExport.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(glbExport.Diagnostics));
    glb.Position = 0;

    var glbImport = await interchange.ImportEditDynamicGlbAsync(glb, glbExport.Value!.Baseline);

    glbImport.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(glbImport.Diagnostics));
    glbImport
      .Value!.Asset.GetSerializedRepresentation()
      .Should()
      .Equal(asset.GetSerializedRepresentation());

    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-sprite-gltf-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
      var path = Path.Combine(directory, "effects.gltf");
      var gltfExport = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(_lineageId, _documentId)
      );
      gltfExport.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(gltfExport.Diagnostics));

      var gltfImport = await interchange.ImportEditDynamicGltfFileAsync(
        path,
        gltfExport.Value!.Baseline
      );

      gltfImport.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(gltfImport.Diagnostics));
      gltfImport
        .Value!.Asset.GetSerializedRepresentation()
        .Should()
        .Equal(asset.GetSerializedRepresentation());
      gltfImport.Value.NextExportOptions.DynamicObjectIds.Should().Equal(1, 2, 3, 4, 5);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task DynamicPackagesPassKhronosValidation()
  {
    var directory = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-dynamic-validation-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(directory);
    try
    {
      var glbPath = Path.Combine(directory, "effect.glb");
      var gltfPath = Path.Combine(directory, "effect.gltf");
      var ribbonGlbPath = Path.Combine(directory, "ribbons.glb");
      var ribbonGltfPath = Path.Combine(directory, "ribbons.gltf");
      var attachedGlbPath = Path.Combine(directory, "attached.glb");
      var attachedGltfPath = Path.Combine(directory, "attached.gltf");
      var scalableGlbPath = Path.Combine(directory, "scalable.glb");
      var scalableGltfPath = Path.Combine(directory, "scalable.gltf");
      var groupPath = Path.Combine(directory, "group.glb");
      var asset = CreateSpriteEffectsAsset();
      var interchange = new GltfInterchange();
      (await interchange.ExportGlbFileAsync(asset, glbPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (await interchange.ExportGltfFileAsync(asset, gltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (await interchange.ExportGlbFileAsync(CreateRibbonEffectsAsset(), ribbonGlbPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (await interchange.ExportGltfFileAsync(CreateRibbonEffectsAsset(), ribbonGltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (
        await interchange.ExportGlbFileAsync(
          CreateAttachedAndProceduralEffectsAsset(),
          attachedGlbPath
        )
      )
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (
        await interchange.ExportGltfFileAsync(
          CreateAttachedAndProceduralEffectsAsset(),
          attachedGltfPath
        )
      )
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      Directory.CreateDirectory(Path.Combine(directory, "Meshes"));
      await File.WriteAllBytesAsync(
        Path.Combine(directory, "Meshes", "preview.msh"),
        CreateReferencedStaticAsset().GetSerializedRepresentation()
      );
      var scalableOptions = new GltfExportOptions(null, null, null, null, [directory]);
      (
        await interchange.ExportGlbFileAsync(
          CreateScalableAsset("preview", -2, 3),
          scalableGlbPath,
          scalableOptions
        )
      )
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (
        await interchange.ExportGltfFileAsync(
          CreateScalableAsset("preview", -2, 3),
          scalableGltfPath,
          scalableOptions
        )
      )
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      var groupBuild = DynamicMeshBuilder
        .Create()
        .SetRoot(DynamicEffectRecipes.Group([DynamicEffectRecipes.Group()]))
        .Build();
      groupBuild.TryGetValue(out var group).Should().BeTrue();
      (await interchange.ExportGlbFileAsync(group!, groupPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);

      await using (var glb = File.OpenRead(glbPath))
      {
        (await interchange.ValidateGlbAsync(glb)).Status.Should().Be(OperationStatus.Succeeded);
      }
      (await interchange.ValidateGltfFileAsync(gltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      await using (var ribbonGlb = File.OpenRead(ribbonGlbPath))
      {
        (await interchange.ValidateGlbAsync(ribbonGlb))
          .Status.Should()
          .Be(OperationStatus.Succeeded);
      }
      (await interchange.ValidateGltfFileAsync(ribbonGltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      await using (var attachedGlb = File.OpenRead(attachedGlbPath))
      {
        (await interchange.ValidateGlbAsync(attachedGlb))
          .Status.Should()
          .Be(OperationStatus.Succeeded);
      }
      (await interchange.ValidateGltfFileAsync(attachedGltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      await using (var scalableGlb = File.OpenRead(scalableGlbPath))
      {
        (await interchange.ValidateGlbAsync(scalableGlb))
          .Status.Should()
          .Be(OperationStatus.Succeeded);
      }
      (await interchange.ValidateGltfFileAsync(scalableGltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);

      await AssertKhronosValidAsync(glbPath);
      await AssertKhronosValidAsync(gltfPath);
      await AssertKhronosValidAsync(ribbonGlbPath);
      await AssertKhronosValidAsync(ribbonGltfPath);
      await AssertKhronosValidAsync(attachedGlbPath);
      await AssertKhronosValidAsync(attachedGltfPath);
      await AssertKhronosValidAsync(scalableGlbPath);
      await AssertKhronosValidAsync(scalableGltfPath);
      await AssertKhronosValidAsync(groupPath);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task HierarchyEditReordersExactDynamicRecords()
  {
    var asset = CreateAsset();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var edited = RewriteGlb(
      package.ToArray(),
      (root, _) =>
      {
        var children = root["nodes"]![1]!["children"]!.AsArray();
        var first = children[0]!.GetValue<int>();
        children[0] = children[1]!.GetValue<int>();
        children[1] = first;
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    var children = imported.Value!.Asset.RootDynamicObject.Children;
    Encoding
      .ASCII.GetString(children[0].Extension.TexturePathBytes.ToArray())
      .Should()
      .Be("Textures\\fx\\second.tex");
    Encoding
      .ASCII.GetString(children[1].Extension.TexturePathBytes.ToArray())
      .Should()
      .Be("Textures\\fx\\first.tex");
    children[1].Extension.AdditiveFlag.Should().Be(1);
    children[1]
      .Extension.EndEffectRectangle.Should()
      .Be(asset.RootDynamicObject.Children[0].Extension.EndEffectRectangle);
    imported
      .Value.Preservation.Changes.Should()
      .Contain(change =>
        change.FieldPath == "RootDynamicObject.Children"
        && change.Disposition == PreservationDisposition.Regenerated
      );
    await using var reexported = new MemoryStream();
    var reexport = await interchange.ExportGlbAsync(
      imported.Value.Asset,
      reexported,
      imported.Value.NextExportOptions
    );
    reexport.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(reexport.Diagnostics));
    reexport
      .Diagnostics.Where(item => item.Code == GltfDiagnosticCodes.TextureResourceMissing)
      .Select(item => item.Path)
      .Should()
      .Equal(
        "DynamicObjectScopes[3].Extension.TexturePathBytes",
        "DynamicObjectScopes[2].Extension.TexturePathBytes"
      );
    using var reexportedJson = ReadGlbJson(reexported.ToArray());
    reexportedJson
      .RootElement.GetProperty("nodes")
      .EnumerateArray()
      .Skip(1)
      .Select(node =>
      {
        var metadata = node.GetProperty("extras").GetProperty("earthtool").GetString()!;
        using var envelope = JsonDocument.Parse(metadata);
        return envelope.RootElement.GetProperty("scope").GetProperty("localId").GetInt32();
      })
      .Should()
      .Equal(1, 3, 2);
  }

  [Fact]
  public async Task ExplosionPreviewEditRegeneratesOnlyOwnedRepresentations()
  {
    var asset = CreateAsset();
    var original = asset.RootDynamicObject.Children[0].Extension;
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var edited = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        root["nodes"]![2]!["translation"] = new JsonArray(10, 20, 30);
        root["materials"]![0]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          0.9f,
          0.8f,
          0.7f,
          0.6f
        );
        var accessor = root["accessors"]![0]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        var offset = view["byteOffset"]!.GetValue<int>();
        var positions = new[]
        {
          new Vector3(-10, -40, 2),
          new Vector3(30, -40, 2),
          new Vector3(30, 20, 2),
          new Vector3(-10, 20, 2),
        };
        for (var index = 0; index < positions.Length; index++)
        {
          WriteVector3(binary, offset + index * 12, positions[index]);
        }
        accessor["min"] = new JsonArray(-10, -40, 2);
        accessor["max"] = new JsonArray(30, 20, 2);
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    var extension = imported.Value!.Asset.RootDynamicObject.Children[0].Extension;
    extension.ChildStartTranslation.Should().Be(new Vector3(10, -30, 20));
    extension.ChildEndTranslation.Should().Be(original.ChildEndTranslation);
    extension.StartEffectRectangle.Should().Be(new EffectRectangle(-10, 20, 30, -40));
    extension.EndEffectRectangle.Should().Be(original.EndEffectRectangle);
    extension.EffectDepthOffset.Should().Be(2);
    extension.VisibleEffectColor.Should().Be(new Vector3(0.9f, 0.8f, 0.7f));
    extension.StartAlpha.Should().BeApproximately(0.6f, 0.0001f);
    extension.EndAlpha.Should().Be(original.EndAlpha);
    extension.AdditiveFlag.Should().Be(original.AdditiveFlag);
    extension.LightType.Should().Be(original.LightType);
    extension.TexturePathBytes.Should().Equal(original.TexturePathBytes);
  }

  [Fact]
  public async Task FlatExplosionPreviewEditRegeneratesOnlyOwnedRepresentations()
  {
    var asset = CreateSpriteEffectsAsset();
    var original = asset.RootDynamicObject.Children[1].Children[0].Extension;
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var edited = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        root["materials"]![3]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          0.9f,
          0.8f,
          0.7f,
          0.6f
        );
        var accessor = root["accessors"]![12]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        var offset = view["byteOffset"]!.GetValue<int>();
        var positions = new[]
        {
          new Vector3(-10, 2, 40),
          new Vector3(30, 2, 40),
          new Vector3(30, 2, -20),
          new Vector3(-10, 2, -20),
        };
        for (var index = 0; index < positions.Length; index++)
        {
          WriteVector3(binary, offset + index * 12, positions[index]);
        }
        accessor["min"] = new JsonArray(-10, 2, -20);
        accessor["max"] = new JsonArray(30, 2, 40);
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    var extension = imported.Value!.Asset.RootDynamicObject.Children[1].Children[0].Extension;
    extension.StartEffectRectangle.Should().Be(new EffectRectangle(-10, 20, 30, -40));
    extension.EndEffectRectangle.Should().Be(original.EndEffectRectangle);
    extension.EffectDepthOffset.Should().Be(2);
    extension.VisibleEffectColor.Should().Be(new Vector3(0.9f, 0.8f, 0.7f));
    extension.StartAlpha.Should().BeApproximately(0.6f, 0.0001f);
    extension.EndAlpha.Should().Be(original.EndAlpha);
    extension.AdditiveFlag.Should().Be(original.AdditiveFlag);
    extension.LightType.Should().Be(original.LightType);
    extension.TerrainLightColor.Should().Be(original.TerrainLightColor);
    extension.TexturePathBytes.Should().Equal(original.TexturePathBytes);
  }

  [Fact]
  public async Task TrackMappedExplosionAndSmokeEditsRespectEffectOwnership()
  {
    var asset = CreateSpriteEffectsAsset();
    var originalTrack = asset.RootDynamicObject.Children[0].Extension;
    var originalSmoke = asset.RootDynamicObject.Children[0].Children[0].Extension;
    var originalMapped = asset.RootDynamicObject.Children[1].Extension;
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      package,
      new GltfExportOptions(_lineageId, _documentId)
    );
    var edited = RewriteGlb(
      package.ToArray(),
      (root, binary) =>
      {
        root["materials"]![0]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          0.1f,
          0.2f,
          0.3f,
          0.7f
        );
        root["materials"]![1]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          0.15f,
          0.1f,
          0.05f,
          0.7f
        );
        root["materials"]![2]!["pbrMetallicRoughness"]!["baseColorFactor"] = new JsonArray(
          0.6f,
          0.5f,
          0.4f,
          0.7f
        );
        var accessor = root["accessors"]![0]!;
        var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
        var offset = view["byteOffset"]!.GetValue<int>();
        var positions = new[]
        {
          new Vector3(-2, 0, 5),
          new Vector3(4, 0, 5),
          new Vector3(4, 0, -3),
          new Vector3(-2, 0, -3),
        };
        for (var index = 0; index < positions.Length; index++)
        {
          WriteVector3(binary, offset + index * 12, positions[index]);
        }
        accessor["min"] = new JsonArray(-2, 0, -3);
        accessor["max"] = new JsonArray(4, 0, 5);
      }
    );
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline
    );

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    var track = imported.Value!.Asset.RootDynamicObject.Children[0].Extension;
    var smoke = imported.Value.Asset.RootDynamicObject.Children[0].Children[0].Extension;
    var mapped = imported.Value.Asset.RootDynamicObject.Children[1].Extension;
    track.StartEffectRectangle.Should().Be(new EffectRectangle(-2, 3, 4, -5));
    track.VisibleEffectColor.Should().Be(originalTrack.VisibleEffectColor);
    track.StartAlpha.Should().BeApproximately(0.7f, 0.0001f);
    track.EndEffectRectangle.Should().Be(originalTrack.EndEffectRectangle);
    smoke.VisibleEffectColor.X.Should().BeApproximately(0.3f, 0.0001f);
    smoke.VisibleEffectColor.Y.Should().BeApproximately(0.2f, 0.0001f);
    smoke.VisibleEffectColor.Z.Should().BeApproximately(0.1f, 0.0001f);
    smoke.VisibleTerrainLightGain.Should().Be(originalSmoke.VisibleTerrainLightGain);
    smoke.EndAlpha.Should().Be(originalSmoke.EndAlpha);
    mapped.VisibleEffectColor.X.Should().BeApproximately(0.6f, 0.0001f);
    mapped.VisibleEffectColor.Y.Should().BeApproximately(0.5f, 0.0001f);
    mapped.VisibleEffectColor.Z.Should().BeApproximately(0.4f, 0.0001f);
    mapped.EffectDepthOffset.Should().Be(originalMapped.EffectDepthOffset);
    mapped.TerrainLightColor.Should().Be(originalMapped.TerrainLightColor);
  }

  private static DynamicMeshAsset CreateAsset()
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 3, 4), 5, 2);
    var alpha = new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.LifetimeProgress);
    var light = new CanonicalDynamicTerrainLight(
      DynamicLightType.Trapezium,
      new Vector3(0.1f, 0.2f, 0.3f)
    );
    var first = DynamicEffectRecipes
      .Explosion(
        sprite,
        new CanonicalDynamicEffectShape(
          new EffectRectangle(-1, 2, 3, -4),
          new EffectRectangle(-5, 6, 7, -8),
          0.25f
        ),
        "Textures\\fx\\first.tex",
        new Vector3(0.4f, 0.5f, 0.6f),
        alpha,
        true,
        light
      )
      .SetChildTranslation(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
    var second = DynamicEffectRecipes.Explosion(
      sprite,
      new CanonicalDynamicEffectShape(
        new EffectRectangle(-2, 3, 4, -5),
        new EffectRectangle(-6, 7, 8, -9),
        0.5f
      ),
      "Textures\\fx\\second.tex",
      new Vector3(0.7f, 0.8f, 0.9f),
      alpha,
      false,
      light
    );
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([first, second]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateScalableAsset(
    string meshResourceKey,
    float startScale,
    float endScale
  )
  {
    return CreateSingleEffectAsset(CreateScalableRecipe(meshResourceKey, startScale, endScale));
  }

  private static CanonicalDynamicObject CreateScalableRecipe(
    string meshResourceKey,
    float startScale,
    float endScale
  )
  {
    return DynamicEffectRecipes.ScalableObject(
      new CanonicalDynamicFrameSequence(0, 1, 0),
      meshResourceKey,
      "Textures\\fx\\scalable.tex",
      startScale,
      endScale,
      new Vector3(0.4f, 0.5f, 0.6f),
      new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.FramePhase),
      false,
      new CanonicalDynamicTerrainLight(DynamicLightType.Constant, Vector3.Zero)
    );
  }

  private static StaticMeshAsset CreateReferencedStaticAsset()
  {
    var vertices = new[]
    {
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
    };
    var build = StaticMeshBuilder
      .Create()
      .SetRootSourceObject(
        new CanonicalStaticSourceObject([
          new CanonicalStaticRenderObject(vertices, [new CanonicalTriangle(0, 1, 2)]),
        ])
      )
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateSpriteEffectsAsset()
  {
    var frames = new CanonicalDynamicFrameSequence(2, 3, 4);
    var sprite = new CanonicalDynamicSpriteSheet(frames, 5, 2);
    var alpha = new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.LifetimeProgress);
    var light = new CanonicalDynamicTerrainLight(
      DynamicLightType.Trapezium,
      new Vector3(0.1f, 0.2f, 0.3f)
    );
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 2, 3, -4),
      new EffectRectangle(-5, 6, 7, -8),
      0.25f
    );
    var smoke = DynamicEffectRecipes.Smoke(
      sprite,
      shape,
      "Textures\\fx\\smoke.tex",
      new Vector3(0.4f, 0.5f, 0.6f),
      0.5f,
      alpha,
      true
    );
    var track = DynamicEffectRecipes.Track(
      frames,
      shape.StartEffectRectangle,
      shape.EndEffectRectangle,
      "Textures\\fx\\track.tex",
      alpha,
      false,
      [smoke]
    );
    var flat = DynamicEffectRecipes.FlatExplosion(
      sprite,
      shape,
      "Textures\\fx\\flat.tex",
      new Vector3(0.7f, 0.8f, 0.9f),
      alpha,
      false,
      light
    );
    var mapped = DynamicEffectRecipes.MappedExplosion(
      frames,
      shape.StartEffectRectangle,
      shape.EndEffectRectangle,
      "Textures\\fx\\mapped.tex",
      new Vector3(0.2f, 0.3f, 0.4f),
      alpha,
      true,
      light,
      [flat]
    );
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([track, mapped]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateRibbonEffectsAsset()
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 3, 4), 5, 2);
    var alpha = new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.LifetimeProgress);
    var light = new CanonicalDynamicTerrainLight(
      DynamicLightType.Trapezium,
      new Vector3(0.1f, 0.2f, 0.3f)
    );
    var electrical = DynamicEffectRecipes.ElectricalCannon(
      sprite,
      -0.25f,
      "Textures\\fx\\electrical.tex",
      new Vector3(0.2f, 0.3f, 0.4f),
      alpha,
      true
    );
    var laser = DynamicEffectRecipes.Laser(
      sprite,
      0.5f,
      "Textures\\fx\\laser.tex",
      new Vector3(0.4f, 0.5f, 0.6f),
      alpha,
      false,
      light,
      [electrical]
    );
    var lightning = DynamicEffectRecipes.Lightning(
      sprite,
      -0.75f,
      "Textures\\fx\\lightning.tex",
      new Vector3(0.7f, 0.8f, 0.9f),
      alpha,
      true,
      light
    );
    var laserWall = DynamicEffectRecipes.LaserWall(
      sprite,
      1,
      "Textures\\fx\\laser-wall.tex",
      new Vector3(0.3f, 0.6f, 0.9f),
      alpha,
      false,
      new Vector3(0.9f, 0.6f, 0.3f),
      [lightning]
    );
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([laser, laserWall]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateAttachedAndProceduralEffectsAsset()
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 3, 4), 5, 2);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 2, 3, -4),
      new EffectRectangle(-5, 6, 7, -8),
      0.25f
    );
    var line = DynamicEffectRecipes
      .Line(
        sprite,
        shape,
        "Textures\\fx\\line.tex",
        new Vector3(0.2f, 0.3f, 0.4f),
        0.5f,
        0.8f,
        0.2f,
        true
      )
      .SetChildTranslation(new Vector3(2, 3, 4), new Vector3(5, 6, 7));
    var shockwave = DynamicEffectRecipes.Shockwave(
      sprite,
      shape,
      "Textures\\fx\\shockwave.tex",
      new Vector3(0.4f, 0.5f, 0.6f),
      0.5f,
      0.8f,
      0.2f,
      false,
      [line]
    );
    var keelwater = DynamicEffectRecipes.Keelwater(
      sprite,
      shape,
      "Textures\\fx\\keelwater.tex",
      0.8f,
      0.2f,
      false
    );
    var sphere = DynamicEffectRecipes.Sphere(
      "Textures\\fx\\sphere.tex",
      new Vector3(0.7f, 0.8f, 0.9f),
      true,
      [keelwater]
    );
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([shockwave, sphere]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static IReadOnlyList<DynamicEffectExtension> GetRibbonExtensions(DynamicMeshAsset asset)
  {
    return new[]
    {
      asset.RootDynamicObject.Children[0].Extension,
      asset.RootDynamicObject.Children[0].Children[0].Extension,
      asset.RootDynamicObject.Children[1].Extension,
      asset.RootDynamicObject.Children[1].Children[0].Extension,
    };
  }

  private static DynamicMeshAsset CreateSingleEffectAsset(CanonicalDynamicObject effect)
  {
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([effect]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateSingleAttachedEffectAsset(DynamicEffectType effectType)
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 3, 4), 5, 2);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 2, 3, -4),
      new EffectRectangle(-5, 6, 7, -8),
      0.25f
    );
    var effect = effectType switch
    {
      DynamicEffectType.Shockwave => DynamicEffectRecipes.Shockwave(
        sprite,
        shape,
        "Textures\\fx\\shockwave.tex",
        new Vector3(0.4f, 0.5f, 0.6f),
        0.5f,
        0.8f,
        0.2f,
        false
      ),
      DynamicEffectType.Line => DynamicEffectRecipes.Line(
        sprite,
        shape,
        "Textures\\fx\\line.tex",
        new Vector3(0.2f, 0.3f, 0.4f),
        0.5f,
        0.8f,
        0.2f,
        true
      ),
      DynamicEffectType.Keelwater => DynamicEffectRecipes.Keelwater(
        sprite,
        shape,
        "Textures\\fx\\keelwater.tex",
        0.8f,
        0.2f,
        false
      ),
      _ => throw new ArgumentOutOfRangeException(nameof(effectType)),
    };
    return CreateSingleEffectAsset(
      effect.SetChildTranslation(new Vector3(1, 2, 3), new Vector3(4, 5, 6))
    );
  }

  private static JsonDocument ReadGlbJson(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return JsonDocument.Parse(glb.AsMemory(20, jsonLength));
  }

  private static JsonElement ReadDynamicObjectMetadata(JsonElement node)
  {
    return JsonDocument
      .Parse(node.GetProperty("extras").GetProperty("earthtool").GetString()!)
      .RootElement.Clone();
  }

  private static byte[] ReadGlbBinary(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var binaryHeader = 20 + jsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(binaryHeader));
    return glb.AsSpan(binaryHeader + 8, binaryLength).ToArray();
  }

  private static Vector3[] ReadVector3Accessor(JsonElement root, byte[] binary, int accessorIndex)
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    var offset = view.GetProperty("byteOffset").GetInt32();
    return Enumerable
      .Range(0, accessor.GetProperty("count").GetInt32())
      .Select(index => new Vector3(
        BitConverter.ToSingle(binary, offset + index * 12),
        BitConverter.ToSingle(binary, offset + index * 12 + 4),
        BitConverter.ToSingle(binary, offset + index * 12 + 8)
      ))
      .ToArray();
  }

  private static float[] ReadFloatAccessor(JsonElement root, byte[] binary, int accessorIndex)
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    var offset = view.GetProperty("byteOffset").GetInt32();
    return Enumerable
      .Range(0, accessor.GetProperty("count").GetInt32())
      .Select(index => BitConverter.ToSingle(binary, offset + index * sizeof(float)))
      .ToArray();
  }

  private static Vector2[] ReadVector2Accessor(JsonElement root, byte[] binary, int accessorIndex)
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    var offset = view.GetProperty("byteOffset").GetInt32();
    return Enumerable
      .Range(0, accessor.GetProperty("count").GetInt32())
      .Select(index => new Vector2(
        BitConverter.ToSingle(binary, offset + index * 8),
        BitConverter.ToSingle(binary, offset + index * 8 + 4)
      ))
      .ToArray();
  }

  private static ushort[] ReadUInt16Accessor(JsonElement root, byte[] binary, int accessorIndex)
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    var offset = view.GetProperty("byteOffset").GetInt32();
    return Enumerable
      .Range(0, accessor.GetProperty("count").GetInt32())
      .Select(index => BinaryPrimitives.ReadUInt16LittleEndian(binary.AsSpan(offset + index * 2)))
      .ToArray();
  }

  private static byte[] RewriteGlb(byte[] glb, Action<JsonNode, byte[]> rewrite)
  {
    var oldJsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var binaryHeader = 20 + oldJsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(binaryHeader));
    var root = JsonNode.Parse(Encoding.UTF8.GetString(glb, 20, oldJsonLength))!;
    var binary = glb.AsSpan(binaryHeader + 8, binaryLength).ToArray();
    rewrite(root, binary);
    var json = Encoding.UTF8.GetBytes(root.ToJsonString());
    var paddedJsonLength = (json.Length + 3) & ~3;
    var paddedBinaryLength = (binary.Length + 3) & ~3;
    var result = new byte[12 + 8 + paddedJsonLength + 8 + paddedBinaryLength];
    BinaryPrimitives.WriteUInt32LittleEndian(result, 0x46546C67);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), 2);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), paddedJsonLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
    json.CopyTo(result.AsSpan(20));
    result.AsSpan(20 + json.Length, paddedJsonLength - json.Length).Fill(0x20);
    var newBinaryHeader = 20 + paddedJsonLength;
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(newBinaryHeader), paddedBinaryLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(newBinaryHeader + 4), 0x004E4942);
    binary.CopyTo(result.AsSpan(newBinaryHeader + 8));
    return result;
  }

  private static byte[] RewriteGlbExpanded(byte[] glb, Action<JsonNode, List<byte>> rewrite)
  {
    var oldJsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var binaryHeader = 20 + oldJsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(binaryHeader));
    var root = JsonNode.Parse(Encoding.UTF8.GetString(glb, 20, oldJsonLength))!;
    var binary = glb.AsSpan(binaryHeader + 8, binaryLength).ToArray().ToList();
    rewrite(root, binary);
    root["buffers"]![0]!["byteLength"] = binary.Count;
    var json = Encoding.UTF8.GetBytes(root.ToJsonString());
    var paddedJsonLength = (json.Length + 3) & ~3;
    var paddedBinaryLength = (binary.Count + 3) & ~3;
    var result = new byte[12 + 8 + paddedJsonLength + 8 + paddedBinaryLength];
    BinaryPrimitives.WriteUInt32LittleEndian(result, 0x46546C67);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), 2);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), paddedJsonLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
    json.CopyTo(result.AsSpan(20));
    result.AsSpan(20 + json.Length, paddedJsonLength - json.Length).Fill(0x20);
    var newBinaryHeader = 20 + paddedJsonLength;
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(newBinaryHeader), paddedBinaryLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(newBinaryHeader + 4), 0x004E4942);
    binary.ToArray().CopyTo(result, newBinaryHeader + 8);
    return result;
  }

  private static int AppendFloatAccessor(
    JsonNode root,
    List<byte> binary,
    IReadOnlyList<float> values,
    string type,
    int count,
    float? minimum = null,
    float? maximum = null
  )
  {
    while (binary.Count % 4 != 0)
    {
      binary.Add(0);
    }
    var offset = binary.Count;
    foreach (var value in values)
    {
      binary.AddRange(BitConverter.GetBytes(value));
    }
    var views = root["bufferViews"]!.AsArray();
    var viewIndex = views.Count;
    views.Add(
      new JsonObject
      {
        ["buffer"] = 0,
        ["byteOffset"] = offset,
        ["byteLength"] = values.Count * sizeof(float),
      }
    );
    var accessor = new JsonObject
    {
      ["bufferView"] = viewIndex,
      ["componentType"] = 5126,
      ["count"] = count,
      ["type"] = type,
    };
    if (minimum.HasValue)
    {
      accessor["min"] = new JsonArray(minimum.Value);
    }
    if (maximum.HasValue)
    {
      accessor["max"] = new JsonArray(maximum.Value);
    }
    var accessors = root["accessors"]!.AsArray();
    accessors.Add(accessor);
    return accessors.Count - 1;
  }

  private static void WriteVector3(byte[] destination, int offset, Vector3 value)
  {
    WriteSingle(destination, offset, value.X);
    WriteSingle(destination, offset + 4, value.Y);
    WriteSingle(destination, offset + 8, value.Z);
  }

  private static void RewriteQuad(
    JsonNode root,
    byte[] binary,
    int accessorIndex,
    EffectRectangle rectangle,
    float depth
  )
  {
    var accessor = root["accessors"]![accessorIndex]!;
    var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
    var offset = view["byteOffset"]!.GetValue<int>();
    var positions = new[]
    {
      new Vector3(rectangle.Left, rectangle.Bottom, depth),
      new Vector3(rectangle.Right, rectangle.Bottom, depth),
      new Vector3(rectangle.Right, rectangle.Top, depth),
      new Vector3(rectangle.Left, rectangle.Top, depth),
    };
    for (var index = 0; index < positions.Length; index++)
    {
      WriteVector3(binary, offset + index * 12, positions[index]);
    }
    accessor["min"] = new JsonArray(rectangle.Left, rectangle.Bottom, depth);
    accessor["max"] = new JsonArray(rectangle.Right, rectangle.Top, depth);
  }

  private static Vector3 ReadVector3(byte[] source, int offset)
  {
    return new Vector3(
      BitConverter.ToSingle(source, offset),
      BitConverter.ToSingle(source, offset + 4),
      BitConverter.ToSingle(source, offset + 8)
    );
  }

  private static void WriteSingle(byte[] destination, int offset, float value)
  {
    BinaryPrimitives.WriteInt32LittleEndian(
      destination.AsSpan(offset),
      BitConverter.SingleToInt32Bits(value)
    );
  }

  private static byte[] CreateRgbaTex(byte[] pixels)
  {
    pixels.Length.Should().Be(4);
    var result = new byte[24 + pixels.Length];
    "TEX\0\x01\0\0\0"u8.CopyTo(result);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), 0x03000012);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), 0x8888);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(16), 1);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(20), 1);
    pixels.CopyTo(result, 24);
    return result;
  }

  private static string Diagnostics(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return string.Join(
      "; ",
      diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Path}: {diagnostic.Message}")
    );
  }

  private static async Task AssertKhronosValidAsync(string path)
  {
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var startInfo = new ProcessStartInfo(
      "node",
      $"\"{Path.Combine(root, "test-tools", "validate-glb.mjs")}\" \"{path}\""
    )
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
    };
    using var process =
      Process.Start(startInfo) ?? throw new InvalidOperationException("Node did not start.");
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    process.ExitCode.Should().Be(0, $"validator stdout: {output} stderr: {error}");
    output.Should().Contain("\"errors\":0").And.Contain("\"warnings\":0");
  }

  private sealed class ManifestFailingFileSystem : ITransactionalFileSystem
  {
    private int _commitCount;

    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + ".tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      _commitCount++;
      if (_commitCount == 2)
      {
        throw new IOException("Injected manifest commit failure.");
      }
      File.Move(temporaryPath, destinationPath);
    }

    public bool TryDelete(string temporaryPath)
    {
      if (File.Exists(temporaryPath))
      {
        File.Delete(temporaryPath);
      }
      return true;
    }
  }
}
