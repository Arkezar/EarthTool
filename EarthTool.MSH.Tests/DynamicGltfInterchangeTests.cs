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
      new GltfExportOptions(_lineageId, _documentId));

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result.Value!.Fingerprint.Name.Should().Be("dynamic-group-explosion-preview");
    result.Value.Fingerprint.Version.Should().Be(1);
    using var json = ReadGlbJson(destination.ToArray());
    var nodes = json.RootElement.GetProperty("nodes");
    nodes.GetArrayLength().Should().Be(3);
    nodes[0].GetProperty("children").EnumerateArray().Select(item => item.GetInt32())
      .Should().Equal(1, 2);
    nodes[0].TryGetProperty("mesh", out _).Should().BeFalse();
    nodes[1].GetProperty("mesh").GetInt32().Should().Be(0);
    nodes[2].GetProperty("mesh").GetInt32().Should().Be(1);
    json.RootElement.GetProperty("meshes").GetArrayLength().Should().Be(2);
    json.RootElement.GetProperty("images").GetArrayLength().Should().Be(1);
    json.RootElement.GetProperty("images")[0].GetProperty("mimeType").GetString().Should()
      .Be("image/png");
    json.RootElement.GetProperty("images")[0].GetProperty("bufferView").GetInt32()
      .Should().BeGreaterThan(0);
    result.Diagnostics.Select(diagnostic => diagnostic.Code).Should()
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
      new GltfExportOptions(_lineageId, _documentId));
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    package.Position = 0;

    var imported = await interchange.ImportEditDynamicGlbAsync(package, export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported.Value!.Asset.GetSerializedRepresentation().Should()
      .Equal(asset.GetSerializedRepresentation());
    imported.Value.NextBaseline.AssetLineageId.Should().Be(_lineageId);
    imported.Value.NextBaseline.DocumentId.Should().NotBe(_documentId);
    imported.Value.RestoredSerializedRepresentationPaths.Should().Contain("RootDynamicObject");
  }

  [Fact]
  public async Task GroupOnlyExportHasNoSyntheticEffectGeometry()
  {
    var build = DynamicMeshBuilder.Create(
        Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"),
        new MeshAssetLineageId(Guid.Parse("99999999-8888-4777-a666-555555555555")))
      .SetRoot(DynamicEffectRecipes.Group([DynamicEffectRecipes.Group()]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset!,
      destination,
      new GltfExportOptions(_lineageId, _documentId));

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    using var json = ReadGlbJson(destination.ToArray());
    json.RootElement.GetProperty("nodes").GetArrayLength().Should().Be(2);
    json.RootElement.TryGetProperty("meshes", out _).Should().BeFalse();
    json.RootElement.TryGetProperty("materials", out _).Should().BeFalse();
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
      new GltfExportOptions(_lineageId, _documentId));
    var malformed = RewriteGlb(package.ToArray(), (root, _) =>
    {
      var metadataText = root["nodes"]![2]!["extras"]!["earthtool"]!.GetValue<string>();
      var metadata = JsonNode.Parse(metadataText)!;
      metadata["scope"]!["localId"] = 2;
      root["nodes"]![2]!["extras"]!["earthtool"] = metadata.ToJsonString();
    });
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Select(diagnostic => diagnostic.Code).Should()
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
      new GltfExportOptions(_lineageId, _documentId));
    var malformed = RewriteGlb(package.ToArray(), (root, _) =>
      root["nodes"]![2]!["mesh"] = root["nodes"]![1]!["mesh"]!.GetValue<int>());
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Select(diagnostic => diagnostic.Code).Should()
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
      new GltfExportOptions(_lineageId, _documentId));
    var malformed = RewriteGlb(package.ToArray(), (root, _) =>
      root["meshes"]![1]!["primitives"]![0]!["attributes"]!["POSITION"] =
        root["meshes"]![0]!["primitives"]![0]!["attributes"]!["POSITION"]!.GetValue<int>());
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Select(diagnostic => diagnostic.Code).Should()
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
      new GltfExportOptions(_lineageId, _documentId));
    var malformed = RewriteGlb(package.ToArray(), (root, _) =>
    {
      var metadataText = root["nodes"]![1]!["extras"]!["earthtool"]!.GetValue<string>();
      var metadata = JsonNode.Parse(metadataText)!;
      metadata["guards"]!.AsObject().Remove("orderedChildren");
      root["nodes"]![1]!["extras"]!["earthtool"] = metadata.ToJsonString();
    });
    await using var malformedStream = new MemoryStream(malformed);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      malformedStream,
      export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Select(diagnostic => diagnostic.Code).Should()
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
    var expert = MshExpert.CreateDynamic(bytes, canonical.LineageId);
    expert.TryGetValue(out var asset).Should().BeTrue();
    await using var package = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset!,
      package,
      new GltfExportOptions(_lineageId, _documentId));
    package.Position = 0;

    var imported = await interchange.ImportEditDynamicGlbAsync(
      package,
      export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    imported.Value!.Asset.GetSerializedRepresentation().Should().Equal(bytes);
    var extension = imported.Value.Asset.RootDynamicObject.Children[0].Extension;
    extension.LightType.Should().Be(99);
    extension.KnownLightType.Should().BeNull();
    extension.AdditiveFlag.Should().Be(7);
  }

  [Fact]
  public async Task UnsupportedEffectAndObjectLimitFailWithoutOutput()
  {
    var frames = new CanonicalDynamicFrameSequence(0, 1, 0);
    var track = DynamicEffectRecipes.Track(
      frames,
      new EffectRectangle(-1, 1, 1, -1),
      new EffectRectangle(-2, 2, 2, -2),
      "Textures\\fx\\track.tex",
      new CanonicalDynamicAlpha(1, 0, DynamicAlphaTiming.FramePhase),
      false);
    var unsupportedBuild = DynamicMeshBuilder.Create()
      .SetRoot(DynamicEffectRecipes.Group([track]))
      .Build();
    unsupportedBuild.TryGetValue(out var unsupported).Should().BeTrue();
    await using var unsupportedOutput = new MemoryStream();

    var unsupportedResult = await new GltfInterchange().ExportGlbAsync(
      unsupported!,
      unsupportedOutput);
    var limitedResult = await new GltfInterchange().ExportGlbAsync(
      CreateAsset(),
      new MemoryStream(),
      profile: new GltfOperationProfile(
        32 * 1024 * 1024,
        32 * 1024 * 1024,
        4 * 1024 * 1024,
        32,
        65536,
        2,
        15));

    unsupportedResult.Status.Should().Be(OperationStatus.Failed);
    unsupportedResult.Diagnostics.Select(item => item.Code).Should()
      .Contain(GltfDiagnosticCodes.UnsupportedDomain);
    unsupportedOutput.Length.Should().Be(0);
    limitedResult.Status.Should().Be(OperationStatus.Failed);
    limitedResult.Diagnostics.Select(item => item.Code).Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
  }

  [Fact]
  public async Task SeparateManifestFailureRemovesNewDynamicSidecar()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-dynamic-transaction-{Guid.NewGuid():N}");
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
      var asset = CreateAsset();
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(_lineageId, _documentId));
      export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));

      var imported = await interchange.ImportEditDynamicGltfFileAsync(path, export.Value!.Baseline);

      imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
      imported.Value!.Asset.GetSerializedRepresentation().Should()
        .Equal(asset.GetSerializedRepresentation());
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task DynamicPackagesPassKhronosValidation()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-dynamic-validation-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    try
    {
      var glbPath = Path.Combine(directory, "effect.glb");
      var gltfPath = Path.Combine(directory, "effect.gltf");
      var groupPath = Path.Combine(directory, "group.glb");
      var asset = CreateAsset();
      var interchange = new GltfInterchange();
      (await interchange.ExportGlbFileAsync(asset, glbPath)).Status.Should()
        .Be(OperationStatus.Succeeded);
      (await interchange.ExportGltfFileAsync(asset, gltfPath)).Status.Should()
        .Be(OperationStatus.Succeeded);
      var groupBuild = DynamicMeshBuilder.Create()
        .SetRoot(DynamicEffectRecipes.Group([DynamicEffectRecipes.Group()]))
        .Build();
      groupBuild.TryGetValue(out var group).Should().BeTrue();
      (await interchange.ExportGlbFileAsync(group!, groupPath)).Status.Should()
        .Be(OperationStatus.Succeeded);

      await using (var glb = File.OpenRead(glbPath))
      {
        (await interchange.ValidateGlbAsync(glb)).Status.Should().Be(OperationStatus.Succeeded);
      }
      (await interchange.ValidateGltfFileAsync(gltfPath)).Status.Should()
        .Be(OperationStatus.Succeeded);

      await AssertKhronosValidAsync(glbPath);
      await AssertKhronosValidAsync(gltfPath);
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
      new GltfExportOptions(_lineageId, _documentId));
    var edited = RewriteGlb(package.ToArray(), (root, _) =>
    {
      var children = root["nodes"]![0]!["children"]!.AsArray();
      var first = children[0]!.GetValue<int>();
      children[0] = children[1]!.GetValue<int>();
      children[1] = first;
    });
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline);

    imported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(imported.Diagnostics));
    var children = imported.Value!.Asset.RootDynamicObject.Children;
    Encoding.ASCII.GetString(children[0].Extension.TexturePathBytes.ToArray())
      .Should().Be("Textures\\fx\\second.tex");
    Encoding.ASCII.GetString(children[1].Extension.TexturePathBytes.ToArray())
      .Should().Be("Textures\\fx\\first.tex");
    children[1].Extension.AdditiveFlag.Should().Be(1);
    children[1].Extension.EndEffectRectangle.Should().Be(
      asset.RootDynamicObject.Children[0].Extension.EndEffectRectangle);
    imported.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "RootDynamicObject.Children"
      && change.Disposition == PreservationDisposition.Regenerated);
    await using var reexported = new MemoryStream();
    var reexport = await interchange.ExportGlbAsync(
      imported.Value.Asset,
      reexported,
      imported.Value.NextExportOptions);
    reexport.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(reexport.Diagnostics));
    reexport.Diagnostics.Where(item => item.Code == GltfDiagnosticCodes.TextureResourceMissing)
      .Select(item => item.Path).Should().Equal(
        "DynamicObjectScopes[3].Extension.TexturePathBytes",
        "DynamicObjectScopes[2].Extension.TexturePathBytes");
    using var reexportedJson = ReadGlbJson(reexported.ToArray());
    reexportedJson.RootElement.GetProperty("nodes").EnumerateArray()
      .Select(node =>
      {
        var metadata = node.GetProperty("extras").GetProperty("earthtool").GetString()!;
        using var envelope = JsonDocument.Parse(metadata);
        return envelope.RootElement.GetProperty("scope").GetProperty("localId").GetInt32();
      }).Should().Equal(1, 3, 2);
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
      new GltfExportOptions(_lineageId, _documentId));
    var edited = RewriteGlb(package.ToArray(), (root, binary) =>
    {
      root["nodes"]![1]!["translation"] = new JsonArray(10, 20, 30);
      root["materials"]![0]!["pbrMetallicRoughness"]!["baseColorFactor"] =
        new JsonArray(0.9f, 0.8f, 0.7f, 0.6f);
      var accessor = root["accessors"]![0]!;
      var view = root["bufferViews"]![accessor["bufferView"]!.GetValue<int>()]!;
      var offset = view["byteOffset"]!.GetValue<int>();
      var positions = new[]
      {
        new Vector3(-10, -40, 2),
        new Vector3(30, -40, 2),
        new Vector3(30, 20, 2),
        new Vector3(-10, 20, 2)
      };
      for (var index = 0; index < positions.Length; index++)
      {
        WriteVector3(binary, offset + index * 12, positions[index]);
      }
      accessor["min"] = new JsonArray(-10, -40, 2);
      accessor["max"] = new JsonArray(30, 20, 2);
    });
    await using var editedStream = new MemoryStream(edited);

    var imported = await interchange.ImportEditDynamicGlbAsync(
      editedStream,
      export.Value!.Baseline);

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

  private static DynamicMeshAsset CreateAsset()
  {
    var sprite = new CanonicalDynamicSpriteSheet(
      new CanonicalDynamicFrameSequence(2, 3, 4),
      5,
      2);
    var alpha = new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.LifetimeProgress);
    var light = new CanonicalDynamicTerrainLight(
      DynamicLightType.Trapezium,
      new Vector3(0.1f, 0.2f, 0.3f));
    var first = DynamicEffectRecipes.Explosion(
      sprite,
      new CanonicalDynamicEffectShape(
        new EffectRectangle(-1, 2, 3, -4),
        new EffectRectangle(-5, 6, 7, -8),
        0.25f),
      "Textures\\fx\\first.tex",
      new Vector3(0.4f, 0.5f, 0.6f),
      alpha,
      true,
      light)
      .SetChildTranslation(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
    var second = DynamicEffectRecipes.Explosion(
      sprite,
      new CanonicalDynamicEffectShape(
        new EffectRectangle(-2, 3, 4, -5),
        new EffectRectangle(-6, 7, 8, -9),
        0.5f),
      "Textures\\fx\\second.tex",
      new Vector3(0.7f, 0.8f, 0.9f),
      alpha,
      false,
      light);
    var build = DynamicMeshBuilder.Create(
        Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"),
        new MeshAssetLineageId(Guid.Parse("99999999-8888-4777-a666-555555555555")))
      .SetRoot(DynamicEffectRecipes.Group([first, second]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static JsonDocument ReadGlbJson(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return JsonDocument.Parse(glb.AsMemory(20, jsonLength));
  }

  private static byte[] RewriteGlb(
    byte[] glb,
    Action<JsonNode, byte[]> rewrite)
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

  private static void WriteVector3(byte[] destination, int offset, Vector3 value)
  {
    WriteSingle(destination, offset, value.X);
    WriteSingle(destination, offset + 4, value.Y);
    WriteSingle(destination, offset + 8, value.Z);
  }

  private static void WriteSingle(byte[] destination, int offset, float value)
  {
    BinaryPrimitives.WriteInt32LittleEndian(
      destination.AsSpan(offset),
      BitConverter.SingleToInt32Bits(value));
  }

  private static string Diagnostics(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return string.Join("; ", diagnostics.Select(diagnostic =>
      $"{diagnostic.Code} {diagnostic.Path}: {diagnostic.Message}"));
  }

  private static async Task AssertKhronosValidAsync(string path)
  {
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var startInfo = new ProcessStartInfo(
      "node",
      $"\"{Path.Combine(root, "test-tools", "validate-glb.mjs")}\" \"{path}\"")
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };
    using var process = Process.Start(startInfo)
      ?? throw new InvalidOperationException("Node did not start.");
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
