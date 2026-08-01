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
    foreach (var node in root.GetProperty("nodes").EnumerateArray())
    {
      node.TryGetProperty("mesh", out var _).Should().BeTrue();
      node.TryGetProperty("matrix", out var _).Should().BeFalse();
      node.TryGetProperty("rotation", out var _).Should().BeFalse();
    }
    root.GetProperty("meshes")[0].GetProperty("primitives").GetArrayLength().Should().Be(2);
    root.GetProperty("meshes")[1].GetProperty("primitives").GetArrayLength().Should().Be(1);
    root.GetProperty("meshes")[2].GetProperty("primitives").GetArrayLength().Should().Be(1);
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

      reversedImport.Status.Should().Be(OperationStatus.Failed);
      reversedImport.Value.Should().BeNull();
      reversedImport.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.StaleNativeProjection);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
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

  [Theory]
  [InlineData("children")]
  [InlineData("mesh-owner")]
  public async Task EditImportRejectsHierarchyAndMeshOwnershipChanges(string mutation)
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    if (mutation == "children")
    {
      ReplaceFirst(bytes, "\"children\":[1,2]", "\"children\":[2,1]");
    }
    else
    {
      ReplaceFirst(bytes, "\"mesh\":0", "\"mesh\":9");
      ReplaceFirst(bytes, "\"mesh\":1", "\"mesh\":0");
      ReplaceFirst(bytes, "\"mesh\":9", "\"mesh\":1");
    }

    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Failed);
    import.Value.Should().BeNull();
    import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.UnsupportedDomain);
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
  public async Task EditImportRejectsStaleNativeProjectionWithoutPartialAsset()
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

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.StaleNativeProjection);
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
