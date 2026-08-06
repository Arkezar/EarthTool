#nullable enable

using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace EarthTool.MSH.Tests;

public sealed class GltfMeshCreationFallbackTests
{
  [Fact]
  public async Task MalformedMetadataIsDiscardedBeforeCanonicalCreation()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(sourceAsset, exported);
    var malformed = RewriteJson(exported.ToArray(), root =>
      root["scenes"]![0]!["extras"]!["earthtool"] = "{");
    await using var input = new MemoryStream(malformed);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result.Value!.Asset.Should().BeOfType<StaticMeshAsset>();
    result.Value.Asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    result.Value.Preservation.Changes.Should().OnlyContain(change =>
      change.Disposition != PreservationDisposition.Retained);
    result.Diagnostics.Should().ContainSingle().Subject.Should()
      .Match<OperationDiagnostic>(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.MalformedMetadata
        && diagnostic.Severity == DiagnosticSeverity.Warning);
  }

  [Theory]
  [InlineData("missing-scope", GltfDiagnosticCodes.MissingExpectedScope)]
  [InlineData("mixed-lineage", GltfDiagnosticCodes.AssetLineageMismatch)]
  [InlineData("mixed-document", GltfDiagnosticCodes.DocumentMismatch)]
  [InlineData("stale", GltfDiagnosticCodes.StaleNativeProjection)]
  public async Task ConflictingMetadataGraphIsDiscardedAtomically(
    string mutation,
    string expectedCode)
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(sourceAsset, exported);
    var conflicted = RewriteJson(exported.ToArray(), root =>
    {
      var mesh = root["meshes"]![0]!;
      if (mutation == "missing-scope")
      {
        mesh.AsObject().Remove("extras");
        return;
      }

      var envelope = ReadEnvelope(mesh);
      if (mutation == "mixed-lineage")
      {
        envelope["lineage"] = Guid.NewGuid().ToString("D");
      }
      else if (mutation == "mixed-document")
      {
        envelope["document"] = Guid.NewGuid().ToString("D");
      }
      else
      {
        envelope["guards"]!["nativeProjection"]!["digest"] = new string('A', 43);
      }
      WriteEnvelope(mesh, envelope);
    });
    await using var input = new MemoryStream(conflicted);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result.Value!.Asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    result.Value.Preservation.Changes.Should().OnlyContain(change =>
      change.Disposition != PreservationDisposition.Retained);
    result.Diagnostics.Should().ContainSingle().Subject.Should()
      .Match<OperationDiagnostic>(diagnostic =>
        diagnostic.Code == expectedCode
        && diagnostic.Severity == DiagnosticSeverity.Warning);
  }

  [Fact]
  public async Task DynamicMetadataFallbackPrecedesCanonicalConstructionFailure()
  {
    var build = DynamicMeshBuilder.Create()
      .SetRoot(DynamicEffectRecipes.Group())
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(asset!, exported);
    var conflicted = RewriteJson(exported.ToArray(), root =>
    {
      var dynamicObject = root["nodes"]![1]!;
      var envelope = ReadEnvelope(dynamicObject);
      envelope["documentId"] = Guid.NewGuid().ToString("D");
      WriteEnvelope(dynamicObject, envelope);
    });
    await using var input = new MemoryStream(conflicted);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().HaveCount(2);
    result.Diagnostics[0].Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.DocumentMismatch
      && diagnostic.Severity == DiagnosticSeverity.Warning);
    result.Diagnostics[0].Message.Should().Be(
      "EarthTool metadata is unusable and was discarded; mesh asset creation with canonical authored defaults was attempted.");
    result.Diagnostics[1].Severity.Should().Be(DiagnosticSeverity.Error);
  }

  [Fact]
  public async Task InvalidPreservedDynamicMshIsTreatedAsUnusableMetadata()
  {
    var build = DynamicMeshBuilder.Create()
      .SetRoot(DynamicEffectRecipes.Group())
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(asset!, exported);
    var malformed = RewriteJson(exported.ToArray(), root =>
    {
      var manifest = ReadEnvelope(root["scenes"]![0]!);
      manifest["payload"]!["sourceMsh"] = EncodeBase64Url([0]);
      WriteEnvelope(root["scenes"]![0]!, manifest);
    });
    await using var input = new MemoryStream(malformed);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().HaveCount(2);
    result.Diagnostics[0].Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.MalformedMetadata
      && diagnostic.Severity == DiagnosticSeverity.Warning);
    result.Diagnostics[1].Severity.Should().Be(DiagnosticSeverity.Error);
  }

  [Fact]
  public async Task UnsupportedDynamicMetadataProjectionFallsBackBeforeConstructionFailure()
  {
    var build = DynamicMeshBuilder.Create()
      .SetRoot(DynamicEffectRecipes.Group())
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(asset!, exported);
    var unsupported = RewriteJson(exported.ToArray(), root =>
    {
      var manifest = ReadEnvelope(root["scenes"]![0]!);
      manifest["payload"]!["nativeProjection"]!["version"] = 0;
      WriteEnvelope(root["scenes"]![0]!, manifest);
    });
    await using var input = new MemoryStream(unsupported);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().HaveCount(2);
    result.Diagnostics[0].Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.UnsupportedGuard
      && diagnostic.Severity == DiagnosticSeverity.Warning);
    result.Diagnostics[1].Severity.Should().Be(DiagnosticSeverity.Error);
  }

  [Fact]
  public async Task DiscardedMetadataCannotSupplySerializedMeshState()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var alternateBuild = StaticMeshBuilder.Create()
      .SetRenderObject(
        [
          new CanonicalStaticVertex(new Vector3(10, 0, 0), Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(new Vector3(11, 0, 0), Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(new Vector3(10, 1, 0), Vector3.UnitZ, Vector2.UnitY)
        ],
        [new CanonicalTriangle(0, 1, 2)])
      .Build();
    alternateBuild.TryGetValue(out var alternateAsset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(sourceAsset, exported);
    var conflicted = RewriteJson(exported.ToArray(), root =>
    {
      var manifest = ReadEnvelope(root["scenes"]![0]!);
      manifest["payload"]!["asset"]!["sourceMsh"] = EncodeBase64Url(
        alternateAsset!.GetSerializedRepresentation());
      WriteEnvelope(root["scenes"]![0]!, manifest);
      root["meshes"]![0]!.AsObject().Remove("extras");
    });
    await using var input = new MemoryStream(conflicted);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var created = result.Value!.Asset.Should().BeOfType<StaticMeshAsset>().Subject;
    created.Origin.Should().Be(MeshAssetOrigin.Canonical);
    created.StaticRenderObjectSequence[0].RenderVertices.Select(vertex => vertex.Position)
      .Should().Equal(
        sourceAsset.StaticRenderObjectSequence[0].RenderVertices.Select(vertex => vertex.Position));
    created.StaticRenderObjectSequence[0].RenderVertices.Select(vertex => vertex.Position)
      .Should().NotEqual(
        alternateAsset!.StaticRenderObjectSequence[0].RenderVertices.Select(vertex => vertex.Position));
  }

  [Fact]
  public async Task StructurallyUnsafeCanonicalCreationFailsAfterFallbackWarning()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(sourceAsset, exported);
    var bytes = exported.ToArray();
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(GetBinaryChunkOffset(bytes)),
      BitConverter.SingleToInt32Bits(256f));
    var unsafeInput = RewriteJson(bytes, root =>
    {
      root["scenes"]![0]!["extras"]!["earthtool"] = "{";
      root["accessors"]![0]!["max"]![0] = 256;
    });
    await using var input = new MemoryStream(unsafeInput);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().HaveCount(2);
    result.Diagnostics[0].Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.MalformedMetadata
      && diagnostic.Severity == DiagnosticSeverity.Warning);
    result.Diagnostics[1].Severity.Should().Be(DiagnosticSeverity.Error);
  }

  [Fact]
  public async Task SeparateGltfDiscardsMalformedMetadataBeforeCanonicalCreation()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      await interchange.ExportGltfFileAsync(sourceAsset, path);
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      root["scenes"]![0]!["extras"]!["earthtool"] = "{";
      await File.WriteAllTextAsync(path, root.ToJsonString());

      var result = await interchange.CreateMeshFileAsync(path);

      result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
      result.Value!.Asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
      result.Diagnostics.Should().ContainSingle().Subject.Should()
        .Match<OperationDiagnostic>(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.MalformedMetadata
          && diagnostic.Severity == DiagnosticSeverity.Warning);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  private static async Task<StaticMeshAsset> ReadAssetAsync(byte[] source)
  {
    await using var stream = new MemoryStream(source);
    var result = await new MshReader().ReadAsync(stream);
    return result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
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

  private static JsonObject ReadEnvelope(JsonNode carrier)
  {
    return JsonNode.Parse(carrier["extras"]!["earthtool"]!.GetValue<string>())!.AsObject();
  }

  private static int GetBinaryChunkOffset(byte[] glb)
  {
    return 28 + BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
  }

  private static void WriteEnvelope(JsonNode carrier, JsonObject envelope)
  {
    carrier["extras"]!["earthtool"] = envelope.ToJsonString();
  }

  private static string EncodeBase64Url(byte[] value)
  {
    return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
  }

  private static string Diagnostics(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return string.Join(
      "; ",
      diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Path}: {diagnostic.Message}"));
  }
}
