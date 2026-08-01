using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EarthTool.MSH.Tests;

public class GltfWalkingSkeletonTests
{
  private static readonly Guid LineageId = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
  private static readonly Guid DocumentId = new("11111111-2222-3333-4444-555555555555");

  [Fact]
  public async Task StaticSourceObjectsAndMaterialPartitionsExportAsNativeHierarchy()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var asset = await ReadAssetAsync(fixture.Data);
    await using var glb = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    using var json = ReadGlbJson(glb.ToArray());
    var root = json.RootElement;
    root.GetProperty("scenes")[0].GetProperty("nodes").EnumerateArray()
      .Select(node => node.GetInt32()).Should().Equal(0);
    root.GetProperty("nodes").GetArrayLength().Should().Be(3);
    root.GetProperty("meshes").GetArrayLength().Should().Be(3);
    root.GetProperty("nodes")[0].GetProperty("children").EnumerateArray()
      .Select(node => node.GetInt32()).Should().Equal(1, 2);
    root.GetProperty("nodes")[0].TryGetProperty("translation", out _).Should().BeFalse();
    root.GetProperty("nodes")[1].GetProperty("translation").EnumerateArray()
      .Select(value => value.GetSingle()).Should().Equal(1, 3, -2);
    root.GetProperty("nodes")[2].GetProperty("translation").EnumerateArray()
      .Select(value => value.GetSingle()).Should().Equal(7, 9, -8);
    root.GetProperty("meshes")[0].GetProperty("primitives").GetArrayLength().Should().Be(2);
    root.GetProperty("meshes")[1].GetProperty("primitives").GetArrayLength().Should().Be(1);
    root.GetProperty("meshes")[2].GetProperty("primitives").GetArrayLength().Should().Be(1);
  }

  [Fact]
  public async Task LoadedTextureBindingsExportAsUnlitMaterialsWithoutUsingDisplayNamesAsIdentity()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      var root = json.RootElement;
      root.GetProperty("extensionsUsed").EnumerateArray()
        .Select(value => value.GetString()).Should().Contain("KHR_materials_unlit");
      var materials = root.GetProperty("materials");
      materials.GetArrayLength().Should().Be(asset.StaticRenderObjectSequence.Count);
      for (var index = 0; index < materials.GetArrayLength(); index++)
      {
        var material = materials[index];
        material.GetProperty("extensions").TryGetProperty("KHR_materials_unlit", out _)
          .Should().BeTrue();
        var metadata = JsonDocument.Parse(
          material.GetProperty("extras").GetProperty("earthtool").GetString()!);
        var localId = metadata.RootElement.GetProperty("scope").GetProperty("localId").GetInt32();
        Convert.FromBase64String(metadata.RootElement.GetProperty("textureBinding").GetString()!)
          .Should().Equal(asset.StaticRenderObjectSequence.Single(record =>
            record.LocalId == localId).TexturePathBytes);
      }
    }

    var renamed = RewriteJson(glb.ToArray(), root =>
    {
      var materials = root["materials"]!.AsArray();
      for (var index = 0; index < materials.Count; index++)
      {
        materials[index]!["name"] = $"unrelated preview {materials.Count - index}";
      }
    });
    await using var edited = new MemoryStream(renamed);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence
      .Select(record => record.TexturePathBytes.ToArray()).Should()
      .BeEquivalentTo(
        asset.StaticRenderObjectSequence.Select(record => record.TexturePathBytes.ToArray()),
        options => options.WithStrictOrdering());
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].TexturePathBytes"
      && change.Disposition == PreservationDisposition.Retained);
  }

  [Theory]
  [InlineData("")]
  [InlineData("Textures\\authored\\replacement.tex")]
  public async Task ExplicitMaterialBindingEditRegeneratesOnlyTheAssignedTexKey(string replacement)
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var editedBytes = RewriteJson(glb.ToArray(), root =>
    {
      var material = root["materials"]![0]!.AsObject();
      var metadata = JsonNode.Parse(material["extras"]!["earthtool"]!.GetValue<string>())!.AsObject();
      metadata["textureBinding"] = Convert.ToBase64String(Encoding.ASCII.GetBytes(replacement));
      material["extras"]!["earthtool"] = metadata.ToJsonString();
    });
    await using var edited = new MemoryStream(editedBytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var changed = import.Value!.Asset.StaticRenderObjectSequence.Single(record => record.LocalId == 1);
    changed.TexturePathBytes.Should().Equal(Encoding.ASCII.GetBytes(replacement));
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].TexturePathBytes"
      && change.Disposition == PreservationDisposition.Regenerated);
    for (var index = 1; index < asset.StaticRenderObjectSequence.Count; index++)
    {
      import.Value.Asset.StaticRenderObjectSequence[index].GetSerializedRepresentation().Should()
        .Equal(asset.StaticRenderObjectSequence[index].GetSerializedRepresentation());
    }
  }

  [Fact]
  public async Task MaterialSharingForkingAndReassignmentCopyOnlyTheExplicitBinding()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var editedBytes = RewriteJson(glb.ToArray(), root =>
    {
      var materials = root["materials"]!.AsArray();
      var fork = materials[1]!.DeepClone().AsObject();
      fork["name"] = "forked display material";
      materials.Add(fork);
      root["meshes"]![0]!["primitives"]![0]!["material"] = materials.Count - 1;
    });
    await using var edited = new MemoryStream(editedBytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var records = import.Value!.Asset.StaticRenderObjectSequence;
    records.Single(record => record.LocalId == 1).TexturePathBytes.Should()
      .Equal(records.Single(record => record.LocalId == 3).TexturePathBytes);
    records.Single(record => record.LocalId == 2).TexturePathBytes.Should()
      .Equal(asset.StaticRenderObjectSequence.Single(record => record.LocalId == 2).TexturePathBytes);
    import.Value.Preservation.Changes.Count(change =>
      change.FieldPath.EndsWith(".TexturePathBytes", StringComparison.Ordinal)
      && change.Disposition == PreservationDisposition.Regenerated).Should().Be(1);
  }

  [Fact]
  public async Task ReassignmentCanReuseExactLoadedLegacyBindingBytesWithoutCanonicalizingThem()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var legacyBinding = "Legacy??\\root-a.tex"u8.ToArray();
    legacyBinding.Length.Should().Be(19);
    legacyBinding.CopyTo(fixture.Data, fixture.RecordOffsets[0] + 8 + 0xA0 + 8);
    var asset = await ReadAssetAsync(fixture.Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var editedBytes = RewriteJson(glb.ToArray(), root =>
    {
      root["meshes"]![0]!["primitives"]![1]!["material"] = 0;
    });
    await using var edited = new MemoryStream(editedBytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence.Single(record => record.LocalId == 3)
      .TexturePathBytes.Should().Equal(legacyBinding);
  }

  [Fact]
  public async Task PrimitiveReorderingKeepsEachMaterialBindingWithItsSemanticPartition()
  {
    var source = CreateTwoPartitionAsset();
    var bindingEdit = source.Edit();
    bindingEdit.SetTextureResourceBinding(
      source.StaticRenderObjectSequence[0].Id,
      "Textures\\authored\\first.tex");
    bindingEdit.SetTextureResourceBinding(
      source.StaticRenderObjectSequence[1].Id,
      "Textures\\authored\\second.tex");
    var committed = bindingEdit.Commit();
    committed.TryGetValue(out var boundAsset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      boundAsset!,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var reordered = RewriteJson(glb.ToArray(), root =>
    {
      var primitives = root["meshes"]![0]!["primitives"]!.AsArray();
      primitives.Insert(0, primitives[1]!.DeepClone());
      primitives.RemoveAt(2);
    });
    await using var edited = new MemoryStream(reordered);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence.Single(record => record.LocalId == 1)
      .TexturePathBytes.Should().Equal("Textures\\authored\\first.tex"u8.ToArray());
    import.Value.Asset.StaticRenderObjectSequence.Single(record => record.LocalId == 2)
      .TexturePathBytes.Should().Equal("Textures\\authored\\second.tex"u8.ToArray());
  }

  [Fact]
  public async Task ExplicitTexRootProducesDeterministicEmbeddedUnlitPngPreview()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "root-a.tex"),
      CreateRgbaTex(2, 1, [0xFF, 0, 0, 0xFF, 0, 0, 0xFF, 0xFF]));
    try
    {
      var options = new GltfExportOptions(LineageId, DocumentId, [directory]);
      var interchange = new GltfInterchange();
      await using var first = new MemoryStream();
      await using var second = new MemoryStream();

      var firstResult = await interchange.ExportGlbAsync(asset, first, options);
      var secondResult = await interchange.ExportGlbAsync(asset, second, options);

      firstResult.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", firstResult.Diagnostics.Select(diagnostic => diagnostic.Message)));
      secondResult.Status.Should().Be(OperationStatus.Succeeded);
      second.ToArray().Should().Equal(first.ToArray());
      firstResult.Diagnostics.Should().Contain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing
        && diagnostic.Severity == DiagnosticSeverity.Warning);
      using var json = ReadGlbJson(first.ToArray());
      var root = json.RootElement;
      root.GetProperty("images").GetArrayLength().Should().Be(1);
      root.GetProperty("images")[0].GetProperty("mimeType").GetString().Should().Be("image/png");
      root.GetProperty("textures").GetArrayLength().Should().Be(1);
      root.GetProperty("materials")[0].GetProperty("pbrMetallicRoughness")
        .GetProperty("baseColorTexture").GetProperty("index").GetInt32().Should().Be(0);
      await using var withoutPreview = new MemoryStream();
      await interchange.ExportGlbAsync(
        asset,
        withoutPreview,
        new GltfExportOptions(LineageId, DocumentId));
      var imageBufferView = root.GetProperty("images")[0].GetProperty("bufferView").GetInt32();
      var pngLength = root.GetProperty("bufferViews")[imageBufferView]
        .GetProperty("byteLength").GetInt32();
      await using var constrained = new MemoryStream();
      var constrainedResult = await interchange.ExportGlbAsync(
        asset,
        constrained,
        options,
        new GltfOperationProfile(
          maxInputBytes: 32 * 1024 * 1024,
          maxOutputBytes: withoutPreview.ToArray().Length + pngLength,
          maxMetadataBytes: 4 * 1024 * 1024,
          maxJsonDepth: 32,
          maxActiveRenderVertices: 65536,
          maxNodes: 4096,
          maxHierarchyDepth: 15,
          maxTextureBytes: 1024,
          maxPreviewPixels: 16));
      constrainedResult.Status.Should().Be(OperationStatus.Succeeded);
      constrainedResult.Diagnostics.Should().Contain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable);
      using var constrainedJson = ReadGlbJson(constrained.ToArray());
      constrainedJson.RootElement.TryGetProperty("images", out _).Should().BeFalse();
      var metadataFreePreview = RewriteJson(first.ToArray(), RemoveEarthToolMetadata);
      await using var genericSource = new MemoryStream(metadataFreePreview);
      var genericImport = await new GltfInterchange().ImportNewModelGlbAsync(genericSource);
      genericImport.Status.Should().Be(OperationStatus.Failed);
      genericImport.Diagnostics.Should().ContainSingle().Subject.Data.Should()
        .Contain(new KeyValuePair<string, string>("domain", "TexResourceBinding"));
      var path = Path.Combine(directory, "preview.glb");
      await File.WriteAllBytesAsync(path, first.ToArray());
      await AssertKhronosValidAsync(path);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task PreviewPixelLimitKeepsTheExplicitBindingAsAReferenceOnlyMaterial()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "root-a.tex"),
      CreateRgbaTex(2, 1, [0xFF, 0, 0, 0xFF, 0, 0, 0xFF, 0xFF]));
    try
    {
      await using var glb = new MemoryStream();
      var result = await new GltfInterchange().ExportGlbAsync(
        asset,
        glb,
        new GltfExportOptions(LineageId, DocumentId, [directory]),
        new GltfOperationProfile(
          maxInputBytes: 32 * 1024 * 1024,
          maxOutputBytes: 32 * 1024 * 1024,
          maxMetadataBytes: 4 * 1024 * 1024,
          maxJsonDepth: 32,
          maxActiveRenderVertices: 65536,
          maxNodes: 4096,
          maxHierarchyDepth: 15,
          maxTextureBytes: 1024,
          maxPreviewPixels: 1));

      result.Status.Should().Be(OperationStatus.Succeeded);
      result.Diagnostics.Should().Contain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable);
      using var json = ReadGlbJson(glb.ToArray());
      json.RootElement.TryGetProperty("images", out _).Should().BeFalse();
      var metadata = JsonDocument.Parse(json.RootElement.GetProperty("materials")[0]
        .GetProperty("extras").GetProperty("earthtool").GetString()!);
      Convert.FromBase64String(metadata.RootElement.GetProperty("textureBinding").GetString()!)
        .Should().Equal("Textures\\root-a.tex"u8.ToArray());
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task NewModelImportAuthorsCanonicalAssetAndUsableFirstMetadataBaseline()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    var result = imported.Value!;
    result.Asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    result.Asset.ArchiveFraming.Declaration.Should().Be(0x20D0A1FF);
    result.Asset.ArchiveFraming.CreationGuid.Should().NotBeNull().And.NotBe(Guid.Empty);
    result.Baseline.AssetLineageId.Should().Be(result.Asset.LineageId.Value);
    result.Baseline.DocumentId.Should().NotBe(Guid.Empty);
    result.Asset.CommonBaseHeader.BoxPresenceMask.Should().Be(0x00008000);
    result.Asset.StaticRenderObjectSequence.Should().ContainSingle();
    result.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "CommonBaseHeader.Footprint"
      && change.Disposition == PreservationDisposition.Canonicalized);

    await using var baseline = new MemoryStream();
    var firstBaseline = await interchange.ExportGlbAsync(
      result.Asset,
      baseline,
      new GltfExportOptions(result.Baseline.AssetLineageId, result.Baseline.DocumentId));
    baseline.Position = 0;
    var editImport = await interchange.ImportEditGlbAsync(baseline, firstBaseline.Value!.Baseline);
    editImport.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public async Task NewModelImportAcceptsOnlyExplicitCanonicalTexResourceBindings()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var canonicalSource = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(
      canonicalSource,
      options: new GltfNewModelImportOptions(new Dictionary<int, string?>
      {
        [0] = "Textures\\authored\\hull.tex"
      }));

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    imported.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle().Subject
      .TexturePathBytes.Should().Equal("Textures\\authored\\hull.tex"u8.ToArray());

    await using var unsafeSource = new MemoryStream(metadataFree);
    var rejected = await interchange.ImportNewModelGlbAsync(
      unsafeSource,
      options: new GltfNewModelImportOptions(new Dictionary<int, string?>
      {
        [0] = "..\\outside.tex"
      }));
    rejected.Status.Should().Be(OperationStatus.Failed);
    rejected.Value.Should().BeNull();
    rejected.Diagnostics.Should().ContainSingle().Subject.Data.Should()
      .Contain(new KeyValuePair<string, string>("domain", "TexResourceBinding"));
  }

  [Fact]
  public async Task EquivalentMetadataFreeGlbAndSeparateGltfAuthorEquivalentCanonicalAssets()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exportedGlb = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exportedGlb,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFreeGlb = RewriteJson(exportedGlb.ToArray(), RemoveEarthToolMetadata);
    await using var glbSource = new MemoryStream(metadataFreeGlb);
    var glbImport = await interchange.ImportNewModelGlbAsync(glbSource);

    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var separateExport = await interchange.ExportGltfFileAsync(
        sourceAsset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      separateExport.Status.Should().Be(OperationStatus.Succeeded);
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveEarthToolMetadata(root);
      await File.WriteAllTextAsync(path, root.ToJsonString());

      var separateImport = await interchange.ImportNewModelGltfFileAsync(path);

      glbImport.Status.Should().Be(OperationStatus.Succeeded);
      separateImport.Status.Should().Be(OperationStatus.Succeeded);
      var glbBytes = glbImport.Value!.Asset.GetSerializedRepresentation();
      var separateBytes = separateImport.Value!.Asset.GetSerializedRepresentation();
      glbBytes.AsSpan(4, 16).Clear();
      separateBytes.AsSpan(4, 16).Clear();
      separateBytes.Should().Equal(glbBytes);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task NewModelImportCollapsesGroupsAndPreservesCanonicalHierarchyAndPartitionOrder()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      var nodes = root["nodes"]!.AsArray();
      var meshNode = nodes[0]!.AsObject();
      meshNode["translation"] = new JsonArray(2, 3, 4);
      meshNode["children"] = new JsonArray(2, 3);
      nodes.Insert(0, new JsonObject
      {
        ["scale"] = new JsonArray(-1, 1, 1),
        ["children"] = new JsonArray(1)
      });
      nodes.Add(new JsonObject
      {
        ["mesh"] = 0,
        ["translation"] = new JsonArray(5, 0, 0),
        ["scale"] = new JsonArray(10, 10, 10)
      });
      nodes.Add(new JsonObject
      {
        ["mesh"] = 0,
        ["translation"] = new JsonArray(6, 0, 0)
      });
      root["scenes"]![0]!["nodes"] = new JsonArray(0);
      var primitives = root["meshes"]![0]!["primitives"]!.AsArray();
      primitives.Add(primitives[0]!.DeepClone());
    });
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    var asset = imported.Value!.Asset;
    asset.RootSourceObject.Children.Should().HaveCount(2);
    asset.RootSourceObject.Children.Select(child => child.StaticRenderObjectIds.Count).Should().Equal(2, 2);
    asset.StaticRenderObjectSequence.Select(record => record.SourceObjectId.Value).Should()
      .Equal(1, 2, 2, 3, 3, 1);
    asset.StaticRenderObjectSequence[0].Pivot.Should().Be(new Vector3(-2, -4, 3));
    asset.RootSourceObject.Children.Select(child =>
      asset.StaticRenderObjectSequence.Single(record =>
        record.Id.Equals(child.StaticRenderObjectIds[0])).Pivot.X).Should().Equal(-5, -6);
    var rootTriangle = asset.StaticRenderObjectSequence[0].Triangles.Should().ContainSingle().Subject;
    (rootTriangle.Vertex0, rootTriangle.Vertex1, rootTriangle.Vertex2).Should().Be((0, 2, 1));

    var rootVertices = asset.RootSourceObject.StaticRenderObjectIds
      .SelectMany(id => asset.StaticRenderObjectSequence.Single(record => record.Id.Equals(id)).RenderVertices)
      .ToArray();
    BinaryPrimitives.ReadUInt16LittleEndian(
      asset.CommonBaseHeader.HorizontalExtents.Skip(4).Take(2).ToArray()).Should().Be(
      ToUnsignedFixedPoint(Math.Max(0, rootVertices.Max(vertex => vertex.Position.X))));
    BinaryPrimitives.ReadUInt16LittleEndian(
      asset.CommonBaseHeader.HorizontalExtents.Skip(6).Take(2).ToArray()).Should().Be(
      ToUnsignedFixedPoint(-Math.Min(0, rootVertices.Min(vertex => vertex.Position.X))));
    BinaryPrimitives.ReadUInt16LittleEndian(asset.CommonBaseHeader.BoxTopElevations.Take(2).ToArray())
      .Should().Be(ToUnsignedFixedPoint(rootVertices.Max(vertex => vertex.Position.Z)));
  }

  [Fact]
  public async Task NewModelImportRejectsClaimedEarthToolLineage()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var claimed = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      claimed,
      new GltfExportOptions(LineageId, DocumentId));
    claimed.Position = 0;

    var imported = await interchange.ImportNewModelGlbAsync(claimed);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.MisplacedMetadata);
  }

  [Theory]
  [InlineData("ambiguous-root")]
  [InlineData("singular-transform")]
  [InlineData("normal-overflow")]
  [InlineData("unsupported-material")]
  [InlineData("invalid-index")]
  public async Task NewModelImportRejectsAmbiguousLossyUnsupportedAndUnsafeInput(string mutation)
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      if (mutation == "ambiguous-root")
      {
        var nodes = root["nodes"]!.AsArray();
        nodes.Add(nodes[0]!.DeepClone());
        nodes.Insert(0, new JsonObject { ["children"] = new JsonArray(1, 2) });
        root["scenes"]![0]!["nodes"] = new JsonArray(0);
      }
      else if (mutation == "singular-transform")
      {
        root["nodes"]![0]!["scale"] = new JsonArray(0, 1, 1);
      }
      else if (mutation == "normal-overflow")
      {
        root["nodes"]![0]!["scale"] = new JsonArray(1e-30, 1, 1);
      }
      else if (mutation == "unsupported-material")
      {
        root["materials"] = new JsonArray(new JsonObject());
        root["meshes"]![0]!["primitives"]![0]!["material"] = 0;
      }
    });
    if (mutation == "invalid-index")
    {
      BinaryPrimitives.WriteUInt16LittleEndian(
        metadataFree.AsSpan(GetBinaryChunkOffset(metadataFree) + 96),
        3);
    }
    else if (mutation == "normal-overflow")
    {
      var binaryOffset = GetBinaryChunkOffset(metadataFree);
      for (var normalOffset = 36; normalOffset <= 60; normalOffset += 12)
      {
        BinaryPrimitives.WriteInt32LittleEndian(
          metadataFree.AsSpan(binaryOffset + normalOffset),
          BitConverter.SingleToInt32Bits(1));
        BinaryPrimitives.WriteInt32LittleEndian(
          metadataFree.AsSpan(binaryOffset + normalOffset + 8),
          0);
      }
    }
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Should().ContainSingle();
  }

  [Fact]
  public async Task NewModelImportPreservesStructuredOutputLimitDiagnostic()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(
      source,
      profile: new GltfOperationProfile(maxOutputBytes: 1));

    imported.Status.Should().Be(OperationStatus.Failed);
    var diagnostic = imported.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(GltfDiagnosticCodes.ResourceLimitExceeded);
    diagnostic.Path.Should().Be("$");
    diagnostic.Data.Should().ContainKeys("actual", "maximum");
  }

  [Fact]
  public async Task NewModelImportEnforcesConfiguredHierarchyDepthBeforeConversion()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      var nodes = root["nodes"]!.AsArray();
      nodes.Insert(0, new JsonObject { ["children"] = new JsonArray(1) });
      root["scenes"]![0]!["nodes"] = new JsonArray(0);
    });
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(
      source,
      profile: new GltfOperationProfile(
        maxInputBytes: 32 * 1024 * 1024,
        maxOutputBytes: 32 * 1024 * 1024,
        maxMetadataBytes: 4 * 1024 * 1024,
        maxJsonDepth: 32,
        maxActiveRenderVertices: 65536,
        maxNodes: 4096,
        maxHierarchyDepth: 1));

    imported.Status.Should().Be(OperationStatus.Failed);
    var diagnostic = imported.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(GltfDiagnosticCodes.ResourceLimitExceeded);
    diagnostic.Data.Should().Contain(new KeyValuePair<string, string>("actual", "2"));
    diagnostic.Data.Should().Contain(new KeyValuePair<string, string>("maximum", "1"));
  }

  [Fact]
  public async Task NewModelImportRejectsOversizedIndexDeclarationBeforeMaterialization()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      root["accessors"]![3]!["count"] = 3_145_731;
    });
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    var diagnostic = imported.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(GltfDiagnosticCodes.ResourceLimitExceeded);
    diagnostic.Data.Should().Contain(
      new KeyValuePair<string, string>("actual", "1048577"));
  }

  [Fact]
  public async Task UnchangedMultiPartitionGlbImportRestoresExactMshTopologyAndState()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var asset = await ReadAssetAsync(fixture.Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    glb.Position = 0;

    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    import.Value!.RestoredSerializedRepresentationPaths.Should().Contain(
      "StaticRenderObjectSequence[3]");
    await using var restored = new MemoryStream();
    var write = await new MshWriter().WriteAsync(import.Value.Asset, restored);
    write.Status.Should().Be(OperationStatus.Succeeded);
    restored.ToArray().Should().Equal(fixture.Data);
  }

  [Fact]
  public async Task BufferOnlySeparateGltfPackageValidatesAndRestoresExactMshState()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var asset = await ReadAssetAsync(fixture.Data);
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));

      export.Status.Should().Be(OperationStatus.Succeeded);
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(path));
      var buffers = json.RootElement.GetProperty("buffers");
      buffers.GetArrayLength().Should().Be(1);
      var bufferUri = buffers[0].GetProperty("uri").GetString();
      bufferUri.Should().MatchRegex("^[0-9a-f]{64}\\.bin$");
      var bufferPath = Path.Combine(directory, bufferUri!);
      File.Exists(bufferPath).Should().BeTrue();

      var validation = await interchange.ValidateGltfFileAsync(path);
      validation.Status.Should().Be(OperationStatus.Succeeded);

      var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);
      import.Status.Should().Be(OperationStatus.Succeeded);
      await using var restored = new MemoryStream();
      var write = await new MshWriter().WriteAsync(import.Value!.Asset, restored);
      write.Status.Should().Be(OperationStatus.Succeeded);
      restored.ToArray().Should().Equal(fixture.Data);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task ExportEnforcesFiniteGeometryAndActiveRenderVertexLimit()
  {
    var source = OneTriangleMshFixture.Create();
    var recordOffset = 0x14 + 0x368 + sizeof(uint);
    BinaryPrimitives.WriteInt32LittleEndian(
      source.AsSpan(recordOffset + 0x08),
      BitConverter.SingleToInt32Bits(float.NaN));
    var nonFinite = await ReadAssetAsync(source);
    await using var destination = new MemoryStream();

    var invalid = await new GltfInterchange().ExportGlbAsync(nonFinite, destination);
    var limited = await new GltfInterchange().ExportGlbAsync(
      await ReadAssetAsync(OneTriangleMshFixture.Create()),
      destination,
      profile: new GltfOperationProfile(maxActiveRenderVertices: 2));

    invalid.Status.Should().Be(OperationStatus.Failed);
    invalid.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.InvalidGeometry);
    destination.Length.Should().Be(0);
    limited.Status.Should().Be(OperationStatus.Failed);
    limited.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
  }

  [Fact]
  public async Task EditImportIgnoresIndexWidthAndVertexNumberRepacking()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      var json = await File.ReadAllTextAsync(path);
      json = json.Replace("\"componentType\":5123", "\"componentType\":5121", StringComparison.Ordinal);
      await File.WriteAllTextAsync(path, json);
      using var document = JsonDocument.Parse(json);
      var bufferPath = Path.Combine(
        directory,
        document.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!);
      var binary = await File.ReadAllBytesAsync(bufferPath);
      SwapBlocks(binary, 0, 24, 12);
      SwapBlocks(binary, 36, 60, 12);
      SwapBlocks(binary, 72, 88, 8);
      binary[96] = 2;
      binary[97] = 1;
      binary[98] = 0;
      await File.WriteAllBytesAsync(bufferPath, binary);

      var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);

      import.Status.Should().Be(OperationStatus.Succeeded);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task EditImportAcceptsNonIndexedTriangleList()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), "\"indices\":3,", string.Empty);
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    result.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
      .Subject.Triangles.Should().ContainSingle().Which.Should().Be(
        asset.StaticRenderObjectSequence[0].Triangles[0]);
  }

  [Fact]
  public async Task ExportUsesUnsignedIntIndicesForMaximumVertexIndex()
  {
    var vertices = Enumerable.Range(0, 65536)
      .Select(_ => new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero))
      .ToArray();
    var build = StaticMeshBuilder.Create(
        OneTriangleMshFixture.CreationGuid,
        new MeshAssetLineageId(Guid.Parse("99999999-aaaa-bbbb-cccc-dddddddddddd")))
      .SetRenderObject(vertices, [new CanonicalTriangle(0, 1, ushort.MaxValue)])
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    await using var glb = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset!,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    using var json = ReadGlbJson(glb.ToArray());
    json.RootElement.GetProperty("accessors")[3].GetProperty("componentType").GetInt32()
      .Should().Be(5125);
    glb.Position = 0;
    var validation = await new GltfInterchange().ValidateGlbAsync(glb);
    validation.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public async Task EditImportIgnoresTriangleOrderButRetainsWinding()
  {
    var asset = CreateTwoTriangleAsset();
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(path));
      var bufferPath = Path.Combine(
        directory,
        json.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!);
      var original = await File.ReadAllBytesAsync(bufferPath);
      var reordered = (byte[])original.Clone();
      original.AsSpan(128, 6).CopyTo(reordered.AsSpan(134, 6));
      original.AsSpan(134, 6).CopyTo(reordered.AsSpan(128, 6));
      await File.WriteAllBytesAsync(bufferPath, reordered);

      var reorderedImport = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);

      reorderedImport.Status.Should().Be(OperationStatus.Succeeded);

      var reversed = (byte[])original.Clone();
      BinaryPrimitives.WriteUInt16LittleEndian(reversed.AsSpan(130), 2);
      BinaryPrimitives.WriteUInt16LittleEndian(reversed.AsSpan(132), 1);
      await File.WriteAllBytesAsync(bufferPath, reversed);

      var reversedImport = await interchange.ImportEditGltfFileAsync(path, export.Value.Baseline);

      reversedImport.Status.Should().Be(OperationStatus.Succeeded);
      reversedImport.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
        .Subject.Triangles[0].Should().Be(new StaticTriangle(0, 2, 1, 1));
      reversedImport.Value.RestoredSerializedRepresentationPaths.Should().NotContain(
        "StaticRenderObjectSequence[0]");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task PrimitiveSplitRestoresOriginalPartitionBoundaryExactly()
  {
    var asset = CreateTwoTriangleAsset();
    var original = asset.StaticRenderObjectSequence[0].GetSerializedRepresentation();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    const string primitive =
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0}";
    var bytes = RewriteJson(
      glb.ToArray(),
      primitive,
      primitive + "," + primitive.Replace("\"indices\":3", "\"indices\":4", StringComparison.Ordinal));
    bytes = RewriteJson(
      bytes,
      "{\"bufferView\":3,\"componentType\":5123,\"count\":6,\"type\":\"SCALAR\"}",
      "{\"bufferView\":3,\"componentType\":5123,\"count\":3,\"type\":\"SCALAR\"},"
      + "{\"bufferView\":3,\"byteOffset\":6,\"componentType\":5123,\"count\":3,\"type\":\"SCALAR\"}");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    result.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
      .Subject.GetSerializedRepresentation().Should().Equal(original);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task UnchangedImportRestoresGuardedTopologyFromBothPackageForms(bool separate)
  {
    var source = await CreateGuardedTopologyFixtureAsync();
    var asset = await ReadAssetAsync(source);
    var renderObject = asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    renderObject.RenderVertices.Should().HaveCount(5);
    renderObject.RenderVertices[0].Position.Should().Be(renderObject.RenderVertices[3].Position);
    renderObject.Triangles.Should().Contain(triangle => triangle.Vertex1 == triangle.Vertex2);
    renderObject.RenderVertices[1].NormalSharingIndex.Should().Be(0);
    renderObject.VertexBlockPadding.Should().Contain(0x5A);
    renderObject.Triangles[0].TriangleRenderPassFlags.Should().Be(0x1234);
    var interchange = new GltfInterchange();

    StaticMeshAsset restoredAsset;
    if (separate)
    {
      var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
      var path = Path.Combine(directory, "model.gltf");
      Directory.CreateDirectory(directory);
      try
      {
        var export = await interchange.ExportGltfFileAsync(
          asset,
          path,
          new GltfExportOptions(LineageId, DocumentId));
        var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);
        import.Status.Should().Be(OperationStatus.Succeeded);
        restoredAsset = import.Value!.Asset;
      }
      finally
      {
        Directory.Delete(directory, true);
      }
    }
    else
    {
      await using var glb = new MemoryStream();
      var export = await interchange.ExportGlbAsync(
        asset,
        glb,
        new GltfExportOptions(LineageId, DocumentId));
      glb.Position = 0;
      var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);
      import.Status.Should().Be(OperationStatus.Succeeded);
      restoredAsset = import.Value!.Asset;
    }

    await using var restored = new MemoryStream();
    var write = await new MshWriter().WriteAsync(restoredAsset, restored);
    write.Status.Should().Be(OperationStatus.Succeeded);
    restored.ToArray().Should().Equal(source);
  }

  [Fact]
  public async Task TransactionalSeparateGltfExportPreservesManifestWhenCommitFails()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var interchange = new GltfInterchange(new FailingManifestTransactionalFileSystem());
      var result = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().HaveCount(2);
      Directory.EnumerateFiles(directory).Should().NotContain(file => file.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateGltfValidationAndImportRejectMissingBufferWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(path));
      var bufferPath = Path.Combine(
        directory,
        json.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!);
      File.Delete(bufferPath);

      var validation = await interchange.ValidateGltfFileAsync(path);
      var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);

      validation.Status.Should().Be(OperationStatus.Failed);
      validation.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      import.Status.Should().Be(OperationStatus.Failed);
      import.Value.Should().BeNull();
      import.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task DeepReparentRegeneratesCanonicalSequenceAndHierarchyState()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var commonHeader = asset.CommonBaseHeader.SerializedRepresentation.ToArray();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), "\"children\":[1,2]", "\"children\":[1]");
    bytes = RewriteJson(
      bytes,
      "\"name\":\"Source object 2\",\"mesh\":1,\"translation\":[1,3,-2],",
      "\"name\":\"Source object 2\",\"mesh\":1,\"translation\":[1,3,-2],\"children\":[2],");

    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var reconciled = import.Value!.Asset;
    reconciled.StaticRenderObjectSequence.Select(record => record.LocalId).Should().Equal(1, 2, 4, 3);
    reconciled.StaticRenderObjectSequence.Select(record => record.HierarchyUnwindCount).Should()
      .Equal(0, 0, 0, 2);
    reconciled.StaticRenderObjectSequence.Select(record => record.KnownFlags
      .HasFlag(StaticRenderObjectFlags.BeginsNestedSourceObject)).Should()
      .Equal(false, true, true, false);
    reconciled.StaticRenderObjectSequence.Select(record => record.NextRecordMarker).Should()
      .Equal(1, 1, 1, 0);
    reconciled.StoredTrailingHierarchyUnwindCount.Should().Be(1);
    reconciled.RootSourceObject.Children.Should().ContainSingle().Subject.Children.Should()
      .ContainSingle();
    reconciled.CommonBaseHeader.SerializedRepresentation.Should().Equal(commonHeader);
    foreach (var record in reconciled.StaticRenderObjectSequence)
    {
      var original = asset.StaticRenderObjectSequence.Single(item => item.LocalId == record.LocalId);
      record.TexturePathBytes.Should().Equal(original.TexturePathBytes);
      record.RenderVertices.Should().Equal(original.RenderVertices);
      record.Triangles.Should().Equal(original.Triangles);
      record.AnimationTracks.ScaleFrames.Should().Equal(original.AnimationTracks.ScaleFrames);
      record.AnimationTracks.TranslationFrames.Should().Equal(original.AnimationTracks.TranslationFrames);
      record.AnimationTracks.Matrices.Should().Equal(original.AnimationTracks.Matrices);
      (record.ObjectFlags & ~0x000008FFu).Should().Be(original.ObjectFlags & ~0x000008FFu);
    }
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence" &&
      change.Disposition == PreservationDisposition.Regenerated);
  }

  [Fact]
  public async Task TranslationEditRegeneratesOnlyEffectivePivot()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var sourceRecord = asset.StaticRenderObjectSequence[1];
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), "\"translation\":[1,3,-2]", "\"translation\":[2,3,-2]");
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var record = import.Value!.Asset.StaticRenderObjectSequence[1];
    record.Pivot.Should().Be(new Vector3(2, 2, 3));
    record.RenderVertices.Should().Equal(sourceRecord.RenderVertices);
    record.Triangles.Should().Equal(sourceRecord.Triangles);
    record.TexturePathBytes.Should().Equal(sourceRecord.TexturePathBytes);
    record.ObjectFlags.Should().Be(sourceRecord.ObjectFlags);
    record.AnimationTracks.ScaleFrames.Should().Equal(sourceRecord.AnimationTracks.ScaleFrames);
    import.Value.Asset.StaticRenderObjectSequence[0].GetSerializedRepresentation().Should()
      .Equal(asset.StaticRenderObjectSequence[0].GetSerializedRepresentation());
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[1].Pivot" &&
      change.Disposition == PreservationDisposition.Regenerated);
  }

  [Fact]
  public async Task UniqueSourceObjectDeletionRetainsUnrelatedRecordsAndIdentityGaps()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      root["nodes"]!.AsArray().RemoveAt(2);
      root["meshes"]!.AsArray().RemoveAt(2);
      root["nodes"]![0]!["children"]!.AsArray().RemoveAt(1);
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var reconciled = import.Value!.Asset;
    reconciled.StaticRenderObjectSequence.Select(record => record.LocalId).Should().Equal(1, 2, 3);
    reconciled.StaticRenderObjectSequence.Should().NotContain(record => record.LocalId == 4);
    reconciled.RootSourceObject.Children.Should().ContainSingle().Subject.Id.Value.Should().Be(2);
    reconciled.StaticRenderObjectSequence.Select(record => record.NextRecordMarker).Should()
      .Equal(1, 1, 0);
    reconciled.StoredTrailingHierarchyUnwindCount.Should().Be(1);
    reconciled.CommonBaseHeader.SerializedRepresentation.Should()
      .Equal(asset.CommonBaseHeader.SerializedRepresentation);
  }

  [Fact]
  public async Task ReflectedNodeTransformRegeneratesGeometryAndReversesWindingOnce()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
      root["nodes"]![0]!["scale"] = new JsonArray(-1, 1, 1));
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var record = import.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    record.RenderVertices.Select(vertex => vertex.Position.X).Should().Equal(0, -1, 0);
    record.Triangles.Should().ContainSingle().Subject.Should().Match<StaticTriangle>(triangle =>
      triangle.Vertex0 == 0 && triangle.Vertex1 == 2 && triangle.Vertex2 == 1);
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].RenderVertices" &&
      change.Disposition == PreservationDisposition.Regenerated);
  }

  [Fact]
  public async Task MatrixTransformUsesTheSameGeometryDependencyPathAsTrs()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
      root["nodes"]![0]!["matrix"] = new JsonArray(
        2, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1));
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence[0].RenderVertices.Should().NotEqual(
      asset.StaticRenderObjectSequence[0].RenderVertices);
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].RenderVertices" &&
      change.Disposition == PreservationDisposition.Regenerated);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task UntaggedCopyExpandsLinkedMeshAndRequiresForkForDuplicateMesh(bool singleUserMesh)
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var copy = nodes[1]!.DeepClone().AsObject();
      copy["name"] = "Copied source object";
      copy.Remove("extras");
      if (singleUserMesh)
      {
        var meshes = root["meshes"]!.AsArray();
        meshes.Add(meshes[1]!.DeepClone());
        copy["mesh"] = meshes.Count - 1;
      }
      nodes.Add(copy);
      root["nodes"]![0]!["children"]!.AsArray().Add(nodes.Count - 1);
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    if (singleUserMesh)
    {
      import.Status.Should().Be(OperationStatus.Failed);
      import.Value.Should().BeNull();
      import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
      return;
    }
    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var reconciled = import.Value!.Asset;
    reconciled.RootSourceObject.Children.Select(child => child.Id.Value).Should().Equal(2, 3, 4);
    reconciled.StaticRenderObjectSequence.Select(record => record.LocalId).Should()
      .Equal(1, 2, 4, 5, 3);
    var copied = reconciled.StaticRenderObjectSequence.Single(record => record.LocalId == 5);
    copied.SourceObjectId.Value.Should().Be(4);
    copied.Pivot.Should().Be(new Vector3(1, 2, 3));
    copied.RenderVertices.Select(vertex => vertex.Position).Should().Equal(
      asset.StaticRenderObjectSequence[1].RenderVertices.Select(vertex => vertex.Position));
    copied.RenderVertices.Should().OnlyContain(vertex =>
      vertex.NormalSharingIndex == ushort.MaxValue
      && vertex.PositionSharingIndex == ushort.MaxValue
      && vertex.ReservedTextureComponent == 0);
    copied.TexturePathBytes.Should().Equal(asset.StaticRenderObjectSequence[1].TexturePathBytes);
    copied.KnownFlags.Should().Be(StaticRenderObjectFlags.BeginsNestedSourceObject);
    copied.HierarchyUnwindCount.Should().Be(1);
  }

  [Fact]
  public async Task TransformOnlyScaffoldingCollapsesIntoDescendantLocalTransform()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      nodes.Add(new JsonObject
      {
        ["name"] = "Blender Empty",
        ["translation"] = new JsonArray(1, 0, 0),
        ["children"] = new JsonArray(1)
      });
      root["nodes"]![0]!["children"] = new JsonArray(nodes.Count - 1, 2);
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var reconciled = import.Value!.Asset;
    reconciled.RootSourceObject.Children.Select(child => child.Id.Value).Should().Equal(2, 3);
    reconciled.StaticRenderObjectSequence[1].Pivot.Should().Be(new Vector3(2, 2, 3));
    reconciled.StaticRenderObjectSequence.Select(record => record.LocalId).Should().Equal(1, 2, 3, 4);
    reconciled.StaticRenderObjectSequence[0].GetSerializedRepresentation().Should()
      .Equal(asset.StaticRenderObjectSequence[0].GetSerializedRepresentation());
  }

  [Fact]
  public async Task SeparateGltfHierarchyAndTransformEditUsesSameReconciliationRules()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      var nodes = root["nodes"]!.AsArray();
      nodes.Add(new JsonObject
      {
        ["name"] = "Blender Empty",
        ["translation"] = new JsonArray(1, 0, 0),
        ["children"] = new JsonArray(1)
      });
      root["nodes"]![1]!["translation"] = new JsonArray(2, 3, -2);
      root["nodes"]![0]!["children"] = new JsonArray(nodes.Count - 1);
      root["nodes"]![1]!["children"] = new JsonArray(2);
      await File.WriteAllTextAsync(path, root.ToJsonString());

      var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);

      import.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
      import.Value!.Asset.StaticRenderObjectSequence.Select(record => record.LocalId).Should()
        .Equal(1, 2, 4, 3);
      import.Value.Asset.StaticRenderObjectSequence[1].Pivot.Should().Be(new Vector3(3, 2, 3));
      import.Value.Asset.StoredTrailingHierarchyUnwindCount.Should().Be(1);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task UntaggedObjectWithMissingExpectedScopeIsAmbiguous()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      nodes.RemoveAt(2);
      root["meshes"]!.AsArray().RemoveAt(2);
      var unidentified = nodes[1]!.DeepClone().AsObject();
      unidentified.Remove("extras");
      unidentified["name"] = "Unidentified object";
      nodes.Add(unidentified);
      root["nodes"]![0]!["children"] = new JsonArray(1, 2);
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Failed);
    import.Value.Should().BeNull();
    import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
  }

  [Fact]
  public async Task DuplicateObjectIdentityRequiresExplicitForkResolution()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      nodes.Add(nodes[1]!.DeepClone());
      root["nodes"]![0]!["children"]!.AsArray().Add(nodes.Count - 1);
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Failed);
    import.Value.Should().BeNull();
    import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
  }

  [Fact]
  public async Task DeletedHighestIdentitiesAreNotReusedAfterReExport()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var firstGlb = new MemoryStream();
    var firstExport = await interchange.ExportGlbAsync(
      asset,
      firstGlb,
      new GltfExportOptions(LineageId, DocumentId));
    var deletedBytes = RewriteJson(firstGlb.ToArray(), root =>
    {
      root["nodes"]!.AsArray().RemoveAt(2);
      root["meshes"]!.AsArray().RemoveAt(2);
      root["nodes"]![0]!["children"]!.AsArray().RemoveAt(1);
    });
    await using var deletedGlb = new MemoryStream(deletedBytes);
    var deleted = await interchange.ImportEditGlbAsync(deletedGlb, firstExport.Value!.Baseline);
    deleted.Status.Should().Be(OperationStatus.Succeeded);
    await using var secondGlb = new MemoryStream();
    var secondExport = await interchange.ExportGlbAsync(
      deleted.Value!.Asset,
      secondGlb,
      new GltfExportOptions(LineageId, deleted.Value.NextBaseline.DocumentId));
    var copiedBytes = RewriteJson(secondGlb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var copy = nodes[1]!.DeepClone().AsObject();
      copy.Remove("extras");
      nodes.Add(copy);
      root["nodes"]![0]!["children"]!.AsArray().Add(nodes.Count - 1);
    });
    await using var copiedGlb = new MemoryStream(copiedBytes);

    var copied = await interchange.ImportEditGlbAsync(copiedGlb, secondExport.Value!.Baseline);

    copied.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", copied.Diagnostics.Select(diagnostic => diagnostic.Message)));
    copied.Value!.Asset.RootSourceObject.Children.Select(child => child.Id.Value).Should().Equal(2, 4);
    copied.Value.Asset.StaticRenderObjectSequence.Select(record => record.LocalId).Should()
      .Equal(1, 2, 5, 3);
  }

  [Fact]
  public async Task SiblingReorderTriggersDeterministicCanonicalSequencing()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), "\"children\":[1,2]", "\"children\":[2,1]");
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.RootSourceObject.Children.Select(child => child.Id.Value).Should().Equal(2, 3);
    import.Value.Asset.StaticRenderObjectSequence.Select(record => record.LocalId).Should()
      .Equal(1, 2, 4, 3);
    import.Value.Asset.StaticRenderObjectSequence.Select(record => record.NextRecordMarker).Should()
      .Equal(1, 1, 1, 0);
  }

  [Fact]
  public async Task SeparateExportRejectsManifestBufferCollisionWithoutChangingBuffer()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var manifestPath = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var first = await interchange.ExportGltfFileAsync(
        asset,
        manifestPath,
        new GltfExportOptions(LineageId, DocumentId));
      first.Status.Should().Be(OperationStatus.Succeeded);
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(manifestPath));
      var bufferPath = Path.Combine(
        directory,
        json.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!);
      var originalBuffer = await File.ReadAllBytesAsync(bufferPath);

      var collisionPath = Path.Combine(directory, Path.GetFileName(bufferPath).ToUpperInvariant());
      var collision = await interchange.ExportGltfFileAsync(
        asset,
        collisionPath,
        new GltfExportOptions(LineageId, DocumentId));

      collision.Status.Should().Be(OperationStatus.Failed);
      collision.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(bufferPath)).Should().Equal(originalBuffer);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task StandaloneValidationEnforcesMetadataLimitForBothPackageForms()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    var profile = new GltfOperationProfile(maxMetadataBytes: 64);
    await using var glb = new MemoryStream();
    var glbExport = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    glbExport.Status.Should().Be(OperationStatus.Succeeded);
    glb.Position = 0;
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var gltfExport = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      gltfExport.Status.Should().Be(OperationStatus.Succeeded);

      var glbValidation = await interchange.ValidateGlbAsync(glb, profile);
      var gltfValidation = await interchange.ValidateGltfFileAsync(path, profile);

      glbValidation.Status.Should().Be(OperationStatus.Failed);
      glbValidation.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
      gltfValidation.Status.Should().Be(OperationStatus.Failed);
      gltfValidation.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task CancelledSeparateExportPreservesDestinationAndCreatesNoSidecars()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        path,
        cancellationToken: cancellation.Token);

      result.Status.Should().Be(OperationStatus.Cancelled);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateExportPreservesDestinationWhenSidecarCommitFails()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var interchange = new GltfInterchange(new FailingSidecarTransactionalFileSystem());
      var result = await interchange.ExportGltfFileAsync(asset, path);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task CancellationAfterSidecarCommitPreservesManifestAndCleansTemporaryFiles()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);
    using var cancellation = new CancellationTokenSource();

    try
    {
      var fileSystem = new CancellingAfterSidecarTransactionalFileSystem(cancellation);
      var result = await new GltfInterchange(fileSystem).ExportGltfFileAsync(
        asset,
        path,
        cancellationToken: cancellation.Token);

      result.Status.Should().Be(OperationStatus.Cancelled);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().HaveCount(2);
      Directory.EnumerateFiles(directory).Should().NotContain(file => file.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task ExportEnforcesMetadataLimitBeforeWritingEitherPackageForm()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var profile = new GltfOperationProfile(maxMetadataBytes: 64);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var glbResult = await interchange.ExportGlbAsync(asset, glb, profile: profile);
      var gltfResult = await interchange.ExportGltfFileAsync(asset, path, profile: profile);

      glbResult.Status.Should().Be(OperationStatus.Failed);
      glbResult.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
      glb.Length.Should().Be(0);
      gltfResult.Status.Should().Be(OperationStatus.Failed);
      gltfResult.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task IndependentOneTriangleFixtureCompletesUnchangedGlbRoundTripByteExactly()
  {
    var sourceBytes = OneTriangleMshFixture.Create();
    var asset = await ReadAssetAsync(sourceBytes);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    export.Value!.Baseline.AssetLineageId.Should().Be(LineageId);
    export.Value.Baseline.DocumentId.Should().Be(DocumentId);
    export.Value.Fingerprint.Name.Should().Be("static-geometry");
    export.Value.Fingerprint.Version.Should().Be(1);
    var glbBytes = glb.ToArray();
    var binaryChunkOffset = GetBinaryChunkOffset(glbBytes);
    ReadSingle(glbBytes, binaryChunkOffset + 32).Should().Be(-1f);
    ReadSingle(glbBytes, binaryChunkOffset + 40).Should().Be(1f);

    glb.Position = 0;
    var validation = await interchange.ValidateGlbAsync(glb);
    validation.Status.Should().Be(OperationStatus.Succeeded);

    glb.Position = 0;
    var imported = await interchange.ImportEditGlbAsync(glb, export.Value.Baseline);
    imported.Status.Should().Be(OperationStatus.Succeeded);
    imported.Value!.NextBaseline.DocumentId.Should().NotBe(DocumentId);

    await using var msh = new MemoryStream();
    var write = await new MshWriter().WriteAsync(imported.Value.Asset, msh);
    write.Status.Should().Be(OperationStatus.Succeeded);
    msh.ToArray().Should().Equal(sourceBytes);
  }

  [Fact]
  public async Task GeneratedGlbPassesPinnedKhronosValidatorWithoutErrorsOrWarnings()
  {
    if (Environment.GetEnvironmentVariable("EARTHTOOL_RUN_KHRONOS_VALIDATOR") != "1")
    {
      return;
    }

    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    export.Succeeded.Should().BeTrue();
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
    await File.WriteAllBytesAsync(path, glb.ToArray());

    try
    {
      await AssertKhronosValidAsync(path);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public async Task GeneratedSeparateGltfPassesPinnedKhronosValidatorWithoutErrorsOrWarnings()
  {
    if (Environment.GetEnvironmentVariable("EARTHTOOL_RUN_KHRONOS_VALIDATOR") != "1")
    {
      return;
    }

    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
      var export = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      export.Succeeded.Should().BeTrue();

      await AssertKhronosValidAsync(path);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task IsolatedPositionEditRegeneratesAffectedPartition()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    var binaryChunkOffset = GetBinaryChunkOffset(bytes);
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(binaryChunkOffset),
      BitConverter.SingleToInt32Bits(0.25f));
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Succeeded);
    var renderObject = result.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    renderObject.RenderVertices[0].Position.Should().Be(new Vector3(0.25f, 0, 0));
    renderObject.RenderVertices.Should().OnlyContain(vertex =>
      vertex.NormalSharingIndex == ushort.MaxValue
      && vertex.PositionSharingIndex == ushort.MaxValue
      && vertex.ReservedTextureComponent == 0);
    result.Value.RestoredSerializedRepresentationPaths.Should().NotContain(
      "StaticRenderObjectSequence[0]");
    result.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].RenderVertices"
      && change.Disposition == PreservationDisposition.Regenerated);
  }

  [Theory]
  [InlineData("position", 0)]
  [InlineData("normal", 36)]
  [InlineData("uv", 72)]
  public async Task IsolatedGeometryChannelEditRegeneratesOnlyAffectedPartition(
    string channel,
    int channelOffset)
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    BinaryPrimitives.WriteInt32LittleEndian(
      fixture.Data.AsSpan(fixture.RecordOffsets[2] + 8),
      BitConverter.SingleToInt32Bits(10f));
    var asset = await ReadAssetAsync(fixture.Data);
    var originalRecords = asset.StaticRenderObjectSequence
      .Select(record => record.GetSerializedRepresentation())
      .ToArray();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(GetBinaryChunkOffset(bytes) + channelOffset),
      BitConverter.SingleToInt32Bits(channel == "normal" ? 1f : 0.25f));
    if (channel == "normal")
    {
      BinaryPrimitives.WriteInt32LittleEndian(
        bytes.AsSpan(GetBinaryChunkOffset(bytes) + channelOffset + sizeof(float)),
        BitConverter.SingleToInt32Bits(0));
    }
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var records = result.Value!.Asset.StaticRenderObjectSequence;
    var dependentRecord = (byte[])originalRecords[1].Clone();
    BinaryPrimitives.WriteUInt16LittleEndian(dependentRecord.AsSpan(8 + 0x90), ushort.MaxValue);
    records[1].GetSerializedRepresentation().Should().Equal(dependentRecord);
    for (var index = 2; index < records.Count; index++)
    {
      records[index].GetSerializedRepresentation().Should().Equal(originalRecords[index]);
    }
    records[0].ObjectFlags.Should().Be(asset.StaticRenderObjectSequence[0].ObjectFlags);
    records[0].TexturePathBytes.Should().Equal(asset.StaticRenderObjectSequence[0].TexturePathBytes);
    records[0].VertexBlockPadding.Should().OnlyContain(value => value == 0);
    records[0].RenderVertices.Should().OnlyContain(vertex =>
      vertex.NormalSharingIndex == ushort.MaxValue
      && vertex.PositionSharingIndex == ushort.MaxValue
      && vertex.ReservedTextureComponent == 0);
    if (channel == "position")
    {
      records[0].RenderVertices[0].Position.X.Should().Be(0.25f);
    }
    else if (channel == "normal")
    {
      records[0].RenderVertices[0].Normal.X.Should().Be(1f);
    }
    else
    {
      records[0].RenderVertices[0].TextureCoordinate.X.Should().Be(0.25f);
    }
    result.Value.RestoredSerializedRepresentationPaths.Should().Contain(
      "StaticRenderObjectSequence[2]");
    result.Value.RestoredSerializedRepresentationPaths.Should().NotContain(
      "StaticRenderObjectSequence[0]");
    result.Value.RestoredSerializedRepresentationPaths.Should().NotContain(
      "StaticRenderObjectSequence[1]");
    result.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[1].RenderVertices[0].NormalSharingIndex"
      && change.Disposition == PreservationDisposition.Canonicalized);
  }

  [Fact]
  public async Task DuplicatePartitionDeletionProducesAmbiguousCorrespondenceWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var originalIds = asset.StaticRenderObjectSequence.Select(record => record.LocalId).ToArray();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(
      glb.ToArray(),
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0},",
      string.Empty);
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
    asset.StaticRenderObjectSequence.Select(record => record.LocalId).Should().Equal(originalIds);
  }

  [Fact]
  public async Task MultipleStalePartitionsProduceAmbiguousCorrespondence()
  {
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      CreateTwoPartitionAsset(),
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    var binaryOffset = GetBinaryChunkOffset(bytes);
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(binaryOffset),
      BitConverter.SingleToInt32Bits(0.25f));
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(binaryOffset + 104),
      BitConverter.SingleToInt32Bits(11f));
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
  }

  [Fact]
  public async Task UniquePartitionDeletionRetainsUnaffectedPartitionExactly()
  {
    var asset = CreateTwoPartitionAsset();
    var retained = asset.StaticRenderObjectSequence[1];
    var retainedBytes = retained.GetSerializedRepresentation();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(
      glb.ToArray(),
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0},",
      string.Empty);
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var resultRecord = result.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    resultRecord.Id.Value.Should().Be(retained.Id.Value);
    resultRecord.Id.Lineage.Value.Should().Be(LineageId);
    resultRecord.GetSerializedRepresentation().Should().Equal(retainedBytes);
    result.Value.Asset.RootSourceObject.StaticRenderObjectIds.Select(id => id.Value).Should()
      .Equal(retained.Id.Value);
  }

  [Fact]
  public async Task ReExportAfterDeletionPreservesSparsePartitionIdentity()
  {
    var interchange = new GltfInterchange();
    await using var firstGlb = new MemoryStream();
    var firstExport = await interchange.ExportGlbAsync(
      CreateTwoPartitionAsset(),
      firstGlb,
      new GltfExportOptions(LineageId, DocumentId));
    var deletedBytes = RewriteJson(
      firstGlb.ToArray(),
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0},",
      string.Empty);
    await using var deletedGlb = new MemoryStream(deletedBytes);
    var deleted = await interchange.ImportEditGlbAsync(
      deletedGlb,
      firstExport.Value!.Baseline);
    deleted.Status.Should().Be(OperationStatus.Succeeded);
    var retainedId = deleted.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
      .Subject.LocalId;
    await using var secondGlb = new MemoryStream();
    var secondExport = await interchange.ExportGlbAsync(
      deleted.Value.Asset,
      secondGlb,
      new GltfExportOptions(LineageId, deleted.Value.NextBaseline.DocumentId));
    secondGlb.Position = 0;

    var secondImport = await interchange.ImportEditGlbAsync(
      secondGlb,
      secondExport.Value!.Baseline);

    secondImport.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", secondImport.Diagnostics.Select(diagnostic => diagnostic.Message)));
    secondImport.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
      .Subject.LocalId.Should().Be(retainedId);
  }

  [Fact]
  public async Task UniquePartitionCopyCreatesCanonicalForkWithFreshIdentity()
  {
    var source = CreateTwoPartitionAsset();
    var bindingEdit = source.Edit()
      .SetTextureResourceBinding(
        source.StaticRenderObjectSequence[0].Id,
        "Textures\\authored\\shared.tex")
      .Commit();
    bindingEdit.TryGetValue(out var editedAsset).Should().BeTrue();
    var asset = editedAsset!;
    var originalIds = asset.StaticRenderObjectSequence.Select(record => record.LocalId).ToArray();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    const string firstPrimitive =
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0}";
    var bytes = RewriteJson(
      glb.ToArray(),
      firstPrimitive + ",",
      firstPrimitive + "," + firstPrimitive + ",");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var records = result.Value!.Asset.StaticRenderObjectSequence;
    records.Should().HaveCount(3);
    records.Take(2).Select(record => record.LocalId).Should().Equal(originalIds);
    records[2].LocalId.Should().BeGreaterThan(originalIds.Max());
    records[2].RenderVertices.Select(vertex => vertex.Position).Should().Equal(
      records[0].RenderVertices.Select(vertex => vertex.Position));
    records[2].RenderVertices.Should().OnlyContain(vertex =>
      vertex.NormalSharingIndex == ushort.MaxValue
      && vertex.PositionSharingIndex == ushort.MaxValue
      && vertex.ReservedTextureComponent == 0);
    records[2].TexturePathBytes.Should().Equal("Textures\\authored\\shared.tex"u8.ToArray());
    result.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[2].TexturePathBytes"
      && change.Disposition == PreservationDisposition.Canonicalized);
  }

  [Fact]
  public async Task EditImportRejectsForeignLineageWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    await interchange.ExportGlbAsync(asset, glb, new GltfExportOptions(LineageId, DocumentId));
    glb.Position = 0;

    var result = await interchange.ImportEditGlbAsync(
      glb,
      new InterchangeBaseline(Guid.NewGuid(), DocumentId));

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.AssetLineageMismatch);
  }

  [Fact]
  public async Task EditImportRejectsDetachedMeshNodeWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    ReplaceFirst(bytes, "\"nodes\":[0]", "\"nodes\":[] ");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.UnsupportedDomain);
  }

  [Fact]
  public async Task EditImportRejectsUnsupportedFingerprintProjection()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    ReplaceFirst(bytes, "static-geometry", "static-geometrx");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.MalformedMetadata);
  }

  [Fact]
  public async Task EditImportRejectsUnsupportedPrimitiveAttribute()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(
      glb.ToArray(),
      "\"TEXCOORD_0\":2}",
      "\"TEXCOORD_0\":2,\"COLOR_0\":1}");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.UnsupportedDomain);
  }

  [Theory]
  [InlineData("missing-uv")]
  [InlineData("non-triangle")]
  [InlineData("invalid-index")]
  public async Task EditImportRejectsInvalidGeometryWithoutPartialAsset(string mutation)
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    if (mutation == "missing-uv")
    {
      bytes = RewriteJson(bytes, ",\"TEXCOORD_0\":2", string.Empty);
    }
    else if (mutation == "non-triangle")
    {
      ReplaceFirst(bytes, "\"mode\":4", "\"mode\":1");
    }
    else
    {
      BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(GetBinaryChunkOffset(bytes) + 96), 3);
    }

    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle();
  }

  [Fact]
  public async Task TransactionalGlbExportPreservesDestinationWhenCommitFails()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var interchange = new GltfInterchange(new FailingTransactionalFileSystem());
      var result = await interchange.ExportGlbFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public async Task TransactionalGlbExportPreservesDestinationWhenCancelled()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(path, original);
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    try
    {
      var result = await new GltfInterchange().ExportGlbFileAsync(
        asset,
        path,
        cancellationToken: cancellation.Token);

      result.Status.Should().Be(OperationStatus.Cancelled);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public void PublicApiMatchesInitialApproval()
  {
    PublicApiApproval.Verify(
      "gltf",
      typeof(GltfInterchange).Assembly.ExportedTypes.Where(type => type.Namespace == "EarthTool.GLTF"));
  }

  private static async Task<StaticMeshAsset> ReadAssetAsync(byte[] source)
  {
    await using var stream = new MemoryStream(source);
    var result = await new MshReader().ReadAsync(stream);
    return result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
  }

  private static StaticMeshAsset CreateTwoTriangleAsset()
  {
    var build = StaticMeshBuilder.Create(
        OneTriangleMshFixture.CreationGuid,
        new MeshAssetLineageId(Guid.Parse("99999999-8888-7777-6666-555555555555")))
      .SetRenderObject(
        [
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
          new CanonicalStaticVertex(Vector3.One, Vector3.UnitZ, Vector2.One)
        ],
        [new CanonicalTriangle(0, 1, 2), new CanonicalTriangle(2, 1, 3)])
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static StaticMeshAsset CreateTwoPartitionAsset()
  {
    var vertices = new[]
    {
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY)
    };
    var translated = vertices.Select(vertex => new CanonicalStaticVertex(
      vertex.Position + new Vector3(10, 0, 0),
      vertex.Normal,
      vertex.TextureCoordinate));
    var build = StaticMeshBuilder.Create(
        OneTriangleMshFixture.CreationGuid,
        new MeshAssetLineageId(Guid.Parse("88888888-9999-aaaa-bbbb-cccccccccccc")))
      .SetRootSourceObject(new CanonicalStaticSourceObject(
      [
        new CanonicalStaticRenderObject(vertices, [new CanonicalTriangle(0, 1, 2)]),
        new CanonicalStaticRenderObject(translated, [new CanonicalTriangle(0, 1, 2)])
      ]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static async Task<byte[]> CreateGuardedTopologyFixtureAsync()
  {
    var build = StaticMeshBuilder.Create(
        OneTriangleMshFixture.CreationGuid,
        new MeshAssetLineageId(Guid.Parse("77777777-8888-9999-aaaa-bbbbbbbbbbbb")))
      .SetRenderObject(
        [
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.One, Vector3.UnitZ, Vector2.One)
        ],
        [new CanonicalTriangle(0, 1, 2), new CanonicalTriangle(3, 1, 1)])
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    await using var stream = new MemoryStream();
    var write = await new MshWriter().WriteAsync(asset!, stream);
    write.Status.Should().Be(OperationStatus.Succeeded);
    var bytes = stream.ToArray();
    var recordOffset = 0x14 + 0x368 + sizeof(uint);
    const int vertexBlockSize = 0xA0;
    bytes[recordOffset + 0x08 + vertexBlockSize + sizeof(float)] = 0x5A;
    BinaryPrimitives.WriteUInt16LittleEndian(
      bytes.AsSpan(recordOffset + 0x08 + 0x90 + sizeof(ushort)),
      0);
    var objectFlagsOffset = recordOffset + 0x08 + (2 * vertexBlockSize);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(objectFlagsOffset), 0x12340000);
    var firstTriangleOffset = objectFlagsOffset + sizeof(uint) + sizeof(uint) + sizeof(uint);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(firstTriangleOffset + 6), 0x1234);
    return bytes;
  }

  private static int GetBinaryChunkOffset(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return 12 + 8 + jsonLength + 8;
  }

  private static JsonDocument ReadGlbJson(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return JsonDocument.Parse(glb.AsMemory(20, jsonLength));
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
    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Node did not start.");
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    process.ExitCode.Should().Be(0, $"validator stdout: {output} stderr: {error}");
    output.Should().Contain("\"errors\":0");
    output.Should().Contain("\"warnings\":0");
  }

  private static float ReadSingle(byte[] data, int offset)
  {
    return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset)));
  }

  private static ushort ToUnsignedFixedPoint(float value)
  {
    return checked((ushort)Math.Truncate(value * 256d));
  }

  private static byte[] CreateRgbaTex(int width, int height, byte[] pixels)
  {
    pixels.Length.Should().Be(width * height * 4);
    var result = new byte[24 + pixels.Length];
    "TEX\0\x01\0\0\0"u8.CopyTo(result);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), 0x03000012);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), 0x8888);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(16), width);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(20), height);
    pixels.CopyTo(result, 24);
    return result;
  }

  private static void SwapBlocks(byte[] data, int left, int right, int length)
  {
    var temporary = data.AsSpan(left, length).ToArray();
    data.AsSpan(right, length).CopyTo(data.AsSpan(left, length));
    temporary.CopyTo(data, right);
  }

  private static void ReplaceFirst(byte[] data, string oldValue, string newValue)
  {
    var oldBytes = Encoding.UTF8.GetBytes(oldValue);
    var newBytes = Encoding.UTF8.GetBytes(newValue);
    newBytes.Length.Should().Be(oldBytes.Length);
    var offset = data.AsSpan().IndexOf(oldBytes);
    offset.Should().BeGreaterThanOrEqualTo(0, $"'{oldValue}' should exist in the GLB");
    newBytes.CopyTo(data, offset);
  }

  private static byte[] RewriteJson(byte[] glb, string oldValue, string newValue)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var json = Encoding.UTF8.GetString(glb, 20, jsonLength).TrimEnd();
    json.Should().Contain(oldValue);
    var rewrittenJson = Encoding.UTF8.GetBytes(json.Replace(oldValue, newValue, StringComparison.Ordinal));
    return RewriteJsonChunk(glb, rewrittenJson);
  }

  private static byte[] RewriteJson(byte[] glb, Action<JsonObject> rewrite)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var root = JsonNode.Parse(glb.AsSpan(20, jsonLength))!.AsObject();
    rewrite(root);
    return RewriteJsonChunk(glb, Encoding.UTF8.GetBytes(root.ToJsonString()));
  }

  private static void RemoveEarthToolMetadata(JsonNode? node)
  {
    if (node is JsonObject owner)
    {
      if (owner["extras"] is JsonObject extras)
      {
        extras.Remove("earthtool");
        if (extras.Count == 0)
        {
          owner.Remove("extras");
        }
      }

      foreach (var child in owner.ToArray())
      {
        RemoveEarthToolMetadata(child.Value);
      }
    }
    else if (node is JsonArray array)
    {
      foreach (var child in array)
      {
        RemoveEarthToolMetadata(child);
      }
    }
  }

  private static byte[] RewriteJsonChunk(byte[] glb, byte[] rewrittenJson)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var paddedJsonLength = (rewrittenJson.Length + 3) & ~3;
    var oldBinaryHeader = 20 + jsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(oldBinaryHeader));
    var result = new byte[12 + 8 + paddedJsonLength + 8 + binaryLength];
    BinaryPrimitives.WriteUInt32LittleEndian(result, 0x46546C67);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), 2);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), paddedJsonLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
    rewrittenJson.CopyTo(result, 20);
    result.AsSpan(20 + rewrittenJson.Length, paddedJsonLength - rewrittenJson.Length).Fill(0x20);
    var newBinaryHeader = 20 + paddedJsonLength;
    glb.AsSpan(oldBinaryHeader, 8 + binaryLength).CopyTo(result.AsSpan(newBinaryHeader));
    return result;
  }

  private sealed class FailingTransactionalFileSystem : EarthTool.GLTF.Internal.ITransactionalFileSystem
  {
    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + ".test.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new MemoryStream();
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      throw new IOException("Injected commit failure.");
    }

    public bool TryDelete(string temporaryPath)
    {
      return true;
    }
  }

  private sealed class FailingManifestTransactionalFileSystem : ITransactionalFileSystem
  {
    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + $".{Guid.NewGuid():N}.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      if (destinationPath.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase))
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

  private sealed class FailingSidecarTransactionalFileSystem : ITransactionalFileSystem
  {
    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + $".{Guid.NewGuid():N}.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      if (destinationPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
      {
        throw new IOException("Injected sidecar commit failure.");
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

  private sealed class CancellingAfterSidecarTransactionalFileSystem : ITransactionalFileSystem
  {
    private readonly CancellationTokenSource _cancellation;

    internal CancellingAfterSidecarTransactionalFileSystem(CancellationTokenSource cancellation)
    {
      _cancellation = cancellation;
    }

    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + $".{Guid.NewGuid():N}.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      File.Move(temporaryPath, destinationPath);
      if (destinationPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
      {
        _cancellation.Cancel();
      }
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
