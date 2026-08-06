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

  [Fact]
  public async Task AssemblerUsesSequenceOrdinalsAndLeavesSourceReferencesUnchanged()
  {
    await using var stream = new MemoryStream(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var read = await new MshReader().ReadAsync(stream);
    var source = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    var sourceSequence = source.StaticRenderObjectSequence.ToArray();
    var sourceTextureOrder = sourceSequence.Select(TexturePath).ToArray();
    var assembler = new StaticMeshAssembler(source);
    var target = source.StaticRenderObjectSequence[1];
    var ordinal = assembler.GetRenderObjectOrdinal(target);

    assembler.ReplacePivot(ordinal, new Vector3(9, 8, 7));
    var result = assembler.Commit();

    result.TryGetValue(out var asset).Should().BeTrue();
    source.StaticRenderObjectSequence.Should().Equal(sourceSequence);
    source.StaticRenderObjectSequence[1].Should().BeSameAs(target);
    asset!.StaticRenderObjectSequence.Select(TexturePath)
      .Should().Equal(sourceTextureOrder);
    asset.StaticRenderObjectSequence[1].Pivot.Should().Be(new Vector3(9, 8, 7));
    asset.StaticRenderObjectSequence[0].Pivot.Should().NotBe(new Vector3(9, 8, 7));
    assembler.Trace.ResultRenderObjectOrdinals.Should().Equal(0, 1, 2, 3);
  }

  [Fact]
  public void HierarchyAndPartitionMembershipEditsProduceOneFinalCanonicalSequence()
  {
    var build = StaticMeshBuilder
      .Create()
      .SetRootSourceObject(
        new CanonicalStaticSourceObject(
          [RenderObject(), RenderObject()],
          [
            new CanonicalStaticSourceObject([RenderObject()]),
            new CanonicalStaticSourceObject([RenderObject()]),
          ]
        )
      )
      .Build();
    build.TryGetValue(out var source).Should().BeTrue();
    var root = source!.RootSourceObject;
    var assembler = new StaticMeshAssembler(source);
    var rootOrdinal = assembler.GetSourceObjectOrdinal(root);
    var rootRenderObjectOrdinals = root.StaticRenderObjects
      .Select(assembler.GetRenderObjectOrdinal)
      .ToArray();
    assembler.RemoveRenderObject(rootRenderObjectOrdinals[0]);
    var additionOrdinal = assembler.AddRenderObject(rootOrdinal, Vertices(), Triangles());
    var reorderedRoot = new StaticSourceObjectAssembly(
      rootOrdinal,
      rootRenderObjectOrdinals,
      root.Children.Reverse().Select(assembler.CreateSourceObjectAssembly)
    );
    var finalSequence = new[]
    {
      rootRenderObjectOrdinals[1],
      assembler.GetRenderObjectOrdinal(root.Children[1].StaticRenderObjects[0]),
      assembler.GetRenderObjectOrdinal(root.Children[0].StaticRenderObjects[0]),
      additionOrdinal,
    };
    assembler.ApplyHierarchy(
      reorderedRoot,
      finalSequence
    );

    var result = assembler.Commit();

    result.TryGetValue(out var edited).Should().BeTrue();
    assembler.Trace.ResultRenderObjectOrdinals.Should().Equal(finalSequence);
    edited!.RootSourceObject.StaticRenderObjects.Should()
      .Equal(edited.StaticRenderObjectSequence[0], edited.StaticRenderObjectSequence[3]);
    edited.RootSourceObject.Children[0].StaticRenderObjects.Should()
      .Equal(edited.StaticRenderObjectSequence[1]);
    edited.RootSourceObject.Children[1].StaticRenderObjects.Should()
      .Equal(edited.StaticRenderObjectSequence[2]);
  }

  [Fact]
  public async Task HierarchyEditPreservesAuthoritativeNonFlattenedSequence()
  {
    await using var stream = new MemoryStream(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var read = await new MshReader().ReadAsync(stream);
    var source = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    var sourceSequence = source.StaticRenderObjectSequence.ToArray();
    var expectedTextureOrder = sourceSequence.Select(TexturePath).ToArray();
    var assembler = new StaticMeshAssembler(source);
    var authoritativeSequence = sourceSequence.Select(assembler.GetRenderObjectOrdinal).ToArray();
    assembler.ApplyHierarchy(
      assembler.CreateSourceObjectAssembly(source.RootSourceObject),
      authoritativeSequence
    );

    var result = assembler.Commit();

    result.TryGetValue(out var edited).Should().BeTrue();
    source.StaticRenderObjectSequence.Should().Equal(sourceSequence);
    assembler.Trace.ResultRenderObjectOrdinals.Should().Equal(authoritativeSequence);
    edited!.StaticRenderObjectSequence.Select(TexturePath)
      .Should().Equal(expectedTextureOrder);
  }

  [Theory]
  [InlineData("duplicate")]
  [InlineData("missing")]
  public async Task HierarchyEditRejectsSequenceThatDoesNotExactlyMatchFinalMembership(
    string invalidity
  )
  {
    await using var stream = new MemoryStream(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var read = await new MshReader().ReadAsync(stream);
    var source = read.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    var sourceSequence = source.StaticRenderObjectSequence.ToArray();
    var assembler = new StaticMeshAssembler(source);
    var sourceOrdinals = sourceSequence.Select(assembler.GetRenderObjectOrdinal).ToArray();
    var invalidSequence = invalidity == "duplicate"
      ? sourceOrdinals.Take(sourceOrdinals.Length - 1).Append(sourceOrdinals[0]).ToArray()
      : sourceOrdinals.Take(sourceOrdinals.Length - 1).ToArray();
    assembler.ApplyHierarchy(
      assembler.CreateSourceObjectAssembly(source.RootSourceObject),
      invalidSequence
    );

    var result = assembler.Commit();

    result.TryGetValue(out _).Should().BeFalse();
    result.Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.InvalidAuthoringInput);
    source.StaticRenderObjectSequence.Should().Equal(sourceSequence);
  }

  [Fact]
  public void HierarchyEditRejectsSequencePlannedBeforeMembershipChanges()
  {
    var build = StaticMeshBuilder
      .Create()
      .SetRootSourceObject(new CanonicalStaticSourceObject([RenderObject(), RenderObject()]))
      .Build();
    build.TryGetValue(out var source).Should().BeTrue();
    var sourceSequence = source!.StaticRenderObjectSequence.ToArray();
    var assembler = new StaticMeshAssembler(source);
    var sourceOrdinals = sourceSequence.Select(assembler.GetRenderObjectOrdinal).ToArray();
    assembler.RemoveRenderObject(sourceOrdinals[0]);
    assembler.AddRenderObject(
      assembler.GetSourceObjectOrdinal(source.RootSourceObject),
      Vertices(),
      Triangles()
    );
    assembler.ApplyHierarchy(
      assembler.CreateSourceObjectAssembly(source.RootSourceObject),
      sourceOrdinals
    );

    var result = assembler.Commit();

    result.TryGetValue(out _).Should().BeFalse();
    result.Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.InvalidAuthoringInput);
    source.StaticRenderObjectSequence.Should().Equal(sourceSequence);
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
  public void GeometryAssemblyRetainsSequenceOrderAndSourceMemberships()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var build = EarthTool.MSH.Expert.MshExpert.CreateStatic(fixture.Data);
    build.TryGetValue(out var source).Should().BeTrue();
    var sourceSequence = source!.StaticRenderObjectSequence.ToArray();
    var sourceTextureOrder = sourceSequence.Select(TexturePath).ToArray();
    var sourceMemberships = SourceObjectMemberships(source).ToArray();
    var replacement = source.StaticRenderObjectSequence[1];
    var assembler = new StaticMeshAssembler(source);
    var replacementOrdinal = assembler.GetRenderObjectOrdinal(replacement);

    assembler.ReplaceGeometry(replacementOrdinal, Vertices(), Triangles());
    var edit = assembler.Commit();

    edit.TryGetValue(out var edited).Should().BeTrue();
    source.StaticRenderObjectSequence.Should().Equal(sourceSequence);
    source.StaticRenderObjectSequence[1].Should().BeSameAs(replacement);
    edited!.StaticRenderObjectSequence.Select(TexturePath)
      .Should().Equal(sourceTextureOrder);
    SourceObjectMemberships(edited).Should().Equal(sourceMemberships);
    assembler.Trace.Changes.Should().ContainSingle(change =>
      change.Kind == StaticMeshAssemblyChangeKind.Geometry
      && change.RenderObjectOrdinal == replacementOrdinal
    );
  }

  [Fact]
  public void GeometryEditCanonicalizesLaterLinkIntoRegeneratedTopology()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var build = EarthTool.MSH.Expert.MshExpert.CreateStatic(fixture.Data);
    build.TryGetValue(out var source).Should().BeTrue();
    var vertices = Vertices()
      .Append(new CanonicalStaticVertex(Vector3.One, Vector3.UnitZ, Vector2.One));
    var sourceAsset = source!;
    var assembler = new StaticMeshAssembler(sourceAsset);
    var replacedOrdinal = assembler.GetRenderObjectOrdinal(sourceAsset.StaticRenderObjectSequence[0]);

    assembler.ReplaceGeometry(replacedOrdinal, vertices, Triangles());
    var edit = assembler.Commit();

    edit.TryGetValue(out var edited).Should().BeTrue();
    edited!
      .StaticRenderObjectSequence[1]
      .RenderVertices[0]
      .NormalSharingIndex.Should()
      .Be(ushort.MaxValue);
    sourceAsset.StaticRenderObjectSequence[1].RenderVertices[0].NormalSharingIndex.Should()
      .NotBe(ushort.MaxValue);
    assembler.Trace.Changes.Should().ContainSingle(change =>
      change.Kind == StaticMeshAssemblyChangeKind.Geometry
      && change.RenderObjectOrdinal == replacedOrdinal
    );
  }

  [Fact]
  public void PartitionDeletionRebasesLaterLinkToRetainedTopology()
  {
    var build = StaticMeshBuilder
      .Create(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
      .SetRootSourceObject(
        new CanonicalStaticSourceObject([RenderObject(), RenderObject(), RenderObject()])
      )
      .Build();
    build.TryGetValue(out var canonical).Should().BeTrue();
    var bytes = canonical!.GetSerializedRepresentation();
    const int firstRecordOffset = 0x14 + 0x368 + sizeof(uint);
    const int canonicalRecordLength = 0xDD;
    BinaryPrimitives.WriteUInt16LittleEndian(
      bytes.AsSpan(firstRecordOffset + (2 * canonicalRecordLength) + 8 + 0x90),
      3
    );
    var expert = EarthTool.MSH.Expert.MshExpert.CreateStatic(bytes);
    expert.TryGetValue(out var source).Should().BeTrue();
    var sourceAsset = source!;
    var assembler = new StaticMeshAssembler(sourceAsset);
    var removedOrdinal = assembler.GetRenderObjectOrdinal(sourceAsset.StaticRenderObjectSequence[0]);

    assembler.RemoveRenderObject(removedOrdinal);
    var edit = assembler.Commit();

    edit.TryGetValue(out var edited).Should().BeTrue();
    edited!.StaticRenderObjectSequence[1].RenderVertices[0].NormalSharingIndex.Should().Be(0);
    assembler.Trace.Changes.Should().ContainSingle(change =>
      change.Kind == StaticMeshAssemblyChangeKind.RemovedRenderObject
      && change.RenderObjectOrdinal == removedOrdinal
    );
  }

  [Fact]
  public void PartitionDeletionTransfersNestedSourceHierarchyMarker()
  {
    var build = StaticMeshBuilder
      .Create(new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
      .SetRootSourceObject(
        new CanonicalStaticSourceObject(
          [RenderObject()],
          [new CanonicalStaticSourceObject([RenderObject(), RenderObject()])]
        )
      )
      .Build();
    build.TryGetValue(out var source).Should().BeTrue();
    var retainedChild = source!.StaticRenderObjectSequence[2];
    var assembler = new StaticMeshAssembler(source);
    var removedOrdinal = assembler.GetRenderObjectOrdinal(source.StaticRenderObjectSequence[1]);

    assembler.RemoveRenderObject(removedOrdinal);
    var edit = assembler.Commit();

    edit.TryGetValue(out var edited).Should().BeTrue();
    edited!.StaticRenderObjectSequence.Should().HaveCount(2);
    source.StaticRenderObjectSequence[2].Should().BeSameAs(retainedChild);
    edited.StaticRenderObjectSequence[1].Should().NotBeSameAs(retainedChild);
    edited
      .StaticRenderObjectSequence[1]
      .KnownFlags.Should()
      .HaveFlag(StaticRenderObjectFlags.BeginsNestedSourceObject);
    edited
      .RootSourceObject.Children.Should()
      .ContainSingle()
      .Subject.StaticRenderObjects.Should()
      .Equal(edited.StaticRenderObjectSequence[1]);
    assembler.Trace.Changes.Should().ContainSingle(change =>
      change.Kind == StaticMeshAssemblyChangeKind.RemovedRenderObject
      && change.RenderObjectOrdinal == removedOrdinal
    );
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

  [Fact]
  public void AssemblyCommitReturnsProfileFailureWithoutThrowing()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var build = EarthTool.MSH.Expert.MshExpert.CreateStatic(fixture.Data);
    build.TryGetValue(out var source).Should().BeTrue();

    var edit = new StaticMeshAssembler(source!).Commit(
      new MshOperationProfile(maxStaticRenderObjects: 3)
    );

    edit.TryGetValue(out _).Should().BeFalse();
    edit.Diagnostics.Should()
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

  private static IEnumerable<string> SourceObjectMemberships(StaticMeshAsset asset)
  {
    return SourceObjectMemberships(asset.RootSourceObject, asset.StaticRenderObjectSequence);
  }

  private static IEnumerable<string> SourceObjectMemberships(
    StaticSourceObject source,
    IReadOnlyList<StaticRenderObject> sequence
  )
  {
    yield return string.Join(",", source.StaticRenderObjects
      .Select(renderObject => ReferenceIndexOf(sequence, renderObject)));
    foreach (var child in source.Children)
    {
      foreach (var membership in SourceObjectMemberships(child, sequence))
      {
        yield return membership;
      }
    }
  }

  private static string TexturePath(StaticRenderObject renderObject)
  {
    return Encoding.ASCII.GetString(renderObject.TexturePathBytes.ToArray());
  }

  private static int ReferenceIndexOf(
    IReadOnlyList<StaticRenderObject> sequence,
    StaticRenderObject renderObject
  )
  {
    return sequence
      .Select((candidate, index) => (candidate, index))
      .Single(item => ReferenceEquals(item.candidate, renderObject))
      .index;
  }

  private static async Task<byte[]> WriteAsync(MeshAsset asset)
  {
    await using var destination = new MemoryStream();
    var result = await new MshWriter().WriteAsync(asset, destination);
    result.Status.Should().Be(OperationStatus.Succeeded);
    return destination.ToArray();
  }
}
