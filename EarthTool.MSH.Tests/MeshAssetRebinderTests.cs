#nullable enable

using AwesomeAssertions;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;

namespace EarthTool.MSH.Tests
{
  public sealed class MeshAssetRebinderTests
  {
    private static readonly MeshAssetLineageId ReboundLineage = new(
      new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

    [Fact]
    public void StaticRebindAppliesOrderedIdentityStateWithoutChangingSerializedBytes()
    {
      var decoded = MshV1Decoder.Decode(
        StaticMeshSequenceFixture.CreateInterleaved().Data,
        MshOperationProfile.Default,
        CancellationToken.None);
      var source = (StaticMeshAsset)decoded.Asset;
      var state = new StaticMeshIdentityState(
        ReboundLineage,
        source.StaticRenderObjectSequence.Select((_, index) =>
          new StaticRenderObjectId(ReboundLineage, 10 + (index * 2))),
        SourceObjects(source.RootSourceObject).Select((_, index) =>
          new SourceObjectId(ReboundLineage, 20 + (index * 2))),
        100,
        200);

      var rebound = MeshAssetRebinder.RebindStatic(source, MeshAssetOrigin.Expert, state);

      rebound.Origin.Should().Be(MeshAssetOrigin.Expert);
      rebound.LineageId.Should().Be(ReboundLineage);
      rebound.StaticRenderObjectSequence.Select(item => item.LocalId).Should()
        .Equal(10, 12, 14, 16);
      SourceObjects(rebound.RootSourceObject).Select(item => item.Id.Value).Should()
        .Equal(20, 22, 24);
      rebound.NextStaticRenderObjectLocalId.Should().Be(100);
      rebound.NextSourceObjectLocalId.Should().Be(200);
      rebound.GetSerializedRepresentation().Should().Equal(source.GetSerializedRepresentation());
      source.Origin.Should().Be(MeshAssetOrigin.Loaded);
    }

    [Fact]
    public void StaticRebindRejectsInvalidIdentityStateWithoutChangingSource()
    {
      var decoded = MshV1Decoder.Decode(
        StaticMeshSequenceFixture.CreateSingle().Data,
        MshOperationProfile.Default,
        CancellationToken.None);
      var source = (StaticMeshAsset)decoded.Asset;
      var sourceBytes = source.GetSerializedRepresentation();
      var validRenderId = new StaticRenderObjectId(ReboundLineage, 3);
      var validSourceId = new SourceObjectId(ReboundLineage, 4);

      Action wrongCount = () => MeshAssetRebinder.RebindStatic(
        source,
        MeshAssetOrigin.Expert,
        new StaticMeshIdentityState(
          ReboundLineage,
          Array.Empty<StaticRenderObjectId>(),
          [validSourceId],
          5,
          5));
      Action nonpositive = () => MeshAssetRebinder.RebindStatic(
        source,
        MeshAssetOrigin.Expert,
        new StaticMeshIdentityState(
          ReboundLineage,
          [new StaticRenderObjectId(ReboundLineage, 0)],
          [validSourceId],
          5,
          5));
      Action foreignLineage = () => MeshAssetRebinder.RebindStatic(
        source,
        MeshAssetOrigin.Expert,
        new StaticMeshIdentityState(
          ReboundLineage,
          [new StaticRenderObjectId(new MeshAssetLineageId(Guid.NewGuid()), 3)],
          [validSourceId],
          5,
          5));
      Action invalidNext = () => MeshAssetRebinder.RebindStatic(
        source,
        MeshAssetOrigin.Expert,
        new StaticMeshIdentityState(
          ReboundLineage,
          [validRenderId],
          [validSourceId],
          3,
          5));
      var interleavedAsset = (StaticMeshAsset)MshV1Decoder.Decode(
        StaticMeshSequenceFixture.CreateInterleaved().Data,
        MshOperationProfile.Default,
        CancellationToken.None).Asset;
      Action duplicate = () => MeshAssetRebinder.RebindStatic(
        interleavedAsset,
        MeshAssetOrigin.Expert,
        new StaticMeshIdentityState(
          ReboundLineage,
          interleavedAsset.StaticRenderObjectSequence.Select(_ => validRenderId),
          SourceObjects(interleavedAsset.RootSourceObject).Select((_, index) =>
            new SourceObjectId(ReboundLineage, index + 1)),
          5,
          5));
      var reversedRoot = new StaticSourceObject(
        interleavedAsset.RootSourceObject.Id,
        interleavedAsset.RootSourceObject.StaticRenderObjectIds.Reverse(),
        interleavedAsset.RootSourceObject.Children);
      var outOfOrderAsset = WithRoot(interleavedAsset, reversedRoot);
      Action outOfOrder = () => MeshAssetRebinder.RebindStatic(
        outOfOrderAsset,
        MeshAssetOrigin.Expert,
        StaticMeshIdentityState.ForLineage(outOfOrderAsset, ReboundLineage));
      var reversedChildrenRoot = new StaticSourceObject(
        interleavedAsset.RootSourceObject.Id,
        interleavedAsset.RootSourceObject.StaticRenderObjectIds,
        interleavedAsset.RootSourceObject.Children.Reverse());
      var reversedChildrenAsset = WithRoot(interleavedAsset, reversedChildrenRoot);
      Action reversedChildren = () => MeshAssetRebinder.RebindStatic(
        reversedChildrenAsset,
        MeshAssetOrigin.Expert,
        StaticMeshIdentityState.ForLineage(reversedChildrenAsset, ReboundLineage));

      wrongCount.Should().Throw<ArgumentException>();
      nonpositive.Should().Throw<ArgumentException>();
      foreignLineage.Should().Throw<ArgumentException>();
      invalidNext.Should().Throw<ArgumentException>();
      duplicate.Should().Throw<ArgumentException>();
      outOfOrder.Should().Throw<ArgumentException>();
      reversedChildren.Should().Throw<ArgumentException>();
      source.GetSerializedRepresentation().Should().Equal(sourceBytes);
      source.StaticRenderObjectSequence.Should().ContainSingle().Subject.LocalId.Should().Be(1);
    }

    [Fact]
    public void DynamicRebindAppliesLineageAndOriginWithoutChangingSerializedBytes()
    {
      var build = DynamicMeshBuilder.Create().Build();
      build.TryGetValue(out var source).Should().BeTrue();

      var rebound = MeshAssetRebinder.RebindDynamic(
        source!,
        MeshAssetOrigin.Expert,
        ReboundLineage);

      rebound.Origin.Should().Be(MeshAssetOrigin.Expert);
      rebound.LineageId.Should().Be(ReboundLineage);
      rebound.GetSerializedRepresentation().Should().Equal(source!.GetSerializedRepresentation());
    }

    private static StaticSourceObject[] SourceObjects(StaticSourceObject root)
    {
      return new[] { root }.Concat(root.Children.SelectMany(SourceObjects)).ToArray();
    }

    private static StaticMeshAsset WithRoot(StaticMeshAsset source, StaticSourceObject root)
    {
      return new StaticMeshAsset(
        source.LineageId,
        source.ArchiveFraming,
        source.CommonBaseHeader,
        source.RootTrailingBytes.ToArray(),
        source.StaticRenderObjectSequence,
        source.GetSerializedRepresentation(),
        source.Origin,
        root,
        source.StoredTrailingHierarchyUnwindCount,
        source.ExpectedTrailingHierarchyUnwindCount,
        source.NextStaticRenderObjectLocalId,
        source.NextSourceObjectLocalId);
    }
  }
}
