using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Expert;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Numerics;

namespace EarthTool.MSH.Tests;

public sealed class GltfSourceLossDiagnosticsTests
{
  private const string SourceRepresentationNotPreserved = "ETG1030";

  [Fact]
  public async Task CanonicallyAuthoredAssetsHaveNoSourceLossWarning()
  {
    var staticAsset = CreateCanonicalStaticAsset();
    var dynamicAsset = CreateCanonicalDynamicAsset();
    var interchange = new GltfInterchange();
    await using var staticDestination = new MemoryStream();
    await using var dynamicDestination = new MemoryStream();

    var staticResult = await interchange.ExportGlbAsync(staticAsset, staticDestination);
    var dynamicResult = await interchange.ExportGlbAsync(dynamicAsset, dynamicDestination);

    staticResult.Status.Should().Be(OperationStatus.Succeeded);
    dynamicResult.Status.Should().Be(OperationStatus.Succeeded);
    staticResult.Diagnostics.Should().NotContain(item =>
      item.Code == SourceRepresentationNotPreserved
    );
    dynamicResult.Diagnostics.Should().NotContain(item =>
      item.Code == SourceRepresentationNotPreserved
    );
  }

  [Fact]
  public async Task LoadedStaticAssetWarnsEquallyForGlbAndSeparateGltfWithoutChangingMshBytes()
  {
    var source = CreateCanonicalStaticAsset()
      .GetSerializedRepresentation()
      .Concat(new byte[] { 0xA5, 0x5A })
      .ToArray();
    source[0x18 + 0x1A8] ^= 0x01;
    source[0x18 + 0x18] = 0x01;
    var asset = (StaticMeshAsset)await ReadAssetAsync(source);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var directory = CreateTemporaryDirectory();
    var gltfPath = Path.Combine(directory, "asset.gltf");
    try
    {
      var glbResult = await interchange.ExportGlbAsync(asset, glb);
      var gltfResult = await interchange.ExportGltfFileAsync(asset, gltfPath);
      await using var rewritten = new MemoryStream();
      var writeResult = await new MshWriter().WriteAsync(asset, rewritten);

      glbResult.Status.Should().Be(OperationStatus.Succeeded);
      gltfResult.Status.Should().Be(OperationStatus.Succeeded);
      writeResult.Status.Should().Be(OperationStatus.Succeeded);
      WarningInventory(glbResult.Diagnostics).Should().Equal(
        WarningInventory(gltfResult.Diagnostics)
      );
      SourceLossInventory(glbResult.Diagnostics).Should().Equal(
        (SourceRepresentationNotPreserved, 1130, DiagnosticSeverity.Warning,
          "ArchiveFraming.CreationGuid"),
        (SourceRepresentationNotPreserved, 1130, DiagnosticSeverity.Warning,
          "RootTrailingBytes"),
        (SourceRepresentationNotPreserved, 1130, DiagnosticSeverity.Warning,
          "CommonBaseHeader.RotatedOccupancyDescriptors"),
        (SourceRepresentationNotPreserved, 1130, DiagnosticSeverity.Warning,
          "CommonBaseHeader.CannonRenderPositions[1]")
      );
      glbResult.Diagnostics.Should().OnlyContain(item =>
        item.Code != SourceRepresentationNotPreserved
        || item.Message.Contains("does not restore", StringComparison.Ordinal)
      );
      rewritten.ToArray().Should().Equal(source);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task LoadedDynamicAssetWarnsEquallyForGlbAndSeparateGltfWithoutChangingMshBytes()
  {
    var source = CreateCanonicalDynamicAsset().GetSerializedRepresentation();
    BinaryPrimitives.WriteUInt32LittleEndian(source.AsSpan(0x18 + 0x368 + 0x4C), 0xA5A5A5A5);
    BinaryPrimitives.WriteInt32LittleEndian(source.AsSpan(0x18 + 0x368 + 0x50), 1);
    var asset = (DynamicMeshAsset)await ReadAssetAsync(source);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var directory = CreateTemporaryDirectory();
    var gltfPath = Path.Combine(directory, "asset.gltf");
    try
    {
      var glbResult = await interchange.ExportGlbAsync(asset, glb);
      var gltfResult = await interchange.ExportGltfFileAsync(asset, gltfPath);
      await using var rewritten = new MemoryStream();
      var writeResult = await new MshWriter().WriteAsync(asset, rewritten);

      glbResult.Status.Should().Be(OperationStatus.Succeeded);
      gltfResult.Status.Should().Be(OperationStatus.Succeeded);
      writeResult.Status.Should().Be(OperationStatus.Succeeded);
      WarningInventory(glbResult.Diagnostics).Should().Equal(
        WarningInventory(gltfResult.Diagnostics)
      );
      SourceLossInventory(glbResult.Diagnostics).Should().Equal(
        (SourceRepresentationNotPreserved, 1130, DiagnosticSeverity.Warning,
          "ArchiveFraming.CreationGuid"),
        (SourceRepresentationNotPreserved, 1130, DiagnosticSeverity.Warning,
          "RootDynamicObject.Extension.ReservedWord"),
        (SourceRepresentationNotPreserved, 1130, DiagnosticSeverity.Warning,
          "RootDynamicObject.Extension.InertRepresentations")
      );
      glbResult.Diagnostics.Should().OnlyContain(item =>
        item.Code != SourceRepresentationNotPreserved
        || item.Message.Contains("does not restore", StringComparison.Ordinal)
      );
      rewritten.ToArray().Should().Equal(source);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task ExpertAssetReportsItsSourceOnlyRepresentations()
  {
    var source = CreateCanonicalStaticAsset()
      .GetSerializedRepresentation()
      .Concat(new byte[] { 0xC3 })
      .ToArray();
    var build = MshExpert.CreateStatic(source);
    build.TryGetValue(out var asset).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(asset!, destination);

    result.Status.Should().Be(OperationStatus.Succeeded);
    SourceLossInventory(result.Diagnostics).Should().Contain(item =>
      item.Path == "RootTrailingBytes"
    );
    result.Diagnostics.Should().OnlyContain(item =>
      item.Code != SourceRepresentationNotPreserved
      || item.Data["origin"] == MeshAssetOrigin.Expert.ToString()
    );
  }

  [Fact]
  public async Task LoadedCanonicalAttachmentExtraDoesNotWarn()
  {
    var source = CreateCanonicalStaticAsset().GetSerializedRepresentation();
    var attachmentOffset = 0x18 + 0x1D8 + (4 * 8);
    BinaryPrimitives.WriteInt16LittleEndian(source.AsSpan(attachmentOffset), 0);
    BinaryPrimitives.WriteInt16LittleEndian(source.AsSpan(attachmentOffset + 2), 0);
    BinaryPrimitives.WriteInt16LittleEndian(source.AsSpan(attachmentOffset + 4), 0);
    source[attachmentOffset + 7] = 0x80;
    var asset = (StaticMeshAsset)await ReadAssetAsync(source);
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(asset, destination);

    result.Status.Should().Be(OperationStatus.Succeeded);
    SourceLossInventory(result.Diagnostics).Should().NotContain(item =>
      item.Path == "CommonBaseHeader.AttachmentTable[5].Extra"
    );
  }

  [Fact]
  public async Task LoadedCannonWithIndependentAttachmentPositionWarnsAtAttachmentPath()
  {
    var source = CreateCanonicalStaticAsset().GetSerializedRepresentation();
    var attachmentOffset = 0x18 + 0x1D8;
    BinaryPrimitives.WriteInt16LittleEndian(source.AsSpan(attachmentOffset), 0);
    BinaryPrimitives.WriteInt16LittleEndian(source.AsSpan(attachmentOffset + 2), 0);
    BinaryPrimitives.WriteInt16LittleEndian(source.AsSpan(attachmentOffset + 4), 0);
    source[attachmentOffset + 7] = 0x80;
    BinaryPrimitives.WriteInt32LittleEndian(
      source.AsSpan(0x18 + 0x18),
      BitConverter.SingleToInt32Bits(1)
    );
    var asset = (StaticMeshAsset)await ReadAssetAsync(source);
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(asset, destination);

    result.Status.Should().Be(OperationStatus.Succeeded);
    SourceLossInventory(result.Diagnostics).Should().Contain(item =>
      item.Path == "CommonBaseHeader.AttachmentTable[1]"
    );
  }

  [Fact]
  public async Task HighCornerFlagReportsItsIndependentSemanticPath()
  {
    var source = CreateCanonicalStaticAsset().GetSerializedRepresentation();
    source[0x18 + 0x198] = 0x80;
    var asset = (StaticMeshAsset)await ReadAssetAsync(source);
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(asset, destination);

    result.Status.Should().Be(OperationStatus.Succeeded);
    SourceLossInventory(result.Diagnostics).Should().Contain(item =>
      item.Path == "CommonBaseHeader.BoxCornerPassageFlags"
    );
    SourceLossInventory(result.Diagnostics).Should().NotContain(item =>
      item.Path == "CommonBaseHeader.RotatedOccupancyDescriptors"
      || item.Path == "CommonBaseHeader.RotatedCornerPassageMaps"
    );
  }

  private static StaticMeshAsset CreateCanonicalStaticAsset()
  {
    var result = StaticMeshBuilder.Create()
      .SetRenderObject(
        [
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
        ],
        [new CanonicalTriangle(0, 1, 2)]
      )
      .Build();
    result.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateCanonicalDynamicAsset()
  {
    var result = DynamicMeshBuilder.Create().SetRoot(DynamicEffectRecipes.Group()).Build();
    result.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static async Task<MeshAsset> ReadAssetAsync(byte[] source)
  {
    await using var stream = new MemoryStream(source);
    var result = await new MshReader().ReadAsync(stream);
    result.Status.Should().Be(OperationStatus.Succeeded);
    return result.Value!;
  }

  private static IReadOnlyList<(
    string Code,
    int EventId,
    DiagnosticSeverity Severity,
    string Path
  )> SourceLossInventory(
    IEnumerable<OperationDiagnostic> diagnostics
  )
  {
    return diagnostics
      .Where(item => item.Code == SourceRepresentationNotPreserved)
      .Select(item => (item.Code, item.EventId, item.Severity, item.Path))
      .ToArray();
  }

  private static IReadOnlyList<(
    string Code,
    int EventId,
    DiagnosticSeverity Severity,
    string Path
  )> WarningInventory(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return diagnostics
      .Where(item => item.Severity == DiagnosticSeverity.Warning)
      .Select(item => (item.Code, item.EventId, item.Severity, item.Path))
      .ToArray();
  }

  private static string CreateTemporaryDirectory()
  {
    var path = Path.Combine(Path.GetTempPath(), $"earthtool-source-loss-{Guid.NewGuid():N}");
    Directory.CreateDirectory(path);
    return path;
  }
}
