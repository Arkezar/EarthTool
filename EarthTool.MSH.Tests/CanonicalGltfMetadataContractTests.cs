using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;

namespace EarthTool.MSH.Tests;

public sealed class CanonicalGltfMetadataContractTests
{
  [Fact]
  public async Task StaticAndDynamicExportsContainOnlyCanonicalNamedOwnerMetadata()
  {
    var staticAsset = CreateStaticAsset();
    var dynamicAsset = CreateDynamicAsset();
    var interchange = new GltfInterchange();
    await using var staticDestination = new MemoryStream();
    await using var dynamicDestination = new MemoryStream();

    var staticResult = await interchange.ExportGlbAsync(staticAsset, staticDestination);
    var dynamicResult = await interchange.ExportGlbAsync(dynamicAsset, dynamicDestination);

    staticResult.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(staticResult.Diagnostics));
    dynamicResult.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(dynamicResult.Diagnostics));
    AssertCanonicalMetadataOnly(ReadJson(staticDestination.ToArray()));
    AssertCanonicalMetadataOnly(ReadJson(dynamicDestination.ToArray()));
  }

  [Fact]
  public async Task LegacyMetadataOnACanonicalOwnerHasNoAuthoringAuthority()
  {
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(CreateStaticAsset(), exported);
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    var rewritten = RewriteJson(exported.ToArray(), root =>
    {
      RemoveMetadata(root);
      var owner = root["nodes"]!.AsArray()
        .Select(node => node!.AsObject())
        .Single(node => node.ContainsKey("mesh"));
      owner["name"] = "ET_Static_1";
      owner["extras"] = new JsonObject
      {
        ["earthtool"] = "{\"format\":\"earthtool.msh.authoring\",\"version\":1,"
          + "\"values\":{\"role\":{\"viewerFaced\":true,\"barrel\":false,"
          + "\"rotor\":false,\"barrelMaximumAngle\":0}}}",
      };
    });
    await using var source = new MemoryStream(rewritten);

    var result = await interchange.CreateMeshAsync(source);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var asset = result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    asset.RootSourceObject.StaticRenderObjects[0].KnownFlags.Should().Be(
      StaticRenderObjectFlags.None);
  }

  [Fact]
  public async Task MalformedPlacementRootMetadataStillUsesTheMetadataElementLimit()
  {
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(CreateDynamicAsset(), exported);
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    var rewritten = RewriteJson(exported.ToArray(), root =>
    {
      var placementRoot = root["nodes"]![0]!.AsObject();
      placementRoot["extras"]!["earthtoolAuthoring"] = "[1,2,3,4,5,6,7,8,9,10,";
    });
    await using var source = new MemoryStream(rewritten);
    var profile = new GltfOperationProfile(
      maxInputBytes: 1024 * 1024,
      maxOutputBytes: 1024 * 1024,
      maxMetadataBytes: 1024,
      maxJsonDepth: 16,
      maxActiveRenderVertices: 16,
      maxNodes: 16,
      maxHierarchyDepth: 8,
      maxTextureBytes: 1024,
      maxPreviewPixels: 1024,
      maxTextureSearchRoots: 4,
      maxTextureDirectoryEntries: 16,
      maxTotalMetadataBytes: 4096,
      maxMetadataEnvelopes: 16,
      maxMetadataElements: 8,
      maxUnknownMetadataMembers: 16);

    var result = await interchange.CreateMeshAsync(source, profile: profile);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfDiagnosticCodes.MetadataResourceLimitExceeded);
  }

  private static StaticMeshAsset CreateStaticAsset()
  {
    var result = StaticMeshBuilder.Create()
      .SetRenderObject(
        [
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
        ],
        [new CanonicalTriangle(0, 1, 2)])
      .Build();
    result.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateDynamicAsset()
  {
    var result = DynamicMeshBuilder.Create().SetRoot(DynamicEffectRecipes.Group()).Build();
    result.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static JsonObject ReadJson(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return JsonNode.Parse(glb.AsSpan(20, jsonLength))!.AsObject();
  }

  private static void AssertCanonicalMetadataOnly(JsonObject root)
  {
    var json = root.ToJsonString();
    json.Should().Contain("earthtoolAuthoring");
    json.Should().NotContain("\"earthtool\"");
    json.Should().NotContain("sourceMsh");
    json.Should().NotContain("assetLineageId");
    json.Should().NotContain("documentId");
    json.Should().NotContain("nativeProjection");
    json.Should().NotContain("serializedRepresentation");

    var owners = root["nodes"]!.AsArray()
      .Select(node => node!.AsObject())
      .Where(node => node["extras"]?["earthtoolAuthoring"] is not null)
      .ToArray();
    owners.Should().NotBeEmpty();
    owners.All(node =>
      node["name"] is JsonValue name
      && IsCanonicalOwnerName(name.GetValue<string>())).Should().BeTrue();
  }

  private static bool IsCanonicalOwnerName(string name)
  {
    return CanonicalAuthoringOwner.TryParse(name, out _);
  }

  private static void RemoveMetadata(JsonNode node)
  {
    if (node is JsonObject @object)
    {
      @object.Remove("earthtool");
      @object.Remove("earthtoolAuthoring");
      foreach (var child in @object.ToArray())
      {
        if (child.Value is not null)
        {
          RemoveMetadata(child.Value);
        }
      }
    }
    else if (node is JsonArray array)
    {
      foreach (var child in array)
      {
        if (child is not null)
        {
          RemoveMetadata(child);
        }
      }
    }
  }

  private static byte[] RewriteJson(byte[] glb, Action<JsonObject> rewrite)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var root = ReadJson(glb);
    rewrite(root);
    var json = Encoding.UTF8.GetBytes(root.ToJsonString());
    var paddedJsonLength = (json.Length + 3) & ~3;
    var oldBinaryHeader = 20 + jsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(oldBinaryHeader));
    var result = new byte[12 + 8 + paddedJsonLength + 8 + binaryLength];
    BinaryPrimitives.WriteUInt32LittleEndian(result, 0x46546C67);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), 2);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), paddedJsonLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
    json.CopyTo(result.AsSpan(20));
    result.AsSpan(20 + json.Length, paddedJsonLength - json.Length).Fill(0x20);
    glb.AsSpan(oldBinaryHeader, 8 + binaryLength).CopyTo(result.AsSpan(20 + paddedJsonLength));
    return result;
  }

  private static string Diagnostics(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return string.Join(Environment.NewLine, diagnostics.Select(item => item.ToString()));
  }
}
