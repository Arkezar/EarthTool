using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System.Buffers.Binary;
using System.Text;
using System.Text.Json.Nodes;

namespace EarthTool.MSH.Tests;

public sealed class GltfMeshCreationFallbackTests
{
  [Fact]
  public async Task LegacyDynamicSourceAssetIsIgnoredByCanonicalCreation()
  {
    var build = DynamicMeshBuilder.Create().SetRoot(DynamicEffectRecipes.Group()).Build();
    build.TryGetValue(out var sourceAsset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(sourceAsset!, exported);
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    var sourceFree = RewriteJson(exported.ToArray(), root =>
    {
      root["scenes"]![0]!["extras"] = new JsonObject
      {
        ["earthtool"] = "{\"format\":\"earthtool.msh.gltf\",\"version\":2,"
          + "\"payload\":{\"sourceMsh\":\"AA\"}}",
      };
    });
    await using var input = new MemoryStream(sourceFree);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result.Value.Should().BeOfType<DynamicMeshAsset>();
    result.Value!.Origin.Should().Be(MeshAssetOrigin.Canonical);
  }

  private static byte[] RewriteJson(byte[] glb, Action<JsonObject> rewrite)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var root = JsonNode.Parse(glb.AsSpan(20, jsonLength))!.AsObject();
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
    json.CopyTo(result, 20);
    result.AsSpan(20 + json.Length, paddedJsonLength - json.Length).Fill(0x20);
    glb.AsSpan(oldBinaryHeader, 8 + binaryLength).CopyTo(result.AsSpan(20 + paddedJsonLength));
    return result;
  }

  private static string Diagnostics(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return string.Join(
      "; ",
      diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Path}: {diagnostic.Message}")
    );
  }
}
