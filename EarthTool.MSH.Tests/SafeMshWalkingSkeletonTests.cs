using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using System.Numerics;

namespace EarthTool.MSH.Tests;

public class SafeMshWalkingSkeletonTests
{
  [Fact]
  public async Task PublicReaderReturnsImmutableOneTriangleAsset()
  {
    await using var source = new MemoryStream(OneTriangleMshFixture.Create());

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Succeeded);
    var asset = result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    asset.ArchiveFraming.CreationGuid.Should().Be(OneTriangleMshFixture.CreationGuid);
    var renderObject = asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    renderObject.RenderVertices.Select(vertex => vertex.Position)
      .Should().Equal(Vector3.Zero, Vector3.UnitX, Vector3.UnitY);
    renderObject.RenderVertices.Should().OnlyContain(vertex => vertex.Normal == Vector3.UnitZ);
    renderObject.Triangles.Should().ContainSingle().Subject.Should().Be(new StaticTriangle(0, 1, 2, 1));
    Action mutate = () => ((IList<RenderVertex>)renderObject.RenderVertices).Add(default);
    mutate.Should().Throw<NotSupportedException>();
    source.CanRead.Should().BeTrue();
  }

  [Fact]
  public async Task PublicReaderRejectsUnsupportedAttachmentDomainWithoutPartialAsset()
  {
    var fixture = OneTriangleMshFixture.Create();
    fixture[0x14 + 0x1D8] = 0;
    fixture[0x14 + 0x1D8 + 1] = 0;
    await using var source = new MemoryStream(fixture);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(MshDiagnosticCodes.UnsupportedDomain);
    diagnostic.Data["domain"].Should().Be("Attachment");
  }

  [Fact]
  public async Task TransactionalWriterPreservesExistingDestinationWhenCancelled()
  {
    var asset = await ReadAssetAsync();
    var path = GetTemporaryPath();
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(path, original);
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    try
    {
      var result = await new MshWriter().WriteFileAsync(asset, path, cancellationToken: cancellation.Token);

      result.Status.Should().Be(OperationStatus.Cancelled);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public async Task TransactionalWriterPreservesExistingDestinationWhenCommitFails()
  {
    var asset = await ReadAssetAsync();
    var path = GetTemporaryPath();
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var result = await new MshWriter(new FailingTransactionalFileSystem()).WriteFileAsync(asset, path);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(MshDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public async Task TransactionalWriterPreservesExistingDestinationWhenValidationFails()
  {
    var asset = await ReadAssetAsync();
    var path = GetTemporaryPath();
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var profile = new MshOperationProfile(maxOutputBytes: 1);
      var result = await new MshWriter().WriteFileAsync(asset, path, profile);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(MshDiagnosticCodes.ResourceLimitExceeded);
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
    var types = typeof(OperationResult).Assembly.ExportedTypes
      .Where(type => type.Namespace == "EarthTool.Common.Operations")
      .Concat(typeof(MeshAsset).Assembly.ExportedTypes
        .Where(type => type.Namespace is "EarthTool.MSH.Assets" or "EarthTool.MSH.Operations"))
      .Concat(new[] { typeof(MshReader), typeof(MshValidator), typeof(MshWriter) });

    PublicApiApproval.Verify("msh", types);
  }

  private static async Task<StaticMeshAsset> ReadAssetAsync()
  {
    await using var stream = new MemoryStream(OneTriangleMshFixture.Create());
    var result = await new MshReader().ReadAsync(stream);
    return result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
  }

  private static string GetTemporaryPath()
  {
    return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.msh");
  }

  private sealed class FailingTransactionalFileSystem : ITransactionalFileSystem
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
