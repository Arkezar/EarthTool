using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Internal;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;

namespace EarthTool.MSH.Tests;

public sealed class CanonicalStaticGltfCreationTests
{
  [Fact]
  public async Task UntouchedRichExportCarriesCanonicalAuthoringValues()
  {
    var source = CreateSourceAsset(includeStaticLights: true);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(source, glb);
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    glb.Position = 0;

    var creation = await interchange.CreateMeshAsync(glb);

    creation.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(creation.Diagnostics));
    var actual = creation.Value.Should().BeOfType<StaticMeshAsset>().Subject;
    actual.CommonBaseHeader.BoxPresenceMask.Should().Be(source.CommonBaseHeader.BoxPresenceMask);
    actual.CommonBaseHeader.HorizontalExtents.Should().Equal(
      source.CommonBaseHeader.HorizontalExtents);
    actual.RootSourceObject.StaticRenderObjects[0].KnownFlags.Should().HaveFlag(
      StaticRenderObjectFlags.ViewerFaced);
    var barrel = actual.RootSourceObject.Children.Should().ContainSingle().Subject
      .StaticRenderObjects[0];
    barrel.KnownFlags.Should().HaveFlag(StaticRenderObjectFlags.Barrel);
    barrel.BarrelMaximumAngle.Should().Be(37);
  }

  private static readonly Guid _creationGuid = new("20220220-2222-4222-8222-202202202202");

  [Fact]
  public async Task CompleteStaticSemanticsAssembleWithoutSourceMsh()
  {
    var source = CreateSourceAsset();
    var glb = await ExportCanonicalGlbAsync(source, (root, meshNodes) =>
    {
      SetStaticOwner(
        meshNodes[0],
        1,
        new StaticSourceAuthoringValues(
          roles: GltfStaticObjectRoles.ViewerFaced
        )
      );
      SetStaticOwner(
        meshNodes[1],
        2,
        new StaticSourceAuthoringValues(
          roles: GltfStaticObjectRoles.Barrel,
          barrelMaximumAngle: 37
        )
      );
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var asset = result.Value!;
    asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    asset.ArchiveFraming.CreationGuid.Should().Be(_creationGuid);
    asset.RootSourceObject.StaticRenderObjects.Should().HaveCount(2);
    asset.RootSourceObject.Children.Should().ContainSingle();
    asset.RootSourceObject.StaticRenderObjects[0].KnownFlags.Should().HaveFlag(
      StaticRenderObjectFlags.ViewerFaced
    );
    var child = asset.RootSourceObject.Children[0];
    child.StaticRenderObjects.Should().ContainSingle();
    child.StaticRenderObjects[0].KnownFlags.Should().HaveFlag(StaticRenderObjectFlags.Barrel);
    child.StaticRenderObjects[0].BarrelMaximumAngle.Should().Be(37);
    child.StaticRenderObjects[0].Pivot.Should().Be(new Vector3(4, -6, 5));
    child.StaticRenderObjects[0].AnimationClassValue.Should().Be((uint)StaticAnimationClass.B);
    child.StaticRenderObjects[0].AnimationTracks.ScaleFrames.Should().HaveCount(2);
    child.StaticRenderObjects[0].AnimationTracks.TranslationFrames.Should().HaveCount(2);
    child.StaticRenderObjects[0].AnimationTracks.Matrices.Should().HaveCount(2);
  }

  [Fact]
  public async Task StaticLightArtistObjectsPopulateTheCompleteBaseHeader()
  {
    var footprint = new CanonicalStaticFootprint(
      0x8421,
      Enumerable.Repeat(1.5f, 16),
      Enumerable.Repeat((byte)3, 16)
    );
    var extents = new CanonicalHorizontalExtents(1.25f, 2.5f, 3.75f, 4.5f);
    var glb = await ExportCanonicalGlbAsync(
      CreateSourceAsset(includeStaticLights: true),
      (root, meshNodes) =>
      {
        SetStaticOwner(
          meshNodes[0],
          1,
          new StaticSourceAuthoringValues(
            footprint,
            extents,
            GltfStaticObjectRoles.ViewerFaced
          )
        );
        SetStaticOwner(
          meshNodes[1],
          2,
          new StaticSourceAuthoringValues(
            roles: GltfStaticObjectRoles.Barrel,
            barrelMaximumAngle: 37
          )
        );
        SetStaticLightOwner(
          root,
          CanonicalAuthoringOwner.Parse("ET_SpotLight_2"),
          new StaticLightAuthoringValues(0.625f, 8)
        );
        SetStaticLightOwner(
          root,
          CanonicalAuthoringOwner.Parse("ET_OmniLight_4"),
          new StaticLightAuthoringValues(0.375f)
        );
      }
    );

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var asset = result.Value!;
    asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    asset.CommonBaseHeader.BoxPresenceMask.Should().Be(0x8421);
    asset.CommonBaseHeader.BoxTopElevations.Should().Equal(
      Enumerable.Repeat(new byte[] { 0x80, 0x01 }, 16).SelectMany(value => value)
    );
    asset.CommonBaseHeader.BoxCornerPassageFlags.Should().OnlyContain(value => value == 3);
    asset.CommonBaseHeader.HorizontalExtents.Should().Equal(
      0x40, 0x01, 0x80, 0x02, 0xC0, 0x03, 0x80, 0x04
    );
    asset.CommonBaseHeader.AnimationLengths.Should().Be(new AnimationClassBytes(0, 2, 0, 0));
    asset.CommonBaseHeader.AnimationFrameIndices.Should().Be(default(AnimationClassBytes));

    var bytes = asset.GetSerializedRepresentation().ToArray();
    var spot = StaticLightMshFixture.GetSpot(bytes, 2);
    ReadSingle(spot, 0).Should().Be(2);
    ReadSingle(spot, 4).Should().Be(3);
    ReadSingle(spot, 8).Should().Be(4);
    ReadSingle(spot, 0x0C).Should().BeApproximately(0.25f, 0.00001f);
    ReadSingle(spot, 0x10).Should().BeApproximately(0.5f, 0.00001f);
    ReadSingle(spot, 0x14).Should().BeApproximately(0.75f, 0.00001f);
    ReadSingle(spot, 0x18).Should().Be(8);
    spot[0x1C].Should().Be(32);
    spot.AsSpan(0x1D, 3).ToArray().Should().OnlyContain(value => value == 0);
    ReadSingle(spot, 0x20).Should().BeApproximately(0.25f, 0.00001f);
    ReadSingle(spot, 0x24).Should().BeApproximately(4, 0.00001f);
    ReadSingle(spot, 0x28).Should().BeApproximately(0.5f, 0.00001f);
    ReadSingle(spot, 0x2C).Should().Be(0.625f);

    var omni = StaticLightMshFixture.GetOmni(bytes, 4);
    ReadSingle(omni, 0).Should().Be(6);
    ReadSingle(omni, 4).Should().Be(7);
    ReadSingle(omni, 8).Should().Be(8);
    ReadSingle(omni, 0x0C).Should().BeApproximately(0.6f, 0.00001f);
    ReadSingle(omni, 0x10).Should().BeApproximately(0.4f, 0.00001f);
    ReadSingle(omni, 0x14).Should().BeApproximately(0.2f, 0.00001f);
    ReadSingle(omni, 0x18).Should().Be(0.375f);

    StaticLightMshFixture.GetAttachment(bytes, 13).Should().Equal(
      0, 128, 0, 128, 0, 128, 0, 0
    );
    BinaryPrimitives.ReadInt16LittleEndian(
      StaticLightMshFixture.GetAttachment(bytes, 14)
    ).Should().NotBe(short.MinValue);
    BinaryPrimitives.ReadInt16LittleEndian(
      StaticLightMshFixture.GetAttachment(bytes, 20)
    ).Should().NotBe(short.MinValue);
    foreach (var number in Enumerable.Range(1, 4).Where(number => number != 2))
    {
      StaticLightMshFixture.GetSpot(bytes, number).Should().OnlyContain(value => value == 0);
      StaticLightMshFixture.GetAttachment(bytes, number + 12).Should().Equal(
        0, 128, 0, 128, 0, 128, 0, 0
      );
    }
    foreach (var number in Enumerable.Range(1, 4).Where(number => number != 4))
    {
      StaticLightMshFixture.GetOmni(bytes, number).Should().OnlyContain(value => value == 0);
      StaticLightMshFixture.GetAttachment(bytes, number + 16).Should().Equal(
        0, 128, 0, 128, 0, 128, 0, 0
      );
    }
  }

  [Fact]
  public async Task AttachmentAndCannonArtistObjectsRegenerateTheirCanonicalRecords()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      AddCanonicalOwners(root, meshNodes);
      AddArtistObject(
        root,
        meshNodes[0],
        "ET_HitPoint_2",
        new Vector3(1.25f, 2.5f, -3.75f),
        32
      );
      AddArtistObject(
        root,
        meshNodes[0],
        "ET_Turret_3",
        new Vector3(4.125f, 5.25f, -6.5f),
        96,
        new CannonAuthoringValues(0x31)
      );
      var separateCannonRecord = AddArtistObject(
        root,
        meshNodes[0],
        "ET_CannonRenderPosition_3",
        new Vector3(100, 100, 100),
        128
      );
      separateCannonRecord["extras"] = new JsonObject
      {
        ["earthtoolAuthoring"] = CanonicalAuthoringMetadata.Write(
          CanonicalAuthoringOwner.Parse("ET_Turret_3"),
          new CannonAuthoringValues(0x77),
          GltfOperationProfile.Default
        ),
      };
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var bytes = result.Value!.GetSerializedRepresentation().ToArray();
    AttachmentAndCannonMshFixture.GetAttachment(bytes, 26).Should().Equal(
      0x40, 0x01, 0x40, 0xFC, 0x80, 0x02, 32, 0x80
    );
    AttachmentAndCannonMshFixture.GetAttachment(bytes, 3).Should().Equal(
      0x20, 0x04, 0x80, 0xF9, 0x40, 0x05, 96, 0x31
    );
    var cannon = AttachmentAndCannonMshFixture.GetCannonRenderPosition(bytes, 3);
    ReadSingle(cannon, 0).Should().Be(4.125f);
    ReadSingle(cannon, 4).Should().Be(-6.5f);
    ReadSingle(cannon, 8).Should().Be(5.25f);
  }

  [Fact]
  public async Task EmitterOwnershipCrossesTransformOnlyGroupsAndCombinesMarkerRoles()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      AddCanonicalOwners(root, meshNodes);
      var group = AddTransformGroup(root, meshNodes[0], new Vector3(2, 0, 0));
      AddArtistObject(root, group, "ET_Emitter_1", new Vector3(1, 0, 0), 16);
      AddArtistObject(root, group, "ET_Emitter_3", new Vector3(3, 0, 0), 48);
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var asset = result.Value!;
    asset.RootSourceObject.StaticRenderObjects[0].KnownFlags.Should()
      .HaveFlag(StaticRenderObjectFlags.MarkerAttachment1)
      .And.HaveFlag(StaticRenderObjectFlags.MarkerAttachment3);
    asset.RootSourceObject.StaticRenderObjects[1].KnownFlags.Should()
      .NotHaveFlag(StaticRenderObjectFlags.MarkerAttachment1)
      .And.NotHaveFlag(StaticRenderObjectFlags.MarkerAttachment3);
    asset.RootSourceObject.Children[0].StaticRenderObjects[0].KnownFlags.Should()
      .NotHaveFlag(StaticRenderObjectFlags.MarkerAttachment1)
      .And.NotHaveFlag(StaticRenderObjectFlags.MarkerAttachment3);
    var bytes = asset.GetSerializedRepresentation().ToArray();
    BinaryPrimitives.ReadInt16LittleEndian(
      AttachmentAndCannonMshFixture.GetAttachment(bytes, 5)
    ).Should().Be(3 * 256);
    BinaryPrimitives.ReadInt16LittleEndian(
      AttachmentAndCannonMshFixture.GetAttachment(bytes, 7)
    ).Should().Be(5 * 256);
  }

  [Fact]
  public async Task EmitterOwnershipThroughANonTransformGroupFailsAtomically()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      AddCanonicalOwners(root, meshNodes);
      root["cameras"] = new JsonArray(
        new JsonObject
        {
          ["type"] = "perspective",
          ["perspective"] = new JsonObject { ["yfov"] = 0.7, ["znear"] = 0.1 },
        }
      );
      var camera = AddTransformGroup(root, meshNodes[0], Vector3.Zero);
      camera["camera"] = 0;
      AddArtistObject(root, camera, "ET_Emitter_1", Vector3.Zero, 0);
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfDiagnosticCodes.UnsupportedDomain
      && item.Data.ContainsKey("domain")
      && item.Data["domain"] == "EmitterMarkerHierarchy"
    );
  }

  [Theory]
  [InlineData("ET_HitPoint_1")]
  [InlineData("ET_Turret_1")]
  public async Task DuplicateArtistObjectOwnersFailAtomically(string name)
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      AddCanonicalOwners(root, meshNodes);
      AddArtistObject(root, meshNodes[0], name, Vector3.Zero, 0);
      AddArtistObject(root, meshNodes[0], name, Vector3.One, 0);
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.DuplicateOwner
    );
  }

  [Fact]
  public async Task InvalidCannonTypedValueDefaultsWithAWarning()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      AddCanonicalOwners(root, meshNodes);
      var cannon = AddArtistObject(
        root,
        meshNodes[0],
        "ET_Turret_1",
        new Vector3(1, 2, 3),
        64
      );
      cannon["extras"] = new JsonObject
      {
        ["earthtoolAuthoring"] = "{\"format\":\"earthtool.msh.authoring\",\"version\":1,"
          + "\"values\":{\"cannonYawHalfRange\":999}}",
      };
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    AttachmentAndCannonMshFixture.GetAttachment(
      result.Value!.GetSerializedRepresentation().ToArray(),
      1
    )[7].Should().Be(0x80);
    result.Diagnostics.Should().Contain(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.OptionalValueDefaulted
      && item.Severity == DiagnosticSeverity.Warning
      && item.Path.EndsWith(".values.cannonYawHalfRange", StringComparison.Ordinal)
    );
  }

  [Fact]
  public async Task UnsupportedAttachmentPoseFailsAtomically()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      AddCanonicalOwners(root, meshNodes);
      var attachment = AddArtistObject(
        root,
        meshNodes[0],
        "ET_LandingSpot_1",
        Vector3.Zero,
        0
      );
      attachment["rotation"] = new JsonArray(0, 0, 0, 1);
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfDiagnosticCodes.UnsupportedDomain
      && item.Data.ContainsKey("domain")
      && item.Data["domain"] == "AttachmentPose"
    );
  }

  [Fact]
  public async Task EquivalentArtistObjectAndMetadataOrderingProducesIdenticalBytes()
  {
    var baseGlb = await ExportCanonicalGlbAsync(CreateSourceAsset(), AddCanonicalOwners);
    var first = RewriteGlb(baseGlb, root =>
    {
      var source = MeshNodes(root)[0];
      AddArtistObject(root, source, "ET_HitPoint_1", new Vector3(1, 2, 3), 16);
      AddArtistObject(
        root,
        source,
        "ET_Turret_2",
        new Vector3(4, 5, 6),
        32,
        new CannonAuthoringValues(0x31)
      );
    });
    var second = RewriteGlb(baseGlb, root =>
    {
      var source = MeshNodes(root)[0];
      var cannon = AddArtistObject(
        root,
        source,
        "ET_Turret_2",
        new Vector3(4, 5, 6),
        32
      );
      cannon["extras"] = new JsonObject
      {
        ["earthtoolAuthoring"] = "{\"values\":{\"cannonYawHalfRange\":49},"
          + "\"version\":1,\"format\":\"earthtool.msh.authoring\"}",
      };
      AddArtistObject(root, source, "ET_HitPoint_1", new Vector3(1, 2, 3), 16);
    });
    var options = new CanonicalStaticGltfCreationOptions(_creationGuid);

    var firstResult = GltfInterchange.ImportCanonicalStaticGlb(
      first,
      options,
      GltfOperationProfile.Default,
      CancellationToken.None
    );
    var secondResult = GltfInterchange.ImportCanonicalStaticGlb(
      second,
      options,
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    firstResult.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(firstResult.Diagnostics));
    secondResult.Status.Should().Be(
      OperationStatus.Succeeded,
      Diagnostics(secondResult.Diagnostics)
    );
    secondResult.Value!.GetSerializedRepresentation().Should().Equal(
      firstResult.Value!.GetSerializedRepresentation()
    );
  }

  [Fact]
  public async Task TypedTargetDistanceAuthorsASpotWithoutNativeRange()
  {
    var glb = await ExportCanonicalGlbAsync(
      CreateSourceAsset(includeStaticLights: true),
      (root, meshNodes) =>
      {
        AddCanonicalOwners(root, meshNodes);
        var spotOwner = CanonicalAuthoringOwner.Parse("ET_SpotLight_2");
        LightDefinition(root, spotOwner).Remove("range");
        SetStaticLightOwner(
          root,
          spotOwner,
          new StaticLightAuthoringValues(0.625f, 12)
        );
        SetStaticLightOwner(
          root,
          CanonicalAuthoringOwner.Parse("ET_OmniLight_4"),
          new StaticLightAuthoringValues()
        );
      }
    );

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var spot = StaticLightMshFixture.GetSpot(
      result.Value!.GetSerializedRepresentation().ToArray(),
      2
    );
    ReadSingle(spot, 0x18).Should().Be(12);
    ReadSingle(spot, 0x2C).Should().Be(0.625f);
  }

  [Fact]
  public async Task MissingOptionalLightValuesDefaultWithWarnings()
  {
    string? spotNodePath = null;
    var glb = await ExportCanonicalGlbAsync(
      CreateSourceAsset(includeStaticLights: true),
      (root, meshNodes) =>
      {
        AddCanonicalOwners(root, meshNodes);
        var spotOwner = CanonicalAuthoringOwner.Parse("ET_SpotLight_2");
        LightDefinition(root, spotOwner)["range"] = 8;
        spotNodePath = $"nodes[{NodeIndex(root, spotOwner)}]";
        SetStaticLightOwner(
          root,
          CanonicalAuthoringOwner.Parse("ET_OmniLight_4"),
          new StaticLightAuthoringValues(0.375f)
        );
      }
    );

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var spot = StaticLightMshFixture.GetSpot(
      result.Value!.GetSerializedRepresentation().ToArray(),
      2
    );
    ReadSingle(spot, 0x2C).Should().Be(1);
    result.Diagnostics.Should().Contain(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.OptionalValueDefaulted
      && item.Severity == DiagnosticSeverity.Warning
      && item.Path == spotNodePath
    );
  }

  [Fact]
  public async Task SpotWithoutNativeOrTypedTargetDistanceFailsAtomically()
  {
    var glb = await ExportCanonicalGlbAsync(
      CreateSourceAsset(includeStaticLights: true),
      (root, meshNodes) =>
      {
        AddCanonicalOwners(root, meshNodes);
        var spotOwner = CanonicalAuthoringOwner.Parse("ET_SpotLight_2");
        LightDefinition(root, spotOwner).Remove("range");
        SetStaticLightOwner(root, spotOwner, new StaticLightAuthoringValues());
        SetStaticLightOwner(
          root,
          CanonicalAuthoringOwner.Parse("ET_OmniLight_4"),
          new StaticLightAuthoringValues()
        );
      }
    );

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfDiagnosticCodes.UnsupportedDomain
      && item.Path.EndsWith(".range", StringComparison.Ordinal)
    );
  }

  [Fact]
  public async Task DuplicateStaticIdentifiersFailBeforeAssembly()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      SetStaticOwner(meshNodes[0], 1, new StaticSourceAuthoringValues());
      SetStaticOwner(meshNodes[1], 1, new StaticSourceAuthoringValues());
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.DuplicateOwner
    );
  }

  [Fact]
  public async Task TextureIdentityComesOnlyFromExplicitCreationBindings()
  {
    const string sourceKey = "Textures\\source-name.tex";
    const string explicitKey = "Textures\\explicit-binding.tex";
    var glb = await ExportCanonicalGlbAsync(
      CreateSourceAsset(sourceKey),
      (root, meshNodes) =>
      {
        SetStaticOwner(meshNodes[0], 1, new StaticSourceAuthoringValues());
        SetStaticOwner(meshNodes[1], 2, new StaticSourceAuthoringValues());
        root["materials"]![0]!["name"] = sourceKey;
        root["images"]![0]!["name"] = sourceKey;
      }
    );

    var withoutBinding = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );
    var withBinding = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(
        _creationGuid,
        new Dictionary<GltfMaterialHandle, string?>
        {
          [new GltfMaterialHandle(1)] = explicitKey,
        }
      ),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    withoutBinding.Status.Should().Be(OperationStatus.Failed);
    withoutBinding.Diagnostics.Should().Contain(item =>
      item.Code == GltfDiagnosticCodes.TextureResourceBindingRequired
    );
    withBinding.Status.Should().Be(
      OperationStatus.Succeeded,
      Diagnostics(withBinding.Diagnostics)
    );
    Encoding.ASCII.GetString(
      withBinding.Value!.StaticRenderObjectSequence[0].TexturePathBytes.ToArray()
    ).Should().Be(explicitKey);
  }

  [Fact]
  public async Task InvalidOptionalTypedValuesDefaultWithWarnings()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      meshNodes[0]["name"] = "ET_Static_1";
      meshNodes[0]["extras"] = new JsonObject
      {
        ["earthtoolAuthoring"] = "{\"format\":\"earthtool.msh.authoring\",\"version\":1,"
          + "\"values\":{\"role\":{\"viewerFaced\":true}}}",
      };
      SetStaticOwner(meshNodes[1], 2, new StaticSourceAuthoringValues());
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result.Value!.RootSourceObject.StaticRenderObjects[0].KnownFlags.Should().Be(
      StaticRenderObjectFlags.None
    );
    result.Diagnostics.Should().Contain(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.OptionalValueDefaulted
      && item.Severity == DiagnosticSeverity.Warning
      && item.Path.Contains("role", StringComparison.Ordinal)
    );
  }

  [Fact]
  public async Task PublicCreationEntryPointUsesCanonicalStaticPath()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), AddCanonicalOwners);
    await using var input = new MemoryStream(glb);

    var result = await new GltfInterchange().CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var asset = result.Value!.Should().BeOfType<StaticMeshAsset>().Subject;
    asset.RootSourceObject.StaticRenderObjects[0].KnownFlags.Should().Be(
      StaticRenderObjectFlags.ViewerFaced
    );
  }

  [Fact]
  public async Task PublicCreationRandomGuidIsTheOnlyPermittedDifference()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), AddCanonicalOwners);
    await using var firstInput = new MemoryStream(glb);
    await using var secondInput = new MemoryStream(glb);

    var first = await new GltfInterchange().CreateMeshAsync(firstInput);
    var second = await new GltfInterchange().CreateMeshAsync(secondInput);

    first.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(first.Diagnostics));
    second.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(second.Diagnostics));
    var firstBytes = first.Value!.GetSerializedRepresentation().ToArray();
    var secondBytes = second.Value!.GetSerializedRepresentation().ToArray();
    firstBytes[..4].Should().Equal(secondBytes[..4]);
    firstBytes[20..].Should().Equal(secondBytes[20..]);
    firstBytes[4..20].Should().NotEqual(secondBytes[4..20]);
  }

  [Fact]
  public async Task RequiredDynamicOwnerFailsInStaticCreation()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), (root, meshNodes) =>
    {
      AddCanonicalOwners(root, meshNodes);
      var nodes = root["nodes"]!.AsArray();
      var rootIndex = root["scenes"]![0]!["nodes"]![0]!.GetValue<int>();
      nodes[rootIndex]!["name"] = "ET_Dynamic_1_Group";
    });

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.RequiredValueMissing
    );
  }

  [Fact]
  public async Task LegacyMetadataOutsideNamedNodesIsIgnored()
  {
    var glb = await ExportCanonicalGlbAsync(CreateSourceAsset(), AddCanonicalOwners);
    glb = RewriteGlb(glb, root =>
      root["scenes"]![0]!["extras"] = new JsonObject
      {
        ["earthtool"] = "{\"format\":\"earthtool.msh.authoring\",\"version\":1,"
          + "\"values\":{}}",
      }
    );

    var result = GltfInterchange.ImportCanonicalStaticGlb(
      glb,
      new CanonicalStaticGltfCreationOptions(_creationGuid),
      GltfOperationProfile.Default,
      CancellationToken.None
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result.Value.Should().NotBeNull();
  }

  [Fact]
  public async Task EquivalentGlbAndSeparateGltfProduceIdenticalBytesWithFixedGuid()
  {
    var source = CreateSourceAsset(includeStaticLights: true);
    var interchange = new GltfInterchange();
    var glb = await ExportCanonicalGlbAsync(source, (root, meshNodes) =>
    {
      AddCanonicalOwners(root, meshNodes);
      AddCanonicalStaticLightOwners(root);
    });
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-static-202-{Guid.NewGuid():N}");
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var exported = await interchange.ExportGltfFileAsync(source, path);
      exported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(exported.Diagnostics));
      var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveEarthToolMetadata(json);
      AddCanonicalOwners(json, MeshNodes(json));
      AddCanonicalStaticLightOwners(json);
      await File.WriteAllTextAsync(path, json.ToJsonString());
      var bufferUri = json["buffers"]![0]!["uri"]!.GetValue<string>();
      var binary = await File.ReadAllBytesAsync(Path.Combine(directory, bufferUri));
      var options = new CanonicalStaticGltfCreationOptions(_creationGuid);

      var glbResult = GltfInterchange.ImportCanonicalStaticGlb(
        glb,
        options,
        GltfOperationProfile.Default,
        CancellationToken.None
      );
      var gltfResult = GltfInterchange.ImportCanonicalStaticSeparate(
        Encoding.UTF8.GetBytes(json.ToJsonString()),
        binary,
        options,
        GltfOperationProfile.Default,
        CancellationToken.None
      );

      glbResult.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(glbResult.Diagnostics));
      gltfResult.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(gltfResult.Diagnostics));
      gltfResult.Value!.GetSerializedRepresentation().Should().Equal(
        glbResult.Value!.GetSerializedRepresentation()
      );
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  private static StaticMeshAsset CreateSourceAsset(
    string? textureResourceKey = null,
    bool includeStaticLights = false
  )
  {
    var root = new CanonicalStaticSourceObject(
      [CreateRenderObject(0, textureResourceKey), CreateRenderObject(10)],
      [
        new CanonicalStaticSourceObject(
          [CreateRenderObject(20)],
          role: new CanonicalStaticObjectRole(
            StaticRenderObjectFlags.Barrel,
            barrelMaximumAngle: 37
          )
        ),
      ],
      new CanonicalStaticObjectRole(StaticRenderObjectFlags.ViewerFaced)
    );
    var frames = new[]
    {
      new ProjectedAnimationFrame(Vector3.One, Quaternion.Identity, Vector3.Zero),
      new ProjectedAnimationFrame(new Vector3(2), Quaternion.Identity, new Vector3(1, 2, 3)),
    };
    var animation = new StaticAnimationReplacement(
      StaticAnimationProjection.CreateCanonicalTracks(frames),
      (uint)StaticAnimationClass.B
    );
    var attachmentRecords = includeStaticLights
      ? new Dictionary<int, CanonicalAttachmentRecord>
      {
        [14] = new CanonicalAttachmentRecord(new Vector3(2, 3, 4), 0, 0),
        [20] = new CanonicalAttachmentRecord(new Vector3(6, 7, 8), 0, 0),
      }
      : null;
    var staticSpotLights = includeStaticLights
      ? new Dictionary<int, CanonicalSpotLight>
      {
        [2] = new CanonicalSpotLight(
          new Vector3(2, 3, 4),
          new Vector3(0.25f, 0.5f, 0.75f),
          8,
          32,
          0.25f,
          4,
          0.5f,
          0.875f
        ),
      }
      : null;
    var staticOmniLights = includeStaticLights
      ? new Dictionary<int, CanonicalOmniLight>
      {
        [4] = new CanonicalOmniLight(
          new Vector3(6, 7, 8),
          new Vector3(0.6f, 0.4f, 0.2f),
          0.875f
        ),
      }
      : null;
    var build = CanonicalStaticMeshAssembler.Assemble(
      new CanonicalStaticMeshAssemblyInput(
        Guid.NewGuid(),
        new CanonicalStaticBaseHeaderInput(
          new AnimationClassBytes(0, 2, 0, 0),
          root.RenderObjects.Concat(root.Children[0].RenderObjects).SelectMany(item =>
            item.RenderVertices
          ),
          attachmentRecords: attachmentRecords,
          staticSpotLights: staticSpotLights,
          staticOmniLights: staticOmniLights
        ),
        root,
        new Dictionary<int, Vector3> { [1] = new Vector3(4, -6, 5) },
        new Dictionary<int, StaticAnimationReplacement> { [1] = animation },
        new Dictionary<int, string?>
        {
          [0] = textureResourceKey,
          [1] = null,
          [2] = null,
        }
      )
    );
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static CanonicalStaticRenderObject CreateRenderObject(
    float x,
    string? textureResourceKey = null
  )
  {
    return new CanonicalStaticRenderObject(
      [
        new CanonicalStaticVertex(new Vector3(x, 0, 0), Vector3.UnitZ, Vector2.Zero),
        new CanonicalStaticVertex(new Vector3(x + 1, 0, 0), Vector3.UnitZ, Vector2.UnitX),
        new CanonicalStaticVertex(new Vector3(x, 1, 0), Vector3.UnitZ, Vector2.UnitY),
      ],
      [new CanonicalTriangle(0, 1, 2)],
      textureResourceKey
    );
  }

  private static async Task<byte[]> ExportCanonicalGlbAsync(
    StaticMeshAsset source,
    Action<JsonObject, IReadOnlyList<JsonObject>> rewrite
  )
  {
    await using var destination = new MemoryStream();
    var exported = await new GltfInterchange().ExportGlbAsync(source, destination);
    exported.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(exported.Diagnostics));
    return RewriteGlb(destination.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      rewrite(root, MeshNodes(root));
    });
  }

  private static void AddCanonicalOwners(
    JsonObject root,
    IReadOnlyList<JsonObject> meshNodes
  )
  {
    SetStaticOwner(
      meshNodes[0],
      1,
      new StaticSourceAuthoringValues(roles: GltfStaticObjectRoles.ViewerFaced)
    );
    SetStaticOwner(
      meshNodes[1],
      2,
      new StaticSourceAuthoringValues(
        roles: GltfStaticObjectRoles.Barrel,
        barrelMaximumAngle: 37
      )
    );
  }

  private static void AddCanonicalStaticLightOwners(JsonObject root)
  {
    SetStaticLightOwner(
      root,
      CanonicalAuthoringOwner.Parse("ET_SpotLight_2"),
      new StaticLightAuthoringValues(0.625f, 8)
    );
    SetStaticLightOwner(
      root,
      CanonicalAuthoringOwner.Parse("ET_OmniLight_4"),
      new StaticLightAuthoringValues(0.375f)
    );
  }

  private static void SetStaticOwner(
    JsonObject node,
    int number,
    StaticSourceAuthoringValues values
  )
  {
    var name = $"ET_Static_{number}";
    node["name"] = name;
    node["extras"] = new JsonObject
    {
      ["earthtoolAuthoring"] = CanonicalAuthoringMetadata.Write(
        CanonicalAuthoringOwner.Parse(name),
        values,
        GltfOperationProfile.Default
      ),
    };
  }

  private static void SetStaticLightOwner(
    JsonObject root,
    CanonicalAuthoringOwner owner,
    StaticLightAuthoringValues values
  )
  {
    var node = root["nodes"]!
      .AsArray()
      .Select(item => item!.AsObject())
      .Single(item => item["name"]?.GetValue<string>() == owner.CanonicalName);
    node["extras"] = new JsonObject
    {
      ["earthtoolAuthoring"] = CanonicalAuthoringMetadata.Write(
        owner,
        values,
        GltfOperationProfile.Default
      ),
    };
  }

  private static JsonObject AddArtistObject(
    JsonObject root,
    JsonObject parent,
    string name,
    Vector3 translation,
    byte heading,
    AuthoringMetadataValues? values = null
  )
  {
    var rotation = AttachmentHeadingProjection.CreateRotation(heading);
    var node = new JsonObject
    {
      ["name"] = name,
      ["translation"] = new JsonArray(translation.X, translation.Y, translation.Z),
      ["rotation"] = new JsonArray(rotation.X, rotation.Y, rotation.Z, rotation.W),
    };
    if (values is not null)
    {
      node["extras"] = new JsonObject
      {
        ["earthtoolAuthoring"] = CanonicalAuthoringMetadata.Write(
          CanonicalAuthoringOwner.Parse(name),
          values,
          GltfOperationProfile.Default
        ),
      };
    }
    return AppendChildNode(root, parent, node);
  }

  private static JsonObject AddTransformGroup(
    JsonObject root,
    JsonObject parent,
    Vector3 translation
  )
  {
    return AppendChildNode(
      root,
      parent,
      new JsonObject
      {
        ["translation"] = new JsonArray(translation.X, translation.Y, translation.Z),
      }
    );
  }

  private static JsonObject AppendChildNode(
    JsonObject root,
    JsonObject parent,
    JsonObject child
  )
  {
    var nodes = root["nodes"]!.AsArray();
    var index = nodes.Count;
    nodes.Add(child);
    if (parent["children"] is not JsonArray children)
    {
      children = new JsonArray();
      parent["children"] = children;
    }
    children.Add(index);
    return child;
  }

  private static JsonObject LightDefinition(
    JsonObject root,
    CanonicalAuthoringOwner owner
  )
  {
    return root["extensions"]!["KHR_lights_punctual"]!["lights"]!
      .AsArray()
      .Select(item => item!.AsObject())
      .Single(item => item["name"]?.GetValue<string>() == owner.CanonicalName);
  }

  private static int NodeIndex(JsonObject root, CanonicalAuthoringOwner owner)
  {
    return root["nodes"]!
      .AsArray()
      .Select((item, index) => (item, index))
      .Single(item => item.item!["name"]?.GetValue<string>() == owner.CanonicalName)
      .index;
  }

  private static IReadOnlyList<JsonObject> MeshNodes(JsonObject root)
  {
    return root["nodes"]!
      .AsArray()
      .Select(node => node!.AsObject())
      .Where(node => node.ContainsKey("mesh"))
      .ToArray();
  }

  private static void RemoveEarthToolMetadata(JsonNode node)
  {
    if (node is JsonObject @object)
    {
      @object.Remove("earthtool");
      @object.Remove("earthtoolAuthoring");
      foreach (var child in @object.ToArray())
      {
        if (child.Value is not null)
        {
          RemoveEarthToolMetadata(child.Value);
        }
      }
    }
    else if (node is JsonArray array)
    {
      foreach (var child in array)
      {
        if (child is not null)
        {
          RemoveEarthToolMetadata(child);
        }
      }
    }
  }

  private static byte[] RewriteGlb(byte[] glb, Action<JsonObject> rewrite)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var root = JsonNode.Parse(glb.AsSpan(20, jsonLength))!.AsObject();
    rewrite(root);
    var json = Encoding.UTF8.GetBytes(root.ToJsonString());
    var paddedJsonLength = (json.Length + 3) & ~3;
    var oldBinaryHeader = 20 + jsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(oldBinaryHeader));
    var result = new byte[12 + 8 + paddedJsonLength + 8 + binaryLength];
    BinaryPrimitives.WriteUInt32LittleEndian(result, 0x46546C67);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), 2);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), paddedJsonLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
    json.CopyTo(result.AsSpan(20));
    result.AsSpan(20 + json.Length, paddedJsonLength - json.Length).Fill(0x20);
    var newBinaryHeader = 20 + paddedJsonLength;
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(newBinaryHeader), binaryLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(newBinaryHeader + 4), 0x004E4942);
    glb.AsSpan(oldBinaryHeader + 8, binaryLength).CopyTo(result.AsSpan(newBinaryHeader + 8));
    return result;
  }

  private static float ReadSingle(byte[] bytes, int offset)
  {
    return BitConverter.Int32BitsToSingle(
      BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset))
    );
  }

  private static string Diagnostics(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return string.Join(
      Environment.NewLine,
      diagnostics.Select(item => $"{item.Code} {item.Path}: {item.Message}")
    );
  }
}
