using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Expert;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace EarthTool.MSH.Tests;

public class CanonicalMeshAuthoringTests
{
  private static readonly Guid CreationGuid = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
  private static readonly MeshAssetLineageId LineageId = new(
    new Guid("11111111-2222-3333-4444-555555555555"));

  [Fact]
  public async Task CanonicalStaticBuilderProducesCoherentDeterministicAsset()
  {
    var vertices = CreateVertices();
    var triangles = CreateTriangles();
    var build = StaticMeshBuilder.Create(CreationGuid, LineageId)
      .SetAnimationLengths(2, 0, 0, 0)
      .SetRenderObject(vertices, triangles)
      .Build();

    build.TryGetValue(out var asset).Should().BeTrue();
    asset.Should().NotBeNull();
    asset!.Origin.Should().Be(MeshAssetOrigin.Canonical);
    asset.Kind.Should().Be(MeshAssetKind.Static);
    asset.LineageId.Should().Be(LineageId);
    asset.ArchiveFraming.Declaration.Should().Be(0x20D0A1FF);
    asset.ArchiveFraming.ArchiveType.Should().BeNull();
    asset.ArchiveFraming.CreationGuid.Should().Be(CreationGuid);
    asset.CommonBaseHeader.AnimationLengths.Should().Be(new AnimationClassBytes(2, 0, 0, 0));
    asset.CommonBaseHeader.BoxPresenceMask.Should().Be(0x00008000);
    BinaryPrimitives.ReadUInt16LittleEndian(asset.CommonBaseHeader.BoxTopElevations.Take(2).ToArray())
      .Should().Be(256);
    asset.CommonBaseHeader.RotatedOccupancyDescriptors.Should().Equal(
      ToBytes(0x3A000008u, 0x00008000u, 0xCA001000u, 0xFF000001u));
    asset.CommonBaseHeader.RotatedCornerPassageMaps.Should().Equal(
      ToBytes(0xFFFFFFFFFFFF0FFFul, 0x0FFFFFFFFFFFFFFFul,
        0xFFF0FFFFFFFFFFFFul, 0xFFFFFFFFFFFFFFF0ul));
    asset.CommonBaseHeader.AttachmentTable.Chunk(8).Should()
      .OnlyContain(record => IsCanonicalAbsentAttachment(record));
    asset.RootSourceObjectId.Lineage.Should().Be(LineageId);
    asset.StaticRenderObjectSequence.Should().ContainSingle().Subject.Id.Lineage.Should().Be(LineageId);

    var first = await WriteAsync(asset);
    var second = await WriteAsync(asset);

    first.Should().Equal(second);
    AssertSerializedApproval("msh-canonical-static", first);
    await using var source = new MemoryStream(first);
    var read = await new MshReader().ReadAsync(source);
    read.Status.Should().Be(OperationStatus.Succeeded);
    read.Value.Should().BeOfType<StaticMeshAsset>();
  }

  [Fact]
  public void CanonicalBuilderCopiesInputsAndRejectsNonFiniteValues()
  {
    var vertices = CreateVertices().ToList();
    var triangles = CreateTriangles().ToList();
    var builder = StaticMeshBuilder.Create(CreationGuid, LineageId)
      .SetRenderObject(vertices, triangles);

    vertices[0] = new CanonicalStaticVertex(new Vector3(99), Vector3.UnitZ, Vector2.Zero);
    triangles.Clear();
    var copied = builder.Build();
    copied.TryGetValue(out var copiedAsset).Should().BeTrue();
    copiedAsset!.StaticRenderObjectSequence[0].RenderVertices[0].Position.Should().Be(Vector3.Zero);

    var invalid = StaticMeshBuilder.Create(CreationGuid, LineageId)
      .SetRenderObject(
        new[]
        {
          new CanonicalStaticVertex(new Vector3(float.NaN, 0, 0), Vector3.UnitZ, Vector2.Zero),
          CreateVertices()[1],
          CreateVertices()[2]
        },
        CreateTriangles())
      .Build();

    invalid.TryGetValue(out _).Should().BeFalse();
    var diagnostic = invalid.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(MshDiagnosticCodes.InvalidAuthoringInput);
    diagnostic.EventId.Should().Be(1011);
    diagnostic.Path.Should().Be("StaticRenderObject.RenderVertices[0].Position");
  }

  [Fact]
  public async Task CanonicalDynamicBuilderProducesChildlessGroupWithFixedCommonHeaderProfile()
  {
    var build = DynamicMeshBuilder.Create(CreationGuid, LineageId).Build();

    build.TryGetValue(out var asset).Should().BeTrue();
    asset.Should().NotBeNull();
    asset!.Kind.Should().Be(MeshAssetKind.Dynamic);
    asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    asset.ArchiveFraming.Declaration.Should().Be(0x30D0A1FF);
    asset.ArchiveFraming.ArchiveType.Should().Be(1);
    asset.CommonBaseHeader.MeshKind.Should().Be(1);
    asset.CommonBaseHeader.SerializedRepresentation.Skip(0x0C).Take(0x1CC)
      .Should().OnlyContain(value => value == 0);
    asset.CommonBaseHeader.AttachmentTable.Chunk(8).Should()
      .OnlyContain(record => IsCanonicalAbsentAttachment(record));
    asset.RootDynamicObject.Children.Should().BeEmpty();

    var bytes = await WriteAsync(asset);
    AssertSerializedApproval("msh-canonical-dynamic", bytes);
    BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0x18 + 0x368)).Should().Be(0);
    BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0x18 + 0x40C)).Should().Be(0);
    await using var source = new MemoryStream(bytes);
    var read = await new MshReader().ReadAsync(source);
    read.Status.Should().Be(OperationStatus.Succeeded);
    read.Value.Should().BeOfType<DynamicMeshAsset>();
  }

  [Fact]
  public async Task ExpertConstructionPreservesAcceptedExactSerializedValues()
  {
    var fixture = OneTriangleMshFixture.Create();
    fixture[0x14 + 0x1D8] = 0;
    fixture[0x14 + 0x1D8 + 1] = 0;

    var build = MshExpert.CreateStatic(fixture, LineageId);

    build.TryGetValue(out var asset).Should().BeTrue();
    asset!.Origin.Should().Be(MeshAssetOrigin.Expert);
    asset.LineageId.Should().Be(LineageId);
    (await WriteAsync(asset)).Should().Equal(fixture);
  }

  [Fact]
  public async Task ExpertConstructionCannotBypassMalformedDynamicChildren()
  {
    var dynamic = DynamicMeshBuilder.Create(CreationGuid, LineageId).Build();
    dynamic.TryGetValue(out var asset).Should().BeTrue();
    var bytes = await WriteAsync(asset!);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x18 + 0x40C), 1);

    var build = MshExpert.CreateDynamic(bytes, LineageId);

    build.TryGetValue(out _).Should().BeFalse();
    build.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(MshDiagnosticCodes.StructuralHazard);
  }

  [Fact]
  public void BuildersAndExpertConstructionEnforceResourceLimitsBeforeAcceptance()
  {
    var staticBuild = StaticMeshBuilder.Create(CreationGuid, LineageId)
      .SetRenderObject(CreateVertices(), CreateTriangles())
      .Build(new MshOperationProfile(maxOutputBytes: 1));
    var oversized = new CountingByteEnumerable(100);

    var expertBuild = MshExpert.CreateStatic(
      oversized,
      LineageId,
      new MshOperationProfile(maxInputBytes: 4));

    staticBuild.TryGetValue(out _).Should().BeFalse();
    staticBuild.Diagnostics.Should().ContainSingle().Subject.Code
      .Should().Be(MshDiagnosticCodes.ResourceLimitExceeded);
    expertBuild.TryGetValue(out _).Should().BeFalse();
    expertBuild.Diagnostics.Should().ContainSingle().Subject.Code
      .Should().Be(MshDiagnosticCodes.ResourceLimitExceeded);
    oversized.ValuesProduced.Should().Be(5);
  }

  [Fact]
  public async Task EditCommitRetainsIdentityReturnsNewSnapshotAndReportsPreservation()
  {
    var source = BuildStatic();
    var id = source.StaticRenderObjectSequence[0].Id;
    var changedVertices = CreateVertices().ToArray();
    changedVertices[1] = new CanonicalStaticVertex(new Vector3(2, 0, 0), Vector3.UnitZ, Vector2.UnitX);

    var edit = source.Edit()
      .ReplaceGeometry(id, changedVertices, CreateTriangles())
      .Commit();

    edit.TryGetValue(out var edited).Should().BeTrue();
    edited.Should().NotBeSameAs(source);
    edited!.LineageId.Should().Be(source.LineageId);
    edited.RootSourceObjectId.Should().Be(source.RootSourceObjectId);
    edited.StaticRenderObjectSequence[0].Id.Should().Be(id);
    source.StaticRenderObjectSequence[0].RenderVertices[1].Position.Should().Be(Vector3.UnitX);
    edited.StaticRenderObjectSequence[0].RenderVertices[1].Position.Should().Be(new Vector3(2, 0, 0));
    edit.Preservation.Changes.Select(change => (change.FieldPath, change.Disposition)).Should().Equal(
      ("ArchiveFraming", PreservationDisposition.Retained),
      ("CommonBaseHeader", PreservationDisposition.Retained),
      ("RootSourceObjectId", PreservationDisposition.Retained),
      ("StaticRenderObjectSequence[0].Id", PreservationDisposition.Retained),
      ("StaticRenderObjectSequence[0].RenderVertices", PreservationDisposition.Regenerated),
      ("StaticRenderObjectSequence[0].Triangles", PreservationDisposition.Regenerated),
      ("StaticRenderObjectSequence[0].VertexBlockPadding", PreservationDisposition.Canonicalized),
      ("RootTrailingBytes", PreservationDisposition.Retained));

    var output = await WriteAsync(edited);
    await using var roundTripSource = new MemoryStream(output);
    var roundTrip = await new MshReader().ReadAsync(roundTripSource);
    roundTrip.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public void EditSessionAllocatesNewIdentityAndCanCommitOnlyOnce()
  {
    var source = BuildStatic();
    var oldId = source.StaticRenderObjectSequence[0].Id;
    var session = source.Edit();
    session.RemoveRenderObject(oldId);
    var newId = session.AddRenderObject(CreateVertices(), CreateTriangles());

    var edit = session.Commit();

    edit.TryGetValue(out var edited).Should().BeTrue();
    newId.Lineage.Should().Be(source.LineageId);
    newId.Value.Should().BeGreaterThan(oldId.Value);
    edited!.StaticRenderObjectSequence.Should().ContainSingle().Subject.Id.Should().Be(newId);
    edit.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0]"
      && change.Disposition == PreservationDisposition.Invalidated);
    edit.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0]"
      && change.Disposition == PreservationDisposition.Canonicalized);
    Action secondCommit = () => session.Commit();
    secondCommit.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void UnsupportedMultiObjectEditSequenceFailsWithoutDiscardingAnAddition()
  {
    var source = BuildStatic();
    var session = source.Edit();
    session.RemoveRenderObject(source.StaticRenderObjectSequence[0].Id);
    var firstId = session.AddRenderObject(CreateVertices(), CreateTriangles());
    var secondId = session.AddRenderObject(CreateVertices(), CreateTriangles());

    var edit = session.Commit();

    secondId.Value.Should().Be(firstId.Value + 1);
    edit.TryGetValue(out _).Should().BeFalse();
    edit.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(MshDiagnosticCodes.InvalidEdit);
    source.StaticRenderObjectSequence.Should().ContainSingle().Subject.Id.Value.Should().Be(1);
  }

  [Fact]
  public void MeshAssetMatchSelectsTheClosedBranch()
  {
    MeshAsset staticAsset = BuildStatic();
    var dynamicBuild = DynamicMeshBuilder.Create(CreationGuid, LineageId).Build();
    dynamicBuild.TryGetValue(out var dynamicAsset).Should().BeTrue();

    staticAsset.Match(_ => "static", _ => "dynamic").Should().Be("static");
    dynamicAsset!.Match(_ => "static", _ => "dynamic").Should().Be("dynamic");
  }

  private static StaticMeshAsset BuildStatic()
  {
    var build = StaticMeshBuilder.Create(CreationGuid, LineageId)
      .SetRenderObject(CreateVertices(), CreateTriangles())
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static CanonicalStaticVertex[] CreateVertices()
  {
    return
    [
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(new Vector3(0, 1, 1), Vector3.UnitZ, Vector2.UnitY)
    ];
  }

  private static CanonicalTriangle[] CreateTriangles()
  {
    return [new CanonicalTriangle(0, 1, 2)];
  }

  private static async Task<byte[]> WriteAsync(MeshAsset asset)
  {
    await using var destination = new MemoryStream();
    var write = await new MshWriter().WriteAsync(asset, destination);
    write.Status.Should().Be(OperationStatus.Succeeded);
    return destination.ToArray();
  }

  private static void AssertSerializedApproval(string name, byte[] bytes)
  {
    var actual = Convert.ToHexString(SHA256.HashData(bytes));
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var approvedPath = Path.Combine(root, "EarthTool.MSH.Tests", "Approvals", $"{name}.approved.sha256.txt");
    var receivedPath = Path.Combine(root, "EarthTool.MSH.Tests", "Approvals", $"{name}.received.sha256.txt");
    var approved = File.ReadAllText(approvedPath).Trim();
    if (actual != approved)
    {
      File.WriteAllText(receivedPath, actual + Environment.NewLine);
    }
    else
    {
      File.Delete(receivedPath);
    }

    actual.Should().Be(approved);
  }

  private static byte[] ToBytes(params uint[] values)
  {
    var bytes = new byte[values.Length * sizeof(uint)];
    for (var index = 0; index < values.Length; index++)
    {
      BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(index * sizeof(uint)), values[index]);
    }

    return bytes;
  }

  private static byte[] ToBytes(params ulong[] values)
  {
    var bytes = new byte[values.Length * sizeof(ulong)];
    for (var index = 0; index < values.Length; index++)
    {
      BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(index * sizeof(ulong)), values[index]);
    }

    return bytes;
  }

  private static bool IsCanonicalAbsentAttachment(byte[] record)
  {
    return BinaryPrimitives.ReadInt16LittleEndian(record) == short.MinValue
      && BinaryPrimitives.ReadInt16LittleEndian(record.AsSpan(2)) == short.MinValue
      && BinaryPrimitives.ReadInt16LittleEndian(record.AsSpan(4)) == short.MinValue;
  }

  private sealed class CountingByteEnumerable : IEnumerable<byte>
  {
    private readonly int _count;

    internal int ValuesProduced { get; private set; }

    internal CountingByteEnumerable(int count)
    {
      _count = count;
    }

    public IEnumerator<byte> GetEnumerator()
    {
      for (var index = 0; index < _count; index++)
      {
        ValuesProduced++;
        yield return 0;
      }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}
