using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Tests;

public class StaticMeshAssetTests
{
  [Fact]
  public async Task ReaderPreservesSingleRecordSequenceAndRootGrouping()
  {
    var fixture = StaticMeshSequenceFixture.CreateSingle();
    await using var source = new MemoryStream(fixture.Data);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Succeeded);
    var asset = result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    asset.StaticRenderObjectSequence.Should().ContainSingle();
    asset
      .RootSourceObject.StaticRenderObjects.Should()
      .ContainSingle()
      .Subject.Should()
      .BeSameAs(asset.StaticRenderObjectSequence[0]);
    asset.RootSourceObject.Children.Should().BeEmpty();
    asset.StoredTrailingHierarchyUnwindCount.Should().Be(1);
  }

  [Fact]
  public async Task ReaderPreservesAuthoritativeSequenceAndReconstructsGroupingTree()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    await using var source = new MemoryStream(fixture.Data);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Succeeded);
    var asset = result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    asset
      .StaticRenderObjectSequence.Select(item =>
        Encoding.ASCII.GetString(item.TexturePathBytes.ToArray())
      )
      .Should()
      .Equal(
        "Textures\\root-a.tex",
        "Textures\\barrel.tex",
        "Textures\\root-b.tex",
        "Textures\\rotor.tex"
      );
    asset
      .RootSourceObject.StaticRenderObjects.Should()
      .Equal(asset.StaticRenderObjectSequence[0], asset.StaticRenderObjectSequence[2]);
    asset.RootSourceObject.Children.Should().HaveCount(2);
    asset
      .RootSourceObject.Children[0]
      .StaticRenderObjects.Should()
      .Equal(asset.StaticRenderObjectSequence[1]);
    asset
      .RootSourceObject.Children[1]
      .StaticRenderObjects.Should()
      .Equal(asset.StaticRenderObjectSequence[3]);
    asset
      .RootSourceObject.StaticRenderObjects[0]
      .Should()
      .BeSameAs(asset.StaticRenderObjectSequence[0]);
    asset
      .RootSourceObject.StaticRenderObjects[1]
      .Should()
      .BeSameAs(asset.StaticRenderObjectSequence[2]);
    asset
      .RootSourceObject.Children[0]
      .StaticRenderObjects.Should()
      .ContainSingle()
      .Subject.Should()
      .BeSameAs(asset.StaticRenderObjectSequence[1]);
    asset
      .RootSourceObject.Children[1]
      .StaticRenderObjects.Should()
      .ContainSingle()
      .Subject.Should()
      .BeSameAs(asset.StaticRenderObjectSequence[3]);
    asset.StoredTrailingHierarchyUnwindCount.Should().Be(2);
    asset.ExpectedTrailingHierarchyUnwindCount.Should().Be(2);

    var barrel = asset.StaticRenderObjectSequence[1];
    barrel.KnownFlags.Should().HaveFlag(StaticRenderObjectFlags.Barrel);
    barrel.KnownFlags.Should().HaveFlag(StaticRenderObjectFlags.BeginsNestedSourceObject);
    barrel.Pivot.Should().Be(new Vector3(1, 2, 3));
    barrel.BarrelMaximumAngle.Should().Be(64);
    barrel.RenderVertices[0].NormalSharingIndex.Should().Be(0);
    barrel.RenderVertices[0].ReservedTextureComponent.Should().Be(0.25f);
    barrel.VertexBlockPadding.Should().Contain(0x5A);
    barrel.AnimationTracks.ScaleFrames.Should().ContainSingle().Subject.Should().Be(Vector3.One);
    barrel
      .AnimationTracks.TranslationFrames.Should()
      .ContainSingle()
      .Subject.Should()
      .Be(new Vector3(10, 20, 30));
    barrel
      .AnimationTracks.Matrices.Should()
      .ContainSingle()
      .Subject.Should()
      .Be(Matrix4x4.Identity);
    asset
      .StaticRenderObjectSequence.Select(item => item.NextRecordMarker)
      .Should()
      .Equal(0xDEADBEEFu, 2u, 3u, 0u);

    await using var destination = new MemoryStream();
    var write = await new MshWriter().WriteAsync(asset, destination);
    write.Status.Should().Be(OperationStatus.Succeeded);
    destination.ToArray().Should().Equal(fixture.Data);
  }

  [Fact]
  public async Task CanonicalBuilderDerivesSequenceHierarchyMarkersAndTrailingUnwind()
  {
    var root = new CanonicalStaticSourceObject(
      [RenderObject()],
      [
        new CanonicalStaticSourceObject([RenderObject(), RenderObject()]),
        new CanonicalStaticSourceObject([RenderObject()]),
      ]
    );
    var build = StaticMeshBuilder
      .Create(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
      .SetRootSourceObject(root)
      .Build();

    build.TryGetValue(out var asset).Should().BeTrue();
    asset!.StaticRenderObjectSequence.Should().HaveCount(4);
    asset
      .StaticRenderObjectSequence.Select(item => item.HierarchyUnwindCount)
      .Should()
      .Equal(0, 0, 0, 1);
    asset
      .StaticRenderObjectSequence.Select(item =>
        item.KnownFlags.HasFlag(StaticRenderObjectFlags.BeginsNestedSourceObject)
      )
      .Should()
      .Equal(false, true, false, true);
    asset
      .StaticRenderObjectSequence.Select(item => item.NextRecordMarker)
      .Should()
      .Equal(1, 1, 1, 0);
    asset.StoredTrailingHierarchyUnwindCount.Should().Be(2);
    asset.ExpectedTrailingHierarchyUnwindCount.Should().Be(2);

    var first = await WriteAsync(asset);
    var secondBuild = StaticMeshBuilder
      .Create(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
      .SetRootSourceObject(root)
      .Build();
    secondBuild.TryGetValue(out var second).Should().BeTrue();
    (await WriteAsync(second!)).Should().Equal(first);
  }

  [Fact]
  public void CanonicalAssemblerAuthorsFinalRepresentationsInOneCommit()
  {
    var root = new CanonicalStaticSourceObject([RenderObject()]);
    var input = new CanonicalStaticMeshAssemblyInput(
      new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
      new CanonicalStaticBaseHeaderInput(
        new AnimationClassBytes(1, 0, 0, 0),
        root.RenderObjects.SelectMany(record => record.RenderVertices),
        new CanonicalStaticFootprint(0x8000, new float[16], new byte[16]),
        new CanonicalHorizontalExtents(1, 2, 3, 4)
      ),
      root,
      new Dictionary<int, Vector3>
      {
        [0] = new Vector3(5, 6, 7),
      }
    );

    var result = CanonicalStaticMeshAssembler.Assemble(input);

    result.TryGetValue(out var asset).Should().BeTrue();
    asset!.Origin.Should().Be(MeshAssetOrigin.Canonical);
    asset.StaticRenderObjectSequence[0].Pivot.Should().Be(new Vector3(5, 6, 7));
    asset.CommonBaseHeader.AnimationLengths.Should().Be(new AnimationClassBytes(1, 0, 0, 0));
  }

  [Theory]
  [InlineData("underflow")]
  [InlineData("trailing")]
  [InlineData("sharing")]
  [InlineData("position-sharing")]
  [InlineData("next-marker")]
  public async Task ReaderRejectsMalformedHierarchyAndSharingWithoutPartialState(string mutation)
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    if (mutation == "underflow")
    {
      BinaryPrimitives.WriteUInt32LittleEndian(
        fixture.Data.AsSpan(fixture.RecordOffsets[0] + 8 + 0xA0),
        1
      );
    }
    else if (mutation == "trailing")
    {
      BinaryPrimitives.WriteUInt32LittleEndian(fixture.Data.AsSpan(0x14 + 0x368), 1);
    }
    else if (mutation == "sharing")
    {
      BinaryPrimitives.WriteUInt16LittleEndian(
        fixture.Data.AsSpan(fixture.RecordOffsets[0] + 8 + 0x90),
        0
      );
    }
    else if (mutation == "position-sharing")
    {
      BinaryPrimitives.WriteUInt16LittleEndian(
        fixture.Data.AsSpan(fixture.RecordOffsets[0] + 8 + 0x98),
        0
      );
    }
    else
    {
      var single = StaticMeshSequenceFixture.CreateSingle();
      fixture = single;
      BinaryPrimitives.WriteUInt32LittleEndian(fixture.Data.AsSpan(fixture.Data.Length - 4), 1);
    }

    await using var source = new MemoryStream(fixture.Data);
    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.StructuralHazard);
  }

  [Fact]
  public async Task ReaderEnforcesStaticSequenceAndHierarchyLimitsBeforeAcceptance()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var profiles = new[]
    {
      new MshOperationProfile(maxStaticRenderObjects: 3),
      new MshOperationProfile(maxStaticVerticesPerObject: 2),
      new MshOperationProfile(maxStaticTrianglesPerObject: 0),
      new MshOperationProfile(maxStaticVertexBlocksPerObject: 0),
      new MshOperationProfile(maxStaticAnimationFramesPerTrack: 0),
      new MshOperationProfile(maxStaticTexturePathBytes: 0),
      new MshOperationProfile(maxStaticHierarchyDepth: 1),
    };

    foreach (var profile in profiles)
    {
      await using var source = new MemoryStream(fixture.Data);
      var result = await new MshReader().ReadAsync(source, profile);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Value.Should().BeNull();
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(MshDiagnosticCodes.ResourceLimitExceeded);
    }
  }

  [Fact]
  public void CanonicalBuilderReturnsFailuresForLaterTriangleAndVertexBlockLimit()
  {
    var invalidTriangle = StaticMeshBuilder
      .Create()
      .SetRenderObject(Vertices(), [new CanonicalTriangle(0, 1, 2), new CanonicalTriangle(0, 1, 3)])
      .Build();
    var blockLimited = StaticMeshBuilder
      .Create()
      .SetRenderObject(Vertices(), Triangles())
      .Build(new MshOperationProfile(maxStaticVertexBlocksPerObject: 0));

    invalidTriangle.TryGetValue(out _).Should().BeFalse();
    invalidTriangle
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.InvalidAuthoringInput);
    blockLimited.TryGetValue(out _).Should().BeFalse();
    blockLimited
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.ResourceLimitExceeded);
  }

  private static CanonicalStaticRenderObject RenderObject()
  {
    return new CanonicalStaticRenderObject(Vertices(), Triangles());
  }

  private static CanonicalStaticVertex[] Vertices()
  {
    return
    [
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
    ];
  }

  private static CanonicalTriangle[] Triangles()
  {
    return [new CanonicalTriangle(0, 1, 2)];
  }

  private static async Task<byte[]> WriteAsync(MeshAsset asset)
  {
    await using var destination = new MemoryStream();
    var result = await new MshWriter().WriteAsync(asset, destination);
    result.Status.Should().Be(OperationStatus.Succeeded);
    return destination.ToArray();
  }
}
