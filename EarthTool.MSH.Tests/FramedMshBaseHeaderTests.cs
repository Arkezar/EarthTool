using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using Microsoft.Extensions.Logging;

namespace EarthTool.MSH.Tests;

public class FramedMshBaseHeaderTests
{
  public static TheoryData<uint, uint?, Guid?> AcceptedFraming => new()
  {
    { 0x00D0A1FF, null, null },
    { 0x10D0A1FF, 0, null },
    { 0x20D0A1FF, null, OneTriangleMshFixture.CreationGuid },
    { 0x30D0A1FF, 0, OneTriangleMshFixture.CreationGuid }
  };

  [Theory]
  [MemberData(nameof(AcceptedFraming))]
  public async Task PublicOperationsPreserveEveryAcceptedFramingCombination(
    uint declaration,
    uint? archiveType,
    Guid? creationGuid)
  {
    var fixture = OneTriangleMshFixture.Create(declaration, archiveType, creationGuid);

    var (asset, firstOutput) = await ReadAndWriteAsync(fixture);
    var (_, secondOutput) = await ReadAndWriteAsync(firstOutput);

    asset.ArchiveFraming.Declaration.Should().Be(declaration);
    asset.ArchiveFraming.ArchiveType.Should().Be(archiveType);
    asset.ArchiveFraming.CreationGuid.Should().Be(creationGuid);
    firstOutput.Should().Equal(fixture);
    secondOutput.Should().Equal(firstOutput);
  }

  [Fact]
  public async Task PublicOperationsPreserveEveryFixedCommonBaseHeaderRegion()
  {
    var fixture = OneTriangleMshFixture.Create(
      0x20D0A1FF,
      null,
      OneTriangleMshFixture.CreationGuid,
      OneTriangleMshFixture.WriteDistinctCommonHeaderRegions);

    var (asset, output) = await ReadAndWriteAsync(fixture);

    var header = asset.CommonBaseHeader;
    header.Version.Should().Be(1);
    header.MeshKind.Should().Be(0);
    header.BoxPresenceMask.Should().Be(0xA1B2C3D4);
    header.AnimationLengths.Should().Be(new AnimationClassBytes(1, 2, 3, 4));
    header.AnimationFrameIndices.Should().Be(new AnimationClassBytes(0x11, 0x12, 0x13, 0x14));
    header.CannonRenderPositions.Should().Equal(fixture[(0x14 + 0x18)..(0x14 + 0x48)]);
    header.StaticSpotLights.Should().Equal(fixture[(0x14 + 0x48)..(0x14 + 0x108)]);
    header.StaticOmniLights.Should().Equal(fixture[(0x14 + 0x108)..(0x14 + 0x178)]);
    header.BoxTopElevations.Should().Equal(fixture[(0x14 + 0x178)..(0x14 + 0x198)]);
    header.BoxCornerPassageFlags.Should().Equal(fixture[(0x14 + 0x198)..(0x14 + 0x1A8)]);
    header.RotatedOccupancyDescriptors.Should().Equal(fixture[(0x14 + 0x1A8)..(0x14 + 0x1B8)]);
    header.RotatedCornerPassageMaps.Should().Equal(fixture[(0x14 + 0x1B8)..(0x14 + 0x1D8)]);
    header.AttachmentTable.Should().Equal(fixture[(0x14 + 0x1D8)..(0x14 + 0x360)]);
    header.HorizontalExtents.Should().Equal(fixture[(0x14 + 0x360)..(0x14 + 0x368)]);
    output.Should().Equal(fixture);
  }

  [Fact]
  public async Task PublicReaderPreservesArchiveTypeMeshKindDisagreementAndLogsWarningExactlyOnce()
  {
    var fixture = OneTriangleMshFixture.Create(
      0x30D0A1FF,
      7,
      OneTriangleMshFixture.CreationGuid);
    var logger = new RecordingLogger<MshReader>();
    await using var source = new MemoryStream(fixture);

    var result = await new MshReader(logger).ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Succeeded);
    result.Value.Should().BeOfType<StaticMeshAsset>();
    var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
    AssertCompatibilityDiagnostic(diagnostic, "ArchiveFraming.ArchiveType", 4);
    diagnostic.Data["archiveType"].Should().Be("7");
    diagnostic.Data["meshKind"].Should().Be("0");
    logger.Entries.Should().ContainSingle();
    logger.Entries[0].EventId.Id.Should().Be(diagnostic.EventId);
    logger.Entries[0].Level.Should().Be(LogLevel.Warning);
    var (_, output) = await ReadAndWriteAsync(fixture);
    output.Should().Equal(fixture);
  }

  [Fact]
  public async Task PublicOperationsPreserveOpaqueRootTrailingBytes()
  {
    var trailing = new byte[] { 0x4D, 0x45, 0x53, 0x48, 0x00, 0xFF };
    var fixture = OneTriangleMshFixture.Create(
      0x20D0A1FF,
      null,
      OneTriangleMshFixture.CreationGuid,
      rootTrailingBytes: trailing);

    var (asset, output, diagnostics) = await ReadAndWriteWithDiagnosticsAsync(fixture);

    asset.RootTrailingBytes.Should().Equal(trailing);
    var diagnostic = diagnostics.Should().ContainSingle().Subject;
    AssertCompatibilityDiagnostic(diagnostic, "RootTrailingBytes", fixture.Length - trailing.Length);
    diagnostic.Data["length"].Should().Be(trailing.Length.ToString());
    output.Should().Equal(fixture);
  }

  [Theory]
  [InlineData(0)]
  [InlineData(3)]
  [InlineData(4)]
  [InlineData(7)]
  [InlineData(19)]
  [InlineData(20)]
  [InlineData(0x37B)]
  [InlineData(0x45C)]
  public async Task PublicReaderRejectsRepresentativeTruncationsWithoutPartialAsset(int length)
  {
    var fixture = OneTriangleMshFixture.Create();
    await using var source = new MemoryStream(fixture[..length]);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().BeOneOf(
      MshDiagnosticCodes.InvalidFraming,
      MshDiagnosticCodes.StructuralHazard);
  }

  [Fact]
  public async Task PublicReaderEnforcesInputAndTrailingLimitsBeforeAcceptance()
  {
    var trailing = new byte[] { 1, 2 };
    var fixture = OneTriangleMshFixture.Create(
      0x20D0A1FF,
      null,
      OneTriangleMshFixture.CreationGuid,
      rootTrailingBytes: trailing);
    var exactProfile = new MshOperationProfile(maxInputBytes: fixture.Length, maxRootTrailingBytes: 2);
    var inputLimitedProfile = new MshOperationProfile(maxInputBytes: fixture.Length - 1);
    var limitedProfile = new MshOperationProfile(maxInputBytes: fixture.Length, maxRootTrailingBytes: 1);

    await using var exactSource = new MemoryStream(fixture);
    var exact = await new MshReader().ReadAsync(exactSource, exactProfile);
    await using var inputLimitedSource = new MemoryStream(fixture);
    var inputLimited = await new MshReader().ReadAsync(inputLimitedSource, inputLimitedProfile);
    await using var limitedSource = new MemoryStream(fixture);
    var limited = await new MshReader().ReadAsync(limitedSource, limitedProfile);

    exact.Status.Should().Be(OperationStatus.Succeeded);
    inputLimited.Status.Should().Be(OperationStatus.Failed);
    inputLimited.Diagnostics.Should().ContainSingle().Subject.Code
      .Should().Be(MshDiagnosticCodes.ResourceLimitExceeded);
    limited.Status.Should().Be(OperationStatus.Failed);
    limited.Value.Should().BeNull();
    limited.Diagnostics.Should().ContainSingle().Subject.Code
      .Should().Be(MshDiagnosticCodes.ResourceLimitExceeded);
  }

  [Fact]
  public async Task PublicReaderCapsCompatibilityDiagnostics()
  {
    var fixture = OneTriangleMshFixture.Create(
      0xB0D0A1FF,
      7,
      OneTriangleMshFixture.CreationGuid,
      rootTrailingBytes: new byte[] { 1 });
    var profile = new MshOperationProfile(maxDiagnostics: 2);
    await using var source = new MemoryStream(fixture);

    var result = await new MshReader().ReadAsync(source, profile);

    result.Status.Should().Be(OperationStatus.Succeeded);
    result.Diagnostics.Should().HaveCount(2);
    result.Diagnostics[^1].Code.Should().Be(MshDiagnosticCodes.DiagnosticsTruncated);
    result.Diagnostics[^1].Severity.Should().Be(DiagnosticSeverity.Warning);
    result.Diagnostics[^1].EventId.Should().Be(1010);
    result.Diagnostics[^1].Path.Should().Be("$");
    result.Diagnostics[^1].ByteOffset.Should().BeNull();
    result.Diagnostics[^1].Data["suppressed"].Should().Be("2");
    var (_, output) = await ReadAndWriteAsync(fixture);
    output.Should().Equal(fixture);
  }

  [Fact]
  public async Task PublicReaderSupportsCallerOwnedNonSeekableOneByteReads()
  {
    var source = new ControlledReadStream(OneTriangleMshFixture.Create(), maximumReadSize: 1);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Succeeded);
    source.Disposed.Should().BeFalse();
  }

  [Fact]
  public async Task PublicReaderReturnsIoFailureForFaultingCallerOwnedStream()
  {
    var source = new ControlledReadStream(OneTriangleMshFixture.Create(), failAfterBytes: 32);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(MshDiagnosticCodes.IoFailure);
    diagnostic.EventId.Should().Be(1007);
    diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
    diagnostic.Path.Should().Be("$");
    diagnostic.Data["exceptionType"].Should().Be(typeof(IOException).FullName);
    source.Disposed.Should().BeFalse();
  }

  [Fact]
  public async Task PublicReaderObservesCancellationDuringCallerOwnedStreamRead()
  {
    using var cancellation = new CancellationTokenSource();
    var source = new ControlledReadStream(
      OneTriangleMshFixture.Create(),
      maximumReadSize: 32,
      cancellation: cancellation);

    var result = await new MshReader().ReadAsync(source, cancellationToken: cancellation.Token);

    result.Status.Should().Be(OperationStatus.Cancelled);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(MshDiagnosticCodes.Cancelled);
    source.Disposed.Should().BeFalse();
  }

  [Fact]
  public async Task PublicWriterReturnsIoFailureAndLeavesCallerOwnedStreamOpen()
  {
    var fixture = OneTriangleMshFixture.Create();
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    var destination = new FaultingWriteStream();

    var result = await new MshWriter().WriteAsync(asset, destination);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(MshDiagnosticCodes.IoFailure);
    destination.Disposed.Should().BeFalse();
  }

  [Fact]
  public async Task PublicValidatorReturnsCompatibilityWarningsWithoutChangingAcceptedAsset()
  {
    var fixture = OneTriangleMshFixture.Create(
      0x20D0A1FF,
      null,
      OneTriangleMshFixture.CreationGuid,
      rootTrailingBytes: new byte[] { 1 });
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;

    var validation = await new MshValidator().ValidateAsync(asset);

    validation.Status.Should().Be(OperationStatus.Succeeded);
    validation.Diagnostics.Should().ContainSingle().Subject.Code
      .Should().Be(MshDiagnosticCodes.CompatibilityAnomaly);
    asset.RootTrailingBytes.Should().Equal(1);
  }

  [Fact]
  public async Task SuccessfulValidatorAndTransactionalWriterWarningsAreLoggedExactlyOncePerOperation()
  {
    var fixture = OneTriangleMshFixture.Create(
      0x20D0A1FF,
      null,
      OneTriangleMshFixture.CreationGuid,
      rootTrailingBytes: new byte[] { 1 });
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    var validatorLogger = new RecordingLogger<MshValidator>();
    var writerLogger = new RecordingLogger<MshWriter>();
    var destinationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.msh");

    try
    {
      var validation = await new MshValidator(validatorLogger).ValidateAsync(asset);
      var write = await new MshWriter(writerLogger).WriteFileAsync(asset, destinationPath);

      validation.Status.Should().Be(OperationStatus.Succeeded);
      write.Status.Should().Be(OperationStatus.Succeeded);
      validatorLogger.Entries.Should().ContainSingle();
      writerLogger.Entries.Should().ContainSingle();
      (await File.ReadAllBytesAsync(destinationPath)).Should().Equal(fixture);
    }
    finally
    {
      File.Delete(destinationPath);
    }
  }

  [Theory]
  [InlineData(0u)]
  [InlineData(4u)]
  [InlineData(uint.MaxValue)]
  public async Task PublicReaderRejectsUnsupportedVertexCountsBeforeMaterialization(uint vertexCount)
  {
    var fixture = OneTriangleMshFixture.Create();
    System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(fixture.AsSpan(0x380), vertexCount);
    await using var source = new MemoryStream(fixture);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(MshDiagnosticCodes.UnsupportedDomain);
    diagnostic.Data["domain"].Should().Be("Geometry");
  }

  [Fact]
  public async Task PublicReaderFailsClosedForStoredHierarchyUnwindUntilHierarchyIsSupported()
  {
    var fixture = OneTriangleMshFixture.Create();
    System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(fixture.AsSpan(0x37C), 2);
    await using var source = new MemoryStream(fixture);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(MshDiagnosticCodes.UnsupportedDomain);
    diagnostic.Data["domain"].Should().Be("Hierarchy");
  }

  [Fact]
  public async Task TransactionalWriterValidatesStagedBytesBeforeCommit()
  {
    var fixture = OneTriangleMshFixture.Create();
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    var fileSystem = new CorruptingTransactionalFileSystem();
    var destinationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.msh");
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(destinationPath, original);

    try
    {
      var result = await new MshWriter(fileSystem).WriteFileAsync(asset, destinationPath);

      result.Status.Should().Be(OperationStatus.Failed);
      fileSystem.Committed.Should().BeFalse();
      (await File.ReadAllBytesAsync(destinationPath)).Should().Equal(original);
    }
    finally
    {
      File.Delete(destinationPath);
    }
  }

  [Fact]
  public async Task TransactionalWriterPreservesCancellationDuringStagedValidation()
  {
    var fixture = OneTriangleMshFixture.Create();
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    using var cancellation = new CancellationTokenSource();
    var fileSystem = new CancellingTransactionalFileSystem(cancellation);
    var destinationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.msh");
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(destinationPath, original);

    try
    {
      var result = await new MshWriter(fileSystem)
        .WriteFileAsync(asset, destinationPath, cancellationToken: cancellation.Token);

      result.Status.Should().Be(OperationStatus.Cancelled);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(MshDiagnosticCodes.Cancelled);
      fileSystem.Committed.Should().BeFalse();
      (await File.ReadAllBytesAsync(destinationPath)).Should().Equal(original);
    }
    finally
    {
      File.Delete(destinationPath);
    }
  }

  private static void AssertCompatibilityDiagnostic(OperationDiagnostic diagnostic, string path, long offset)
  {
    diagnostic.Code.Should().Be(MshDiagnosticCodes.CompatibilityAnomaly);
    diagnostic.EventId.Should().Be(1009);
    diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
    diagnostic.Path.Should().Be(path);
    diagnostic.ByteOffset.Should().Be(offset);
  }

  private static async Task<(StaticMeshAsset Asset, byte[] Output)> ReadAndWriteAsync(byte[] fixture)
  {
    var (asset, output, _) = await ReadAndWriteWithDiagnosticsAsync(fixture);
    return (asset, output);
  }

  private static void AssertSemanticallyEquivalent(StaticMeshAsset expected, StaticMeshAsset actual)
  {
    actual.ArchiveFraming.Declaration.Should().Be(expected.ArchiveFraming.Declaration);
    actual.ArchiveFraming.ArchiveType.Should().Be(expected.ArchiveFraming.ArchiveType);
    actual.ArchiveFraming.CreationGuid.Should().Be(expected.ArchiveFraming.CreationGuid);
    actual.CommonBaseHeader.SerializedRepresentation
      .Should().Equal(expected.CommonBaseHeader.SerializedRepresentation);
    actual.RootTrailingBytes.Should().Equal(expected.RootTrailingBytes);
    actual.StaticRenderObjectSequence.Should().HaveSameCount(expected.StaticRenderObjectSequence);
    actual.StaticRenderObjectSequence[0].RenderVertices
      .Should().Equal(expected.StaticRenderObjectSequence[0].RenderVertices);
    actual.StaticRenderObjectSequence[0].Triangles
      .Should().Equal(expected.StaticRenderObjectSequence[0].Triangles);
  }

  private static async Task<(StaticMeshAsset Asset, byte[] Output, IReadOnlyList<OperationDiagnostic> Diagnostics)>
    ReadAndWriteWithDiagnosticsAsync(byte[] fixture)
  {
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    await using var destination = new MemoryStream();

    var write = await new MshWriter().WriteAsync(asset, destination);

    write.Status.Should().Be(OperationStatus.Succeeded);
    source.CanRead.Should().BeTrue();
    destination.CanWrite.Should().BeTrue();
    var output = destination.ToArray();
    await using var roundTripSource = new MemoryStream(output);
    var roundTrip = await new MshReader().ReadAsync(roundTripSource);
    var roundTripAsset = roundTrip.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    AssertSemanticallyEquivalent(asset, roundTripAsset);
    await using var secondDestination = new MemoryStream();
    var secondWrite = await new MshWriter().WriteAsync(roundTripAsset, secondDestination);
    secondWrite.Status.Should().Be(OperationStatus.Succeeded);
    secondDestination.ToArray().Should().Equal(output);
    return (asset, output, read.Diagnostics);
  }

  private sealed class RecordingLogger<T> : ILogger<T>
  {
    internal List<(LogLevel Level, EventId EventId)> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
      return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
      return true;
    }

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      Entries.Add((logLevel, eventId));
    }
  }

  private sealed class ControlledReadStream : MemoryStream
  {
    private readonly int _maximumReadSize;
    private readonly int? _failAfterBytes;
    private readonly CancellationTokenSource? _cancellation;
    private int _bytesRead;

    internal bool Disposed { get; private set; }

    internal ControlledReadStream(
      byte[] bytes,
      int maximumReadSize = int.MaxValue,
      int? failAfterBytes = null,
      CancellationTokenSource? cancellation = null)
      : base(bytes)
    {
      _maximumReadSize = maximumReadSize;
      _failAfterBytes = failAfterBytes;
      _cancellation = cancellation;
    }

    public override bool CanSeek => false;

    public override Task<int> ReadAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (_failAfterBytes.HasValue && _bytesRead >= _failAfterBytes.Value)
      {
        throw new IOException("Injected read failure.");
      }

      var read = base.Read(buffer, offset, Math.Min(count, _maximumReadSize));
      _bytesRead += read;
      if (_cancellation is not null && _bytesRead > 0)
      {
        _cancellation.Cancel();
      }

      return Task.FromResult(read);
    }

    protected override void Dispose(bool disposing)
    {
      Disposed = true;
      base.Dispose(disposing);
    }
  }

  private sealed class FaultingWriteStream : MemoryStream
  {
    internal bool Disposed { get; private set; }

    public override Task WriteAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      throw new IOException("Injected write failure.");
    }

    protected override void Dispose(bool disposing)
    {
      Disposed = true;
      base.Dispose(disposing);
    }
  }

  private sealed class CorruptingTransactionalFileSystem : ITransactionalFileSystem
  {
    private MemoryStream? _temporary;

    internal bool Committed { get; private set; }

    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + ".test.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      _temporary = new MemoryStream();
      return _temporary;
    }

    public Stream OpenTemporaryRead(string temporaryPath)
    {
      var bytes = _temporary!.ToArray();
      return new MemoryStream(bytes[..10]);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      Committed = true;
    }

    public bool TryDelete(string temporaryPath)
    {
      return true;
    }
  }

  private sealed class CancellingTransactionalFileSystem : ITransactionalFileSystem
  {
    private readonly CancellationTokenSource _cancellation;
    private MemoryStream? _temporary;

    internal bool Committed { get; private set; }

    internal CancellingTransactionalFileSystem(CancellationTokenSource cancellation)
    {
      _cancellation = cancellation;
    }

    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + ".test.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      _temporary = new MemoryStream();
      return _temporary;
    }

    public Stream OpenTemporaryRead(string temporaryPath)
    {
      return new ControlledReadStream(
        _temporary!.ToArray(),
        maximumReadSize: 32,
        cancellation: _cancellation);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      Committed = true;
    }

    public bool TryDelete(string temporaryPath)
    {
      return true;
    }
  }
}
