using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Expert;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace EarthTool.MSH.Tests;

public class CanonicalMeshAuthoringTests
{
  private static readonly Guid CreationGuid = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

  [Fact]
  public async Task CanonicalStaticBuilderProducesCoherentDeterministicAsset()
  {
    var vertices = CreateVertices();
    var triangles = CreateTriangles();
    var build = StaticMeshBuilder
      .Create(CreationGuid)
      .SetAnimationLengths(2, 0, 0, 0)
      .SetRenderObject(vertices, triangles)
      .Build();

    build.TryGetValue(out var asset).Should().BeTrue();
    asset.Should().NotBeNull();
    asset!.Origin.Should().Be(MeshAssetOrigin.Canonical);
    asset.Kind.Should().Be(MeshAssetKind.Static);
    asset.ArchiveFraming.Declaration.Should().Be(0x20D0A1FF);
    asset.ArchiveFraming.ArchiveType.Should().BeNull();
    asset.ArchiveFraming.CreationGuid.Should().Be(CreationGuid);
    asset.CommonBaseHeader.AnimationLengths.Should().Be(new AnimationClassBytes(2, 0, 0, 0));
    asset.CommonBaseHeader.BoxPresenceMask.Should().Be(0x00008000);
    BinaryPrimitives
      .ReadUInt16LittleEndian(asset.CommonBaseHeader.BoxTopElevations.Take(2).ToArray())
      .Should()
      .Be(256);
    asset
      .CommonBaseHeader.RotatedOccupancyDescriptors.Should()
      .Equal(ToBytes(0x3A000008u, 0x00008000u, 0xCA001000u, 0xFF000001u));
    asset
      .CommonBaseHeader.RotatedCornerPassageMaps.Should()
      .Equal(
        ToBytes(
          0xFFFFFFFFFFFF0FFFul,
          0x0FFFFFFFFFFFFFFFul,
          0xFFF0FFFFFFFFFFFFul,
          0xFFFFFFFFFFFFFFF0ul
        )
      );
    asset
      .CommonBaseHeader.AttachmentTable.Chunk(8)
      .Should()
      .OnlyContain(record => IsCanonicalAbsentAttachment(record));
    asset
      .RootSourceObject.StaticRenderObjects.Should()
      .ContainSingle()
      .Subject.Should()
      .BeSameAs(asset.StaticRenderObjectSequence[0]);

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
  public void CanonicalBaseHeaderDefaultStaticFormMatchesGoldenBytes()
  {
    var header = CanonicalBaseHeaderEncoder.EncodeStatic(
      new CanonicalStaticBaseHeaderInput(
        new AnimationClassBytes(2, 0, 0, 0),
        CreateVertices()
      )
    );

    AssertSerializedApproval(
      "msh-canonical-base-header-static-default",
      header.SerializedRepresentation.ToArray()
    );
  }

  [Fact]
  public void CanonicalBaseHeaderFullyPopulatedStaticFormMatchesGoldenBytes()
  {
    var header = CanonicalBaseHeaderEncoder.EncodeStatic(
      new CanonicalStaticBaseHeaderInput(
        new AnimationClassBytes(1, 2, 3, 4),
        CreateVertices(),
        new CanonicalStaticFootprint(
          0xFFFF,
          Enumerable.Range(0, 16).Select(index => index + 0.5f),
          Enumerable.Range(0, 16).Select(index => (byte)index)
        ),
        new CanonicalHorizontalExtents(1.25f, 2.5f, 3.75f, 4.5f),
        new AnimationClassBytes(5, 6, 7, 8),
        CreateAttachmentRecords(),
        CreateCannonRenderPositions(),
        CreateStaticSpotLights(),
        CreateStaticOmniLights()
      )
    );

    AssertSerializedApproval(
      "msh-canonical-base-header-static-full",
      header.SerializedRepresentation.ToArray()
    );

    var assetBytes = MshCanonicalSerializer.CreateStatic(
      CreationGuid,
      new AnimationClassBytes(1, 2, 3, 4),
      new CanonicalStaticSourceObject([
        new CanonicalStaticRenderObject(CreateVertices(), CreateTriangles()),
      ]),
      new CanonicalStaticFootprint(
        0xFFFF,
        Enumerable.Range(0, 16).Select(index => index + 0.5f),
        Enumerable.Range(0, 16).Select(index => (byte)index)
      ),
      new CanonicalHorizontalExtents(1.25f, 2.5f, 3.75f, 4.5f),
      animationFrameIndices: new AnimationClassBytes(5, 6, 7, 8),
      attachmentRecords: CreateAttachmentRecords(),
      cannonRenderPositions: CreateCannonRenderPositions(),
      staticSpotLights: CreateStaticSpotLights(),
      staticOmniLights: CreateStaticOmniLights()
    );
    AssertSerializedApproval("msh-canonical-static-full", assetBytes);
  }

  [Fact]
  public void CanonicalBaseHeaderDynamicFormMatchesGoldenBytes()
  {
    AssertSerializedApproval(
      "msh-canonical-base-header-dynamic",
      CanonicalBaseHeaderEncoder.Dynamic.SerializedRepresentation.ToArray()
    );
  }

  [Fact]
  public void CanonicalStaticSequenceOwnsCompleteNestedRecordEncoding()
  {
    var root = CreateCanonicalSequenceSource();
    var pivots = Enumerable
      .Range(0, 4)
      .ToDictionary(index => index, index => new Vector3(index + 1, index + 2, index + 3));
    var animation = new StaticAnimationReplacement(
      new StaticAnimationTracks(
        [new Vector3(1, 2, 3)],
        [new Vector3(4, 5, 6)],
        [Matrix4x4.CreateTranslation(7, 8, 9)]
      ),
      (uint)StaticAnimationClass.C
    );
    var animations = new Dictionary<int, StaticAnimationReplacement> { [1] = animation };

    var sequence = CanonicalStaticRenderObjectSequenceEncoder.Encode(root, pivots, animations);
    var framed = MshCanonicalSerializer.CreateStatic(
      CreationGuid,
      new AnimationClassBytes(0, 0, 1, 0),
      root,
      pivots: pivots,
      animations: animations
    );

    CanonicalStaticRenderObjectSequenceEncoder.GetSerializedLength(root, animations)
      .Should().Be(sequence.Length);
    framed.AsSpan(0x14 + CommonMeshBaseHeader.SerializedSize).ToArray().Should().Equal(sequence);
    var build = MshExpert.CreateStatic(framed);
    build.TryGetValue(out var asset).Should().BeTrue();
    asset!.StaticRenderObjectSequence.Select(record =>
      Encoding.ASCII.GetString(record.TexturePathBytes.ToArray())
    ).Should().Equal(
      "Textures\\root-a.tex",
      "Textures\\child-a.tex",
      "Textures\\child-b.tex",
      "Textures\\root-b.tex"
    );
    asset.StaticRenderObjectSequence.Select(record => record.Pivot)
      .Should().Equal(pivots.OrderBy(item => item.Key).Select(item => item.Value));
    asset.StaticRenderObjectSequence.Select(record => record.NextRecordMarker)
      .Should().Equal(1u, 1u, 1u, 0u);
    asset.StaticRenderObjectSequence.Select(record => record.HierarchyUnwindCount)
      .Should().Equal(0, 0, 0, 1);
    asset.StaticRenderObjectSequence[0].KnownFlags.Should().Be(
      StaticRenderObjectFlags.MarkerAttachment1 | StaticRenderObjectFlags.MarkerAttachment3
    );
    asset.StaticRenderObjectSequence[1].KnownFlags.Should().Be(
      StaticRenderObjectFlags.BeginsNestedSourceObject
        | StaticRenderObjectFlags.MarkerAttachment2
        | StaticRenderObjectFlags.MarkerAttachment4
    );
    asset.StaticRenderObjectSequence[2].KnownFlags.Should().Be(StaticRenderObjectFlags.None);
    asset.StaticRenderObjectSequence[3].KnownFlags.Should().Be(StaticRenderObjectFlags.None);
    asset.StaticRenderObjectSequence[1].AnimationClassValue.Should().Be((uint)StaticAnimationClass.C);
    asset.StaticRenderObjectSequence[1].AnimationTracks.ScaleFrames.Should().Equal(new Vector3(1, 2, 3));
    asset.StaticRenderObjectSequence[1].AnimationTracks.TranslationFrames.Should().Equal(new Vector3(4, 5, 6));
    asset.StaticRenderObjectSequence[1].AnimationTracks.Matrices.Should().Equal(
      Matrix4x4.CreateTranslation(7, 8, 9)
    );
    asset.StaticRenderObjectSequence.Should().OnlyContain(record =>
      record.VertexBlockCount == 1
      && record.VertexBlockPadding.All(value => value == 0)
      && record.RenderVertices.All(vertex =>
        vertex.NormalSharingIndex == ushort.MaxValue
        && vertex.PositionSharingIndex == ushort.MaxValue
      )
      && record.Triangles.Single().TriangleRenderPassFlags == 3
    );
    asset.StoredTrailingHierarchyUnwindCount.Should().Be(1);
  }

  [Fact]
  public void EquivalentCanonicalStaticSequencesHaveExactDeterministicLengths()
  {
    var negativeZero = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));
    var firstRoot = CreateCanonicalSequenceSource();
    var secondRoot = CreateCanonicalSequenceSource(negativeZero);
    var firstAnimations = new Dictionary<int, StaticAnimationReplacement>
    {
      [2] = CreateSequenceAnimation(0),
    };
    var secondAnimations = new Dictionary<int, StaticAnimationReplacement>
    {
      [2] = CreateSequenceAnimation(negativeZero),
    };
    var firstPivots = Enumerable.Range(0, 4).ToDictionary(index => index, _ => Vector3.Zero);
    var secondPivots = Enumerable.Range(0, 4)
      .ToDictionary(index => index, _ => new Vector3(negativeZero));

    var first = CanonicalStaticRenderObjectSequenceEncoder.Encode(
      firstRoot,
      firstPivots,
      animations: firstAnimations
    );
    var second = CanonicalStaticRenderObjectSequenceEncoder.Encode(
      secondRoot,
      secondPivots,
      animations: secondAnimations
    );

    CanonicalStaticRenderObjectSequenceEncoder.GetSerializedLength(firstRoot, firstAnimations)
      .Should().Be(first.Length);
    CanonicalStaticRenderObjectSequenceEncoder.GetSerializedLength(secondRoot, secondAnimations)
      .Should().Be(second.Length);
    first.Should().Equal(second);
  }

  [Fact]
  public void CanonicalBaseHeaderNormalizesNegativeZero()
  {
    var negativeZero = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));
    var zeroVector = new Vector3(negativeZero, negativeZero, negativeZero);
    var header = CanonicalBaseHeaderEncoder.EncodeStatic(
      new CanonicalStaticBaseHeaderInput(
        default,
        CreateVertices(),
        cannonRenderPositions: new Dictionary<int, CanonicalCannonRenderPosition>
        {
          [1] = new(zeroVector),
        },
        staticSpotLights: new Dictionary<int, CanonicalSpotLight>
        {
          [1] = new(zeroVector, zeroVector, negativeZero, 0, negativeZero, negativeZero, negativeZero, negativeZero),
        },
        staticOmniLights: new Dictionary<int, CanonicalOmniLight>
        {
          [1] = new(zeroVector, zeroVector, negativeZero),
        }
      )
    );
    var bytes = header.SerializedRepresentation.ToArray();

    bytes.AsSpan(0x018, 12).ToArray().Should().OnlyContain(value => value == 0);
    bytes.AsSpan(0x048, 0x30).ToArray().Should().OnlyContain(value => value == 0);
    bytes.AsSpan(0x108, 0x1C).ToArray().Should().OnlyContain(value => value == 0);
  }

  [Fact]
  public void BaseHeaderInputsRejectTheWrongAssemblyMode()
  {
    var root = new CanonicalStaticSourceObject([
      new CanonicalStaticRenderObject(CreateVertices(), CreateTriangles()),
    ]);
    var canonical = StaticMeshAssembler.CreateCanonical(
      CreationGuid,
      root,
      new CanonicalStaticFootprint(0x8000, new float[16], new byte[16]),
      new CanonicalHorizontalExtents(1, 1, 1, 1)
    );

    ((Action)(() => canonical.ReplaceAttachmentRecord(1, new byte[8])))
      .Should().Throw<InvalidOperationException>();
    ((Action)(() => canonical.ReplaceCannonRenderPosition(1, new byte[12])))
      .Should().Throw<InvalidOperationException>();
    ((Action)(() => canonical.ReplaceStaticLightRecord(
      StaticLightRecordKind.Spot,
      1,
      new byte[0x30],
      Array.Empty<string>()
    ))).Should().Throw<InvalidOperationException>();

    var exact = MshExpert.CreateStatic(OneTriangleMshFixture.Create());
    exact.TryGetValue(out var loadedAsset).Should().BeTrue();
    var loaded = new StaticMeshAssembler(loadedAsset!);
    ((Action)(() => loaded.ReplaceCanonicalAttachmentRecord(1, default)))
      .Should().Throw<InvalidOperationException>();
    ((Action)(() => loaded.ReplaceCanonicalCannonRenderPosition(1, default)))
      .Should().Throw<InvalidOperationException>();
    ((Action)(() => loaded.ReplaceCanonicalStaticSpotLight(1, default)))
      .Should().Throw<InvalidOperationException>();
    ((Action)(() => loaded.ReplaceCanonicalStaticOmniLight(1, default)))
      .Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void CanonicalStaticBuilderAppliesOnlyTypedFootprintExtentAndRoleOverrides()
  {
    var elevations = Enumerable.Range(0, 16).Select(index => index / 4f).ToArray();
    var cornerPassage = new byte[16];
    cornerPassage[14] = 1;
    cornerPassage[15] = 2;
    var source = new CanonicalStaticSourceObject(
      [new CanonicalStaticRenderObject(CreateVertices(), CreateTriangles())],
      [
        new CanonicalStaticSourceObject(
          [new CanonicalStaticRenderObject(CreateVertices(), CreateTriangles())],
          role: new CanonicalStaticObjectRole(StaticRenderObjectFlags.Rotor)
        ),
      ],
      role: new CanonicalStaticObjectRole(
        StaticRenderObjectFlags.ViewerFaced
          | StaticRenderObjectFlags.Barrel
          | StaticRenderObjectFlags.MarkerAttachment2,
        barrelMaximumAngle: 37
      )
    );

    var build = StaticMeshBuilder
      .Create(CreationGuid)
      .SetRootSourceObject(source)
      .SetFootprint(new CanonicalStaticFootprint(0xC000, elevations, cornerPassage))
      .SetHorizontalExtents(new CanonicalHorizontalExtents(1.25f, 2.5f, 3.75f, 4.5f))
      .Build();

    build.TryGetValue(out var asset).Should().BeTrue();
    asset!.CommonBaseHeader.BoxPresenceMask.Should().Be(0xC000);
    Enumerable
      .Range(0, 16)
      .Select(index =>
        BinaryPrimitives.ReadUInt16LittleEndian(
          asset.CommonBaseHeader.BoxTopElevations.Skip(index * 2).Take(2).ToArray()
        )
      )
      .Should()
      .Equal(elevations.Reverse().Select(value => (ushort)(value * 256)));
    asset.CommonBaseHeader.BoxCornerPassageFlags.Should().Equal(cornerPassage.Reverse());
    asset
      .CommonBaseHeader.RotatedOccupancyDescriptors.Should()
      .Equal(ToBytes(0x3A000088u, 0x0400C000u, 0xCB001100u, 0xFF000003u));
    asset
      .CommonBaseHeader.RotatedCornerPassageMaps.Should()
      .Equal(
        ToBytes(
          0xFFFFFFFF2FFF1FFFul,
          0x81FFFFFFFFFFFFFFul,
          0xFFF4FFF8FFFFFFFFul,
          0xFFFFFFFFFFFFFF42ul
        )
      );
    asset
      .CommonBaseHeader.HorizontalExtents.Should()
      .Equal(ToBytes((ushort)320, (ushort)640, (ushort)960, (ushort)1152));
    asset
      .StaticRenderObjectSequence[0]
      .KnownFlags.Should()
      .Be(
        StaticRenderObjectFlags.ViewerFaced
          | StaticRenderObjectFlags.Barrel
          | StaticRenderObjectFlags.MarkerAttachment2
      );
    asset.StaticRenderObjectSequence[0].BarrelMaximumAngle.Should().Be(37);
    asset
      .StaticRenderObjectSequence[1]
      .KnownFlags.Should()
      .Be(StaticRenderObjectFlags.Rotor | StaticRenderObjectFlags.BeginsNestedSourceObject);

    Action reservedRole = () =>
      new CanonicalStaticObjectRole(StaticRenderObjectFlags.BeginsNestedSourceObject);
    reservedRole.Should().Throw<ArgumentOutOfRangeException>();
  }

  [Fact]
  public void CanonicalBuilderCopiesInputsAndRejectsNonFiniteValues()
  {
    var vertices = CreateVertices().ToList();
    var triangles = CreateTriangles().ToList();
    var builder = StaticMeshBuilder
      .Create(CreationGuid)
      .SetRenderObject(vertices, triangles);

    vertices[0] = new CanonicalStaticVertex(new Vector3(99), Vector3.UnitZ, Vector2.Zero);
    triangles.Clear();
    var copied = builder.Build();
    copied.TryGetValue(out var copiedAsset).Should().BeTrue();
    copiedAsset!.StaticRenderObjectSequence[0].RenderVertices[0].Position.Should().Be(Vector3.Zero);

    var invalid = StaticMeshBuilder
      .Create(CreationGuid)
      .SetRenderObject(
        new[]
        {
          new CanonicalStaticVertex(new Vector3(float.NaN, 0, 0), Vector3.UnitZ, Vector2.Zero),
          CreateVertices()[1],
          CreateVertices()[2],
        },
        CreateTriangles()
      )
      .Build();

    invalid.TryGetValue(out _).Should().BeFalse();
    var diagnostic = invalid.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(MshDiagnosticCodes.InvalidAuthoringInput);
    diagnostic.EventId.Should().Be(1011);
    diagnostic.Path.Should().Be("StaticRenderObject.RenderVertices[0].Position");
  }

  [Fact]
  public void CanonicalStaticAuthoringAndEditControlsEnforceSafeTexResourceKeys()
  {
    var build = StaticMeshBuilder
      .Create(CreationGuid)
      .SetRootSourceObject(
        new CanonicalStaticSourceObject([
          new CanonicalStaticRenderObject(
            CreateVertices(),
            CreateTriangles(),
            "Textures\\authored\\hull.tex"
          ),
        ])
      )
      .Build();

    build.TryGetValue(out var asset).Should().BeTrue();
    asset!
      .StaticRenderObjectSequence.Should()
      .ContainSingle()
      .Subject.TexturePathBytes.Should()
      .Equal("Textures\\authored\\hull.tex"u8.ToArray());
    var assembler = new StaticMeshAssembler(asset);
    assembler.SetTextureResourceBinding(
      assembler.GetRenderObjectOrdinal(asset.StaticRenderObjectSequence[0]),
      "Textures\\authored\\replacement.tex"
    );
    var edit = assembler.Commit();
    edit.TryGetValue(out var edited).Should().BeTrue();
    edited!
      .StaticRenderObjectSequence[0]
      .TexturePathBytes.Should()
      .Equal("Textures\\authored\\replacement.tex"u8.ToArray());

    var unsafeBuild = StaticMeshBuilder
      .Create(CreationGuid)
      .SetRootSourceObject(
        new CanonicalStaticSourceObject([
          new CanonicalStaticRenderObject(CreateVertices(), CreateTriangles(), "..\\outside.tex"),
        ])
      )
      .Build();
    unsafeBuild.TryGetValue(out _).Should().BeFalse();
    unsafeBuild
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.InvalidAuthoringInput);
    var unsafeAssembler = new StaticMeshAssembler(asset);
    Action unsafeEdit = () => unsafeAssembler.SetTextureResourceBinding(
      unsafeAssembler.GetRenderObjectOrdinal(asset.StaticRenderObjectSequence[0]),
      "Textures\\..\\outside.tex"
    );
    unsafeEdit.Should().Throw<ArgumentException>();
  }

  [Fact]
  public async Task CanonicalDynamicBuilderProducesChildlessGroupWithFixedCommonHeaderProfile()
  {
    var build = DynamicMeshBuilder.Create(CreationGuid).Build();

    build.TryGetValue(out var asset).Should().BeTrue();
    asset.Should().NotBeNull();
    asset!.Kind.Should().Be(MeshAssetKind.Dynamic);
    asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    asset.ArchiveFraming.Declaration.Should().Be(0x30D0A1FF);
    asset.ArchiveFraming.ArchiveType.Should().Be(1);
    asset.CommonBaseHeader.MeshKind.Should().Be(1);
    asset
      .CommonBaseHeader.SerializedRepresentation.Skip(0x0C)
      .Take(0x1CC)
      .Should()
      .OnlyContain(value => value == 0);
    asset
      .CommonBaseHeader.AttachmentTable.Chunk(8)
      .Should()
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
  public async Task CanonicalAndExactCreationPreserveCreationGuidAndOrigins()
  {
    var staticBuild = StaticMeshBuilder
      .Create(CreationGuid)
      .SetRenderObject(CreateVertices(), CreateTriangles())
      .Build();
    var dynamicBuild = DynamicMeshBuilder.Create(CreationGuid).Build();

    staticBuild.TryGetValue(out var staticAsset).Should().BeTrue();
    dynamicBuild.TryGetValue(out var dynamicAsset).Should().BeTrue();
    staticAsset.Should().NotBeNull();
    dynamicAsset.Should().NotBeNull();
    staticAsset.ArchiveFraming.CreationGuid.Should().Be(CreationGuid);
    dynamicAsset.ArchiveFraming.CreationGuid.Should().Be(CreationGuid);

    var exactStatic = MshExpert.CreateStatic(await WriteAsync(staticAsset));
    var exactDynamic = MshExpert.CreateDynamic(await WriteAsync(dynamicAsset));

    exactStatic.TryGetValue(out var exactStaticAsset).Should().BeTrue();
    exactDynamic.TryGetValue(out var exactDynamicAsset).Should().BeTrue();
    exactStaticAsset!.Origin.Should().Be(MeshAssetOrigin.Expert);
    exactDynamicAsset!.Origin.Should().Be(MeshAssetOrigin.Expert);
    exactStaticAsset.ArchiveFraming.CreationGuid.Should().Be(CreationGuid);
    exactDynamicAsset.ArchiveFraming.CreationGuid.Should().Be(CreationGuid);
  }

  [Fact]
  public async Task ExpertConstructionPreservesAcceptedExactSerializedValues()
  {
    var fixture = OneTriangleMshFixture.Create();
    fixture[0x14 + 0x1D8] = 0;
    fixture[0x14 + 0x1D8 + 1] = 0;

    var build = MshExpert.CreateStatic(fixture);

    build.TryGetValue(out var asset).Should().BeTrue();
    asset!.Origin.Should().Be(MeshAssetOrigin.Expert);
    (await WriteAsync(asset)).Should().Equal(fixture);
  }

  [Fact]
  public async Task ExpertConstructionCannotBypassMalformedDynamicChildren()
  {
    var dynamic = DynamicMeshBuilder.Create(CreationGuid).Build();
    dynamic.TryGetValue(out var asset).Should().BeTrue();
    var bytes = await WriteAsync(asset!);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x18 + 0x40C), 1);

    var build = MshExpert.CreateDynamic(bytes);

    build.TryGetValue(out _).Should().BeFalse();
    build
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.StructuralHazard);
  }

  [Fact]
  public void BuildersAndExpertConstructionEnforceResourceLimitsBeforeAcceptance()
  {
    var staticBuild = StaticMeshBuilder
      .Create(CreationGuid)
      .SetRenderObject(CreateVertices(), CreateTriangles())
      .Build(new MshOperationProfile(maxOutputBytes: 1));
    var oversized = new CountingByteEnumerable(100);

    var expertBuild = MshExpert.CreateStatic(
      oversized,
      new MshOperationProfile(maxInputBytes: 4)
    );

    staticBuild.TryGetValue(out _).Should().BeFalse();
    staticBuild
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.ResourceLimitExceeded);
    expertBuild.TryGetValue(out _).Should().BeFalse();
    expertBuild
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.ResourceLimitExceeded);
    oversized.ValuesProduced.Should().Be(5);
  }

  [Fact]
  public async Task AssemblyCommitReturnsNewSnapshotAndLeavesSourceReferencesUnchanged()
  {
    var source = BuildStatic();
    var sourceRenderObject = source.StaticRenderObjectSequence[0];
    var changedVertices = CreateVertices().ToArray();
    changedVertices[1] = new CanonicalStaticVertex(
      new Vector3(2, 0, 0),
      Vector3.UnitZ,
      Vector2.UnitX
    );

    var assembler = new StaticMeshAssembler(source);
    var ordinal = assembler.GetRenderObjectOrdinal(sourceRenderObject);
    assembler.ReplaceGeometry(ordinal, changedVertices, CreateTriangles());
    var edit = assembler.Commit();

    edit.TryGetValue(out var edited).Should().BeTrue();
    edited.Should().NotBeSameAs(source);
    source.StaticRenderObjectSequence[0].Should().BeSameAs(sourceRenderObject);
    edited!.StaticRenderObjectSequence[0].Should().NotBeSameAs(sourceRenderObject);
    source.StaticRenderObjectSequence[0].RenderVertices[1].Position.Should().Be(Vector3.UnitX);
    edited
      .StaticRenderObjectSequence[0]
      .RenderVertices[1]
      .Position.Should()
      .Be(new Vector3(2, 0, 0));
    assembler.Trace.Changes.Should().ContainSingle(change =>
      change.Kind == StaticMeshAssemblyChangeKind.Geometry
      && change.RenderObjectOrdinal == ordinal
    );
    assembler.Trace.ResultRenderObjectOrdinals.Should().Equal(ordinal);

    var output = await WriteAsync(edited);
    await using var roundTripSource = new MemoryStream(output);
    var roundTrip = await new MshReader().ReadAsync(roundTripSource);
    roundTrip.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public void AssemblyReplacementUsesFinalOrdinalAndCanCommitOnlyOnce()
  {
    var source = BuildStatic();
    var sourceRenderObject = source.StaticRenderObjectSequence[0];
    var assembler = new StaticMeshAssembler(source);
    assembler.RemoveRenderObject(assembler.GetRenderObjectOrdinal(sourceRenderObject));
    var replacementOrdinal = assembler.AddRenderObject(CreateVertices(), CreateTriangles());

    var edit = assembler.Commit();

    edit.TryGetValue(out var edited).Should().BeTrue();
    replacementOrdinal.Should().Be(0);
    edited!.StaticRenderObjectSequence.Should().ContainSingle()
      .Subject.Should().NotBeSameAs(sourceRenderObject);
    assembler.Trace.ReplacedSingleRenderObject.Should().BeTrue();
    assembler.Trace.ResultRenderObjectOrdinals.Should().Equal(replacementOrdinal);
    Action secondCommit = () => assembler.Commit();
    secondCommit.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void SingleObjectReplacementAuthorsNewRepresentationsUnderNewReference()
  {
    var source = BuildStatic();
    var replacementVertices = CreateVertices();
    replacementVertices[1] = new CanonicalStaticVertex(
      new Vector3(9, 8, 7),
      Vector3.UnitY,
      new Vector2(0.25f, 0.75f)
    );
    var sourceRenderObject = source.StaticRenderObjectSequence[0];
    var assembler = new StaticMeshAssembler(source);
    assembler.RemoveRenderObject(assembler.GetRenderObjectOrdinal(sourceRenderObject));
    var replacementOrdinal = assembler.AddRenderObject(replacementVertices, CreateTriangles());
    assembler.SetTextureResourceBinding(replacementOrdinal, "Textures\\replacement.tex");
    assembler.ReplacePivot(replacementOrdinal, new Vector3(6, 5, 4));

    var result = assembler.Commit();

    result.TryGetValue(out var edited).Should().BeTrue();
    var replacement = edited!.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    replacement.Should().NotBeSameAs(sourceRenderObject);
    replacement.RenderVertices[1].Position.Should().Be(new Vector3(9, 8, 7));
    replacement.RenderVertices[1].Normal.Should().Be(Vector3.UnitY);
    Encoding.ASCII.GetString(replacement.TexturePathBytes.ToArray())
      .Should()
      .Be("Textures\\replacement.tex");
    replacement.Pivot.Should().Be(new Vector3(6, 5, 4));
  }

  [Fact]
  public void UnsupportedRepeatedReplacementFailsWithoutMutatingSource()
  {
    var source = BuildStatic();
    var sourceRenderObject = source.StaticRenderObjectSequence[0];
    var assembler = new StaticMeshAssembler(source);
    assembler.RemoveRenderObject(assembler.GetRenderObjectOrdinal(sourceRenderObject));
    var firstOrdinal = assembler.AddRenderObject(CreateVertices(), CreateTriangles());
    var secondOrdinal = assembler.AddRenderObject(CreateVertices(), CreateTriangles());

    var edit = assembler.Commit();

    secondOrdinal.Should().Be(firstOrdinal);
    edit.TryGetValue(out _).Should().BeFalse();
    edit.Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(MshDiagnosticCodes.InvalidAuthoringInput);
    source.StaticRenderObjectSequence.Should().ContainSingle().Subject.Should().BeSameAs(sourceRenderObject);
  }

  [Fact]
  public void MeshAssetMatchSelectsTheClosedBranch()
  {
    MeshAsset staticAsset = BuildStatic();
    var dynamicBuild = DynamicMeshBuilder.Create(CreationGuid).Build();
    dynamicBuild.TryGetValue(out var dynamicAsset).Should().BeTrue();

    staticAsset.Match(_ => "static", _ => "dynamic").Should().Be("static");
    dynamicAsset!.Match(_ => "static", _ => "dynamic").Should().Be("dynamic");
  }

  private static StaticMeshAsset BuildStatic()
  {
    var build = StaticMeshBuilder
      .Create(CreationGuid)
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
      new CanonicalStaticVertex(new Vector3(0, 1, 1), Vector3.UnitZ, Vector2.UnitY),
    ];
  }

  private static CanonicalStaticSourceObject CreateCanonicalSequenceSource(float zero = 0)
  {
    var child = new CanonicalStaticSourceObject(
      [
        new CanonicalStaticRenderObject(
          CreateSequenceVertices(zero),
          CreateTriangles(),
          "Textures\\child-a.tex"
        ),
        new CanonicalStaticRenderObject(
          CreateSequenceVertices(zero),
          CreateTriangles(),
          "Textures\\child-b.tex"
        ),
      ],
      role: new CanonicalStaticObjectRole(
        StaticRenderObjectFlags.MarkerAttachment2 | StaticRenderObjectFlags.MarkerAttachment4
      )
    );
    return new CanonicalStaticSourceObject(
      [
        new CanonicalStaticRenderObject(
          CreateSequenceVertices(zero),
          CreateTriangles(),
          "Textures\\root-a.tex"
        ),
        new CanonicalStaticRenderObject(
          CreateSequenceVertices(zero),
          CreateTriangles(),
          "Textures\\root-b.tex"
        ),
      ],
      [child],
      new CanonicalStaticObjectRole(
        StaticRenderObjectFlags.MarkerAttachment1 | StaticRenderObjectFlags.MarkerAttachment3
      )
    );
  }

  private static CanonicalStaticVertex[] CreateSequenceVertices(float zero)
  {
    return
    [
      new CanonicalStaticVertex(
        new Vector3(zero, zero, zero),
        new Vector3(zero, zero, 1),
        new Vector2(zero, zero)
      ),
      new CanonicalStaticVertex(
        new Vector3(1, zero, zero),
        new Vector3(zero, zero, 1),
        new Vector2(1, zero)
      ),
      new CanonicalStaticVertex(
        new Vector3(zero, 1, 1),
        new Vector3(zero, zero, 1),
        new Vector2(zero, 1)
      ),
    ];
  }

  private static StaticAnimationReplacement CreateSequenceAnimation(float zero)
  {
    var matrix = new Matrix4x4(
      1, zero, zero, zero,
      zero, 1, zero, zero,
      zero, zero, 1, zero,
      zero, zero, zero, 1
    );
    return new StaticAnimationReplacement(
      new StaticAnimationTracks(
        [new Vector3(1, zero, 1)],
        [new Vector3(2, zero, 4)],
        [matrix]
      ),
      (uint)StaticAnimationClass.B
    );
  }

  private static CanonicalTriangle[] CreateTriangles()
  {
    return [new CanonicalTriangle(0, 1, 2)];
  }

  private static IReadOnlyDictionary<int, CanonicalAttachmentRecord> CreateAttachmentRecords()
  {
    return Enumerable
      .Range(1, 49)
      .ToDictionary(
        number => number,
        number => new CanonicalAttachmentRecord(
          new Vector3(number / 4f, -number / 8f, number / 16f),
          (byte)(number * 3),
          (byte)(0x80 + number)
        )
      );
  }

  private static IReadOnlyDictionary<
    int,
    CanonicalCannonRenderPosition
  > CreateCannonRenderPositions()
  {
    return Enumerable
      .Range(1, 4)
      .ToDictionary(
        number => number,
        number => new CanonicalCannonRenderPosition(
          new Vector3(number + 0.25f, number + 0.5f, number + 0.75f)
        )
      );
  }

  private static IReadOnlyDictionary<int, CanonicalSpotLight> CreateStaticSpotLights()
  {
    return Enumerable
      .Range(1, 4)
      .ToDictionary(
        number => number,
        number => new CanonicalSpotLight(
          new Vector3(number, number + 0.25f, number + 0.5f),
          new Vector3(number / 10f, number / 8f, number / 5f),
          10 + number,
          (byte)(number * 16),
          number / 20f,
          number / 4f,
          -number / 10f,
          number / 2f
        )
      );
  }

  private static IReadOnlyDictionary<int, CanonicalOmniLight> CreateStaticOmniLights()
  {
    return Enumerable
      .Range(1, 4)
      .ToDictionary(
        number => number,
        number => new CanonicalOmniLight(
          new Vector3(-number, number + 0.125f, number + 0.625f),
          new Vector3(number / 6f, number / 7f, number / 9f),
          number / 3f
        )
      );
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
    var approvedPath = Path.Combine(
      root,
      "EarthTool.MSH.Tests",
      "Approvals",
      $"{name}.approved.sha256.txt"
    );
    var receivedPath = Path.Combine(
      root,
      "EarthTool.MSH.Tests",
      "Approvals",
      $"{name}.received.sha256.txt"
    );
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

  private static byte[] ToBytes(params ushort[] values)
  {
    var bytes = new byte[values.Length * sizeof(ushort)];
    for (var index = 0; index < values.Length; index++)
    {
      BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(index * sizeof(ushort)), values[index]);
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
