using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System.Numerics;
using Xunit;

namespace EarthTool.MSH.Tests;

public sealed class GltfAuthoringMetadataTests
{
  [Theory]
  [InlineData("ET_Static_1", "StaticSource", 1)]
  [InlineData("ET_Turret_4", "Cannon", 4)]
  [InlineData("ET_Emitter_1", "Attachment", 5)]
  [InlineData("ET_SpotLight_2", "StaticLight", 2)]
  [InlineData("EarthTool D", "Animation", 4)]
  [InlineData("ET_Dynamic_7_Lightning", "DynamicObject", 7)]
  public void CanonicalOwnerNamesBindCaseSensitively(
    string name,
    string expectedKind,
    int expectedNumber)
  {
    CanonicalAuthoringOwner.TryParse(name, out var owner).Should().BeTrue();
    owner.Kind.ToString().Should().Be(expectedKind);
    owner.Number.Should().Be(expectedNumber);
    owner.CanonicalName.Should().Be(name);

    CanonicalAuthoringOwner.TryParse(name.ToLowerInvariant(), out _).Should().BeFalse();
  }

  [Theory]
  [InlineData("ET_Static_01")]
  [InlineData("ET_Dynamic_01_Smoke")]
  [InlineData("ET_Dynamic_1_Unknown_0000000F")]
  [InlineData("ET_Dynamic_1_smoke")]
  [InlineData("ET_Turret_5")]
  public void ReservedNamesAreNotHeuristicallyCorrected(string name)
  {
    CanonicalAuthoringOwner.TryParse(name, out _).Should().BeFalse();
  }

  [Fact]
  public void WrongCaseDynamicDeclarationFailsAsARequiredSemantic()
  {
    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[0]", "et_dynamic_1_smoke", null) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.RequiredValueMissing);
  }

  [Fact]
  public void DuplicateCanonicalOwnersFailBeforeMetadataIsRead()
  {
    var result = CanonicalAuthoringMetadata.Read(
      new[]
      {
        new AuthoringMetadataCarrier("nodes[4]", "ET_Turret_1", "not json"),
        new AuthoringMetadataCarrier("nodes[2]", "ET_Turret_1", null),
      },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle();
    result.Diagnostics[0].Code.Should().Be(GltfAuthoringMetadataDiagnosticCodes.DuplicateOwner);
    result.Diagnostics[0].Data["paths"].Should().Be("nodes[2],nodes[4]");
  }

  [Fact]
  public void DistinctDynamicNamesDoNotConflictByNumericLabel()
  {
    var result = CanonicalAuthoringMetadata.Read(
      new[]
      {
        new AuthoringMetadataCarrier("nodes[1]", "ET_Dynamic_3_Group", null),
        new AuthoringMetadataCarrier("nodes[2]", "ET_Dynamic_3_Sphere", null),
      },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public void DistinctStaticLightFamiliesDoNotConflictByPhysicalNumber()
  {
    var result = CanonicalAuthoringMetadata.Read(
      new[]
      {
        new AuthoringMetadataCarrier("nodes[1]", "ET_SpotLight_2", null),
        new AuthoringMetadataCarrier("nodes[2]", "ET_OmniLight_2", null),
      },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public void StaticTypedValuesRoundTripWithoutResourceIdentity()
  {
    var owner = CanonicalAuthoringOwner.Parse("ET_Static_1");
    var elevations = Enumerable.Range(0, 16).Select(value => value / 4f).ToArray();
    var values = new StaticSourceAuthoringValues(
      new CanonicalStaticFootprint(0x8421, elevations, Enumerable.Repeat((byte)3, 16)),
      new CanonicalHorizontalExtents(1, 2, 3, 4),
      GltfStaticObjectRoles.ViewerFaced | GltfStaticObjectRoles.Barrel,
      37);

    var metadata = CanonicalAuthoringMetadata.Write(owner, values, GltfOperationProfile.Default);
    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[0]", owner.CanonicalName, metadata) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
    var actual = result.Value!.Get<StaticSourceAuthoringValues>(owner);
    actual.Roles.Should().Be(values.Roles);
    actual.BarrelMaximumAngle.Should().Be(37);
    actual.Footprint!.PresenceMask.Should().Be(0x8421);
    actual.Footprint.TopElevations.Should().Equal(elevations);
    actual.HorizontalExtents!.NegativeX.Should().Be(4);
    metadata.Contains("texture", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    metadata.Contains("resource", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    metadata.Contains("sourceMsh", StringComparison.Ordinal).Should().BeFalse();
    metadata.Contains("identity", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
  }

  [Fact]
  public void MalformedOptionalValuesUseDefaultsWithoutDiscardingValidSiblings()
  {
    var owner = CanonicalAuthoringOwner.Parse("ET_SpotLight_1");
    const string metadata =
      "{\"format\":\"earthtool.msh.authoring\",\"version\":1,\"values\":{"
      + "\"terrainLightAmplitude\":-1,\"futureValue\":12}}";

    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[3]", owner.CanonicalName, metadata) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
    result.Value!.Get<StaticLightAuthoringValues>(owner).TerrainLightAmplitude.Should().Be(1);
    result.Diagnostics.Should().HaveCount(2);
    result.Diagnostics.Should().OnlyContain(item => item.Severity == DiagnosticSeverity.Warning);
  }

  [Fact]
  public void DynamicTypedValuesRoundTripOnTheNamedEffectOwner()
  {
    var owner = CanonicalAuthoringOwner.Parse("ET_Dynamic_9_Explosion");
    var values = new DynamicAuthoringValues(
      new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 6, 3), 4, 2),
      new EffectRectangle(-2, 3, 4, -5),
      new CanonicalDynamicTerrainLight(DynamicLightType.Pyramid, new Vector3(1, 2, 3)),
      1,
      DynamicAlphaTiming.LifetimeProgress,
      0.25f,
      true);

    var metadata = CanonicalAuthoringMetadata.Write(owner, values, GltfOperationProfile.Default);
    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[8]", owner.CanonicalName, metadata) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
    var actual = result.Value!.Get<DynamicAuthoringValues>(owner);
    actual.SpriteSheet.Should().Be(values.SpriteSheet);
    actual.EndEffectRectangle.Should().Be(values.EndEffectRectangle);
    actual.TerrainLight.Should().Be(values.TerrainLight);
    actual.VisibleTerrainLightGain.Should().Be(1);
    actual.AlphaTiming.Should().Be(DynamicAlphaTiming.LifetimeProgress);
    actual.EndAlpha.Should().Be(0.25f);
    actual.Additive.Should().BeTrue();
    metadata.Contains("meshName", StringComparison.Ordinal).Should().BeFalse();
    metadata.Contains("texturePath", StringComparison.Ordinal).Should().BeFalse();
  }

  [Fact]
  public void MissingRequiredDynamicFramesFailWithoutPartialValues()
  {
    var owner = CanonicalAuthoringOwner.Parse("ET_Dynamic_1_Track");

    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[0]", owner.CanonicalName, null) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.RequiredValueMissing);
  }

  [Fact]
  public void LaserWallRetainsTerrainColorWithoutInventingALightMode()
  {
    var owner = CanonicalAuthoringOwner.Parse("ET_Dynamic_2_LaserWall");
    var values = new DynamicAuthoringValues(
      new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(0, 1, 0), 1, 1),
      terrainLight: new CanonicalDynamicTerrainLight(
        DynamicLightType.Random,
        new Vector3(0.1f, 0.2f, 0.3f)));

    var metadata = CanonicalAuthoringMetadata.Write(owner, values, GltfOperationProfile.Default);
    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[1]", owner.CanonicalName, metadata) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
    var actual = result.Value!.Get<DynamicAuthoringValues>(owner);
    actual.TerrainLight.LightType.Should().Be(DynamicLightType.Constant);
    actual.TerrainLight.Color.Should().Be(new Vector3(0.1f, 0.2f, 0.3f));
    metadata.Should().Contain("\"terrainLight\":");
    metadata.Should().NotContain("\"mode\":");
  }

  [Fact]
  public void UnsupportedDynamicValuesWarnAndUseCanonicalDefaults()
  {
    var owner = CanonicalAuthoringOwner.Parse("ET_Dynamic_1_Group");
    const string metadata =
      "{\"format\":\"earthtool.msh.authoring\",\"version\":1,\"values\":{"
      + "\"additive\":true,\"endAlpha\":0.25}}";

    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[0]", owner.CanonicalName, metadata) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
    var actual = result.Value!.Get<DynamicAuthoringValues>(owner);
    actual.Additive.Should().BeFalse();
    actual.EndAlpha.Should().Be(1);
    result.Diagnostics.Should().HaveCount(2);
  }

  [Fact]
  public void UnsupportedDynamicFramesAndNestedMembersDefaultWithWarnings()
  {
    var owner = CanonicalAuthoringOwner.Parse("ET_Dynamic_1_Group");
    const string metadata =
      "{\"format\":\"earthtool.msh.authoring\",\"version\":1,\"values\":{"
      + "\"frames\":{\"first\":0,\"count\":1,\"periodTicks\":0,\"future\":1}}}";

    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[0]", owner.CanonicalName, metadata) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
    result.Value!.Get<DynamicAuthoringValues>(owner).Frames.Should().BeNull();
    result.Diagnostics.Should().HaveCount(2);
  }

  [Fact]
  public void MissingOptionalMembersUseDefaultsWithWarnings()
  {
    var owner = CanonicalAuthoringOwner.Parse("ET_Static_1");
    const string metadata =
      "{\"format\":\"earthtool.msh.authoring\",\"version\":1,\"values\":{}}";

    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[0]", owner.CanonicalName, metadata) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
    result.Value!.Get<StaticSourceAuthoringValues>(owner).Roles.Should().Be(
      GltfStaticObjectRoles.None);
    result.Diagnostics.Should().HaveCount(3);
  }

  [Fact]
  public void OptionalOnlyDynamicEffectRetainsApplicableTypedValues()
  {
    var owner = CanonicalAuthoringOwner.Parse("ET_Dynamic_5_Sphere");
    var metadata = CanonicalAuthoringMetadata.Write(
      owner,
      new DynamicAuthoringValues(additive: true),
      GltfOperationProfile.Default);

    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[4]", owner.CanonicalName, metadata) },
      GltfOperationProfile.Default);

    result.Status.Should().Be(OperationStatus.Succeeded);
    result.Value!.Get<DynamicAuthoringValues>(owner).Additive.Should().BeTrue();
  }

  [Theory]
  [InlineData(DynamicEffectType.Group)]
  [InlineData(DynamicEffectType.Explosion)]
  [InlineData(DynamicEffectType.Track)]
  [InlineData(DynamicEffectType.ScalableObject)]
  [InlineData(DynamicEffectType.MappedExplosion)]
  [InlineData(DynamicEffectType.FlatExplosion)]
  [InlineData(DynamicEffectType.Laser)]
  [InlineData(DynamicEffectType.LaserWall)]
  [InlineData(DynamicEffectType.Shockwave)]
  [InlineData(DynamicEffectType.Line)]
  [InlineData(DynamicEffectType.Sphere)]
  [InlineData(DynamicEffectType.ElectricalCannon)]
  [InlineData(DynamicEffectType.Lightning)]
  [InlineData(DynamicEffectType.Smoke)]
  [InlineData(DynamicEffectType.Keelwater)]
  public void EveryRecognizedDynamicEffectHasDeterministicTypedMetadata(
    DynamicEffectType effectType)
  {
    var owner = CanonicalAuthoringOwner.Parse($"ET_Dynamic_1_{effectType}");
    var values = CreateDynamicValues(effectType);

    var first = CanonicalAuthoringMetadata.Write(owner, values, GltfOperationProfile.Default);
    var second = CanonicalAuthoringMetadata.Write(owner, values, GltfOperationProfile.Default);
    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[0]", owner.CanonicalName, first) },
      GltfOperationProfile.Default);

    second.Should().Be(first);
    result.Status.Should().Be(OperationStatus.Succeeded);
    first.Contains("resource", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    first.Contains("texturePath", StringComparison.Ordinal).Should().BeFalse();
    first.Contains("meshName", StringComparison.Ordinal).Should().BeFalse();
  }

  [Fact]
  public void WarningsAndEnvelopeBytesAreBoundedByTheOperationProfile()
  {
    var profile = CreateProfile(maxMetadataBytes: 180, maxMetadataConflicts: 2);
    var owner = CanonicalAuthoringOwner.Parse("ET_SpotLight_1");
    var oversized = "{\"format\":\"earthtool.msh.authoring\",\"version\":1,\"values\":{\"x\":\""
      + new string('x', 200)
      + "\"}}";

    var result = CanonicalAuthoringMetadata.Read(
      new[] { new AuthoringMetadataCarrier("nodes[0]", owner.CanonicalName, oversized) },
      profile);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfDiagnosticCodes.MetadataResourceLimitExceeded);

    var warnings = CanonicalAuthoringMetadata.Read(
      new[]
      {
        new AuthoringMetadataCarrier(
          "nodes[0]",
          owner.CanonicalName,
          "{\"format\":\"earthtool.msh.authoring\",\"version\":1,\"values\":{"
            + "\"one\":1,\"two\":2,\"three\":3}}"),
      },
      profile);

    warnings.Status.Should().Be(OperationStatus.Succeeded);
    warnings.Diagnostics.Should().HaveCount(2);
    warnings.Diagnostics[^1].Code.Should().Be(
      GltfAuthoringMetadataDiagnosticCodes.DiagnosticsTruncated);
  }

  private static GltfOperationProfile CreateProfile(
    int maxMetadataBytes,
    int maxMetadataConflicts)
  {
    return new GltfOperationProfile(
      maxInputBytes: 1024,
      maxOutputBytes: 1024,
      maxMetadataBytes: maxMetadataBytes,
      maxJsonDepth: 16,
      maxActiveRenderVertices: 16,
      maxNodes: 16,
      maxHierarchyDepth: 8,
      maxTextureBytes: 1024,
      maxPreviewPixels: 1024,
      maxTextureSearchRoots: 4,
      maxTextureDirectoryEntries: 16,
      maxTotalMetadataBytes: 1024,
      maxMetadataEnvelopes: 16,
      maxMetadataElements: 128,
      maxUnknownMetadataMembers: 16,
      maxMetadataGuards: 4,
      maxMetadataConflicts: maxMetadataConflicts,
      meshResourceLimits: GltfMeshResourceLimits.Default);
  }

  private static DynamicAuthoringValues CreateDynamicValues(DynamicEffectType effectType)
  {
    var frames = new CanonicalDynamicFrameSequence(0, 1, 0);
    var sprite = new CanonicalDynamicSpriteSheet(frames, 1, 1);
    return effectType switch
    {
      DynamicEffectType.Group or DynamicEffectType.Sphere =>
        new DynamicAuthoringValues(additive: true),
      DynamicEffectType.Track
        or DynamicEffectType.ScalableObject
        or DynamicEffectType.MappedExplosion => new DynamicAuthoringValues(frames),
      _ => new DynamicAuthoringValues(sprite)
    };
  }
}
