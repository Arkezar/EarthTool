using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace EarthTool.MSH.Tests;

public class GltfWalkingSkeletonTests
{
  private static readonly Guid LineageId = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
  private static readonly Guid DocumentId = new("11111111-2222-3333-4444-555555555555");

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
    finally
    {
      File.Delete(path);
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

  private static int GetBinaryChunkOffset(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return 12 + 8 + jsonLength + 8;
  }

  private static float ReadSingle(byte[] data, int offset)
  {
    return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset)));
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
}
