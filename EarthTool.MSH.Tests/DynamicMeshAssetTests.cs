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

namespace EarthTool.MSH.Tests;

public class DynamicMeshAssetTests
{
  private static readonly Guid CreationGuid = new("12345678-9abc-def0-1234-56789abcdef0");
  private static readonly MeshAssetLineageId LineageId = new(
    new Guid("11111111-2222-3333-4444-555555555555"));

  [Fact]
  public async Task PublicOperationsPreserveCompleteOrderedDynamicTreeExactly()
  {
    var grandchild = CreateDynamicRecord(13, 4, "grand"u8.ToArray(), "smoke.tex"u8.ToArray());
    var group = CreateDynamicRecord(0, 3, children: [grandchild]);
    var unknown = CreateDynamicRecord(0xF1234567, 2, [0x41, 0x00, 0xFF], [0x80, 0x42]);
    var root = CreateDynamicRecord(12, 1, "root-model"u8.ToArray(), "root.tex"u8.ToArray(),
      new Vector3(1.25f, -2.5f, 3.75f), new Vector3(-4.5f, 5.25f, -6.125f),
      [unknown, group]);
    var trailing = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
    var fixture = CreateFixture(root, trailing);

    var (asset, firstWrite) = await ReadAndWriteAsync(fixture);
    var (_, secondWrite) = await ReadAndWriteAsync(firstWrite);

    asset.RootDynamicObject.CommonBaseHeader.Should().BeSameAs(asset.CommonBaseHeader);
    asset.RootDynamicObject.CommonBaseHeader.BoxPresenceMask.Should().Be(0xA0B0C001);
    asset.RootDynamicObject.Extension.EffectType.Should().Be(12);
    asset.RootDynamicObject.Extension.KnownEffectType.Should().Be(DynamicEffectType.Lightning);
    asset.RootDynamicObject.Extension.LightType.Should().Be(0x80000001);
    asset.RootDynamicObject.Extension.KnownLightType.Should().BeNull();
    asset.RootDynamicObject.Extension.FirstSourceFrame.Should().Be(-101);
    asset.RootDynamicObject.Extension.FrameCount.Should().Be(201);
    asset.RootDynamicObject.Extension.SpriteSheetColumnCount.Should().Be(4);
    asset.RootDynamicObject.Extension.SpriteSheetRowCount.Should().Be(5);
    asset.RootDynamicObject.Extension.FramePeriodTicks.Should().Be(6);
    asset.RootDynamicObject.Extension.ReciprocalColumnCount.Should().BeApproximately(0.11f, 0.000001f);
    asset.RootDynamicObject.Extension.ReciprocalRowCount.Should().BeApproximately(0.21f, 0.000001f);
    AssertRectangle(asset.RootDynamicObject.Extension.StartEffectRectangle, 1.1f, 1.2f, 1.3f, 1.4f);
    AssertRectangle(asset.RootDynamicObject.Extension.EndEffectRectangle, 1.5f, 1.6f, 1.7f, 1.8f);
    asset.RootDynamicObject.Extension.EffectDepthOffset.Should().Be(-1.25f);
    asset.RootDynamicObject.Extension.RibbonHalfWidth.Should().Be(-2.5f);
    asset.RootDynamicObject.Extension.ReservedWord.Should().Be(0xAABBCC01);
    asset.RootDynamicObject.Extension.AdditiveFlag.Should().Be(unchecked((int)0x80000001));
    asset.RootDynamicObject.Extension.UsesAdditiveBlending.Should().BeTrue();
    AssertVector(asset.RootDynamicObject.Extension.TerrainLightColor, new Vector3(1.1f, 1.2f, 1.3f));
    AssertVector(asset.RootDynamicObject.Extension.VisibleEffectColor, new Vector3(2.1f, 2.2f, 2.3f));
    asset.RootDynamicObject.Extension.VisibleTerrainLightGain.Should().BeApproximately(3.1f, 0.000001f);
    asset.RootDynamicObject.Extension.AlphaTimingMode.Should().Be(-8);
    asset.RootDynamicObject.Extension.KnownAlphaTiming.Should().BeNull();
    asset.RootDynamicObject.Extension.EndAlpha.Should().BeApproximately(0.21f, 0.000001f);
    asset.RootDynamicObject.Extension.StartAlpha.Should().BeApproximately(0.81f, 0.000001f);
    asset.RootDynamicObject.Extension.EndModelScale.Should().BeApproximately(2.1f, 0.000001f);
    asset.RootDynamicObject.Extension.StartModelScale.Should().BeApproximately(0.51f, 0.000001f);
    asset.RootDynamicObject.Extension.ChildStartTranslation.Should().Be(new Vector3(1.25f, 2.5f, 3.75f));
    asset.RootDynamicObject.Extension.ChildEndTranslation.Should().Be(new Vector3(-4.5f, -5.25f, -6.125f));
    asset.RootDynamicObject.Extension.MeshNameBytes.Should().Equal("root-model"u8.ToArray());
    asset.RootDynamicObject.Extension.TexturePathBytes.Should().Equal("root.tex"u8.ToArray());
    asset.RootDynamicObject.Extension.SerializedRepresentation.Should()
      .Equal(root.AsSpan(0x368, 0x9C).ToArray());
    asset.RootDynamicObject.Children.Select(child => child.Extension.EffectType)
      .Should().Equal(0xF1234567, 0);
    asset.RootDynamicObject.Children[0].Extension.MeshNameBytes.Should().Equal(0x41, 0x00, 0xFF);
    asset.RootDynamicObject.Children[0].Extension.TexturePathBytes.Should().Equal(0x80, 0x42);
    asset.RootDynamicObject.Children[0].CommonBaseHeader.BoxPresenceMask.Should().Be(0xA0B0C002);
    asset.RootDynamicObject.Children[1].Children.Should().ContainSingle()
      .Subject.Extension.KnownEffectType.Should().Be(DynamicEffectType.Smoke);
    asset.RootTrailingBytes.Should().Equal(trailing);
    Action mutate = () => ((IList<DynamicObject>)asset.RootDynamicObject.Children).Clear();
    mutate.Should().Throw<NotSupportedException>();
    fixture.AsSpan(0x18 + root.Length - unknown.Length - group.Length, 4).ToArray()
      .Should().Equal("MESH"u8.ToArray());
    firstWrite.Should().Equal(fixture);
    secondWrite.Should().Equal(firstWrite);
  }

  [Fact]
  public async Task PublicReaderDiagnosesAndPreservesExactNoncanonicalDynamicBaseHeader()
  {
    var build = DynamicMeshBuilder.Create(CreationGuid, LineageId).Build();
    build.TryGetValue(out var canonical).Should().BeTrue();
    var fixture = await WriteAsync(canonical!);
    fixture[0x18 + 0x0C] = 1;
    await using var source = new MemoryStream(fixture);

    var read = await new MshReader().ReadAsync(source);

    read.Status.Should().Be(OperationStatus.Succeeded);
    var asset = read.Value.Should().BeOfType<DynamicMeshAsset>().Subject;
    var diagnostic = read.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(MshDiagnosticCodes.CompatibilityAnomaly);
    diagnostic.EventId.Should().Be(1009);
    diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
    diagnostic.Path.Should().Be("RootDynamicObject.CommonBaseHeader");
    diagnostic.ByteOffset.Should().Be(0x18);
    diagnostic.Message.Should().Be("A noncanonical inherited dynamic base header was preserved.");
    diagnostic.Data.Should().BeEmpty();
    asset.CommonBaseHeader.BoxPresenceMask.Should().Be(1);
    (await WriteAsync(asset)).Should().Equal(fixture);
  }

  [Fact]
  public async Task ExpertConstructionAcceptsUnknownDynamicEffectWithoutCanonicalizingIt()
  {
    var fixture = CreateFixture(CreateDynamicRecord(uint.MaxValue, 7));

    var build = MshExpert.CreateDynamic(fixture, LineageId);

    build.TryGetValue(out var asset).Should().BeTrue();
    asset!.RootDynamicObject.Extension.EffectType.Should().Be(uint.MaxValue);
    asset.RootDynamicObject.Extension.KnownEffectType.Should().BeNull();
    build.Diagnostics.Should().Contain(diagnostic =>
      diagnostic.Code == MshDiagnosticCodes.CompatibilityAnomaly
      && diagnostic.Path == "RootDynamicObject.Extension.EffectType");
    build.Diagnostics.Should().Contain(diagnostic =>
      diagnostic.Code == MshDiagnosticCodes.CompatibilityAnomaly
      && diagnostic.Path == "RootDynamicObject.Extension.LightType");
    build.Diagnostics.Should().Contain(diagnostic =>
      diagnostic.Code == MshDiagnosticCodes.CompatibilityAnomaly
      && diagnostic.Path == "RootDynamicObject.Extension.ReservedWord");
    build.Diagnostics.Should().Contain(diagnostic =>
      diagnostic.Code == MshDiagnosticCodes.CompatibilityAnomaly
      && diagnostic.Path == "RootDynamicObject.Extension.AdditiveFlag");
    build.Diagnostics.Should().Contain(diagnostic =>
      diagnostic.Code == MshDiagnosticCodes.CompatibilityAnomaly
      && diagnostic.Path == "RootDynamicObject.Extension.AlphaTimingMode");
    (await WriteAsync(asset)).Should().Equal(fixture);
  }

  [Fact]
  public async Task LoadedUnsafeFrameValuesFailSemanticEvaluationWithoutChangingTheirBytes()
  {
    var fixture = CreateFixture(CreateDynamicRecord(1, 7));
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<DynamicMeshAsset>().Subject;

    DynamicEffectSemantics.TrySelectFrame(
      asset.RootDynamicObject.Extension,
      DynamicEffectEvaluationContext.Primary,
      10,
      5,
      3,
      out _,
      out var failure).Should().BeFalse();

    failure.Should().Be(DynamicSemanticFailure.InvalidFrameDeclaration);
    (await WriteAsync(asset)).Should().Equal(fixture);
  }

  [Fact]
  public async Task ExpertGroupPreservesAndDiagnosesCanonicalValuedInertRepresentations()
  {
    var canonical = DynamicMeshBuilder.Create(CreationGuid, LineageId).Build();
    canonical.TryGetValue(out var canonicalAsset).Should().BeTrue();
    var fixture = await WriteAsync(canonicalAsset!);
    WriteUInt32(fixture, 0x18 + 0x3B8, 1);
    WriteInt32(fixture, 0x18 + 0x3D8, 1);

    var build = MshExpert.CreateDynamic(fixture, LineageId);

    build.TryGetValue(out var asset).Should().BeTrue();
    build.Diagnostics.Should().Contain(diagnostic =>
      diagnostic.Code == MshDiagnosticCodes.CompatibilityAnomaly
      && diagnostic.Path == "RootDynamicObject.Extension.InertRepresentations");
    (await WriteAsync(asset!)).Should().Equal(fixture);
  }

  [Fact]
  public async Task PublicReaderEnforcesDynamicDepthObjectChildAndStringLimits()
  {
    var leaf = CreateDynamicRecord(1, 3, [1, 2], [3, 4]);
    var child = CreateDynamicRecord(0, 2, children: [leaf]);
    var fixture = CreateFixture(CreateDynamicRecord(0, 1, children: [child]));
    var exactProfile = new MshOperationProfile(
      maxDynamicDepth: 3,
      maxDynamicObjects: 3,
      maxDynamicChildrenPerObject: 1,
      maxDynamicStringBytes: 4);

    await AssertReadStatusAsync(fixture, exactProfile, OperationStatus.Succeeded);
    await AssertResourceLimitAsync(fixture, new MshOperationProfile(maxDynamicDepth: 2),
      "RootDynamicObject.Children[0].Children[0]");
    await AssertResourceLimitAsync(fixture, new MshOperationProfile(maxDynamicObjects: 2),
      "RootDynamicObject.Children[0].Children");
    await AssertResourceLimitAsync(fixture, new MshOperationProfile(maxDynamicChildrenPerObject: 0),
      "RootDynamicObject.Children");
    await AssertResourceLimitAsync(fixture, new MshOperationProfile(maxDynamicStringBytes: 3),
      "RootDynamicObject.Children[0].Children[0].Extension.TexturePathBytes");
  }

  [Theory]
  [InlineData(0x404)]
  [InlineData(0x408)]
  [InlineData(0x40C)]
  [InlineData(0x410)]
  [InlineData(0x500)]
  public async Task PublicReaderRejectsTruncatedDynamicChildrenWithoutPartialAsset(int rootLength)
  {
    var root = CreateDynamicRecord(0, 1, children: [CreateDynamicRecord(1, 2)]);
    var fixture = CreateFixture(root[..rootLength]);
    await using var source = new MemoryStream(fixture);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(MshDiagnosticCodes.StructuralHazard);
  }

  [Theory]
  [InlineData(0x404, "RootDynamicObject.Extension.MeshNameBytes")]
  [InlineData(0x40C, "RootDynamicObject.Children")]
  public async Task PublicReaderRejectsOverflowingDynamicDeclarationsBeforeAllocation(
    int declarationOffset,
    string path)
  {
    var root = CreateDynamicRecord(0, 1);
    WriteUInt32(root, declarationOffset, uint.MaxValue);
    var fixture = CreateFixture(root);

    await AssertResourceLimitAsync(fixture, MshOperationProfile.Default, path);
  }

  [Theory]
  [InlineData(0x04, "CommonBaseHeader.Version")]
  [InlineData(0x08, "CommonBaseHeader.MeshKind")]
  public async Task PublicReaderRejectsMalformedNestedDynamicObjectWithoutPartialAsset(
    int childFieldOffset,
    string expectedPath)
  {
    var child = CreateDynamicRecord(1, 2);
    WriteUInt32(child, childFieldOffset, childFieldOffset == 0x04 ? 2u : 0u);
    var fixture = CreateFixture(CreateDynamicRecord(0, 1, children: [child]));
    await using var source = new MemoryStream(fixture);

    var result = await new MshReader().ReadAsync(source);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(MshDiagnosticCodes.StructuralHazard);
    diagnostic.Path.Should().Be($"RootDynamicObject.Children[0].{expectedPath}");
  }

  [Fact]
  public void CanonicalDynamicBuilderRejectsCyclesAndCopiesReusedInstances()
  {
    var cycle = DynamicEffectRecipes.Group();
    cycle.AddChild(cycle);
    var cycleBuild = DynamicMeshBuilder.Create(CreationGuid, LineageId).SetRoot(cycle).Build();
    var indirectRoot = DynamicEffectRecipes.Group();
    var indirectChild = DynamicEffectRecipes.Group();
    indirectRoot.AddChild(indirectChild);
    indirectChild.AddChild(indirectRoot);
    var indirectBuild = DynamicMeshBuilder.Create(CreationGuid, LineageId).SetRoot(indirectRoot).Build();
    var shared = CreateRecipe(DynamicEffectType.Smoke);
    var reusedRoot = DynamicEffectRecipes.Group([shared, shared]);
    var reusedBuild = DynamicMeshBuilder.Create(CreationGuid, LineageId).SetRoot(reusedRoot).Build();

    cycleBuild.TryGetValue(out _).Should().BeFalse();
    cycleBuild.Diagnostics.Should().ContainSingle().Subject.Code
      .Should().Be(MshDiagnosticCodes.InvalidAuthoringInput);
    indirectBuild.TryGetValue(out _).Should().BeFalse();
    indirectBuild.Diagnostics.Should().ContainSingle().Subject.Code
      .Should().Be(MshDiagnosticCodes.InvalidAuthoringInput);
    reusedBuild.TryGetValue(out var reusedAsset).Should().BeTrue();
    reusedBuild.Diagnostics.Should().ContainSingle().Subject.Code
      .Should().Be(MshDiagnosticCodes.CompatibilityAnomaly);
    reusedAsset!.RootDynamicObject.Children.Select(child => child.Extension.KnownEffectType)
      .Should().Equal(DynamicEffectType.Smoke, DynamicEffectType.Smoke);
  }

  [Fact]
  public async Task CanonicalDynamicBuilderCopiesOrderedTreeIntoDeterministicSnapshot()
  {
    var sourceChildren = new List<CanonicalDynamicObject>
    {
      CreateRecipe(DynamicEffectType.Explosion),
      CreateRecipe(DynamicEffectType.Smoke)
    };
    var root = DynamicEffectRecipes.Group(sourceChildren);
    var build = DynamicMeshBuilder.Create(CreationGuid, LineageId).SetRoot(root).Build();

    sourceChildren.Clear();
    root.AddChild(CreateRecipe(DynamicEffectType.Keelwater));

    build.TryGetValue(out var asset).Should().BeTrue();
    asset!.RootDynamicObject.Extension.KnownEffectType.Should().Be(DynamicEffectType.Group);
    asset.RootDynamicObject.Children.Select(child => child.Extension.KnownEffectType)
      .Should().Equal(DynamicEffectType.Explosion, DynamicEffectType.Smoke);
    var first = await WriteAsync(asset);
    var second = await WriteAsync(asset);
    first.Should().Equal(second);
  }

  [Fact]
  public void ReusedInstanceDiagnosticsRespectTheConfiguredCap()
  {
    var shared = CreateRecipe(DynamicEffectType.Smoke);
    var root = DynamicEffectRecipes.Group([shared, shared, shared]);

    var build = DynamicMeshBuilder.Create(CreationGuid, LineageId)
      .SetRoot(root)
      .Build(new MshOperationProfile(maxDiagnostics: 1));

    build.TryGetValue(out _).Should().BeTrue();
    build.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(MshDiagnosticCodes.DiagnosticsTruncated);
  }

  [Fact]
  public async Task SemanticTextureRegionRejectsOverflowingFinalCoordinates()
  {
    var record = CreateDynamicRecord(1, 1);
    WriteInt32(record, 0x378, 2);
    WriteSingle(record, 0x384, 2e38f);
    WriteSingle(record, 0x388, 1);
    await using var source = new MemoryStream(CreateFixture(record));
    var read = await new MshReader().ReadAsync(source);
    var extension = read.Value.Should().BeOfType<DynamicMeshAsset>()
      .Subject.RootDynamicObject.Extension;

    var frame = new DynamicFrameSelection(1, 1, 0.5f);
    DynamicEffectSemantics.TrySelectTextureRegion(
      extension,
      DynamicEffectEvaluationContext.Primary,
      frame,
      1,
      out _,
      out var failure).Should().BeFalse();
    failure.Should().Be(DynamicSemanticFailure.NonFiniteInput);
  }

  [Fact]
  public async Task CancelledDynamicReadReturnsNoPartialAsset()
  {
    var fixture = CreateFixture(CreateDynamicRecord(0, 1, children: [CreateDynamicRecord(1, 2)]));
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    await using var cancelledSource = new MemoryStream(fixture);

    var cancelled = await new MshReader().ReadAsync(cancelledSource, cancellationToken: cancellation.Token);

    cancelled.Status.Should().Be(OperationStatus.Cancelled);
    cancelled.Value.Should().BeNull();
  }

  [Fact]
  public async Task DynamicCancellationDuringStagedValidationLeavesDestinationUnchanged()
  {
    var fixture = CreateFixture(CreateDynamicRecord(0, 1, children: [CreateDynamicRecord(1, 2)]));
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<DynamicMeshAsset>().Subject;
    using var cancellation = new CancellationTokenSource();
    var destinationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.msh");
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(destinationPath, original);

    try
    {
      var write = await new MshWriter(new CancellingValidationFileSystem(fixture, cancellation))
        .WriteFileAsync(asset, destinationPath, cancellationToken: cancellation.Token);

      write.Status.Should().Be(OperationStatus.Cancelled);
      (await File.ReadAllBytesAsync(destinationPath)).Should().Equal(original);
    }
    finally
    {
      File.Delete(destinationPath);
    }
  }

  [Fact]
  public async Task DynamicInjectedCommitFailureLeavesDestinationUnchanged()
  {
    var fixture = CreateFixture(CreateDynamicRecord(0, 1, children: [CreateDynamicRecord(1, 2)]));
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<DynamicMeshAsset>().Subject;
    var destinationPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.msh");
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(destinationPath, original);

    try
    {
      var write = await new MshWriter(new FailingCommitFileSystem(fixture))
        .WriteFileAsync(asset, destinationPath);

      write.Status.Should().Be(OperationStatus.Failed);
      (await File.ReadAllBytesAsync(destinationPath)).Should().Equal(original);
    }
    finally
    {
      File.Delete(destinationPath);
    }
  }

  private static async Task<(DynamicMeshAsset Asset, byte[] Bytes)> ReadAndWriteAsync(byte[] fixture)
  {
    await using var source = new MemoryStream(fixture);
    var read = await new MshReader().ReadAsync(source);
    var asset = read.Value.Should().BeOfType<DynamicMeshAsset>().Subject;
    var validation = await new MshValidator().ValidateAsync(asset);
    validation.Status.Should().Be(OperationStatus.Succeeded);
    return (asset, await WriteAsync(asset));
  }

  private static CanonicalDynamicObject CreateRecipe(DynamicEffectType effectType)
  {
    var frames = new CanonicalDynamicFrameSequence(0, 1, 0);
    var sprite = new CanonicalDynamicSpriteSheet(frames, 1, 1);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-0.25f, 0.25f, 0.25f, -0.25f),
      new EffectRectangle(-0.25f, 0.25f, 0.25f, -0.25f),
      0.25f);
    var alpha = new CanonicalDynamicAlpha(1, 1, DynamicAlphaTiming.FramePhase);
    return effectType switch
    {
      DynamicEffectType.Explosion => DynamicEffectRecipes.Explosion(
        sprite, shape, "Textures\\effect.tex", Vector3.One, alpha, false,
        new CanonicalDynamicTerrainLight(DynamicLightType.Constant, Vector3.Zero)),
      DynamicEffectType.Smoke => DynamicEffectRecipes.Smoke(
        sprite, shape, "Textures\\effect.tex", Vector3.One, 1, alpha, false),
      DynamicEffectType.Keelwater => DynamicEffectRecipes.Keelwater(
        sprite, shape, "Textures\\effect.tex", 1, 1, false),
      _ => throw new ArgumentOutOfRangeException(nameof(effectType))
    };
  }

  private static async Task<byte[]> WriteAsync(MeshAsset asset)
  {
    await using var destination = new MemoryStream();
    var write = await new MshWriter().WriteAsync(asset, destination);
    write.Status.Should().Be(OperationStatus.Succeeded);
    return destination.ToArray();
  }

  private static async Task AssertReadStatusAsync(
    byte[] fixture,
    MshOperationProfile profile,
    OperationStatus expected)
  {
    await using var source = new MemoryStream(fixture);
    var result = await new MshReader().ReadAsync(source, profile);
    result.Status.Should().Be(expected);
  }

  private static async Task AssertResourceLimitAsync(
    byte[] fixture,
    MshOperationProfile profile,
    string path)
  {
    await using var source = new MemoryStream(fixture);
    var result = await new MshReader().ReadAsync(source, profile);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(MshDiagnosticCodes.ResourceLimitExceeded);
    diagnostic.Path.Should().Be(path);
  }

  private static byte[] CreateFixture(byte[] root, byte[]? trailing = null)
  {
    trailing ??= [];
    var result = new byte[0x18 + root.Length + trailing.Length];
    WriteUInt32(result, 0, 0x30D0A1FF);
    WriteUInt32(result, 4, 1);
    CreationGuid.ToByteArray().CopyTo(result, 8);
    root.CopyTo(result, 0x18);
    trailing.CopyTo(result, 0x18 + root.Length);
    return result;
  }

  private static byte[] CreateDynamicRecord(
    uint effectType,
    byte fixtureSeed,
    byte[]? meshName = null,
    byte[]? texturePath = null,
    Vector3 childStartTranslation = default,
    Vector3 childEndTranslation = default,
    IReadOnlyList<byte[]>? children = null)
  {
    meshName ??= [];
    texturePath ??= [];
    children ??= [];
    var fixedLength = 0x404;
    var result = new byte[fixedLength + 4 + meshName.Length + 4 + texturePath.Length + 4
      + children.Sum(child => child.Length)];
    "MESH"u8.CopyTo(result);
    WriteUInt32(result, 0x04, 1);
    WriteUInt32(result, 0x08, 1);
    WriteUInt32(result, 0x0C, 0xA0B0C000u + fixtureSeed);
    WriteUInt32(result, 0x368, effectType);
    WriteUInt32(result, 0x36C, 0x80000000u + fixtureSeed);
    WriteInt32(result, 0x370, -100 - fixtureSeed);
    WriteInt32(result, 0x374, 200 + fixtureSeed);
    WriteInt32(result, 0x378, 3 + fixtureSeed);
    WriteInt32(result, 0x37C, 4 + fixtureSeed);
    WriteInt32(result, 0x380, 5 + fixtureSeed);
    WriteSingle(result, 0x384, 0.10f + (fixtureSeed / 100f));
    WriteSingle(result, 0x388, 0.20f + (fixtureSeed / 100f));
    WriteRectangle(result, 0x38C, fixtureSeed + 0.1f);
    WriteRectangle(result, 0x39C, fixtureSeed + 0.5f);
    WriteSingle(result, 0x3AC, -1.25f * fixtureSeed);
    WriteSingle(result, 0x3B0, -2.5f * fixtureSeed);
    WriteUInt32(result, 0x3B4, 0xAABBCC00u + fixtureSeed);
    WriteUInt32(result, 0x3B8, 0x80000000u + fixtureSeed);
    WriteVector(result, 0x3BC,
      new Vector3(fixtureSeed + 0.1f, fixtureSeed + 0.2f, fixtureSeed + 0.3f));
    WriteVector(result, 0x3C8,
      new Vector3(fixtureSeed + 1.1f, fixtureSeed + 1.2f, fixtureSeed + 1.3f));
    WriteSingle(result, 0x3D4, fixtureSeed + 2.1f);
    WriteInt32(result, 0x3D8, -7 - fixtureSeed);
    WriteSingle(result, 0x3DC, 0.20f + (fixtureSeed / 100f));
    WriteSingle(result, 0x3E0, 0.80f + (fixtureSeed / 100f));
    WriteSingle(result, 0x3E4, 2f + (fixtureSeed / 10f));
    WriteSingle(result, 0x3E8, 0.5f + (fixtureSeed / 100f));
    WriteVector(result, 0x3EC, childStartTranslation);
    WriteVector(result, 0x3F8, childEndTranslation);

    var cursor = 0x404;
    WriteUInt32(result, cursor, checked((uint)meshName.Length));
    cursor += 4;
    meshName.CopyTo(result, cursor);
    cursor += meshName.Length;
    WriteUInt32(result, cursor, checked((uint)texturePath.Length));
    cursor += 4;
    texturePath.CopyTo(result, cursor);
    cursor += texturePath.Length;
    WriteUInt32(result, cursor, checked((uint)children.Count));
    cursor += 4;
    foreach (var child in children)
    {
      child.CopyTo(result, cursor);
      cursor += child.Length;
    }

    return result;
  }

  private static void WriteRectangle(byte[] data, int offset, float start)
  {
    WriteSingle(data, offset, start);
    WriteSingle(data, offset + 4, start + 0.1f);
    WriteSingle(data, offset + 8, start + 0.2f);
    WriteSingle(data, offset + 12, start + 0.3f);
  }

  private static void AssertRectangle(
    EffectRectangle actual,
    float x0,
    float y1,
    float x1,
    float y0)
  {
    actual.X0.Should().BeApproximately(x0, 0.000001f);
    actual.Y1.Should().BeApproximately(y1, 0.000001f);
    actual.X1.Should().BeApproximately(x1, 0.000001f);
    actual.Y0.Should().BeApproximately(y0, 0.000001f);
  }

  private static void AssertVector(Vector3 actual, Vector3 expected)
  {
    actual.X.Should().BeApproximately(expected.X, 0.000001f);
    actual.Y.Should().BeApproximately(expected.Y, 0.000001f);
    actual.Z.Should().BeApproximately(expected.Z, 0.000001f);
  }

  private static void WriteVector(byte[] data, int offset, Vector3 value)
  {
    WriteSingle(data, offset, value.X);
    WriteSingle(data, offset + 4, value.Y);
    WriteSingle(data, offset + 8, value.Z);
  }

  private static void WriteSingle(byte[] data, int offset, float value)
  {
    WriteInt32(data, offset, BitConverter.SingleToInt32Bits(value));
  }

  private static void WriteInt32(byte[] data, int offset, int value)
  {
    BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset), value);
  }

  private static void WriteUInt32(byte[] data, int offset, uint value)
  {
    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
  }

  private sealed class FailingCommitFileSystem : ITransactionalFileSystem
  {
    private readonly byte[] _staged;

    internal FailingCommitFileSystem(byte[] staged)
    {
      _staged = staged;
    }

    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + ".test.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new MemoryStream();
    }

    public Stream OpenTemporaryRead(string temporaryPath)
    {
      return new MemoryStream(_staged);
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

  private sealed class CancellingValidationFileSystem : ITransactionalFileSystem
  {
    private readonly byte[] _staged;
    private readonly CancellationTokenSource _cancellation;

    internal CancellingValidationFileSystem(byte[] staged, CancellationTokenSource cancellation)
    {
      _staged = staged;
      _cancellation = cancellation;
    }

    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + ".test.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new MemoryStream();
    }

    public Stream OpenTemporaryRead(string temporaryPath)
    {
      return new CancellingReadStream(_staged, _cancellation);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      throw new InvalidOperationException("A cancelled staged validation cannot commit.");
    }

    public bool TryDelete(string temporaryPath)
    {
      return true;
    }
  }

  private sealed class CancellingReadStream : MemoryStream
  {
    private readonly CancellationTokenSource _cancellation;

    internal CancellingReadStream(byte[] bytes, CancellationTokenSource cancellation)
      : base(bytes)
    {
      _cancellation = cancellation;
    }

    public override Task<int> ReadAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var read = base.Read(buffer, offset, Math.Min(count, 32));
      if (read > 0)
      {
        _cancellation.Cancel();
      }

      return Task.FromResult(read);
    }
  }
}
