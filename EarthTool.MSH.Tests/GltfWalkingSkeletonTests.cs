using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Operations;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EarthTool.MSH.Tests;

public class GltfWalkingSkeletonTests
{
  [Fact]
  public async Task NewModelStaticLightRequiresCanonicalDefinitionName()
  {
    var asset = await ReadAssetAsync(
      StaticLightMshFixture.Create(
        new Dictionary<int, StaticLightMshFixture.SpotRecord>
        {
          [1] = new(Vector3.Zero, Vector3.One, 0, 0, [0, 0, 0], 0.2f, 5, 0.25f, 4),
        },
        activeSpots: [1]
      )
    );
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported);
    var noncanonicalDefinition = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!["name"] = "Artist light";
        root["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!["range"] = 5;
      }
    );
    await using var noncanonicalInput = new MemoryStream(noncanonicalDefinition);

    var noncanonical = await interchange.CreateMeshAsync(noncanonicalInput);

    noncanonical.Status.Should().Be(OperationStatus.Failed);
    noncanonical.Value.Should().BeNull();
    noncanonical
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Path == "extensions.KHR_lights_punctual.lights[0].name"
      );
  }

  [Fact]
  public async Task NewModelStaticLightMustBeALeafNode()
  {
    var asset = await ReadAssetAsync(
      StaticLightMshFixture.Create(
        new Dictionary<int, StaticLightMshFixture.SpotRecord>
        {
          [1] = new(Vector3.Zero, Vector3.One, 0, 0, [0, 0, 0], 0.2f, 5, 0.25f, 4),
        },
        activeSpots: [1]
      )
    );
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported);
    var childBearing = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!["range"] = 5;
        var nodes = root["nodes"]!.AsArray();
        var light = nodes
          .Single(node => node!["name"]!.GetValue<string>() == "ET_SpotLight_1")!
          .AsObject();
        var childIndex = nodes.Count;
        nodes.Add(new JsonObject { ["name"] = "Artist note" });
        light["children"] = new JsonArray(childIndex);
      }
    );
    await using var childInput = new MemoryStream(childBearing);

    var childResult = await interchange.CreateMeshAsync(childInput);

    childResult.Status.Should().Be(OperationStatus.Failed);
    childResult.Value.Should().BeNull();
    childResult
      .Diagnostics.Should()
      .ContainSingle(diagnostic => diagnostic.Path.StartsWith("nodes[", StringComparison.Ordinal));
  }

  [Fact]
  public async Task NewModelStaticLightDefinitionCannotBeSharedWithSceneLighting()
  {
    var asset = await ReadAssetAsync(
      StaticLightMshFixture.Create(
        new Dictionary<int, StaticLightMshFixture.SpotRecord>
        {
          [1] = new(Vector3.Zero, Vector3.One, 0, 0, [0, 0, 0], 0.2f, 5, 0.25f, 4),
        },
        activeSpots: [1]
      )
    );
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported);
    var sharedDefinition = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!["range"] = 5;
        var nodes = root["nodes"]!.AsArray();
        nodes[0]!["children"]!.AsArray().Add(nodes.Count);
        nodes.Add(
          new JsonObject
          {
            ["name"] = "Artist fill light",
            ["extensions"] = new JsonObject
            {
              ["KHR_lights_punctual"] = new JsonObject { ["light"] = 0 },
            },
          }
        );
      }
    );
    await using var sharedInput = new MemoryStream(sharedDefinition);

    var sharedResult = await interchange.CreateMeshAsync(sharedInput);

    sharedResult.Status.Should().Be(OperationStatus.Failed);
    sharedResult.Value.Should().BeNull();
    sharedResult
      .Diagnostics.Should()
      .ContainSingle(
        diagnostic => diagnostic.Path.EndsWith(
          "extensions.KHR_lights_punctual.lights[0]",
          StringComparison.Ordinal),
        string.Join("; ", sharedResult.Diagnostics.Select(diagnostic => diagnostic.Message)));
  }

  [Fact]
  public async Task GenericNamedPunctualLightsAuthorCanonicalStaticRecords()
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [2] = new(
          new Vector3(1.25f, -2.5f, 3.75f),
          new Vector3(0.2f, 0.4f, 0.6f),
          10,
          40,
          [1, 2, 3],
          0.2f,
          5,
          0.25f,
          4
        ),
      },
      new Dictionary<int, StaticLightMshFixture.OmniRecord>
      {
        [4] = new(new Vector3(-4.5f, 5.25f, -6.75f), new Vector3(0.7f, 0.8f, 0.9f), 8),
      },
      activeSpots: [2],
      activeOmnis: [4]
    );
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, glb);
    var metadataFree = RewriteJson(glb.ToArray(), RemoveEarthToolMetadata);

    await using var input = new MemoryStream(metadataFree);
    var import = await interchange.CreateMeshAsync(
      input,
      new GltfNewModelImportOptions(
        staticLightOptions: new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>
        {
          [new GltfLightHandle(1)] = new(targetDistance: 10, terrainLightAmplitude: 2.5f),
        }
      )
    );

    import
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    var result = StaticAsset(import).GetSerializedRepresentation().ToArray();
    var spot = StaticLightMshFixture.GetSpot(result, 2);
    new[] { ReadSingle(spot, 0), ReadSingle(spot, 4), ReadSingle(spot, 8) }
      .Should()
      .Equal(1.25f, -2.5f, 3.75f);
    new[] { ReadSingle(spot, 0x0C), ReadSingle(spot, 0x10), ReadSingle(spot, 0x14) }
      .Should()
      .Equal(0.2f, 0.4f, 0.6f);
    ReadSingle(spot, 0x2C).Should().Be(2.5f);
    BinaryPrimitives
      .ReadInt16LittleEndian(StaticLightMshFixture.GetAttachment(result, 14))
      .Should()
      .NotBe(short.MinValue);
    var omni = StaticLightMshFixture.GetOmni(result, 4);
    new[] { ReadSingle(omni, 0), ReadSingle(omni, 4), ReadSingle(omni, 8) }
      .Should()
      .Equal(-4.5f, 5.25f, -6.75f);
    ReadSingle(omni, 0x18).Should().Be(1);
    BinaryPrimitives
      .ReadInt16LittleEndian(StaticLightMshFixture.GetAttachment(result, 20))
      .Should()
      .NotBe(short.MinValue);
    import
      .Diagnostics.Where(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.NewModelPhotometricIntensityIgnored
      )
      .Select(diagnostic => diagnostic.Path)
      .Should()
      .BeEquivalentTo(
        "extensions.KHR_lights_punctual.lights[0].intensity",
        "extensions.KHR_lights_punctual.lights[1].intensity"
      );
  }

  [Fact]
  public async Task PositiveNewModelSpotRangeAuthorsTargetDistance()
  {
    var asset = await ReadAssetAsync(
      StaticLightMshFixture.Create(
        new Dictionary<int, StaticLightMshFixture.SpotRecord>
        {
          [1] = new(Vector3.Zero, Vector3.One, 0, 0, [0, 0, 0], 0.2f, 5, 0.25f, 4),
        },
        activeSpots: [1]
      )
    );
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported);
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!["range"] = 12.5f;
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    var spot = StaticLightMshFixture.GetSpot(
      StaticAsset(imported).GetSerializedRepresentation().ToArray(),
      1
    );
    ReadSingle(spot, 0x18).Should().Be(12.5f);
    ReadSingle(spot, 0x2C).Should().Be(1);
  }

  [Theory]
  [InlineData(10f)]
  [InlineData(6f)]
  public async Task NewModelSpotRangeRejectsTypedTargetDistance(float range)
  {
    var asset = await ReadAssetAsync(
      StaticLightMshFixture.Create(
        new Dictionary<int, StaticLightMshFixture.SpotRecord>
        {
          [1] = new(Vector3.Zero, Vector3.One, 0, 0, [0, 0, 0], 0.2f, 5, 0.25f, 4),
        },
        activeSpots: [1]
      )
    );
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported);
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!["range"] = range;
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(
      source,
      new GltfNewModelImportOptions(
        staticLightOptions: new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>
        {
          [new GltfLightHandle(1)] = new(targetDistance: 10),
        }
      )
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic => diagnostic.Code == GltfDiagnosticCodes.UnsupportedDomain)
      .Subject.Path.Should()
      .Be("extensions.KHR_lights_punctual.lights[0].range");
  }

  [Fact]
  public async Task GenericSpotLightRequiresExplicitPositiveTargetDistance()
  {
    var asset = await ReadAssetAsync(
      StaticLightMshFixture.Create(
        new Dictionary<int, StaticLightMshFixture.SpotRecord>
        {
          [1] = new(Vector3.Zero, Vector3.One, 0, 0, [0, 0, 0], 0.2f, 5, 0.25f, 4),
        },
        new Dictionary<int, StaticLightMshFixture.OmniRecord>(),
        activeSpots: [1]
      )
    );
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported);
    var sourceBytes = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!.AsObject().Remove("range");
      }
    );
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Data.Should()
      .Contain(new KeyValuePair<string, string>("domain", "StaticLights"));

    var missingDefinitionBytes = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        var node = root["nodes"]!
          .AsArray()
          .Single(item => item!["name"]!.GetValue<string>() == "ET_SpotLight_1")!
          .AsObject();
        node.Remove("extensions");
      }
    );
    await using var missingDefinitionSource = new MemoryStream(missingDefinitionBytes);
    var missingDefinition = await interchange.CreateMeshAsync(missingDefinitionSource);
    missingDefinition.Status.Should().Be(OperationStatus.Failed);
    missingDefinition.Value.Should().BeNull();
  }

  [Fact]
  public async Task ExportUsesDescriptiveAttachmentArtistObjectNames()
  {
    var attachments = Enumerable
      .Range(1, 49)
      .ToDictionary(
        number => number,
        number => new AttachmentAndCannonMshFixture.AttachmentRecord(
          checked((short)number),
          checked((short)-number),
          checked((short)(number * 2)),
          checked((byte)number),
          0x80
        )
      );
    var cannonPositions = Enumerable
      .Range(1, 4)
      .ToDictionary(number => number, number => new Vector3(number, -number, number * 2));
    var asset = await ReadAssetAsync(
      AttachmentAndCannonMshFixture.Create(attachments, cannonPositions)
    );
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );

    export.Status.Should().Be(OperationStatus.Succeeded);
    using var json = ReadGlbJson(glb.ToArray());
    var names = json
      .RootElement.GetProperty("nodes")
      .EnumerateArray()
      .Select(node => node.GetProperty("name").GetString())
      .Where(name => name?.StartsWith("ET_", StringComparison.Ordinal) == true)
      .ToArray();
    names
      .Should()
      .BeEquivalentTo(
        "ET_Static_1",
        "ET_Turret_1",
        "ET_Turret_2",
        "ET_Turret_3",
        "ET_Turret_4",
        "ET_Emitter_1",
        "ET_Emitter_2",
        "ET_Emitter_3",
        "ET_Emitter_4",
        "ET_TurretMuzzle_1",
        "ET_TurretMuzzle_2",
        "ET_TurretMuzzle_3",
        "ET_TurretMuzzle_4",
        "ET_SpotLight_1",
        "ET_SpotLight_2",
        "ET_SpotLight_3",
        "ET_SpotLight_4",
        "ET_OmniLight_1",
        "ET_OmniLight_2",
        "ET_OmniLight_3",
        "ET_OmniLight_4",
        "ET_UnloadPoint_1",
        "ET_UnloadPoint_2",
        "ET_UnloadPoint_3",
        "ET_UnloadPoint_4",
        "ET_HitPoint_1",
        "ET_HitPoint_2",
        "ET_HitPoint_3",
        "ET_HitPoint_4",
        "ET_SmokePoint_1",
        "ET_SmokePoint_2",
        "ET_SmokePoint_3",
        "ET_SmokePoint_4",
        "ET_WT_1",
        "ET_WT_2",
        "ET_WT_3",
        "ET_WT_4",
        "ET_Chimney_1",
        "ET_Chimney_2",
        "ET_SmokeTrace_1",
        "ET_SmokeTrace_2",
        "ET_Exhaust_1",
        "ET_Exhaust_2",
        "ET_KeelTrace_1",
        "ET_KeelTrace_2",
        "ET_InterfacePivot_1",
        "ET_CenterPivot_1",
        "ET_ProductionSpotStart_1",
        "ET_ProductionSpotEnd_1",
        "ET_LandingSpot_1"
      );
  }

  [Theory]
  [InlineData(1, StaticRenderObjectFlags.MarkerAttachment1)]
  [InlineData(2, StaticRenderObjectFlags.MarkerAttachment2)]
  [InlineData(3, StaticRenderObjectFlags.MarkerAttachment3)]
  [InlineData(4, StaticRenderObjectFlags.MarkerAttachment4)]
  public async Task NewModelImportInfersEmitterMarkerOwnership(
    int number,
    StaticRenderObjectFlags markerFlag
  )
  {
    var sourceAsset = await ReadAssetAsync(
      AttachmentAndCannonMshFixture.Create(
        new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
        {
          [number + 4] = new(256, -512, 768, 64, 0x80),
        }
      )
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join(
          "; ",
          imported.Diagnostics.Select(diagnostic =>
            $"{diagnostic.Code}:{diagnostic.Path}:{diagnostic.Message}:{string.Join(',', diagnostic.Data)}"
          )
        )
      );
    imported
      .Value.Should().BeOfType<StaticMeshAsset>()
      .Which.StaticRenderObjectSequence.Should()
      .ContainSingle()
      .Subject.KnownFlags.Should()
      .HaveFlag(markerFlag);
  }

  [Fact]
  public async Task NewModelEmitterOwnershipCrossesTransformOnlyGroupsAndCombinesMarkerRoles()
  {
    var vertices = new[]
    {
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
    };
    var renderObject = new CanonicalStaticRenderObject(vertices, [new CanonicalTriangle(0, 1, 2)]);
    var build = StaticMeshBuilder
      .Create(OneTriangleMshFixture.CreationGuid)
      .SetRootSourceObject(new CanonicalStaticSourceObject([renderObject, renderObject]))
      .Build();
    build.TryGetValue(out var builtAsset).Should().BeTrue();
    await using var msh = new MemoryStream();
    (await new MshWriter().WriteAsync(builtAsset!, msh))
      .Status.Should()
      .Be(OperationStatus.Succeeded);
    var sourceBytes = msh.ToArray();
    foreach (var number in new[] { 5, 6 })
    {
      var offset = 0x14 + AttachmentAndCannonMshFixture.AttachmentTableOffset + ((number - 1) * 8);
      BinaryPrimitives.WriteInt16LittleEndian(
        sourceBytes.AsSpan(offset),
        checked((short)(number * 256))
      );
      sourceBytes[offset + 7] = 0x80;
    }
    var sourceAsset = await ReadAssetAsync(sourceBytes);
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    metadataFree = RewriteJson(
      metadataFree,
      root =>
      {
        var nodes = root["nodes"]!.AsArray();
        var sourceIndex = nodes
          .Select((node, index) => (node, index))
          .Single(item => item.node!.AsObject().ContainsKey("mesh"))
          .index;
        var emitterIndices = nodes
          .Select((node, index) => (node, index))
          .Where(item => item.node!["name"]?.GetValue<string>() is "ET_Emitter_1" or "ET_Emitter_2")
          .Select(item => item.index)
          .ToArray();
        var sourceChildren = nodes[sourceIndex]!["children"]!.AsArray();
        for (var index = sourceChildren.Count - 1; index >= 0; index--)
        {
          if (emitterIndices.Contains(sourceChildren[index]!.GetValue<int>()))
          {
            sourceChildren.RemoveAt(index);
          }
        }
        var groupIndex = nodes.Count;
        nodes.Add(
          new JsonObject
          {
            ["translation"] = new JsonArray(2, 0, 0),
            ["children"] = new JsonArray(emitterIndices.Select(index => (JsonNode)index).ToArray()),
          }
        );
        sourceChildren.Add(groupIndex);
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    var importedAsset = StaticAsset(imported);
    importedAsset.StaticRenderObjectSequence.Should().HaveCount(2);
    var flags = importedAsset.StaticRenderObjectSequence[0].KnownFlags;
    flags.Should().HaveFlag(StaticRenderObjectFlags.MarkerAttachment1);
    flags.Should().HaveFlag(StaticRenderObjectFlags.MarkerAttachment2);
    importedAsset
      .StaticRenderObjectSequence[1]
      .KnownFlags.Should()
      .NotHaveFlag(StaticRenderObjectFlags.MarkerAttachment1)
      .And.NotHaveFlag(StaticRenderObjectFlags.MarkerAttachment2);
    BinaryPrimitives
      .ReadInt16LittleEndian(
        AttachmentAndCannonMshFixture.GetAttachment(
          importedAsset.GetSerializedRepresentation().ToArray(),
          5
        )
      )
      .Should()
      .Be(1792);
  }

  [Fact]
  public async Task NewModelImportRejectsEmitterWithoutSourceAncestorWithoutPartialAsset()
  {
    var sourceAsset = await ReadAssetAsync(
      AttachmentAndCannonMshFixture.Create(
        new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
        {
          [5] = new(256, -512, 768, 64, 0x80),
        }
      )
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var emitterPath = "";
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    metadataFree = RewriteJson(
      metadataFree,
      root =>
      {
        var nodes = root["nodes"]!.AsArray();
        var emitterIndex = nodes
          .Select((node, index) => (node, index))
          .Single(item => item.node!["name"]?.GetValue<string>() == "ET_Emitter_1")
          .index;
        emitterPath = $"nodes[{emitterIndex}]";
        foreach (var node in nodes.OfType<JsonObject>())
        {
          if (node["children"] is not JsonArray children)
          {
            continue;
          }
          for (var index = children.Count - 1; index >= 0; index--)
          {
            if (children[index]!.GetValue<int>() == emitterIndex)
            {
              children.RemoveAt(index);
            }
          }
          if (children.Count == 0)
          {
            node.Remove("children");
          }
        }
        var sourceRootIndex = root["scenes"]![0]!["nodes"]![0]!.GetValue<int>();
        var placementRootIndex = nodes.Count;
        nodes.Add(new JsonObject { ["children"] = new JsonArray(sourceRootIndex, emitterIndex) });
        root["scenes"]![0]!["nodes"] = new JsonArray(placementRootIndex);
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    var diagnostic = imported.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Path.Should().Be(emitterPath);
    diagnostic
      .Data.Should()
      .Contain(new KeyValuePair<string, string>("domain", "EmitterMarkerHierarchy"));
  }

  [Fact]
  public async Task MarkedEmitterDoesNotRequireTurretParent()
  {
    var asset = await ReadAssetAsync(
      AttachmentAndCannonMshFixture.Create(
        new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
        {
          [6] = new(256, -512, 768, 64, 0x80),
        },
        objectFlags: (uint)StaticRenderObjectFlags.MarkerAttachment2
      )
    );
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );

    export.Status.Should().Be(OperationStatus.Succeeded);
    export
      .Diagnostics.Should()
      .NotContain(diagnostic => diagnostic.Code == GltfDiagnosticCodes.EmitterHierarchyFallback);
    using var json = ReadGlbJson(glb.ToArray());
    var nodes = json.RootElement.GetProperty("nodes").EnumerateArray().ToArray();
    var emitterIndex = Array.FindIndex(
      nodes,
      node => node.GetProperty("name").GetString() == "ET_Emitter_2"
    );
    var parentIndex = FindParentIndex(nodes, emitterIndex);
    nodes[parentIndex].TryGetProperty("mesh", out _).Should().BeTrue();
  }

  [Fact]
  public async Task MarkerWithoutEmitterWarns()
  {
    var asset = await ReadAssetAsync(
      AttachmentAndCannonMshFixture.Create(
        objectFlags: (uint)StaticRenderObjectFlags.MarkerAttachment3
      )
    );
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );

    export.Status.Should().Be(OperationStatus.Succeeded);
    export
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.EmitterHierarchyFallback
        && diagnostic.Path == "CommonBaseHeader.AttachmentTable[7]"
        && diagnostic.Data["missing"] == "emitter"
      );
  }

  [Theory]
  [InlineData(0)]
  [InlineData(64)]
  [InlineData(128)]
  [InlineData(192)]
  [InlineData(255)]
  public void CanonicalAttachmentRotationPointsLocalPositiveYAlongHeading(byte heading)
  {
    var rotation = AttachmentHeadingProjection.CreateRotation(heading);
    var direction = Vector3.TransformNormal(
      Vector3.UnitY,
      Matrix4x4.CreateFromQuaternion(rotation)
    );
    var angle = heading * MathF.PI * 2 / 256;

    direction.X.Should().BeApproximately(MathF.Cos(angle), 1e-6f);
    direction.Y.Should().BeApproximately(0, 1e-6f);
    direction.Z.Should().BeApproximately(-MathF.Sin(angle), 1e-6f);
    AttachmentHeadingProjection.TryReadHeading(rotation, out var roundTripped).Should().BeTrue();
    roundTripped.Should().Be(heading);
  }

  [Fact]
  public async Task InactiveCannonHasNoHelperOrNonFinitePreviewWarning()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      cannonRenderPositions: new Dictionary<int, Vector3>
      {
        [2] = new(float.NaN, float.PositiveInfinity, float.NegativeInfinity),
      }
    );
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );

    export
      .Diagnostics.Should()
      .NotContain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.CannonRenderPositionPreviewSubstituted
      );
    using var json = ReadGlbJson(glb.ToArray());
    json.RootElement.GetProperty("nodes")
      .EnumerateArray()
      .Select(node => node.TryGetProperty("name", out var name) ? name.GetString() : null)
      .Should()
      .NotContain(GlbDocument.GetCannonHelperName(2));
  }

  [Fact]
  public async Task GenericNamedHelpersAuthorAttachmentsAndCannonPositions()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [2] = new(10, 20, 30, 64, 0xA5),
        [47] = new(256, 512, -768, 192, 0x80),
      },
      new Dictionary<int, Vector3> { [2] = new(1.25f, -2.5f, 3.75f) }
    );
    var asset = await ReadAssetAsync(sourceBytes);
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported);
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    imported
      .Diagnostics.Should()
      .NotContain(diagnostic => diagnostic.Code == GltfDiagnosticCodes.SceneLightIgnored);
    var resultBytes = StaticAsset(imported).GetSerializedRepresentation().ToArray();
    AttachmentAndCannonMshFixture
      .GetAttachment(resultBytes, 47)
      .Should()
      .Equal(AttachmentAndCannonMshFixture.GetAttachment(sourceBytes, 47));
    AttachmentAndCannonMshFixture
      .GetCannonRenderPosition(resultBytes, 2)
      .Should()
      .Equal(AttachmentAndCannonMshFixture.GetCannonRenderPosition(sourceBytes, 2));
    var cannonAttachment = AttachmentAndCannonMshFixture.GetAttachment(resultBytes, 2);
    BinaryPrimitives.ReadInt16LittleEndian(cannonAttachment).Should().Be(320);
    BinaryPrimitives.ReadInt16LittleEndian(cannonAttachment.AsSpan(2)).Should().Be(-640);
    BinaryPrimitives.ReadInt16LittleEndian(cannonAttachment.AsSpan(4)).Should().Be(960);
    cannonAttachment[6].Should().Be(64);
    cannonAttachment[7].Should().Be(0x80);
  }

  [Fact]
  public async Task NewModelHelperGroupsApplyTransformWhileUnknownLeavesAreWarnedAndIgnored()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [21] = new(256, 512, -768, 192, 0xA5),
      }
    );
    var asset = await ReadAssetAsync(sourceBytes);
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported);
    var unknownPaths = new List<string>();
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        var nodes = root["nodes"]!.AsArray();
        var rootIndex = nodes
          .Select((node, index) => (node, index))
          .Single(item => item.node!["mesh"] is not null)
          .index;
        var helperIndex = nodes
          .Select((node, index) => (node, index))
          .Single(item =>
            item.node!["name"]!.GetValue<string>() == GlbDocument.GetAttachmentHelperName(21)
          )
          .index;
        var rootChildren = nodes[rootIndex]!["children"]!.AsArray();
        var helperChildIndex = rootChildren
          .Select((child, index) => (child, index))
          .Single(item => item.child!.GetValue<int>() == helperIndex)
          .index;
        rootChildren.RemoveAt(helperChildIndex);
        var groupIndex = nodes.Count;
        nodes.Add(
          new JsonObject
          {
            ["name"] = "Artist helper group",
            ["translation"] = new JsonArray(1, 0, 0),
            ["children"] = new JsonArray(helperIndex),
          }
        );
        rootChildren.Add(groupIndex);
        foreach (var name in new[] { "Artist note", "ET_UnloadPoint_99", "et_unloadpoint_1" })
        {
          var unknownIndex = nodes.Count;
          nodes.Add(new JsonObject { ["name"] = name });
          rootChildren.Add(unknownIndex);
          unknownPaths.Add($"nodes[{unknownIndex}]");
        }
      }
    );

    await using var input = new MemoryStream(metadataFree);
    var import = await interchange.CreateMeshAsync(input);

    import
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    var result = StaticAsset(import).GetSerializedRepresentation().ToArray();
    var attachment = AttachmentAndCannonMshFixture.GetAttachment(result, 21);
    BinaryPrimitives.ReadInt16LittleEndian(attachment).Should().Be(512);
    BinaryPrimitives.ReadInt16LittleEndian(attachment.AsSpan(2)).Should().Be(512);
    BinaryPrimitives.ReadInt16LittleEndian(attachment.AsSpan(4)).Should().Be(-768);
    attachment[6].Should().Be(192);
    attachment[7].Should().Be(0x80);
    import
      .Diagnostics.Where(diagnostic => diagnostic.Code == GltfDiagnosticCodes.InertDataIgnored)
      .Select(diagnostic => diagnostic.Path)
      .Should()
      .BeEquivalentTo(unknownPaths);
  }

  [Theory]
  [InlineData(0, "EarthTool A")]
  [InlineData(1, "EarthTool B")]
  [InlineData(2, "EarthTool C")]
  [InlineData(3, "EarthTool D")]
  public async Task EffectiveAnimationClassesExportDenseNativeTrsAt24Fps(
    uint animationClassValue,
    string expectedName
  )
  {
    var lengths = animationClassValue switch
    {
      0 => new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      1 => new StaticAnimationMshFixture.AnimationLengths(0, 2, 0, 0),
      2 => new StaticAnimationMshFixture.AnimationLengths(0, 0, 2, 0),
      _ => new StaticAnimationMshFixture.AnimationLengths(0, 0, 0, 2),
    };
    var asset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        animationClassValue,
        lengths,
        [Vector3.One, new Vector3(2, 3, 4)],
        [new Vector3(1, 2, 3), new Vector3(4, 5, 6)],
        [Matrix4x4.Identity, Matrix4x4.CreateRotationZ(MathF.PI / 2)]
      )
    );
    await using var glb = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );

    result
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join(
          "; ",
          result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")
        )
      );
    using var json = ReadGlbJson(glb.ToArray());
    var animations = json.RootElement.GetProperty("animations");
    animations.GetArrayLength().Should().Be(1);
    animations[0].GetProperty("name").GetString().Should().Be(expectedName);
    animations[0]
      .GetProperty("channels")
      .EnumerateArray()
      .Select(channel => channel.GetProperty("target").GetProperty("path").GetString())
      .Should()
      .BeEquivalentTo(["translation", "rotation", "scale"]);
    var timeAccessor = animations[0].GetProperty("samplers")[0].GetProperty("input").GetInt32();
    ReadFloatAccessor(glb.ToArray(), json.RootElement, timeAccessor, 1).Should().Equal(0, 1f / 24f);
    var rotationChannel = animations[0]
      .GetProperty("channels")
      .EnumerateArray()
      .Single(channel =>
        channel.GetProperty("target").GetProperty("path").GetString() == "rotation"
      );
    var rotationSampler = animations[0].GetProperty("samplers")[
      rotationChannel.GetProperty("sampler").GetInt32()
    ];
    var rotations = ReadFloatAccessor(
      glb.ToArray(),
      json.RootElement,
      rotationSampler.GetProperty("output").GetInt32(),
      4
    );
    for (var frame = 0; frame < 2; frame++)
    {
      var rotation = new Quaternion(
        rotations[frame * 4],
        rotations[frame * 4 + 1],
        rotations[frame * 4 + 2],
        rotations[frame * 4 + 3]
      );
      rotation.Length().Should().BeApproximately(1, 1e-6f);
      rotation.W.Should().BeGreaterThanOrEqualTo(0);
    }
  }

  [Fact]
  public async Task AbsentTracksDoNotEmitAnEmptyAnimation()
  {
    var asset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        0,
        new StaticAnimationMshFixture.AnimationLengths(8, 0, 0, 0)
      )
    );
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );

    export.Status.Should().Be(OperationStatus.Succeeded);
    using var json = ReadGlbJson(glb.ToArray());
    json.RootElement.TryGetProperty("animations", out _).Should().BeFalse();
  }

  [Fact]
  public async Task UnrecognizedClassWithoutTracksRemainsWarningBearing()
  {
    var asset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        5,
        new StaticAnimationMshFixture.AnimationLengths(0, 8, 0, 0)
      )
    );
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );

    export.Status.Should().Be(OperationStatus.Succeeded);
    export
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.AnimationClassUnrecognized
      )
      .Which.Code.Should()
      .Be(GltfDiagnosticCodes.AnimationClassUnrecognized);
    using var json = ReadGlbJson(glb.ToArray());
    json.RootElement.TryGetProperty("animations", out _).Should().BeFalse();
  }

  [Fact]
  public async Task ZeroLengthPresentTrackProjectsOnlyEffectiveFrameZero()
  {
    var asset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(0, default, translations: [new Vector3(1, 2, 3)])
    );
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );

    export.Status.Should().Be(OperationStatus.Succeeded);
    using var json = ReadGlbJson(glb.ToArray());
    var animation = json.RootElement.GetProperty("animations")[0];
    var timeAccessor = animation.GetProperty("samplers")[0].GetProperty("input").GetInt32();
    ReadFloatAccessor(glb.ToArray(), json.RootElement, timeAccessor, 1).Should().Equal(0);
  }

  [Theory]
  [InlineData(0u)]
  [InlineData(5u)]
  public async Task TooShortPresentTrackIsRejectedBeforeGltfProjection(uint animationClassValue)
  {
    var source = StaticAnimationMshFixture.Create(
      animationClassValue,
      animationClassValue == 0
        ? new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0)
        : new StaticAnimationMshFixture.AnimationLengths(0, 2, 0, 0),
      translations: [Vector3.Zero]
    );
    await using var stream = new MemoryStream(source);

    var read = await new MshReader().ReadAsync(stream);

    read.Status.Should().Be(OperationStatus.Failed);
    read.Value.Should().BeNull();
    read.Diagnostics.Should()
      .ContainSingle()
      .Which.Should()
      .Match<OperationDiagnostic>(diagnostic =>
        diagnostic.Code == MshDiagnosticCodes.StructuralHazard
        && diagnostic.Path == "StaticRenderObjectSequence[0].AnimationTracks.TranslationFrames"
      );
  }

  [Fact]
  public async Task NondecomposableFrameSuppressesOnlyItsObjectAndClass()
  {
    var shear = Matrix4x4.Identity;
    shear.M21 = 0.5f;
    var asset = await ReadAssetAsync(
      StaticMeshSequenceFixture.CreateTwoAnimationClasses(shear).Data
    );
    var interchange = new GltfInterchange();
    var first = new MemoryStream();
    var second = new MemoryStream();

    var firstExport = await interchange.ExportGlbAsync(
      asset,
      first
    );
    var secondExport = await interchange.ExportGlbAsync(
      asset,
      second
    );

    firstExport.Status.Should().Be(OperationStatus.Succeeded);
    firstExport
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.SourceRepresentationNotPreserved
        && diagnostic.Path == "StaticRenderObjectSequence[1].AnimationTracks")
      .Which.Should()
      .Match<OperationDiagnostic>(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.SourceRepresentationNotPreserved
      );
    secondExport.Diagnostics.Should().BeEquivalentTo(firstExport.Diagnostics);
    second.ToArray().Should().Equal(first.ToArray());
    using var json = ReadGlbJson(first.ToArray());
    json.RootElement.GetProperty("animations")
      .EnumerateArray()
      .Select(animation => animation.GetProperty("name").GetString())
      .Should()
      .Equal("EarthTool A");
  }

  [Fact]
  public async Task StaticSourceObjectsAndMaterialPartitionsExportAsNativeHierarchy()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var asset = await ReadAssetAsync(fixture.Data);
    await using var glb = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(sourceBaseName: "EDBBPP")
    );

    result
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join(
          "; ",
          result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")
        )
      );
    using var json = ReadGlbJson(glb.ToArray());
    var root = json.RootElement;
    root.GetProperty("scenes")[0]
      .GetProperty("nodes")
      .EnumerateArray()
      .Select(node => node.GetInt32())
      .Should()
      .Equal(3);
    root.GetProperty("nodes").GetArrayLength().Should().Be(4);
    root.GetProperty("meshes").GetArrayLength().Should().Be(3);
    root.GetProperty("nodes")[3].GetProperty("name").GetString().Should().Be("EDBBPP");
    root.GetProperty("nodes")[3]
      .GetProperty("children")
      .EnumerateArray()
      .Select(node => node.GetInt32())
      .Should()
      .Equal(0);
    root.GetProperty("nodes")[3]
      .GetProperty("extras")
      .GetProperty("earthtoolPlacementRoot")
      .GetBoolean()
      .Should()
      .BeTrue();
    root.GetProperty("nodes")[0].GetProperty("name").GetString().Should().Be("ET_Static_1");
    root.GetProperty("nodes")[1].GetProperty("name").GetString().Should().Be("ET_Static_2");
    root.GetProperty("nodes")[2].GetProperty("name").GetString().Should().Be("ET_Static_3");
    root.GetProperty("meshes")[0].GetProperty("name").GetString().Should().Be("EDBBPP_1_Mesh");
    root.GetProperty("meshes")[1].GetProperty("name").GetString().Should().Be("EDBBPP_2_Mesh");
    root.GetProperty("meshes")[2].GetProperty("name").GetString().Should().Be("EDBBPP_3_Mesh");
    root.GetProperty("nodes")[0]
      .GetProperty("children")
      .EnumerateArray()
      .Select(node => node.GetInt32())
      .Take(2)
      .Should()
      .Equal(1, 2);
    root.GetProperty("nodes")[0].TryGetProperty("translation", out _).Should().BeFalse();
    root.GetProperty("nodes")[1]
      .GetProperty("translation")
      .EnumerateArray()
      .Select(value => value.GetSingle())
      .Should()
      .Equal(1, 3, -2);
    root.GetProperty("nodes")[2]
      .GetProperty("translation")
      .EnumerateArray()
      .Select(value => value.GetSingle())
      .Should()
      .Equal(7, 9, -8);
    root.GetProperty("meshes")[0].GetProperty("primitives").GetArrayLength().Should().Be(2);
    root.GetProperty("meshes")[1].GetProperty("primitives").GetArrayLength().Should().Be(1);
    root.GetProperty("meshes")[2].GetProperty("primitives").GetArrayLength().Should().Be(1);
  }

  [Fact]
  public async Task ExplicitTexRootProducesDeterministicEmbeddedUnlitPngPreview()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "root-a.tex"),
      CreateRgbaTex(2, 1, [0xFF, 0, 0, 0xFF, 0, 0, 0xFF, 0xFF])
    );
    try
    {
      var options = new GltfExportOptions([directory]);
      var interchange = new GltfInterchange();
      await using var first = new MemoryStream();
      await using var second = new MemoryStream();

      var firstResult = await interchange.ExportGlbAsync(asset, first, options);
      var secondResult = await interchange.ExportGlbAsync(asset, second, options);

      firstResult
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", firstResult.Diagnostics.Select(diagnostic => diagnostic.Message))
        );
      secondResult.Status.Should().Be(OperationStatus.Succeeded);
      second.ToArray().Should().Equal(first.ToArray());
      firstResult
        .Diagnostics.Should()
        .Contain(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing
          && diagnostic.Severity == DiagnosticSeverity.Warning
        );
      using var json = ReadGlbJson(first.ToArray());
      var root = json.RootElement;
      root.GetProperty("images").GetArrayLength().Should().Be(2);
      root.GetProperty("images")[0].GetProperty("mimeType").GetString().Should().Be("image/png");
      root.GetProperty("textures").GetArrayLength().Should().Be(2);
      root.GetProperty("materials")[0]
        .GetProperty("pbrMetallicRoughness")
        .GetProperty("baseColorTexture")
        .GetProperty("index")
        .GetInt32()
        .Should()
        .Be(0);
      await using var withoutPreview = new MemoryStream();
      await interchange.ExportGlbAsync(asset, withoutPreview);
      var imageBufferView = root.GetProperty("images")[0].GetProperty("bufferView").GetInt32();
      var pngLength = root.GetProperty("bufferViews")[imageBufferView]
        .GetProperty("byteLength")
        .GetInt32();
      await using var constrained = new MemoryStream();
      var constrainedResult = await interchange.ExportGlbAsync(
        asset,
        constrained,
        options,
        new GltfOperationProfile(
          maxInputBytes: 32 * 1024 * 1024,
          maxOutputBytes: withoutPreview.ToArray().Length + pngLength,
          maxMetadataBytes: 4 * 1024 * 1024,
          maxJsonDepth: 32,
          maxActiveRenderVertices: 65536,
          maxNodes: 4096,
          maxHierarchyDepth: 15,
          maxTextureBytes: 1024,
          maxPreviewPixels: 16
        )
      );
      constrainedResult.Status.Should().Be(OperationStatus.Succeeded);
      constrainedResult
        .Diagnostics.Should()
        .Contain(diagnostic => diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable);
      constrainedResult
        .Diagnostics.Should()
        .NotContain(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed
          || diagnostic.Code == GltfDiagnosticCodes.TextureDiagnosticPreviewUsed
          || diagnostic.Code == GltfDiagnosticCodes.TextureVariantsNotRepresented
        );
      using var constrainedJson = ReadGlbJson(constrained.ToArray());
      constrainedJson.RootElement.TryGetProperty("images", out _).Should().BeFalse();
      var metadataFreePreview = RewriteJson(
        first.ToArray(),
        root => RemoveMetadataAndSetMisleadingTexturePresentation(root)
      );
      await using var genericSource = new MemoryStream(metadataFreePreview);
      var genericImport = await new GltfInterchange().CreateMeshAsync(genericSource);
      genericImport.Status.Should().Be(OperationStatus.Failed);
      genericImport.Value.Should().BeNull();
      var diagnostic = genericImport.Diagnostics.Should().ContainSingle().Subject;
      diagnostic.Code.Should().Be(GltfDiagnosticCodes.TextureResourceBindingRequired);
      diagnostic.EventId.Should().Be(1121);
      diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
      diagnostic.Path.Should().Be("materials[0]");
      diagnostic.Message.Should().Contain("textureResourceBindings");
      diagnostic.Data.Should().Contain(new KeyValuePair<string, string>("materialHandle", "1"));
      var bindings = Enumerable
        .Range(1, asset.StaticRenderObjectSequence.Count)
        .ToDictionary(
          index => new GltfMaterialHandle(index),
          index => (string?)$"Textures\\authored\\material-{index}.tex"
        );
      await using var typedSource = new MemoryStream(metadataFreePreview);

      var typedImport = await interchange.CreateMeshAsync(
        typedSource,
        new GltfNewModelImportOptions(bindings)
      );

      typedImport
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", typedImport.Diagnostics.Select(item => item.Message))
        );
      typedImport
        .Value.Should().BeOfType<StaticMeshAsset>()
        .Which.StaticRenderObjectSequence[0]
        .TexturePathBytes.Should()
        .Equal("Textures\\authored\\material-1.tex"u8.ToArray());
      var path = Path.Combine(directory, "preview.glb");
      await File.WriteAllBytesAsync(path, first.ToArray());
      await AssertKhronosValidAsync(path);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task PreviewPixelLimitKeepsTheExplicitBindingAsAReferenceOnlyMaterial()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "root-a.tex"),
      CreateRgbaTex(2, 1, [0xFF, 0, 0, 0xFF, 0, 0, 0xFF, 0xFF])
    );
    try
    {
      await using var glb = new MemoryStream();
      var result = await new GltfInterchange().ExportGlbAsync(
        asset,
        glb,
        new GltfExportOptions([directory]),
        new GltfOperationProfile(
          maxInputBytes: 32 * 1024 * 1024,
          maxOutputBytes: 32 * 1024 * 1024,
          maxMetadataBytes: 4 * 1024 * 1024,
          maxJsonDepth: 32,
          maxActiveRenderVertices: 65536,
          maxNodes: 4096,
          maxHierarchyDepth: 15,
          maxTextureBytes: 1024,
          maxPreviewPixels: 1
        )
      );

      result.Status.Should().Be(OperationStatus.Succeeded);
      result
        .Diagnostics.Should()
        .Contain(diagnostic => diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable);
      using var json = ReadGlbJson(glb.ToArray());
      json.RootElement.TryGetProperty("images", out _).Should().BeFalse();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task OmittedLateDefaultPreviewDoesNotReportThatTheFallbackWasUsed()
  {
    var twoMaterialAsset = CreateTwoPartitionAsset(
      "Textures\\root-a.tex",
      "Textures\\barrel.tex"
    );
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var options = new GltfExportOptions([directory]);
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "root-a.tex"),
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF])
    );
    await File.WriteAllBytesAsync(Path.Combine(textureDirectory, "Default.tex"), [1, 2, 3]);

    try
    {
      var interchange = new GltfInterchange();
      await using var firstOnly = new MemoryStream();
      await interchange.ExportGlbAsync(
        twoMaterialAsset,
        firstOnly,
        options,
        new GltfOperationProfile(
          32 * 1024 * 1024,
          32 * 1024 * 1024,
          4 * 1024 * 1024,
          32,
          65536,
          4096,
          15,
          16 * 1024 * 1024,
          1
        )
      );
      var defaultPixels = new byte[64 * 64 * 4];
      new Random(145).NextBytes(defaultPixels);
      await File.WriteAllBytesAsync(
        Path.Combine(textureDirectory, "Default.tex"),
        CreateRgbaTex(64, 64, defaultPixels)
      );
      await using var constrained = new MemoryStream();

      var result = await interchange.ExportGlbAsync(
        twoMaterialAsset,
        constrained,
        options,
        new GltfOperationProfile(
          32 * 1024 * 1024,
          checked((int)firstOnly.Length),
          4 * 1024 * 1024,
          32,
          65536,
          4096,
          15,
          16 * 1024 * 1024,
          64 * 64 + 1
        )
      );

      result.Status.Should().Be(OperationStatus.Succeeded);
      result
        .Diagnostics.Should()
        .Contain(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable
          && diagnostic.Path == "StaticRenderObjectSequence[1].TexturePathBytes"
        );
      result
        .Diagnostics.Should()
        .NotContain(diagnostic => diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed);
      using var json = ReadGlbJson(constrained.ToArray());
      json.RootElement.GetProperty("images").GetArrayLength().Should().Be(1);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateGltfUsesSharedContentAddressedPngSidecarsDeterministically()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var firstPath = Path.Combine(directory, "first.gltf");
    var secondPath = Path.Combine(directory, "second.gltf");
    var pixels = new byte[] { 0xFF, 0, 0, 0xFF, 0, 0, 0xFF, 0xFF };
    Directory.CreateDirectory(textureDirectory);
    foreach (var name in new[] { "root-a.tex", "barrel.tex", "root-b.tex", "rotor.tex" })
    {
      await File.WriteAllBytesAsync(
        Path.Combine(textureDirectory, name),
        CreateRgbaTex(2, 1, pixels)
      );
    }

    try
    {
      var options = new GltfExportOptions([directory]);
      var interchange = new GltfInterchange();

      var first = await interchange.ExportGltfFileAsync(asset, firstPath, options);
      var second = await interchange.ExportGltfFileAsync(asset, secondPath, options);

      first
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", first.Diagnostics.Select(diagnostic => diagnostic.Message))
        );
      second.Status.Should().Be(OperationStatus.Succeeded);
      (await File.ReadAllBytesAsync(secondPath))
        .Should()
        .Equal(await File.ReadAllBytesAsync(firstPath));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(firstPath));
      var root = json.RootElement;
      var image = root.GetProperty("images").EnumerateArray().Should().ContainSingle().Subject;
      image.TryGetProperty("bufferView", out _).Should().BeFalse();
      var expectedImageName = GetPreviewContentAddress(2, 1, pixels) + ".png";
      image.GetProperty("uri").GetString().Should().Be(expectedImageName);
      File.Exists(Path.Combine(directory, expectedImageName)).Should().BeTrue();
      root.GetProperty("textures")
        .EnumerateArray()
        .Select(texture => texture.GetProperty("source").GetInt32())
        .Should()
        .Equal(0);
      root.GetProperty("materials")
        .EnumerateArray()
        .Select(material =>
          material
            .GetProperty("pbrMetallicRoughness")
            .GetProperty("baseColorTexture")
            .GetProperty("index")
            .GetInt32()
        )
        .Should()
        .OnlyContain(index => index == 0);
      Directory.EnumerateFiles(directory, "*.png").Should().ContainSingle();
      Directory.EnumerateFiles(directory, "*.bin").Should().ContainSingle();

      var validation = await interchange.ValidateGltfFileAsync(firstPath);
      validation.Status.Should().Be(OperationStatus.Succeeded);
      await AssertKhronosValidAsync(firstPath);

      var inferredUri = "Textures-presentation-only.tex.png";
      File.Move(Path.Combine(directory, expectedImageName), Path.Combine(directory, inferredUri));
      var metadataFree = JsonNode.Parse(await File.ReadAllTextAsync(firstPath))!.AsObject();
      RemoveMetadataAndSetMisleadingTexturePresentation(metadataFree, inferredUri);
      await File.WriteAllTextAsync(firstPath, metadataFree.ToJsonString());

      var inferred = await interchange.CreateMeshFileAsync(firstPath);

      inferred.Status.Should().Be(OperationStatus.Failed);
      inferred.Value.Should().BeNull();
      var diagnostic = inferred.Diagnostics.Should().ContainSingle().Subject;
      diagnostic.Code.Should().Be(GltfDiagnosticCodes.TextureResourceBindingRequired);
      diagnostic.EventId.Should().Be(1121);
      diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
      diagnostic.Path.Should().Be("materials[0]");
      diagnostic.Data.Should().Contain(new KeyValuePair<string, string>("materialHandle", "1"));
      var bindings = Enumerable
        .Range(1, asset.StaticRenderObjectSequence.Count)
        .ToDictionary(
          index => new GltfMaterialHandle(index),
          index => (string?)$"Textures\\authored\\material-{index}.tex"
        );

      var typed = await interchange.CreateMeshFileAsync(
        firstPath,
        new GltfNewModelImportOptions(bindings)
      );

      typed
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", typed.Diagnostics.Select(item => item.Message))
        );
      typed
        .Value.Should().BeOfType<StaticMeshAsset>()
        .Which.StaticRenderObjectSequence[0]
        .TexturePathBytes.Should()
        .Equal("Textures\\authored\\material-1.tex"u8.ToArray());
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task OrderedRootsWarnAboutShadowingAndMissingResourcesUseDefaultPreview()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var firstRoot = Path.Combine(directory, "first");
    var secondRoot = Path.Combine(directory, "second");
    var output = Path.Combine(directory, "model.gltf");
    var selectedPixels = new byte[] { 0xFF, 0, 0, 0xFF };
    var shadowedPixels = new byte[] { 0, 0, 0xFF, 0xFF };
    var defaultPixels = new byte[] { 0xFF, 0, 0xFF, 0xFF };
    Directory.CreateDirectory(Path.Combine(firstRoot, "Textures"));
    Directory.CreateDirectory(Path.Combine(secondRoot, "Textures"));
    await File.WriteAllBytesAsync(
      Path.Combine(firstRoot, "Textures", "root-a.tex"),
      CreateRgbaTex(1, 1, selectedPixels)
    );
    await File.WriteAllBytesAsync(
      Path.Combine(secondRoot, "Textures", "root-a.tex"),
      CreateRgbaTex(1, 1, shadowedPixels)
    );
    await File.WriteAllBytesAsync(
      Path.Combine(firstRoot, "Textures", "Default.tex"),
      CreateRgbaTex(1, 1, defaultPixels)
    );

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        output,
        new GltfExportOptions([firstRoot, secondRoot])
      );

      result
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message))
        );
      result
        .Diagnostics.Should()
        .ContainSingle(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TextureResourceShadowed
        );
      result
        .Diagnostics.Count(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing
        )
        .Should()
        .Be(3);
      result
        .Diagnostics.Count(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed
        )
        .Should()
        .Be(3);
      result
        .Diagnostics.Where(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed
        )
        .Should()
        .OnlyContain(diagnostic =>
          diagnostic.EventId == 1111
          && diagnostic.Severity == DiagnosticSeverity.Warning
          && diagnostic.Path.EndsWith(".TexturePathBytes", StringComparison.Ordinal)
        );
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(output));
      var imageUris = json
        .RootElement.GetProperty("images")
        .EnumerateArray()
        .Select(image => image.GetProperty("uri").GetString())
        .ToArray();
      imageUris
        .Should()
        .BeEquivalentTo(
          GetPreviewContentAddress(1, 1, selectedPixels) + ".png",
          GetPreviewContentAddress(1, 1, defaultPixels) + ".png"
        );
      imageUris.Should().NotContain(GetPreviewContentAddress(1, 1, shadowedPixels) + ".png");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparatePackageCollisionPreflightPreservesManifestAndWritesNothing()
  {
    var boundAsset = CreateTextureBoundAsset("Textures\\preview.tex");
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var referenceDirectory = Path.Combine(directory, "reference");
    var collisionDirectory = Path.Combine(directory, "collision");
    var textureDirectory = Path.Combine(directory, "Textures");
    var referencePath = Path.Combine(referenceDirectory, "model.gltf");
    var destinationPath = Path.Combine(collisionDirectory, "model.gltf");
    var originalManifest = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(referenceDirectory);
    Directory.CreateDirectory(collisionDirectory);
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "preview.tex"),
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF])
    );

    try
    {
      var options = new GltfExportOptions([directory]);
      var interchange = new GltfInterchange();
      var reference = await interchange.ExportGltfFileAsync(boundAsset, referencePath, options);
      reference
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", reference.Diagnostics.Select(diagnostic => diagnostic.Message))
        );
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(referencePath));
      var imageName = json.RootElement.GetProperty("images")[0].GetProperty("uri").GetString()!;
      await File.WriteAllBytesAsync(destinationPath, originalManifest);
      await File.WriteAllBytesAsync(Path.Combine(collisionDirectory, imageName), [1, 2, 3]);

      var result = await interchange.ExportGltfFileAsync(boundAsset, destinationPath, options);

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(destinationPath)).Should().Equal(originalManifest);
      (await File.ReadAllBytesAsync(Path.Combine(collisionDirectory, imageName)))
        .Should()
        .Equal(1, 2, 3);
      Directory.EnumerateFiles(collisionDirectory, "*.bin").Should().BeEmpty();
      Directory
        .EnumerateFiles(collisionDirectory)
        .Should()
        .NotContain(path => path.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparatePackagePreflightsDirectoryCollisionsBeforeWritingAnySidecar()
  {
    var boundAsset = CreateTextureBoundAsset("Textures\\preview.tex");
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var referenceDirectory = Path.Combine(directory, "reference");
    var collisionDirectory = Path.Combine(directory, "collision");
    var textureDirectory = Path.Combine(directory, "Textures");
    var referencePath = Path.Combine(referenceDirectory, "model.gltf");
    var destinationPath = Path.Combine(collisionDirectory, "model.gltf");
    var originalManifest = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(referenceDirectory);
    Directory.CreateDirectory(collisionDirectory);
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "preview.tex"),
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF])
    );

    try
    {
      var options = new GltfExportOptions([directory]);
      var interchange = new GltfInterchange();
      (await interchange.ExportGltfFileAsync(boundAsset, referencePath, options))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(referencePath));
      var sidecarNames = new[]
      {
        json.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!,
        json.RootElement.GetProperty("images")[0].GetProperty("uri").GetString()!,
      }
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToArray();
      await File.WriteAllBytesAsync(destinationPath, originalManifest);
      Directory.CreateDirectory(Path.Combine(collisionDirectory, sidecarNames[1]));

      var result = await interchange.ExportGltfFileAsync(boundAsset, destinationPath, options);

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(destinationPath)).Should().Equal(originalManifest);
      Directory.EnumerateFiles(collisionDirectory).Should().ContainSingle();
      Directory
        .EnumerateFiles(collisionDirectory)
        .Should()
        .NotContain(path => path.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SpecialTexUsesItsFirstImageAndWarnsThatVariantsAreNotRepresented()
  {
    var boundAsset = CreateTextureBoundAsset("Textures\\special.tex");
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var output = Path.Combine(directory, "model.gltf");
    var pixels = new byte[] { 0x20, 0x40, 0x60, 0x80 };
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "special.tex"),
      CreateContainerTex(1, 1, pixels)
    );

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        boundAsset,
        output,
        new GltfExportOptions([directory])
      );

      result
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message))
        );
      result
        .Diagnostics.Should()
        .ContainSingle(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TextureVariantsNotRepresented
          && diagnostic.EventId == 1113
          && diagnostic.Severity == DiagnosticSeverity.Warning
          && diagnostic.Path == "StaticRenderObjectSequence[0].TexturePathBytes"
        );
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(output));
      json.RootElement.GetProperty("images")[0]
        .GetProperty("uri")
        .GetString()
        .Should()
        .Be(GetPreviewContentAddress(1, 1, pixels) + ".png");
      (await new GltfInterchange().ValidateGltfFileAsync(output))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task CaseAmbiguityInWinningRootBlocksWithoutWritingAPackage()
  {
    if (OperatingSystem.IsWindows())
    {
      return;
    }

    var boundAsset = CreateTextureBoundAsset("Textures\\preview.tex");
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var output = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "preview.tex"),
      CreateRgbaTex(1, 1, [1, 2, 3, 4])
    );
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "PREVIEW.TEX"),
      CreateRgbaTex(1, 1, [5, 6, 7, 8])
    );
    await File.WriteAllBytesAsync(output, original);

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        boundAsset,
        output,
        new GltfExportOptions([directory])
      );

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.AmbiguousTextureResource);
      (await File.ReadAllBytesAsync(output)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SymlinkEscapingTheSearchRootIsNotReadAndUsesTheContainedDefault()
  {
    if (OperatingSystem.IsWindows())
    {
      return;
    }

    var boundAsset = CreateTextureBoundAsset("Textures\\preview.tex");
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var root = Path.Combine(directory, "root");
    var textureDirectory = Path.Combine(root, "Textures");
    var outside = Path.Combine(directory, "outside.tex");
    var output = Path.Combine(directory, "model.gltf");
    var outsidePixels = new byte[] { 0xFF, 0, 0, 0xFF };
    var defaultPixels = new byte[] { 0, 0xFF, 0, 0xFF };
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(outside, CreateRgbaTex(1, 1, outsidePixels));
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "Default.tex"),
      CreateRgbaTex(1, 1, defaultPixels)
    );
    File.CreateSymbolicLink(Path.Combine(textureDirectory, "preview.tex"), outside);

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        boundAsset,
        output,
        new GltfExportOptions([root])
      );

      result.Status.Should().Be(OperationStatus.Succeeded);
      result
        .Diagnostics.Should()
        .Contain(diagnostic => diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing);
      result
        .Diagnostics.Should()
        .Contain(diagnostic => diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed);
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(output));
      var imageUri = json.RootElement.GetProperty("images")[0].GetProperty("uri").GetString();
      imageUri.Should().Be(GetPreviewContentAddress(1, 1, defaultPixels) + ".png");
      imageUri.Should().NotBe(GetPreviewContentAddress(1, 1, outsidePixels) + ".png");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task TextureRootLimitFailsBeforeWritingDestination()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var firstRoot = Path.Combine(directory, "first");
    var secondRoot = Path.Combine(directory, "second");
    var output = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(firstRoot);
    Directory.CreateDirectory(secondRoot);
    await File.WriteAllBytesAsync(output, original);
    var profile = new GltfOperationProfile(
      32 * 1024 * 1024,
      32 * 1024 * 1024,
      4 * 1024 * 1024,
      32,
      65536,
      4096,
      15,
      16 * 1024 * 1024,
      16 * 1024 * 1024,
      1,
      64
    );

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        output,
        new GltfExportOptions([firstRoot, secondRoot]),
        profile
      );

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
      (await File.ReadAllBytesAsync(output)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task TextureDirectoryEntryLimitFailsBeforeWritingDestination()
  {
    var boundAsset = CreateTextureBoundAsset("Textures\\preview.tex");
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var output = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "preview.tex"),
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF])
    );
    await File.WriteAllBytesAsync(Path.Combine(textureDirectory, "other.tex"), [1, 2, 3]);
    await File.WriteAllBytesAsync(output, original);
    var profile = new GltfOperationProfile(
      32 * 1024 * 1024,
      32 * 1024 * 1024,
      4 * 1024 * 1024,
      32,
      65536,
      4096,
      15,
      16 * 1024 * 1024,
      16 * 1024 * 1024,
      64,
      1
    );

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        boundAsset,
        output,
        new GltfExportOptions([directory]),
        profile
      );

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
      (await File.ReadAllBytesAsync(output)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task NewModelImportAuthorsUniquelyNamedAnimationClassAsCanonicalDenseTracks()
  {
    var sourceAsset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        2,
        new StaticAnimationMshFixture.AnimationLengths(0, 0, 2, 0),
        scales: [Vector3.One, new Vector3(2, 3, 4)],
        translations: [Vector3.Zero, new Vector3(5, 6, 7)],
        matrices: [Matrix4x4.Identity, Matrix4x4.CreateRotationZ(0.5f)]
      )
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    var asset = StaticAsset(imported);
    asset.CommonBaseHeader.AnimationLengths.Should().Be(new AnimationClassBytes(0, 0, 2, 0));
    var record = asset.StaticRenderObjectSequence[0];
    record.AnimationClassValue.Should().Be(2);
    record.AnimationTracks.ScaleFrames.Should().Equal(Vector3.One, new Vector3(2, 3, 4));
    record.AnimationTracks.TranslationFrames.Should().Equal(Vector3.Zero, new Vector3(5, 6, 7));
    record.AnimationTracks.Matrices.Should().HaveCount(2);
    record.AnimationTracks.Matrices[0].Should().Be(Matrix4x4.Identity);
    var expectedRotation = Matrix4x4.CreateRotationZ(0.5f);
    record.AnimationTracks.Matrices[1].M11.Should().BeApproximately(expectedRotation.M11, 1e-6f);
    record.AnimationTracks.Matrices[1].M12.Should().BeApproximately(expectedRotation.M12, 1e-6f);
    record.AnimationTracks.Matrices[1].M21.Should().BeApproximately(expectedRotation.M21, 1e-6f);
    record.AnimationTracks.Matrices[1].M22.Should().BeApproximately(expectedRotation.M22, 1e-6f);
  }

  [Theory]
  [InlineData("Artist Walk Cycle")]
  [InlineData("EarthTool E")]
  [InlineData("earthtool A")]
  public async Task NewModelImportIgnoresNoncanonicalAnimationsWithWarning(string name)
  {
    var sourceAsset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        2,
        new StaticAnimationMshFixture.AnimationLengths(0, 0, 2, 0),
        translations: [Vector3.Zero, Vector3.One]
      )
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["animations"]![0]!["name"] = name;
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Succeeded);
    imported
      .Value.Should().BeOfType<StaticMeshAsset>()
      .Which.CommonBaseHeader.AnimationLengths.Should()
      .Be(default(AnimationClassBytes));
    StaticAsset(imported)
      .StaticRenderObjectSequence[0]
      .AnimationTracks.ScaleFrames.Should()
      .BeEmpty();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InertDataIgnored
        && diagnostic.Path == "animations[0]"
      );
  }

  [Fact]
  public async Task NewModelImportUsesNodeRestTransformForUnanimatedTrsPaths()
  {
    var sourceAsset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        3,
        new StaticAnimationMshFixture.AnimationLengths(0, 0, 0, 2),
        translations: [Vector3.Zero, new Vector3(3, 0, 0)]
      )
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["nodes"]![0]!["scale"] = new JsonArray(2, 3, 4);
        var channels = root["animations"]![0]!["channels"]!.AsArray();
        for (var index = channels.Count - 1; index >= 0; index--)
        {
          if (channels[index]!["target"]!["path"]!.GetValue<string>() != "translation")
          {
            channels.RemoveAt(index);
          }
        }
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    var importedAsset = StaticAsset(imported);
    var tracks = importedAsset.StaticRenderObjectSequence[0].AnimationTracks;
    tracks.TranslationFrames.Should().Equal(Vector3.Zero, new Vector3(3, 0, 0));
    tracks.ScaleFrames.Should().Equal(new Vector3(2, 4, 3), new Vector3(2, 4, 3));
    tracks.Matrices.Should().Equal(Matrix4x4.Identity, Matrix4x4.Identity);
    importedAsset
      .StaticRenderObjectSequence[0]
      .RenderVertices.Select(vertex => vertex.Position)
      .Should()
      .Equal(
        sourceAsset.StaticRenderObjectSequence[0].RenderVertices.Select(vertex => vertex.Position)
      );
  }

  [Fact]
  public async Task NewModelImportAccumulatesAnimatedTransformOnlyParentOntoMeshTracks()
  {
    var sourceAsset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        0,
        new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
        translations: [Vector3.Zero, new Vector3(2, 0, 0)]
      )
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        RemoveArtistHelperNodes(root);
        var nodes = root["nodes"]!.AsArray();
        nodes.Insert(0, new JsonObject { ["children"] = new JsonArray(1) });
        root["scenes"]![0]!["nodes"] = new JsonArray(0);
        foreach (var channel in root["animations"]![0]!["channels"]!.AsArray())
        {
          channel!["target"]!["node"] = 0;
        }
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    imported
      .Value.Should().BeOfType<StaticMeshAsset>()
      .Which.StaticRenderObjectSequence[0]
      .AnimationTracks.TranslationFrames.Should()
      .Equal(Vector3.Zero, new Vector3(2, 0, 0));
  }

  [Fact]
  public async Task NewModelImportRejectsAccumulatedAnimationWithUnsupportedMatrixComponents()
  {
    var sourceAsset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        0,
        new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
        translations: [Vector3.Zero, Vector3.UnitX]
      )
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        RemoveArtistHelperNodes(root);
        var nodes = root["nodes"]!.AsArray();
        nodes[0]!["rotation"] = new JsonArray(0, 0, MathF.Sin(0.25f), MathF.Cos(0.25f));
        nodes.Insert(
          0,
          new JsonObject { ["scale"] = new JsonArray(2, 1, 1), ["children"] = new JsonArray(1) }
        );
        root["scenes"]![0]!["nodes"] = new JsonArray(0);
        var channels = root["animations"]![0]!["channels"]!.AsArray();
        for (var index = channels.Count - 1; index >= 0; index--)
        {
          if (channels[index]!["target"]!["path"]!.GetValue<string>() == "scale")
          {
            channels.RemoveAt(index);
          }
          else
          {
            channels[index]!["target"]!["node"] = 0;
          }
        }
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Data.Should()
      .Contain(new KeyValuePair<string, string>("domain", "animations"));
  }

  [Fact]
  public async Task NewModelAnimationBytesParticipateInOutputLimitPreflight()
  {
    var sourceAsset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        0,
        new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
        translations: [Vector3.Zero, Vector3.UnitX]
      )
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);
    var maximum = OneTriangleMshFixture.Create().Length + 100;

    var imported = await interchange.CreateMeshAsync(
      source,
      profile: new GltfOperationProfile(maxOutputBytes: maximum)
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    var diagnostic = imported.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(GltfDiagnosticCodes.ResourceLimitExceeded);
    int.Parse(diagnostic.Data["actual"]).Should().BeGreaterThan(maximum);
  }

  [Theory]
  [InlineData("duplicate-class")]
  [InlineData("multiple-classes")]
  [InlineData("fractional-end")]
  [InlineData("frame-255")]
  public async Task NewModelImportRejectsAmbiguousOrOutOfRangeAnimationClasses(string mutation)
  {
    var sourceAsset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        0,
        new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
        translations: [Vector3.Zero, Vector3.One]
      )
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        var animations = root["animations"]!.AsArray();
        if (mutation is "duplicate-class" or "multiple-classes")
        {
          var copy = animations[0]!.DeepClone();
          if (mutation == "multiple-classes")
          {
            copy!["name"] = "EarthTool B";
          }
          animations.Add(copy);
        }
        else
        {
          var timeAccessor = animations[0]!["samplers"]![0]!["input"]!.GetValue<int>();
          var endTime = mutation == "fractional-end" ? 1.5f / 24f : 255f / 24f;
          root["accessors"]![timeAccessor]!["max"] = new JsonArray(endTime);
        }
      }
    );
    if (mutation is "fractional-end" or "frame-255")
    {
      using var json = ReadGlbJson(metadataFree);
      var animation = json.RootElement.GetProperty("animations")[0];
      var timeAccessor = animation.GetProperty("samplers")[0].GetProperty("input").GetInt32();
      var endTime = mutation == "fractional-end" ? 1.5f / 24f : 255f / 24f;
      BinaryPrimitives.WriteInt32LittleEndian(
        metadataFree.AsSpan(
          GetFloatAccessorOffset(metadataFree, json.RootElement, timeAccessor) + sizeof(float)
        ),
        BitConverter.SingleToInt32Bits(endTime)
      );
    }
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Data.Should()
      .Contain(new KeyValuePair<string, string>("domain", "animations"));
  }

  [Fact]
  public async Task NewModelImportAcceptsOnlyExplicitCanonicalTexResourceBindings()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var canonicalSource = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(
      canonicalSource,
      options: new GltfNewModelImportOptions(
        new Dictionary<GltfMaterialHandle, string?>
        {
          [new GltfMaterialHandle(1)] = "Textures\\authored\\hull.tex",
        }
      )
    );

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing
        && diagnostic.EventId == 1107
        && diagnostic.Path == "materials[0]"
      );
    imported
      .Value.Should().BeOfType<StaticMeshAsset>()
      .Which.StaticRenderObjectSequence.Should()
      .ContainSingle()
      .Subject.TexturePathBytes.Should()
      .Equal("Textures\\authored\\hull.tex"u8.ToArray());

    await using var unsafeSource = new MemoryStream(metadataFree);
    var rejected = await interchange.CreateMeshAsync(
      unsafeSource,
      options: new GltfNewModelImportOptions(
        new Dictionary<GltfMaterialHandle, string?>
        {
          [new GltfMaterialHandle(1)] = "..\\outside.tex",
        }
      )
    );
    rejected.Status.Should().Be(OperationStatus.Failed);
    rejected.Value.Should().BeNull();
    rejected
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Data.Should()
      .Contain(new KeyValuePair<string, string>("domain", "TexResourceBinding"));
  }

  [Fact]
  public async Task GlbAndSeparateGltfUseSharedMaterialAssignmentAsTypedTexAuthority()
  {
    var sourceAsset = CreateTwoPartitionAsset();
    var interchange = new GltfInterchange();
    var options = new GltfNewModelImportOptions(
      new Dictionary<GltfMaterialHandle, string?>
      {
        [new GltfMaterialHandle(1)] = "Textures\\authored\\shared.tex",
      }
    );
    await using var exportedGlb = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exportedGlb
    );
    var metadataFreeGlb = RewriteJson(exportedGlb.ToArray(), ShareSecondMaterial);
    await using var glbSource = new MemoryStream(metadataFreeGlb);

    var glbImport = await interchange.CreateMeshAsync(glbSource, options);

    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "shared.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var export = await interchange.ExportGltfFileAsync(
        sourceAsset,
        path
      );
      export.Status.Should().Be(OperationStatus.Succeeded);
      var metadataFreeGltf = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      ShareSecondMaterial(metadataFreeGltf);
      await File.WriteAllTextAsync(path, metadataFreeGltf.ToJsonString());

      var separateImport = await interchange.CreateMeshFileAsync(path, options);

      glbImport
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", glbImport.Diagnostics.Select(diagnostic => diagnostic.Message))
        );
      separateImport
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", separateImport.Diagnostics.Select(diagnostic => diagnostic.Message))
        );
      foreach (var result in new[] { glbImport, separateImport })
      {
        var diagnostic = result.Diagnostics.Should().ContainSingle(item =>
          item.Code == GltfDiagnosticCodes.TextureResourceMissing).Subject;
        diagnostic.Code.Should().Be(GltfDiagnosticCodes.TextureResourceMissing);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Path.Should().Be("materials[1]");
        diagnostic.Message.Should().Contain("reference-only");
      }
      var expectedBinding = Encoding.ASCII.GetBytes("Textures\\authored\\shared.tex");
      var glbAsset = StaticAsset(glbImport);
      var separateAsset = StaticAsset(separateImport);
      glbAsset
        .StaticRenderObjectSequence.Should()
        .HaveCount(2)
        .And.OnlyContain(record => record.TexturePathBytes.SequenceEqual(expectedBinding));
      glbAsset
        .StaticRenderObjectSequence[0]
        .RenderVertices.Min(vertex => vertex.Position.X)
        .Should()
        .Be(0);
      glbAsset
        .StaticRenderObjectSequence[1]
        .RenderVertices.Min(vertex => vertex.Position.X)
        .Should()
        .Be(10);
      var glbBytes = glbAsset.GetSerializedRepresentation();
      var separateBytes = separateAsset.GetSerializedRepresentation();
      glbBytes.AsSpan(4, 16).Clear();
      separateBytes.AsSpan(4, 16).Clear();
      separateBytes.Should().Equal(glbBytes);
    }
    finally
    {
      Directory.Delete(directory, true);
    }

    static void ShareSecondMaterial(JsonObject root)
    {
      RemoveEarthToolMetadata(root);
      root["materials"]![1]!["name"] = "Textures\\presentation-only.tex";
      var primitives = root["meshes"]![0]!["primitives"]!.AsArray();
      primitives[0]!["material"] = 1;
      primitives[1]!["material"] = 1;
    }
  }

  [Fact]
  public async Task NewModelImportAppliesTypedSemanticOverridesAndReportsConcreteCanonicalPaths()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        RemoveArtistHelperNodes(root);
        var nodes = root["nodes"]!.AsArray();
        nodes.Add(new JsonObject { ["children"] = new JsonArray(0) });
        root["scenes"]![0]!["nodes"] = new JsonArray(nodes.Count - 1);
      }
    );
    await using var source = new MemoryStream(metadataFree);
    var elevations = Enumerable.Repeat(1.5f, 16).ToArray();

    var imported = await interchange.CreateMeshAsync(
      source,
      new GltfNewModelImportOptions(
        textureResourceBindings: new Dictionary<GltfMaterialHandle, string?>(),
        footprint: new GltfNewModelFootprint(0x0003, elevations, new byte[16]),
        horizontalExtents: new GltfNewModelHorizontalExtents(2, 3, 4, 5),
        objectRoles: new Dictionary<GltfNodeHandle, GltfNewModelObjectRole>
        {
          [new GltfNodeHandle(2)] = new(
            GltfStaticObjectRoles.ViewerFaced | GltfStaticObjectRoles.Rotor
          ),
        }
      )
    );

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    var asset = StaticAsset(imported);
    asset.CommonBaseHeader.BoxPresenceMask.Should().Be(3);
    asset.CommonBaseHeader.HorizontalExtents.Should().Equal(new byte[] { 0, 2, 0, 3, 0, 4, 0, 5 });
    asset
      .StaticRenderObjectSequence[0]
      .KnownFlags.Should()
      .Be(StaticRenderObjectFlags.ViewerFaced | StaticRenderObjectFlags.Rotor);
    Action defaultHandle = () =>
      new GltfNewModelImportOptions(
        textureResourceBindings: new Dictionary<GltfMaterialHandle, string?> { [default] = null }
      );
    defaultHandle.Should().Throw<ArgumentOutOfRangeException>();
    Action markerRole = () => new GltfNewModelObjectRole((GltfStaticObjectRoles)8);
    markerRole.Should().Throw<ArgumentOutOfRangeException>();
  }

  [Fact]
  public async Task EquivalentMetadataFreeGlbAndSeparateGltfAuthorEquivalentCanonicalAssets()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exportedGlb = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exportedGlb
    );
    var metadataFreeGlb = RewriteJson(exportedGlb.ToArray(), RemoveEarthToolMetadata);
    await using var glbSource = new MemoryStream(metadataFreeGlb);
    var glbImport = await interchange.CreateMeshAsync(glbSource);

    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var separateExport = await interchange.ExportGltfFileAsync(
        sourceAsset,
        path
      );
      separateExport.Status.Should().Be(OperationStatus.Succeeded);
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveEarthToolMetadata(root);
      await File.WriteAllTextAsync(path, root.ToJsonString());

      var separateImport = await interchange.CreateMeshFileAsync(path);

      glbImport.Status.Should().Be(OperationStatus.Succeeded);
      separateImport.Status.Should().Be(OperationStatus.Succeeded);
      var glbBytes = StaticAsset(glbImport).GetSerializedRepresentation();
      var separateBytes = StaticAsset(separateImport).GetSerializedRepresentation();
      glbBytes.AsSpan(4, 16).Clear();
      separateBytes.AsSpan(4, 16).Clear();
      separateBytes.Should().Equal(glbBytes);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateNewModelImportAcceptsContainedNestedBufferUri()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var export = await interchange.ExportGltfFileAsync(
        sourceAsset,
        path
      );
      export.Status.Should().Be(OperationStatus.Succeeded);
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveEarthToolMetadata(root);
      var bufferName = root["buffers"]![0]!["uri"]!.GetValue<string>();
      var assetsDirectory = Path.Combine(directory, "assets");
      Directory.CreateDirectory(assetsDirectory);
      File.Move(Path.Combine(directory, bufferName), Path.Combine(assetsDirectory, bufferName));
      root["buffers"]![0]!["uri"] = $"assets/{bufferName}";
      await File.WriteAllTextAsync(path, root.ToJsonString());

      var imported = await interchange.CreateMeshFileAsync(path);

      imported
        .Status.Should()
        .Be(
          OperationStatus.Succeeded,
          string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
        );
      StaticAsset(imported).StaticRenderObjectSequence.Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task NewModelDefaultsUseCompleteSourceTreeInRootLocalSpace()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        RemoveArtistHelperNodes(root);
        var nodes = root["nodes"]!.AsArray();
        nodes.Add(
          new JsonObject
          {
            ["name"] = "ET_Static_2",
            ["mesh"] = 0,
            ["translation"] = new JsonArray(5, 3, 2),
            ["scale"] = new JsonArray(2, 2, 2),
          }
        );
        nodes[0]!["children"] = new JsonArray(nodes.Count - 1);
        root["scenes"]![0]!["nodes"] = new JsonArray(0);
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    var header = StaticAsset(imported).CommonBaseHeader;
    header.BoxPresenceMask.Should().Be(0x8000);
    BinaryPrimitives
      .ReadUInt16LittleEndian(header.BoxTopElevations.Take(2).ToArray())
      .Should()
      .Be(ToUnsignedFixedPoint(3));
    header.HorizontalExtents.Should().Equal(new byte[] { 0, 1, 0, 2, 0, 7, 0, 0 });
  }

  [Fact]
  public async Task NewModelOutOfRangeDerivedFootprintFailsTransactionally()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        RemoveArtistHelperNodes(root);
        var nodes = root["nodes"]!.AsArray();
        nodes.Add(new JsonObject
        {
          ["name"] = "ET_Static_2",
          ["mesh"] = 0,
          ["translation"] = new JsonArray(0, 256, 0),
        });
        nodes[0]!["children"] = new JsonArray(nodes.Count - 1);
        root["scenes"]![0]!["nodes"] = new JsonArray(0);
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic => diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry)
      .Subject.Should()
      .Match<OperationDiagnostic>(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "CommonBaseHeader.Footprint"
        && diagnostic.Message.Contains("256", StringComparison.Ordinal)
        && diagnostic.Message.Contains("255.996", StringComparison.Ordinal)
      );
  }

  [Fact]
  public async Task NewModelImportCollapsesGroupsAndPreservesCanonicalHierarchyAndPartitionOrder()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        RemoveArtistHelperNodes(root);
        var nodes = root["nodes"]!.AsArray();
        var meshNode = nodes[0]!.AsObject();
        meshNode["translation"] = new JsonArray(2, 3, 4);
        meshNode["children"] = new JsonArray(2, 3);
        nodes.Insert(
          0,
          new JsonObject { ["scale"] = new JsonArray(-1, 1, 1), ["children"] = new JsonArray(1) }
        );
        nodes.Add(
          new JsonObject
          {
            ["name"] = "ET_Static_2",
            ["mesh"] = 0,
            ["translation"] = new JsonArray(5, 0, 0),
            ["scale"] = new JsonArray(10, 10, 10),
          }
        );
        nodes.Add(new JsonObject
        {
          ["name"] = "ET_Static_3",
          ["mesh"] = 0,
          ["translation"] = new JsonArray(6, 0, 0),
        });
        root["scenes"]![0]!["nodes"] = new JsonArray(0);
        var primitives = root["meshes"]![0]!["primitives"]!.AsArray();
        primitives.Add(primitives[0]!.DeepClone());
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join(
          "; ",
          imported.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")
        )
      );
    var asset = StaticAsset(imported);
    asset.RootSourceObject.Children.Should().HaveCount(2);
    asset
      .RootSourceObject.Children.Select(child => child.StaticRenderObjects.Count)
      .Should()
      .Equal(2, 2);
    var sourceObjects = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject).ToArray();
    asset
      .StaticRenderObjectSequence.Select(record =>
        Array.FindIndex(
          sourceObjects,
          source => source.StaticRenderObjects.Any(candidate => ReferenceEquals(candidate, record))
        )
      )
      .Should()
      .Equal(0, 1, 1, 2, 2, 0);
    asset.StaticRenderObjectSequence[0].Pivot.Should().Be(new Vector3(-2, -4, 3));
    asset
      .RootSourceObject.Children.Select(child => child.StaticRenderObjects[0].Pivot.X)
      .Should()
      .Equal(-5, -6);
    var rootTriangle = asset
      .StaticRenderObjectSequence[0]
      .Triangles.Should()
      .ContainSingle()
      .Subject;
    (rootTriangle.Vertex0, rootTriangle.Vertex1, rootTriangle.Vertex2).Should().Be((0, 2, 1));

    var rootVertices = asset
      .RootSourceObject.StaticRenderObjects.SelectMany(record => record.RenderVertices)
      .ToArray();
    BinaryPrimitives
      .ReadUInt16LittleEndian(asset.CommonBaseHeader.HorizontalExtents.Skip(4).Take(2).ToArray())
      .Should()
      .Be(ToUnsignedFixedPoint(Math.Max(0, rootVertices.Max(vertex => vertex.Position.X))));
    BinaryPrimitives
      .ReadUInt16LittleEndian(asset.CommonBaseHeader.HorizontalExtents.Skip(6).Take(2).ToArray())
      .Should()
      .Be(ToUnsignedFixedPoint(15));
    BinaryPrimitives
      .ReadUInt16LittleEndian(asset.CommonBaseHeader.BoxTopElevations.Take(2).ToArray())
      .Should()
      .Be(ToUnsignedFixedPoint(rootVertices.Max(vertex => vertex.Position.Z)));
  }

  [Fact]
  public async Task NewModelImportIgnoresInertAdditionalTextureCoordinatesWithWarning()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var sourceBytes = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        var attributes = root["meshes"]![0]!["primitives"]![0]!["attributes"]!.AsObject();
        attributes["TEXCOORD_1"] = attributes["TEXCOORD_0"]!.DeepClone();
      }
    );
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Succeeded);
    imported
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InertDataIgnored
        && diagnostic.EventId == 1119
        && diagnostic.Path == "meshes[0].primitives[0].attributes.TEXCOORD_1"
      );
  }

  [Fact]
  public async Task NewModelImportAcceptsSparsePositionAccessorRepresentation()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var sourceBytes = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        var primitive = root["meshes"]![0]!["primitives"]![0]!;
        var accessors = root["accessors"]!.AsArray();
        var bufferViews = root["bufferViews"]!.AsArray();
        var position = accessors[primitive["attributes"]!["POSITION"]!.GetValue<int>()]!.AsObject();
        var indices = accessors[primitive["indices"]!.GetValue<int>()]!.AsObject();
        var sparseIndexView = bufferViews[indices["bufferView"]!.GetValue<int>()]!
          .DeepClone()!
          .AsObject();
        sparseIndexView.Remove("target");
        sparseIndexView.Remove("byteStride");
        bufferViews.Add(sparseIndexView);
        var sparseValueView = bufferViews[position["bufferView"]!.GetValue<int>()]!
          .DeepClone()!
          .AsObject();
        sparseValueView.Remove("target");
        sparseValueView.Remove("byteStride");
        bufferViews.Add(sparseValueView);
        position["sparse"] = new JsonObject
        {
          ["count"] = 1,
          ["indices"] = new JsonObject
          {
            ["bufferView"] = bufferViews.Count - 2,
            ["byteOffset"] = indices["byteOffset"]?.DeepClone() ?? 0,
            ["componentType"] = indices["componentType"]!.DeepClone(),
          },
          ["values"] = new JsonObject
          {
            ["bufferView"] = bufferViews.Count - 1,
            ["byteOffset"] = position["byteOffset"]?.DeepClone() ?? 0,
          },
        };
      }
    );
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.CreateMeshAsync(source);

    imported
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    StaticAsset(imported).StaticRenderObjectSequence.Should().ContainSingle();
  }

  [Fact]
  public async Task NewModelImportIgnoresSceneOnlyCameraAndUnusedSamplerWithWarnings()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var sourceBytes = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        RemoveArtistHelperNodes(root);
        var nodes = root["nodes"]!.AsArray();
        nodes.Insert(
          0,
          new JsonObject
          {
            ["name"] = "Preview Camera Group",
            ["camera"] = 0,
            ["children"] = new JsonArray(1),
          }
        );
        root["scenes"]![0]!["nodes"] = new JsonArray(0);
        root["cameras"] = new JsonArray(
          new JsonObject
          {
            ["type"] = "perspective",
            ["perspective"] = new JsonObject { ["yfov"] = 0.7, ["znear"] = 0.1 },
          }
        );
        root["samplers"] = new JsonArray(new JsonObject());
      }
    );
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Succeeded);
    StaticAsset(imported).StaticRenderObjectSequence.Should().ContainSingle();
    imported
      .Diagnostics.Where(diagnostic => diagnostic.Code == GltfDiagnosticCodes.InertDataIgnored)
      .Select(diagnostic => diagnostic.Path)
      .Should()
      .Equal("nodes[0].camera", "samplers");
  }

  [Fact]
  public async Task NewModelImportAcceptsBlenderRoundTripTextureSamplerReferences()
  {
    var sourceAsset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(sourceAsset, exported);
    var sourceBytes = RewriteJson(
      exported.ToArray(),
      root =>
      {
        root["samplers"] = new JsonArray(
          new JsonObject { ["magFilter"] = 9729, ["minFilter"] = 9987 }
        );
        foreach (var texture in root["textures"]!.AsArray())
        {
          texture!["sampler"] = 0;
        }
      }
    );
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(d => $"{d.Code}:{d.Message}")));
  }

  [Fact]
  public async Task NewModelImportRejectsOutOfRangeTextureSamplerReference()
  {
    var sourceAsset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(sourceAsset, exported);
    var sourceBytes = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["samplers"] = new JsonArray(
          new JsonObject { ["magFilter"] = 9729, ["minFilter"] = 9987 }
        );
        root["textures"]![0]!["sampler"] = 5;
      }
    );
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.UnsupportedDomain
      && diagnostic.Data["domain"] == "TexturePreviews");
  }

  [Theory]
  [InlineData("ambiguous-root")]
  [InlineData("singular-transform")]
  [InlineData("normal-overflow")]
  [InlineData("unsupported-material")]
  [InlineData("invalid-index")]
  public async Task NewModelImportRejectsAmbiguousLossyUnsupportedAndUnsafeInput(string mutation)
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        if (mutation == "ambiguous-root")
        {
          var nodes = root["nodes"]!.AsArray();
          nodes.Add(nodes[0]!.DeepClone());
          nodes.Insert(0, new JsonObject { ["children"] = new JsonArray(1, 2) });
          root["scenes"]![0]!["nodes"] = new JsonArray(0);
        }
        else if (mutation == "singular-transform")
        {
          root["nodes"]![0]!["scale"] = new JsonArray(0, 1, 1);
        }
        else if (mutation == "normal-overflow")
        {
          root["nodes"]![0]!["scale"] = new JsonArray(1e-30, 1, 1);
        }
        else if (mutation == "unsupported-material")
        {
          root["materials"] = new JsonArray(new JsonObject());
          root["meshes"]![0]!["primitives"]![0]!["material"] = 0;
        }
      }
    );
    if (mutation == "invalid-index")
    {
      BinaryPrimitives.WriteUInt16LittleEndian(
        metadataFree.AsSpan(GetBinaryChunkOffset(metadataFree) + 96),
        3
      );
    }
    else if (mutation == "normal-overflow")
    {
      var binaryOffset = GetBinaryChunkOffset(metadataFree);
      for (var normalOffset = 36; normalOffset <= 60; normalOffset += 12)
      {
        BinaryPrimitives.WriteInt32LittleEndian(
          metadataFree.AsSpan(binaryOffset + normalOffset),
          BitConverter.SingleToInt32Bits(1)
        );
        BinaryPrimitives.WriteInt32LittleEndian(
          metadataFree.AsSpan(binaryOffset + normalOffset + 8),
          0
        );
      }
    }
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Should().ContainSingle();
  }

  [Fact]
  public async Task NewModelImportPreservesStructuredOutputLimitDiagnostic()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(
      source,
      profile: new GltfOperationProfile(maxOutputBytes: 1)
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    var diagnostic = imported.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(GltfDiagnosticCodes.ResourceLimitExceeded);
    diagnostic.Path.Should().Be("$");
    diagnostic.Data.Should().ContainKeys("actual", "maximum");
  }

  [Fact]
  public async Task NewModelImportEnforcesConfiguredHierarchyDepthBeforeConversion()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        RemoveArtistHelperNodes(root);
        var nodes = root["nodes"]!.AsArray();
        nodes.Insert(0, new JsonObject { ["children"] = new JsonArray(1) });
        root["scenes"]![0]!["nodes"] = new JsonArray(0);
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(
      source,
      profile: new GltfOperationProfile(
        maxInputBytes: 32 * 1024 * 1024,
        maxOutputBytes: 32 * 1024 * 1024,
        maxMetadataBytes: 4 * 1024 * 1024,
        maxJsonDepth: 32,
        maxActiveRenderVertices: 65536,
        maxNodes: 4096,
        maxHierarchyDepth: 1
      )
    );

    imported.Status.Should().Be(OperationStatus.Failed);
    var diagnostic = imported.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(GltfDiagnosticCodes.ResourceLimitExceeded);
    diagnostic.Data.Should().Contain(new KeyValuePair<string, string>("actual", "2"));
    diagnostic.Data.Should().Contain(new KeyValuePair<string, string>("maximum", "1"));
  }

  [Fact]
  public async Task NewModelImportRejectsOversizedIndexDeclarationBeforeMaterialization()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported
    );
    var metadataFree = RewriteJson(
      exported.ToArray(),
      root =>
      {
        RemoveEarthToolMetadata(root);
        root["accessors"]![3]!["count"] = 3_145_731;
      }
    );
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.CreateMeshAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    var diagnostic = imported.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(GltfDiagnosticCodes.ResourceLimitExceeded);
    diagnostic.Data.Should().Contain(new KeyValuePair<string, string>("actual", "1048577"));
  }

  [Fact]
  public async Task ExportEnforcesFiniteGeometryAndActiveRenderVertexLimit()
  {
    var source = OneTriangleMshFixture.Create();
    var recordOffset = 0x14 + 0x368 + sizeof(uint);
    BinaryPrimitives.WriteInt32LittleEndian(
      source.AsSpan(recordOffset + 0x08),
      BitConverter.SingleToInt32Bits(float.NaN)
    );
    var nonFinite = await ReadAssetAsync(source);
    await using var destination = new MemoryStream();

    var invalid = await new GltfInterchange().ExportGlbAsync(nonFinite, destination);
    var limited = await new GltfInterchange().ExportGlbAsync(
      await ReadAssetAsync(OneTriangleMshFixture.Create()),
      destination,
      profile: new GltfOperationProfile(maxActiveRenderVertices: 2)
    );

    invalid.Status.Should().Be(OperationStatus.Failed);
    invalid
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(GltfDiagnosticCodes.InvalidGeometry);
    destination.Length.Should().Be(0);
    limited.Status.Should().Be(OperationStatus.Failed);
    limited
      .Diagnostics.Should()
      .ContainSingle()
      .Subject.Code.Should()
      .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
  }

  [Fact]
  public async Task ExportUsesUnsignedIntIndicesForMaximumVertexIndex()
  {
    var vertices = Enumerable
      .Range(0, 65536)
      .Select(_ => new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero))
      .ToArray();
    var build = StaticMeshBuilder
      .Create(OneTriangleMshFixture.CreationGuid)
      .SetRenderObject(vertices, [new CanonicalTriangle(0, 1, ushort.MaxValue)])
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    await using var glb = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset!,
      glb
    );

    result
      .Status.Should()
      .Be(
        OperationStatus.Succeeded,
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message))
      );
    using var json = ReadGlbJson(glb.ToArray());
    json.RootElement.GetProperty("accessors")[3]
      .GetProperty("componentType")
      .GetInt32()
      .Should()
      .Be(5125);
    glb.Position = 0;
    var validation = await new GltfInterchange().ValidateGlbAsync(glb);
    validation.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public async Task TransactionalSeparateGltfExportPreservesManifestWhenCommitFails()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var interchange = new GltfInterchange(new FailingManifestTransactionalFileSystem());
      var result = await interchange.ExportGltfFileAsync(
        asset,
        path
      );

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().HaveCount(2);
      Directory
        .EnumerateFiles(directory)
        .Should()
        .NotContain(file => file.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task RepeatedSeparateGltfFailuresProduceSameDiagnostics()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    var options = new GltfExportOptions();
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var failing = new GltfInterchange(new FailingManifestTransactionalFileSystem());

      var firstFailure = await failing.ExportGltfFileAsync(asset, path, options);
      var repeatedFailure = await failing.ExportGltfFileAsync(asset, path, options);

      repeatedFailure
        .Diagnostics.Select(diagnostic =>
          (
            diagnostic.Code,
            diagnostic.EventId,
            diagnostic.Severity,
            diagnostic.Path,
            diagnostic.ByteOffset,
            Data: diagnostic.Data.ToArray()
          )
        )
        .Should()
        .BeEquivalentTo(
          firstFailure.Diagnostics.Select(diagnostic =>
            (
              diagnostic.Code,
              diagnostic.EventId,
              diagnostic.Severity,
              diagnostic.Path,
              diagnostic.ByteOffset,
              Data: diagnostic.Data.ToArray()
            )
          ),
          assertionOptions => assertionOptions.WithStrictOrdering()
        );
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateGltfRetryReusesSidecarsFromFailedManifestCommit()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    var options = new GltfExportOptions();
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);
    await new GltfInterchange(new FailingManifestTransactionalFileSystem()).ExportGltfFileAsync(
      asset,
      path,
      options
    );

    try
    {
      var retry = await new GltfInterchange().ExportGltfFileAsync(asset, path, options);

      retry.Status.Should().Be(OperationStatus.Succeeded);
      (await new GltfInterchange().ValidateGltfFileAsync(path))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      Directory.EnumerateFiles(directory).Should().HaveCount(2);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateExportValidatesCommittedSidecarsBeforeReplacingManifest()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var result = await new GltfInterchange(
        new CorruptingSidecarTransactionalFileSystem()
      ).ExportGltfFileAsync(asset, path);

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().HaveCount(2);
      Directory
        .EnumerateFiles(directory)
        .Should()
        .NotContain(file => file.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateExportRejectsManifestBufferCollisionWithoutChangingBuffer()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var manifestPath = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var first = await interchange.ExportGltfFileAsync(
        asset,
        manifestPath
      );
      first.Status.Should().Be(OperationStatus.Succeeded);
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(manifestPath));
      var bufferPath = Path.Combine(
        directory,
        json.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!
      );
      var originalBuffer = await File.ReadAllBytesAsync(bufferPath);

      var collisionPath = Path.Combine(directory, Path.GetFileName(bufferPath).ToUpperInvariant());
      var collision = await interchange.ExportGltfFileAsync(
        asset,
        collisionPath
      );

      collision.Status.Should().Be(OperationStatus.Failed);
      collision
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(bufferPath)).Should().Equal(originalBuffer);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task StandaloneValidationEnforcesMetadataLimitForBothPackageForms()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    var profile = new GltfOperationProfile(maxMetadataBytes: 64);
    await using var glb = new MemoryStream();
    var glbExport = await interchange.ExportGlbAsync(
      asset,
      glb
    );
    glbExport.Status.Should().Be(OperationStatus.Succeeded);
    glb.Position = 0;
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var gltfExport = await interchange.ExportGltfFileAsync(
        asset,
        path
      );
      gltfExport.Status.Should().Be(OperationStatus.Succeeded);

      var glbValidation = await interchange.ValidateGlbAsync(glb, profile);
      var gltfValidation = await interchange.ValidateGltfFileAsync(path, profile);

      glbValidation.Status.Should().Be(OperationStatus.Failed);
      glbValidation
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.MetadataResourceLimitExceeded);
      gltfValidation.Status.Should().Be(OperationStatus.Failed);
      gltfValidation
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.MetadataResourceLimitExceeded);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task CancelledSeparateExportPreservesDestinationAndCreatesNoSidecars()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        path,
        cancellationToken: cancellation.Token
      );

      result.Status.Should().Be(OperationStatus.Cancelled);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateExportPreservesDestinationWhenSidecarCommitFails()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var interchange = new GltfInterchange(new FailingSidecarTransactionalFileSystem());
      var result = await interchange.ExportGltfFileAsync(asset, path);

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task CancellationAfterSidecarCommitPreservesManifestAndCleansTemporaryFiles()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);
    using var cancellation = new CancellationTokenSource();

    try
    {
      var fileSystem = new CancellingAfterSidecarTransactionalFileSystem(cancellation);
      var result = await new GltfInterchange(fileSystem).ExportGltfFileAsync(
        asset,
        path,
        cancellationToken: cancellation.Token
      );

      result.Status.Should().Be(OperationStatus.Cancelled);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().HaveCount(2);
      Directory
        .EnumerateFiles(directory)
        .Should()
        .NotContain(file => file.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task ExportEnforcesMetadataLimitBeforeWritingEitherPackageForm()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var profile = new GltfOperationProfile(maxMetadataBytes: 64);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var glbResult = await interchange.ExportGlbAsync(asset, glb, profile: profile);
      var gltfResult = await interchange.ExportGltfFileAsync(asset, path, profile: profile);

      glbResult.Status.Should().Be(OperationStatus.Failed);
      glbResult
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.MetadataResourceLimitExceeded);
      glb.Length.Should().Be(0);
      gltfResult.Status.Should().Be(OperationStatus.Failed);
      gltfResult
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.MetadataResourceLimitExceeded);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task GeneratedGlbPassesPinnedKhronosValidatorWithoutErrorsOrWarnings()
  {
    if (Environment.GetEnvironmentVariable("EARTHTOOL_RUN_KHRONOS_VALIDATOR") != "1")
    {
      return;
    }

    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );
    export.Succeeded.Should().BeTrue();
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
    await File.WriteAllBytesAsync(path, glb.ToArray());

    try
    {
      await AssertKhronosValidAsync(path);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public async Task GeneratedAnimationGlbPassesPinnedKhronosValidatorWithoutErrorsOrWarnings()
  {
    if (Environment.GetEnvironmentVariable("EARTHTOOL_RUN_KHRONOS_VALIDATOR") != "1")
    {
      return;
    }

    var asset = await ReadAssetAsync(
      StaticAnimationMshFixture.Create(
        3,
        new StaticAnimationMshFixture.AnimationLengths(0, 0, 0, 2),
        translations: [Vector3.Zero, Vector3.One],
        matrices: [Matrix4x4.Identity, Matrix4x4.CreateRotationY(0.5f)]
      )
    );
    await using var glb = new MemoryStream();
    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb
    );
    export.Succeeded.Should().BeTrue();
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
    await File.WriteAllBytesAsync(path, glb.ToArray());

    try
    {
      await AssertKhronosValidAsync(path);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public async Task GeneratedAnimationSeparateGltfPassesPinnedKhronosValidatorWithoutErrorsOrWarnings()
  {
    if (Environment.GetEnvironmentVariable("EARTHTOOL_RUN_KHRONOS_VALIDATOR") != "1")
    {
      return;
    }

    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var asset = await ReadAssetAsync(
        StaticAnimationMshFixture.Create(
          1,
          new StaticAnimationMshFixture.AnimationLengths(0, 2, 0, 0),
          scales: [Vector3.One, new Vector3(1, 2, 1)],
          matrices: [Matrix4x4.Identity, Matrix4x4.CreateRotationZ(0.5f)]
        )
      );
      var export = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        path
      );
      export.Succeeded.Should().BeTrue();

      await AssertKhronosValidAsync(path);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task TransactionalGlbExportPreservesDestinationWhenCommitFails()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var interchange = new GltfInterchange(new FailingTransactionalFileSystem());
      var result = await interchange.ExportGlbFileAsync(
        asset,
        path
      );

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle()
        .Subject.Code.Should()
        .Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public async Task TransactionalGlbExportPreservesDestinationWhenCancelled()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.glb");
    var original = new byte[] { 7, 8, 9 };
    await File.WriteAllBytesAsync(path, original);
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    try
    {
      var result = await new GltfInterchange().ExportGlbFileAsync(
        asset,
        path,
        cancellationToken: cancellation.Token
      );

      result.Status.Should().Be(OperationStatus.Cancelled);
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
    PublicApiApproval.Verify(
      "gltf",
      typeof(GltfInterchange).Assembly.ExportedTypes.Where(type =>
        type.Namespace == "EarthTool.GLTF"
      )
    );
  }

  private static async Task<StaticMeshAsset> CreateNestedEmitterAssetAsync()
  {
    var vertices = new[]
    {
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
    };
    var renderObject = new CanonicalStaticRenderObject(vertices, [new CanonicalTriangle(0, 1, 2)]);
    var build = StaticMeshBuilder
      .Create(OneTriangleMshFixture.CreationGuid)
      .SetRootSourceObject(
        new CanonicalStaticSourceObject(
          [renderObject],
          [
            new CanonicalStaticSourceObject(
              [renderObject],
              role: new CanonicalStaticObjectRole(StaticRenderObjectFlags.MarkerAttachment1)
            ),
          ]
        )
      )
      .Build();
    build.TryGetValue(out var builtAsset).Should().BeTrue();
    await using var msh = new MemoryStream();
    (await new MshWriter().WriteAsync(builtAsset!, msh))
      .Status.Should()
      .Be(OperationStatus.Succeeded);
    var sourceBytes = msh.ToArray();
    var attachmentOffset = 0x14 + AttachmentAndCannonMshFixture.AttachmentTableOffset + (4 * 8);
    BinaryPrimitives.WriteInt16LittleEndian(sourceBytes.AsSpan(attachmentOffset), 256);
    BinaryPrimitives.WriteInt16LittleEndian(sourceBytes.AsSpan(attachmentOffset + 2), -512);
    BinaryPrimitives.WriteInt16LittleEndian(sourceBytes.AsSpan(attachmentOffset + 4), 768);
    sourceBytes[attachmentOffset + 7] = 0x80;
    return await ReadAssetAsync(sourceBytes);
  }

  private static void ReparentEmitterToRootSource(JsonObject root)
  {
    var nodes = root["nodes"]!.AsArray();
    var emitterIndex = nodes
      .Select((node, index) => (node, index))
      .Single(item => item.node!["name"]?.GetValue<string>() == "ET_Emitter_1")
      .index;
    var sourceIndices = nodes
      .Select((node, index) => (node, index))
      .Where(item => item.node!.AsObject().ContainsKey("mesh"))
      .Select(item => item.index)
      .ToArray();
    var rootSourceIndex = sourceIndices.Single(index =>
      nodes[index]!["children"]!
        .AsArray()
        .Any(child => sourceIndices.Contains(child!.GetValue<int>()))
    );
    foreach (var node in nodes.OfType<JsonObject>())
    {
      if (node["children"] is not JsonArray children)
      {
        continue;
      }
      for (var index = children.Count - 1; index >= 0; index--)
      {
        if (children[index]!.GetValue<int>() == emitterIndex)
        {
          children.RemoveAt(index);
        }
      }
      if (children.Count == 0)
      {
        node.Remove("children");
      }
    }
    nodes[rootSourceIndex]!["children"]!.AsArray().Add(emitterIndex);
  }

  private static async Task<StaticMeshAsset> ReadAssetAsync(byte[] source)
  {
    await using var stream = new MemoryStream(source);
    var result = await new MshReader().ReadAsync(stream);
    return result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
  }

  private static StaticMeshAsset StaticAsset(OperationResult<MeshAsset> result)
  {
    return result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
  }

  private static StaticMeshAsset CreateTwoTriangleAsset()
  {
    var build = StaticMeshBuilder
      .Create(OneTriangleMshFixture.CreationGuid)
      .SetRenderObject(
        [
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
          new CanonicalStaticVertex(Vector3.One, Vector3.UnitZ, Vector2.One),
        ],
        [new CanonicalTriangle(0, 1, 2), new CanonicalTriangle(2, 1, 3)]
      )
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static StaticMeshAsset CreateTextureBoundAsset(string textureResourceKey)
  {
    var vertices = new[]
    {
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
    };
    var build = StaticMeshBuilder
      .Create(OneTriangleMshFixture.CreationGuid)
      .SetRootSourceObject(
        new CanonicalStaticSourceObject([
          new CanonicalStaticRenderObject(
            vertices,
            [new CanonicalTriangle(0, 1, 2)],
            textureResourceKey
          ),
        ])
      )
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static StaticMeshAsset CreateTwoPartitionAsset(
    string? firstTextureResourceKey = null,
    string? secondTextureResourceKey = null
  )
  {
    var vertices = new[]
    {
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
    };
    var translated = vertices.Select(vertex => new CanonicalStaticVertex(
      vertex.Position + new Vector3(10, 0, 0),
      vertex.Normal,
      vertex.TextureCoordinate
    ));
    var build = StaticMeshBuilder
      .Create(OneTriangleMshFixture.CreationGuid)
      .SetRootSourceObject(
        new CanonicalStaticSourceObject([
          new CanonicalStaticRenderObject(
            vertices,
            [new CanonicalTriangle(0, 1, 2)],
            firstTextureResourceKey
          ),
          new CanonicalStaticRenderObject(
            translated,
            [new CanonicalTriangle(0, 1, 2)],
            secondTextureResourceKey
          ),
        ])
      )
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static async Task<byte[]> CreateGuardedTopologyFixtureAsync()
  {
    var build = StaticMeshBuilder
      .Create(OneTriangleMshFixture.CreationGuid)
      .SetRenderObject(
        [
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.One, Vector3.UnitZ, Vector2.One),
        ],
        [new CanonicalTriangle(0, 1, 2), new CanonicalTriangle(3, 1, 1)]
      )
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    await using var stream = new MemoryStream();
    var write = await new MshWriter().WriteAsync(asset!, stream);
    write.Status.Should().Be(OperationStatus.Succeeded);
    var bytes = stream.ToArray();
    var recordOffset = 0x14 + 0x368 + sizeof(uint);
    const int vertexBlockSize = 0xA0;
    bytes[recordOffset + 0x08 + vertexBlockSize + sizeof(float)] = 0x5A;
    BinaryPrimitives.WriteUInt16LittleEndian(
      bytes.AsSpan(recordOffset + 0x08 + 0x90 + sizeof(ushort)),
      0
    );
    var objectFlagsOffset = recordOffset + 0x08 + (2 * vertexBlockSize);
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(objectFlagsOffset), 0x12340000);
    var firstTriangleOffset = objectFlagsOffset + sizeof(uint) + sizeof(uint) + sizeof(uint);
    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(firstTriangleOffset + 6), 0x1234);
    return bytes;
  }

  private static int GetBinaryChunkOffset(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return 12 + 8 + jsonLength + 8;
  }

  private static int FindParentIndex(IReadOnlyList<JsonElement> nodes, int childIndex)
  {
    for (var index = 0; index < nodes.Count; index++)
    {
      if (
        nodes[index].TryGetProperty("children", out var children)
        && children.EnumerateArray().Any(child => child.GetInt32() == childIndex)
      )
      {
        return index;
      }
    }
    return -1;
  }

  private static void WriteVector3(byte[] bytes, int offset, Vector3 value)
  {
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(offset),
      BitConverter.SingleToInt32Bits(value.X)
    );
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(offset + 4),
      BitConverter.SingleToInt32Bits(value.Y)
    );
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(offset + 8),
      BitConverter.SingleToInt32Bits(value.Z)
    );
  }

  private static void RemoveArtistHelperNodes(JsonObject root)
  {
    var nodes = root["nodes"]!.AsArray();
    var helperIndices = nodes
      .Select((node, index) => (node, index))
      .Where(item =>
        item.node?["name"]?.GetValue<string>() is string name
        && (
          GlbDocument.TryParseAttachmentHelperName(name, out _)
          || GlbDocument.TryParseCannonHelperName(name, out _)
        )
      )
      .Select(item => item.index)
      .OrderByDescending(index => index)
      .ToArray();
    foreach (var index in helperIndices)
    {
      RemoveNodeAndReferences(root, index);
    }
    foreach (var node in root["nodes"]!.AsArray().OfType<JsonObject>())
    {
      if (node["children"] is JsonArray children && children.Count == 0)
      {
        node.Remove("children");
      }
    }
  }

  private static void RemoveNodeAndReferences(JsonObject root, int removedIndex)
  {
    root["nodes"]!.AsArray().RemoveAt(removedIndex);
    foreach (var node in root["nodes"]!.AsArray())
    {
      RewriteNodeIndices(node?["children"] is JsonArray children ? children : null, removedIndex);
    }
    foreach (var scene in root["scenes"]!.AsArray())
    {
      RewriteNodeIndices(scene?["nodes"] is JsonArray nodes ? nodes : null, removedIndex);
    }
    if (root["animations"] is JsonArray animations)
    {
      foreach (var channel in animations.SelectMany(animation => animation!["channels"]!.AsArray()))
      {
        var target = channel!["target"]!.AsObject();
        var nodeIndex = target["node"]!.GetValue<int>();
        if (nodeIndex == removedIndex)
        {
          throw new InvalidOperationException("Cannot remove a node targeted by animation.");
        }
        if (nodeIndex > removedIndex)
        {
          target["node"] = nodeIndex - 1;
        }
      }
    }
  }

  private static void RewriteNodeIndices(JsonArray? indices, int removedIndex)
  {
    if (indices is null)
    {
      return;
    }
    for (var index = indices.Count - 1; index >= 0; index--)
    {
      var nodeIndex = indices[index]!.GetValue<int>();
      if (nodeIndex == removedIndex)
      {
        indices.RemoveAt(index);
      }
      else if (nodeIndex > removedIndex)
      {
        indices[index] = nodeIndex - 1;
      }
    }
  }

  private static JsonDocument ReadGlbJson(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return JsonDocument.Parse(glb.AsMemory(20, jsonLength));
  }

  private static async Task AssertKhronosValidAsync(string path)
  {
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var startInfo = new ProcessStartInfo(
      "node",
      $"\"{Path.Combine(root, "test-tools", "validate-glb.mjs")}\" \"{path}\""
    )
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
    };
    using var process =
      Process.Start(startInfo) ?? throw new InvalidOperationException("Node did not start.");
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    process.ExitCode.Should().Be(0, $"validator stdout: {output} stderr: {error}");
    output.Should().Contain("\"errors\":0");
    output.Should().Contain("\"warnings\":0");
  }

  private static float ReadSingle(byte[] data, int offset)
  {
    return BitConverter.Int32BitsToSingle(
      BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset))
    );
  }

  private static float[] ReadFloatAccessor(
    byte[] glb,
    JsonElement root,
    int accessorIndex,
    int dimensions
  )
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    var count = accessor.GetProperty("count").GetInt32();
    var elementSize = dimensions * sizeof(float);
    var stride = view.TryGetProperty("byteStride", out var strideElement)
      ? strideElement.GetInt32()
      : elementSize;
    var offset = GetFloatAccessorOffset(glb, root, accessorIndex);
    var result = new float[count * dimensions];
    for (var element = 0; element < count; element++)
    {
      for (var component = 0; component < dimensions; component++)
      {
        result[element * dimensions + component] = ReadSingle(
          glb,
          offset + element * stride + component * sizeof(float)
        );
      }
    }
    return result;
  }

  private static int GetFloatAccessorOffset(byte[] glb, JsonElement root, int accessorIndex)
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    return GetBinaryChunkOffset(glb)
      + (view.TryGetProperty("byteOffset", out var viewOffset) ? viewOffset.GetInt32() : 0)
      + (
        accessor.TryGetProperty("byteOffset", out var accessorOffset)
          ? accessorOffset.GetInt32()
          : 0
      );
  }

  private static ushort ToUnsignedFixedPoint(float value)
  {
    return checked((ushort)Math.Truncate(value * 256d));
  }

  private static byte[] CreateRgbaTex(int width, int height, byte[] pixels)
  {
    pixels.Length.Should().Be(width * height * 4);
    var result = new byte[24 + pixels.Length];
    "TEX\0\x01\0\0\0"u8.CopyTo(result);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), 0x03000012);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), 0x8888);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(16), width);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(20), height);
    pixels.CopyTo(result, 24);
    return result;
  }

  private static byte[] CreateContainerTex(int width, int height, byte[] pixels)
  {
    var image = CreateRgbaTex(width, height, pixels);
    var result = new byte[16 + image.Length];
    "TEX\0\x01\0\0\0"u8.CopyTo(result);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), 0x80000002);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), 1);
    image.CopyTo(result, 16);
    return result;
  }

  private static string GetPreviewContentAddress(int width, int height, byte[] pixels)
  {
    var preimage = new byte[sizeof(int) * 2 + pixels.Length];
    BinaryPrimitives.WriteInt32LittleEndian(preimage, width);
    BinaryPrimitives.WriteInt32LittleEndian(preimage.AsSpan(sizeof(int)), height);
    pixels.CopyTo(preimage, sizeof(int) * 2);
    return Convert.ToHexString(SHA256.HashData(preimage)).ToLowerInvariant();
  }

  private static void SwapBlocks(byte[] data, int left, int right, int length)
  {
    var temporary = data.AsSpan(left, length).ToArray();
    data.AsSpan(right, length).CopyTo(data.AsSpan(left, length));
    temporary.CopyTo(data, right);
  }

  private static byte[] RewriteJson(byte[] glb, Action<JsonObject> rewrite)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var root = JsonNode.Parse(glb.AsSpan(20, jsonLength))!.AsObject();
    rewrite(root);
    return RewriteJsonChunk(glb, Encoding.UTF8.GetBytes(root.ToJsonString()));
  }

  private static byte[] RewriteGlb(byte[] glb, Action<JsonObject, List<byte>> rewrite)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var root = JsonNode.Parse(glb.AsSpan(20, jsonLength))!.AsObject();
    var binaryHeader = 20 + jsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(binaryHeader));
    var binary = glb.AsSpan(binaryHeader + 8, binaryLength).ToArray().ToList();
    rewrite(root, binary);
    root["buffers"]![0]!["byteLength"] = binary.Count;
    return PackGlb(Encoding.UTF8.GetBytes(root.ToJsonString()), binary.ToArray());
  }

  private static int AppendFloatAccessor(
    JsonObject root,
    List<byte> binary,
    IReadOnlyList<float> values,
    string type,
    int count,
    float? minimum = null,
    float? maximum = null
  )
  {
    while (binary.Count % 4 != 0)
    {
      binary.Add(0);
    }
    var offset = binary.Count;
    foreach (var value in values)
    {
      binary.AddRange(BitConverter.GetBytes(value));
    }
    var views = root["bufferViews"]!.AsArray();
    var viewIndex = views.Count;
    views.Add(
      new JsonObject
      {
        ["buffer"] = 0,
        ["byteOffset"] = offset,
        ["byteLength"] = values.Count * sizeof(float),
      }
    );
    var accessor = new JsonObject
    {
      ["bufferView"] = viewIndex,
      ["componentType"] = 5126,
      ["count"] = count,
      ["type"] = type,
    };
    if (minimum.HasValue)
    {
      accessor["min"] = new JsonArray(minimum.Value);
    }
    if (maximum.HasValue)
    {
      accessor["max"] = new JsonArray(maximum.Value);
    }
    var accessors = root["accessors"]!.AsArray();
    accessors.Add(accessor);
    return accessors.Count - 1;
  }

  private static byte[] PackGlb(byte[] json, byte[] binary)
  {
    var paddedJsonLength = (json.Length + 3) & ~3;
    var paddedBinaryLength = (binary.Length + 3) & ~3;
    var result = new byte[12 + 8 + paddedJsonLength + 8 + paddedBinaryLength];
    BinaryPrimitives.WriteUInt32LittleEndian(result, 0x46546C67);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), 2);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), paddedJsonLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
    json.CopyTo(result, 20);
    result.AsSpan(20 + json.Length, paddedJsonLength - json.Length).Fill(0x20);
    var binaryHeader = 20 + paddedJsonLength;
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(binaryHeader), paddedBinaryLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(binaryHeader + 4), 0x004E4942);
    binary.CopyTo(result, binaryHeader + 8);
    return result;
  }

  private static void RemoveEarthToolMetadata(JsonNode? node)
  {
    if (node is JsonObject owner)
    {
      if (
        owner["nodes"] is JsonArray nodes
        && owner["scenes"]?[0]?["nodes"] is JsonArray sceneNodes
        && sceneNodes.Count == 1
      )
      {
        var sceneRootIndex = sceneNodes[0]!.GetValue<int>();
        var sceneRoot = nodes[sceneRootIndex];
        if (sceneRoot?["extras"]?[GlbDocument.PlacementRootMarker]?.GetValue<bool>() == true)
        {
          sceneNodes[0] = sceneRoot["children"]![0]!.GetValue<int>();
          nodes.RemoveAt(sceneRootIndex);
        }
      }
      if (owner["extras"] is JsonObject extras)
      {
        extras.Remove("earthtoolAuthoring");
        if (extras.Count == 0)
        {
          owner.Remove("extras");
        }
      }

      foreach (var child in owner.ToArray())
      {
        RemoveEarthToolMetadata(child.Value);
      }
    }
    else if (node is JsonArray array)
    {
      foreach (var child in array)
      {
        RemoveEarthToolMetadata(child);
      }
    }
  }

  private static void RemoveMetadataAndSetMisleadingTexturePresentation(
    JsonObject root,
    string? imageUri = null
  )
  {
    RemoveEarthToolMetadata(root);
    root["materials"]![0]!["name"] = "Textures\\presentation-only.tex";
    root["images"]![0]!["name"] = "Textures\\presentation-only.tex";
    if (imageUri is not null)
    {
      root["images"]![0]!["uri"] = imageUri;
    }
  }

  private static byte[] RewriteJsonChunk(byte[] glb, byte[] rewrittenJson)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var paddedJsonLength = (rewrittenJson.Length + 3) & ~3;
    var oldBinaryHeader = 20 + jsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(oldBinaryHeader));
    var result = new byte[12 + 8 + paddedJsonLength + 8 + binaryLength];
    BinaryPrimitives.WriteUInt32LittleEndian(result, 0x46546C67);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), 2);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), result.Length);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), paddedJsonLength);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
    rewrittenJson.CopyTo(result, 20);
    result.AsSpan(20 + rewrittenJson.Length, paddedJsonLength - rewrittenJson.Length).Fill(0x20);
    var newBinaryHeader = 20 + paddedJsonLength;
    glb.AsSpan(oldBinaryHeader, 8 + binaryLength).CopyTo(result.AsSpan(newBinaryHeader));
    return result;
  }

  private sealed class FailingTransactionalFileSystem
    : EarthTool.GLTF.Internal.ITransactionalFileSystem
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

  private sealed class FailingManifestTransactionalFileSystem : ITransactionalFileSystem
  {
    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + $".{Guid.NewGuid():N}.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      if (destinationPath.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase))
      {
        throw new IOException("Injected manifest commit failure.");
      }

      File.Move(temporaryPath, destinationPath);
    }

    public bool TryDelete(string temporaryPath)
    {
      if (File.Exists(temporaryPath))
      {
        File.Delete(temporaryPath);
      }

      return true;
    }
  }

  private sealed class FailingSidecarTransactionalFileSystem : ITransactionalFileSystem
  {
    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + $".{Guid.NewGuid():N}.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      if (destinationPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
      {
        throw new IOException("Injected sidecar commit failure.");
      }

      File.Move(temporaryPath, destinationPath);
    }

    public bool TryDelete(string temporaryPath)
    {
      if (File.Exists(temporaryPath))
      {
        File.Delete(temporaryPath);
      }

      return true;
    }
  }

  private sealed class CorruptingSidecarTransactionalFileSystem : ITransactionalFileSystem
  {
    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + $".{Guid.NewGuid():N}.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      if (File.Exists(destinationPath))
      {
        File.Replace(temporaryPath, destinationPath, null);
      }
      else
      {
        File.Move(temporaryPath, destinationPath);
      }
      if (destinationPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
      {
        File.WriteAllBytes(destinationPath, [1, 2, 3]);
      }
    }

    public bool TryDelete(string temporaryPath)
    {
      if (File.Exists(temporaryPath))
      {
        File.Delete(temporaryPath);
      }
      return true;
    }
  }

  private sealed class CancellingAfterSidecarTransactionalFileSystem : ITransactionalFileSystem
  {
    private readonly CancellationTokenSource _cancellation;

    internal CancellingAfterSidecarTransactionalFileSystem(CancellationTokenSource cancellation)
    {
      _cancellation = cancellation;
    }

    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + $".{Guid.NewGuid():N}.tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      File.Move(temporaryPath, destinationPath);
      if (destinationPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
      {
        _cancellation.Cancel();
      }
    }

    public bool TryDelete(string temporaryPath)
    {
      if (File.Exists(temporaryPath))
      {
        File.Delete(temporaryPath);
      }

      return true;
    }
  }
}
