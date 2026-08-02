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
  private static readonly Guid LineageId = new("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
  private static readonly Guid DocumentId = new("11111111-2222-4333-8444-555555555555");

  [Fact]
  public async Task SparseStaticLightsProjectNativelyAndRestoreExactRecords()
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [1] = new(
          new Vector3(1.25f, -2.5f, 3.75f),
          new Vector3(0.25f, 0.5f, 0.75f),
          16,
          32,
          [0xA1, 0xB2, 0xC3],
          0.25f,
          8,
          -0.5f,
          4),
        [3] = new(
          new Vector3(-4.25f, 5.5f, -6.75f),
          new Vector3(0.1f, 0.2f, 0.3f),
          20,
          96,
          [1, 2, 3],
          0.5f,
          12,
          0.25f,
          2)
      },
      new Dictionary<int, StaticLightMshFixture.OmniRecord>
      {
        [2] = new(new Vector3(7.25f, -8.5f, 9.75f), new Vector3(0.9f, 0.8f, 0.7f), 3),
        [4] = new(new Vector3(-10.25f, 11.5f, -12.75f), new Vector3(0.6f, 0.4f, 0.2f), 5)
      },
      activeSpots: [1, 3],
      activeOmnis: [2, 4]);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", export.Diagnostics.Select(diagnostic => diagnostic.Message)));
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      json.RootElement.GetProperty("extensionsUsed").EnumerateArray()
        .Select(item => item.GetString()).Should().Contain("KHR_lights_punctual");
      var lights = json.RootElement.GetProperty("extensions")
        .GetProperty("KHR_lights_punctual").GetProperty("lights");
      lights.GetArrayLength().Should().Be(4);
      lights.EnumerateArray().Count(light => light.GetProperty("type").GetString() == "spot")
        .Should().Be(2);
      lights.EnumerateArray().Count(light => light.GetProperty("type").GetString() == "point")
        .Should().Be(2);
      var lightNodes = json.RootElement.GetProperty("nodes").EnumerateArray()
        .Where(node => node.TryGetProperty("extensions", out var extensions)
          && extensions.TryGetProperty("KHR_lights_punctual", out _)).ToArray();
      lightNodes.Select(node => node.GetProperty("name").GetString()).Should().BeEquivalentTo(
        "ET_SpotLight_1_Attachment_13",
        "ET_SpotLight_3_Attachment_15",
        "ET_OmniLight_2_Attachment_18",
        "ET_OmniLight_4_Attachment_20");
    }

    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.GetSerializedRepresentation().Should().Equal(sourceBytes);
    import.Value.RestoredSerializedRepresentationPaths.Should()
      .Contain("CommonBaseHeader.StaticSpotLights[1]")
      .And.Contain("CommonBaseHeader.StaticOmniLights[4]");
  }

  [Fact]
  public async Task StaticLightNativeEditsRegenerateOnlyDeclaredDependentFields()
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [1] = new(
          new Vector3(1, 2, 3),
          new Vector3(0.1f, 0.2f, 0.3f),
          10,
          24,
          [0x11, 0x22, 0x33],
          0.2f,
          7,
          0.4f,
          5),
        [2] = new(
          new Vector3(4, 5, 6),
          new Vector3(0.4f, 0.5f, 0.6f),
          20,
          48,
          [0x44, 0x55, 0x66],
          0.3f,
          11,
          -0.25f,
          6)
      },
      new Dictionary<int, StaticLightMshFixture.OmniRecord>
      {
        [3] = new(new Vector3(7, 8, 9), new Vector3(0.7f, 0.8f, 0.9f), 7)
      },
      activeSpots: [1, 2],
      activeOmnis: [3]);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var spot1 = nodes.Single(node =>
        node!["name"]!.GetValue<string>() == "ET_SpotLight_1_Attachment_13")!.AsObject();
      spot1["translation"] = new JsonArray(2.125f, 4.375f, -3.25f);

      var definitions = root["extensions"]!["KHR_lights_punctual"]!["lights"]!.AsArray();
      definitions.Single(light =>
        light!["name"]!.GetValue<string>() == "ET_SpotLight_2_Attachment_14")!["color"] =
        new JsonArray(0.25f, 0.5f, 0.75f);
      var omni3 = definitions.Single(light =>
        light!["name"]!.GetValue<string>() == "ET_OmniLight_3_Attachment_19")!.AsObject();
      omni3["intensity"] = 12.5f;
      var spot1Definition = definitions.Single(light =>
        light!["name"]!.GetValue<string>() == "ET_SpotLight_1_Attachment_13")!.AsObject();
      spot1Definition["spot"]!["innerConeAngle"] = 0.25f;
      spot1Definition["spot"]!["outerConeAngle"] = 0.5f;
    });

    await using var input = new MemoryStream(edited);
    var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var resultBytes = import.Value!.Asset.GetSerializedRepresentation().ToArray();
    var sourceSpot1 = StaticLightMshFixture.GetSpot(sourceBytes, 1);
    var resultSpot1 = StaticLightMshFixture.GetSpot(resultBytes, 1);
    ReadSingle(resultSpot1, 0).Should().Be(2.125f);
    ReadSingle(resultSpot1, 4).Should().Be(-3.25f);
    ReadSingle(resultSpot1, 8).Should().Be(4.375f);
    resultSpot1.AsSpan(0x0C, 0x14).ToArray().Should().Equal(sourceSpot1.AsSpan(0x0C, 0x14).ToArray());
    ReadSingle(resultSpot1, 0x20).Should().BeApproximately(MathF.Tan(0.25f), 1e-6f);
    ReadSingle(resultSpot1, 0x24).Should().Be(5);
    resultSpot1.AsSpan(0x28, 8).ToArray().Should().Equal(sourceSpot1.AsSpan(0x28, 8).ToArray());

    var attachment = StaticLightMshFixture.GetAttachment(resultBytes, 13);
    BinaryPrimitives.ReadInt16LittleEndian(attachment).Should().Be(544);
    BinaryPrimitives.ReadInt16LittleEndian(attachment.AsSpan(2)).Should().Be(-832);
    BinaryPrimitives.ReadInt16LittleEndian(attachment.AsSpan(4)).Should().Be(1120);
    attachment[6..].Should().Equal(StaticLightMshFixture.GetAttachment(sourceBytes, 13)[6..]);

    var resultSpot2 = StaticLightMshFixture.GetSpot(resultBytes, 2);
    resultSpot2.AsSpan(0, 0x0C).ToArray().Should()
      .Equal(StaticLightMshFixture.GetSpot(sourceBytes, 2).AsSpan(0, 0x0C).ToArray());
    new[] { ReadSingle(resultSpot2, 0x0C), ReadSingle(resultSpot2, 0x10), ReadSingle(resultSpot2, 0x14) }
      .Should().Equal(0.25f, 0.5f, 0.75f);
    resultSpot2.AsSpan(0x18).ToArray().Should()
      .Equal(StaticLightMshFixture.GetSpot(sourceBytes, 2).AsSpan(0x18).ToArray());

    var resultOmni3 = StaticLightMshFixture.GetOmni(resultBytes, 3);
    resultOmni3.AsSpan(0, 0x18).ToArray().Should()
      .Equal(StaticLightMshFixture.GetOmni(sourceBytes, 3).AsSpan(0, 0x18).ToArray());
    ReadSingle(resultOmni3, 0x18).Should().Be(12.5f);
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "CommonBaseHeader.StaticSpotLights[1].Position");
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "CommonBaseHeader.StaticSpotLights[1].Cones");
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "CommonBaseHeader.StaticSpotLights[2].Color");
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "CommonBaseHeader.StaticOmniLights[3].TerrainLightAmplitude");
  }

  [Fact]
  public async Task StaticLightDeletionAndTypeConversionPreserveSparsePhysicalIdentity()
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [1] = new(
          new Vector3(1, 2, 3),
          new Vector3(0.2f, 0.4f, 0.6f),
          8,
          64,
          [7, 8, 9],
          0.25f,
          4,
          0.5f,
          3),
        [3] = new(
          new Vector3(4, 5, 6),
          new Vector3(0.3f, 0.5f, 0.7f),
          12,
          32,
          [1, 3, 5],
          0.2f,
          6,
          -0.25f,
          2)
      },
      activeSpots: [1, 3]);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var deletedIndex = nodes.Select((node, index) => (node, index)).Single(item =>
        item.node!["name"]!.GetValue<string>() == "ET_SpotLight_3_Attachment_15").index;
      RemoveNodeAndReferences(root, deletedIndex);

      var definitions = root["extensions"]!["KHR_lights_punctual"]!["lights"]!.AsArray();
      definitions.RemoveAt(1);
      definitions.Single(light =>
        light!["name"]!.GetValue<string>() == "ET_SpotLight_1_Attachment_13")!["type"] = "point";
    });

    await using var input = new MemoryStream(edited);
    var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var resultBytes = import.Value!.Asset.GetSerializedRepresentation().ToArray();
    StaticLightMshFixture.GetSpot(resultBytes, 1).Should().Equal(StaticLightMshFixture.GetSpot(sourceBytes, 1));
    StaticLightMshFixture.GetAttachment(resultBytes, 13).Should().Equal(0, 128, 0, 128, 0, 128, 0, 0);
    var point = StaticLightMshFixture.GetOmni(resultBytes, 1);
    new[] { ReadSingle(point, 0), ReadSingle(point, 4), ReadSingle(point, 8) }
      .Should().Equal(1, 2, 3);
    new[] { ReadSingle(point, 0x0C), ReadSingle(point, 0x10), ReadSingle(point, 0x14) }
      .Should().Equal(0.2f, 0.4f, 0.6f);
    ReadSingle(point, 0x18).Should().Be(3);
    BinaryPrimitives.ReadInt16LittleEndian(StaticLightMshFixture.GetAttachment(resultBytes, 17))
      .Should().NotBe(short.MinValue);

    StaticLightMshFixture.GetSpot(resultBytes, 3).Should().Equal(StaticLightMshFixture.GetSpot(sourceBytes, 3));
    StaticLightMshFixture.GetAttachment(resultBytes, 15).Should().Equal(0, 128, 0, 128, 0, 128, 0, 0);
  }

  [Fact]
  public async Task AnomalousStaticLightUsesWarnedFinitePreviewAndRestoresExactBits()
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [4] = new(
          new Vector3(float.NaN, float.PositiveInfinity, float.NegativeInfinity),
          new Vector3(-1, float.NaN, float.PositiveInfinity),
          float.NaN,
          255,
          [0xDE, 0xAD, 0xBE],
          -2,
          float.PositiveInfinity,
          float.NaN,
          float.NegativeInfinity)
      },
      activeSpots: [4]);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    export.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.StaticLightPreviewSubstituted
      && diagnostic.EventId == 1117
      && diagnostic.Path == "CommonBaseHeader.StaticSpotLights[4]");
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      var light = json.RootElement.GetProperty("extensions")
        .GetProperty("KHR_lights_punctual").GetProperty("lights")[0];
      light.GetProperty("color").EnumerateArray().Select(value => value.GetSingle())
        .Should().Equal(0, 0, 0);
      light.GetProperty("intensity").GetSingle().Should().Be(0);
      var node = json.RootElement.GetProperty("nodes").EnumerateArray().Single(item =>
        item.GetProperty("name").GetString() == "ET_SpotLight_4_Attachment_16");
      node.TryGetProperty("translation", out _).Should().BeFalse();
      node.EnumerateObject().SelectMany(property => property.Value.ValueKind == JsonValueKind.Array
          ? property.Value.EnumerateArray()
          : Enumerable.Empty<JsonElement>())
        .Where(value => value.ValueKind == JsonValueKind.Number)
        .Should().OnlyContain(value => float.IsFinite(value.GetSingle()));
    }

    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.GetSerializedRepresentation().Should().Equal(sourceBytes);
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
          4)
      },
      new Dictionary<int, StaticLightMshFixture.OmniRecord>
      {
        [4] = new(new Vector3(-4.5f, 5.25f, -6.75f), new Vector3(0.7f, 0.8f, 0.9f), 8)
      },
      activeSpots: [2],
      activeOmnis: [4]);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(glb.ToArray(), RemoveEarthToolMetadata);

    await using var input = new MemoryStream(metadataFree);
    var import = await interchange.ImportNewModelGlbAsync(
      input,
      new GltfNewModelImportOptions(
        staticLightOptions: new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>
        {
          [new GltfLightHandle(1)] = new(targetDistance: 10)
        }));

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var result = import.Value!.Asset.GetSerializedRepresentation().ToArray();
    var spot = StaticLightMshFixture.GetSpot(result, 2);
    new[] { ReadSingle(spot, 0), ReadSingle(spot, 4), ReadSingle(spot, 8) }
      .Should().Equal(1.25f, -2.5f, 3.75f);
    new[] { ReadSingle(spot, 0x0C), ReadSingle(spot, 0x10), ReadSingle(spot, 0x14) }
      .Should().Equal(0.2f, 0.4f, 0.6f);
    ReadSingle(spot, 0x2C).Should().Be(4);
    BinaryPrimitives.ReadInt16LittleEndian(StaticLightMshFixture.GetAttachment(result, 14))
      .Should().NotBe(short.MinValue);
    var omni = StaticLightMshFixture.GetOmni(result, 4);
    new[] { ReadSingle(omni, 0), ReadSingle(omni, 4), ReadSingle(omni, 8) }
      .Should().Equal(-4.5f, 5.25f, -6.75f);
    ReadSingle(omni, 0x18).Should().Be(8);
    BinaryPrimitives.ReadInt16LittleEndian(StaticLightMshFixture.GetAttachment(result, 20))
      .Should().NotBe(short.MinValue);

    await using var contradictorySource = new MemoryStream(metadataFree);
    var contradictory = await interchange.ImportNewModelGlbAsync(
      contradictorySource,
      new GltfNewModelImportOptions(
        staticLightOptions: new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>
        {
          [new GltfLightHandle(1)] = new(targetDistance: 10),
          [new GltfLightHandle(2)] = new(targetDistance: 2)
        }));
    contradictory.Status.Should().Be(OperationStatus.Failed);
    contradictory.Value.Should().BeNull();
  }

  [Fact]
  public async Task GenericSpotLightRequiresExplicitPositiveTargetDistance()
  {
    var asset = await ReadAssetAsync(StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [1] = new(Vector3.Zero, Vector3.One, 0, 0, [0, 0, 0], 0.2f, 5, 0.25f, 4)
      },
      new Dictionary<int, StaticLightMshFixture.OmniRecord>(),
      activeSpots: [1]));
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported, new GltfExportOptions(LineageId, DocumentId));
    var sourceBytes = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      root["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!.AsObject().Remove("range");
    });
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Should().ContainSingle().Subject.Data.Should()
      .Contain(new KeyValuePair<string, string>("domain", "StaticLights"));

    var missingDefinitionBytes = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      var node = root["nodes"]!.AsArray().Single(item =>
        item!["name"]!.GetValue<string>() == "ET_SpotLight_1_Attachment_13")!.AsObject();
      node.Remove("extensions");
    });
    await using var missingDefinitionSource = new MemoryStream(missingDefinitionBytes);
    var missingDefinition = await interchange.ImportNewModelGlbAsync(missingDefinitionSource);
    missingDefinition.Status.Should().Be(OperationStatus.Failed);
    missingDefinition.Value.Should().BeNull();
  }

  [Fact]
  public async Task SpotDirectionEditRegeneratesHeadingAndSlopeWhilePointOrientationIsDisplayOnly()
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [1] = new(
          new Vector3(1, 2, 3),
          new Vector3(0.2f, 0.3f, 0.4f),
          10,
          24,
          [0xA1, 0xB2, 0xC3],
          0.2f,
          5,
          0.4f,
          6)
      },
      new Dictionary<int, StaticLightMshFixture.OmniRecord>
      {
        [2] = new(new Vector3(4, 5, 6), new Vector3(0.5f, 0.6f, 0.7f), 8)
      },
      activeSpots: [1],
      activeOmnis: [2]);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      nodes.Single(node =>
        node!["name"]!.GetValue<string>() == "ET_SpotLight_1_Attachment_13")!["rotation"] =
        new JsonArray(0, 0, 0, 1);
      nodes.Single(node =>
        node!["name"]!.GetValue<string>() == "ET_OmniLight_2_Attachment_18")!["rotation"] =
        new JsonArray(0, MathF.Sin(0.4f), 0, MathF.Cos(0.4f));
    });

    await using var input = new MemoryStream(edited);
    var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var result = import.Value!.Asset.GetSerializedRepresentation().ToArray();
    var spot = StaticLightMshFixture.GetSpot(result, 1);
    spot[0x1C].Should().Be(64);
    spot.AsSpan(0x1D, 3).ToArray().Should().Equal(0xA1, 0xB2, 0xC3);
    ReadSingle(spot, 0x28).Should().Be(0);
    spot.AsSpan(0, 0x1C).ToArray().Should()
      .Equal(StaticLightMshFixture.GetSpot(sourceBytes, 1).AsSpan(0, 0x1C).ToArray());
    spot.AsSpan(0x20, 8).ToArray().Should()
      .Equal(StaticLightMshFixture.GetSpot(sourceBytes, 1).AsSpan(0x20, 8).ToArray());
    StaticLightMshFixture.GetOmni(result, 2).Should().Equal(StaticLightMshFixture.GetOmni(sourceBytes, 2));
  }

  [Fact]
  public async Task CanonicallyNamedLightAdditionUsesFreePhysicalTargetAndIgnoresSceneLighting()
  {
    var sourceBytes = StaticLightMshFixture.Create();
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root =>
    {
      root["extensionsUsed"]!.AsArray().Add("KHR_lights_punctual");
      root["extensions"] = new JsonObject
      {
        ["KHR_lights_punctual"] = new JsonObject
        {
          ["lights"] = new JsonArray
          {
            new JsonObject
            {
              ["name"] = "ET_OmniLight_2_Attachment_18",
              ["type"] = "point",
              ["color"] = new JsonArray(0.25f, 0.5f, 0.75f),
              ["intensity"] = 9f
            },
            new JsonObject
            {
              ["name"] = "Artist key light",
              ["type"] = "point",
              ["intensity"] = 20f
            }
          }
        }
      };
      var nodes = root["nodes"]!.AsArray();
      var rootChildren = nodes[0]!["children"]!.AsArray();
      rootChildren.Add(nodes.Count);
      nodes.Add(new JsonObject
      {
        ["name"] = "ET_OmniLight_2_Attachment_18",
        ["translation"] = new JsonArray(1.25f, 3.75f, -2.5f),
        ["extensions"] = new JsonObject
        {
          ["KHR_lights_punctual"] = new JsonObject { ["light"] = 0 }
        }
      });
      rootChildren.Add(nodes.Count);
      nodes.Add(new JsonObject
      {
        ["name"] = "Artist key light",
        ["extensions"] = new JsonObject
        {
          ["KHR_lights_punctual"] = new JsonObject { ["light"] = 1 }
        }
      });
    });

    await using var input = new MemoryStream(edited);
    var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.SceneLightIgnored
      && diagnostic.Path.StartsWith("nodes[", StringComparison.Ordinal));
    var result = import.Value!.Asset.GetSerializedRepresentation().ToArray();
    var omni = StaticLightMshFixture.GetOmni(result, 2);
    new[] { ReadSingle(omni, 0), ReadSingle(omni, 4), ReadSingle(omni, 8) }
      .Should().Equal(1.25f, -2.5f, 3.75f);
    new[] { ReadSingle(omni, 0x0C), ReadSingle(omni, 0x10), ReadSingle(omni, 0x14) }
      .Should().Equal(0.25f, 0.5f, 0.75f);
    ReadSingle(omni, 0x18).Should().Be(9);
    BinaryPrimitives.ReadInt16LittleEndian(StaticLightMshFixture.GetAttachment(result, 18))
      .Should().NotBe(short.MinValue);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  [Trait("Category", "BlenderQualification")]
  public async Task BlenderRoundTripPreservesCombinedStaticLightRecords(bool separate)
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [1] = new(
          new Vector3(1.125f, -2.25f, 3.5f),
          new Vector3(0.2f, 0.4f, 0.6f),
          10,
          48,
          [0x91, 0xA2, 0xB3],
          0.2f,
          5,
          0.25f,
          4)
      },
      new Dictionary<int, StaticLightMshFixture.OmniRecord>
      {
        [3] = new(new Vector3(-4.25f, 5.5f, -6.75f), new Vector3(0.7f, 0.8f, 0.9f), 8)
      },
      activeSpots: [1],
      activeOmnis: [3]);
    var asset = await ReadAssetAsync(sourceBytes);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
      var interchange = new GltfInterchange();
      var sourcePath = Path.Combine(directory, separate ? "source.gltf" : "source.glb");
      var blenderPath = Path.Combine(directory, separate ? "blender.gltf" : "blender.glb");
      OperationResult<GltfExportReceipt> export = separate
        ? await interchange.ExportGltfFileAsync(
          asset,
          sourcePath,
          new GltfExportOptions(LineageId, DocumentId))
        : await interchange.ExportGlbFileAsync(
          asset,
          sourcePath,
          new GltfExportOptions(LineageId, DocumentId));
      export.Status.Should().Be(OperationStatus.Succeeded);

      var blenderEvidence = await RoundTripThroughBlenderAsync(sourcePath, blenderPath, separate);
      using (var blenderJson = separate
        ? JsonDocument.Parse(await File.ReadAllBytesAsync(blenderPath))
        : ReadGlbJson(await File.ReadAllBytesAsync(blenderPath)))
      {
        var names = blenderJson.RootElement.GetProperty("nodes").EnumerateArray()
          .Select(node => node.TryGetProperty("name", out var name) ? name.GetString() : null)
          .ToArray();
        names.Should().Contain("ET_SpotLight_1_Attachment_13").And
          .Contain("ET_OmniLight_3_Attachment_19");
      }
      OperationResult<GltfEditImportResult> import;
      if (separate)
      {
        import = await interchange.ImportEditGltfFileAsync(blenderPath, export.Value!.Baseline);
      }
      else
      {
        await using var blenderGlb = File.OpenRead(blenderPath);
        import = await interchange.ImportEditGlbAsync(blenderGlb, export.Value!.Baseline);
      }

      import.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
      import.Value!.Asset.GetSerializedRepresentation().Should().Equal(
        sourceBytes,
        string.Join("; ", import.Value.Preservation.Changes
          .Where(change => change.Disposition != PreservationDisposition.Retained)
          .Select(change => $"{change.FieldPath}:{change.Reason}")));
      await RecordBlenderEvidenceAsync(
        blenderEvidence,
        import.Status.ToString(),
        import.Value.Preservation.Changes,
        separate ? "no-edit-separate-gltf" : "no-edit-glb");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task StaticLightSharingDuplicateIdentityAndOccupiedTypeTargetBlockWithoutAsset()
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [1] = new(
          Vector3.One,
          new Vector3(0.2f, 0.3f, 0.4f),
          8,
          32,
          [1, 2, 3],
          0.2f,
          4,
          0.1f,
          2)
      },
      new Dictionary<int, StaticLightMshFixture.OmniRecord>
      {
        [1] = new(new Vector3(4, 5, 6), new Vector3(0.5f, 0.6f, 0.7f), 3)
      },
      activeSpots: [1],
      activeOmnis: [1]);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    var occupied = RewriteJson(glb.ToArray(), root =>
    {
      root["extensions"]!["KHR_lights_punctual"]!["lights"]!.AsArray().Single(light =>
        light!["name"]!.GetValue<string>() == "ET_SpotLight_1_Attachment_13")!["type"] = "point";
    });
    await using var occupiedInput = new MemoryStream(occupied);
    var occupiedResult = await interchange.ImportEditGlbAsync(occupiedInput, export.Value!.Baseline);
    occupiedResult.Status.Should().Be(OperationStatus.Failed);
    occupiedResult.Value.Should().BeNull();
    occupiedResult.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == "ETG2012");

    var duplicate = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var copy = nodes.Single(node =>
        node!["name"]!.GetValue<string>() == "ET_SpotLight_1_Attachment_13")!.DeepClone();
      nodes[0]!["children"]!.AsArray().Add(nodes.Count);
      nodes.Add(copy);
    });
    await using var duplicateInput = new MemoryStream(duplicate);
    var duplicateResult = await interchange.ImportEditGlbAsync(duplicateInput, export.Value.Baseline);
    duplicateResult.Status.Should().Be(OperationStatus.Failed);
    duplicateResult.Value.Should().BeNull();
    duplicateResult.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.DuplicateScopeIdentity);
  }

  [Fact]
  public async Task ReactivationRestoresMatchingInactiveFullStaticLightRecord()
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [2] = new(
          new Vector3(1, 2, 3),
          new Vector3(0.2f, 0.4f, 0.6f),
          10,
          64,
          [0xA1, 0xB2, 0xC3],
          MathF.Tan(0.2f),
          5,
          0,
          4)
      });
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root =>
    {
      root["extensionsUsed"]!.AsArray().Add("KHR_lights_punctual");
      root["extensions"] = new JsonObject
      {
        ["KHR_lights_punctual"] = new JsonObject
        {
          ["lights"] = new JsonArray
          {
            new JsonObject
            {
              ["name"] = "ET_SpotLight_2_Attachment_14",
              ["type"] = "spot",
              ["color"] = new JsonArray(0.2f, 0.4f, 0.6f),
              ["intensity"] = 4f,
              ["spot"] = new JsonObject
              {
                ["innerConeAngle"] = 0.2f,
                ["outerConeAngle"] = 0.5f
              }
            }
          }
        }
      };
      var nodes = root["nodes"]!.AsArray();
      nodes[0]!["children"]!.AsArray().Add(nodes.Count);
      nodes.Add(new JsonObject
      {
        ["name"] = "ET_SpotLight_2_Attachment_14",
        ["translation"] = new JsonArray(1, 3, 2),
        ["extensions"] = new JsonObject
        {
          ["KHR_lights_punctual"] = new JsonObject { ["light"] = 0 }
        }
      });
    });

    await using var input = new MemoryStream(edited);
    var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    var result = import.Value!.Asset.GetSerializedRepresentation().ToArray();
    StaticLightMshFixture.GetSpot(result, 2).Should().Equal(StaticLightMshFixture.GetSpot(sourceBytes, 2));
    BinaryPrimitives.ReadInt16LittleEndian(StaticLightMshFixture.GetAttachment(result, 14))
      .Should().NotBe(short.MinValue);
    import.Value.Preservation.Changes.Should().NotContain(change =>
      change.FieldPath.StartsWith("CommonBaseHeader.StaticSpotLights[2]", StringComparison.Ordinal));
  }

  [Fact]
  public async Task SparseAttachmentsAndCannonPositionsProjectIndependentlyAndRestoreExactRecords()
  {
    var activeNumbers = new[] { 1, 5, 9, 21, 25, 29, 33, 37, 39, 41, 43, 45, 46, 47, 48, 49 };
    var attachments = activeNumbers.ToDictionary(
      number => number,
      number => new AttachmentAndCannonMshFixture.AttachmentRecord(
        checked((short)(number * 17)),
        number == 21 ? short.MinValue : checked((short)(number * -11)),
        checked((short)(number * 7)),
        unchecked((byte)(number * 13)),
        unchecked((byte)(0x80 + number))));
    attachments.Add(13, new AttachmentAndCannonMshFixture.AttachmentRecord(10, 20, 30, 40, 50));
    attachments.Add(17, new AttachmentAndCannonMshFixture.AttachmentRecord(11, 21, 31, 41, 51));
    attachments.Add(44, new AttachmentAndCannonMshFixture.AttachmentRecord(
      short.MinValue,
      123,
      -456,
      255,
      0xA5));
    var cannonPositions = Enumerable.Range(1, 4).ToDictionary(
      number => number,
      number => new Vector3(number + 0.125f, -number - 0.25f, number + 0.5f));
    var sourceBytes = AttachmentAndCannonMshFixture.Create(attachments, cannonPositions);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      var helperNames = json.RootElement.GetProperty("nodes").EnumerateArray()
        .Where(node => !node.TryGetProperty("mesh", out _))
        .Select(node => node.GetProperty("name").GetString())
        .ToArray();
      helperNames.Should().Contain(activeNumbers.Select(GlbDocument.GetAttachmentHelperName));
      helperNames.Should().NotContain(GlbDocument.GetAttachmentHelperName(13));
      helperNames.Should().NotContain(GlbDocument.GetAttachmentHelperName(17));
      helperNames.Should().NotContain(GlbDocument.GetAttachmentHelperName(44));
      helperNames.Should().Contain(Enumerable.Range(1, 4)
        .Select(number => $"ET_CannonRenderPosition_{number}"));
    }

    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    import.Value!.Asset.GetSerializedRepresentation().Should().Equal(sourceBytes);
  }

  [Fact]
  public async Task AttachmentPoseEditRegeneratesOnlyPoseAndLeavesExtraCannonAndMarkerRoleExact()
  {
    const int markerFlags = 0x00001000;
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [5] = new(256, -512, 768, 64, 0xB1)
      },
      new Dictionary<int, Vector3> { [1] = new(9.25f, -8.5f, 7.75f) },
      markerFlags);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root =>
    {
      var helper = root["nodes"]!.AsArray().Single(node =>
        node!["name"]!.GetValue<string>() == GlbDocument.GetAttachmentHelperName(5))!.AsObject();
      helper["translation"] = new JsonArray(1.003f, -3.999f, 2.007f);
      helper["rotation"] = new JsonArray(0, MathF.Sin(MathF.PI / 4), 0, MathF.Cos(MathF.PI / 4));
    });

    await using var input = new MemoryStream(edited);
    var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    var resultBytes = import.Value!.Asset.GetSerializedRepresentation().ToArray();
    var record = AttachmentAndCannonMshFixture.GetAttachment(resultBytes, 5);
    BinaryPrimitives.ReadInt16LittleEndian(record).Should().Be(256);
    BinaryPrimitives.ReadInt16LittleEndian(record.AsSpan(2)).Should().Be(513);
    BinaryPrimitives.ReadInt16LittleEndian(record.AsSpan(4)).Should().Be(-1023);
    record[6].Should().Be(128);
    record[7].Should().Be(0xB1);
    AttachmentAndCannonMshFixture.GetCannonRenderPosition(resultBytes, 1)
      .Should().Equal(AttachmentAndCannonMshFixture.GetCannonRenderPosition(sourceBytes, 1));
    BinaryPrimitives.ReadUInt32LittleEndian(resultBytes.AsSpan(0x14 + 0x368 + 4 + 0xA8))
      .Should().Be(markerFlags);
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "CommonBaseHeader.AttachmentTable[5]"
      && change.Disposition == PreservationDisposition.Regenerated);
    import.Value.Preservation.Changes.Should().NotContain(change =>
      change.FieldPath.StartsWith("CommonBaseHeader.CannonRenderPositions", StringComparison.Ordinal)
      && change.Disposition != PreservationDisposition.Retained);
    import.Value.RestoredSerializedRepresentationPaths.Should()
      .Contain("CommonBaseHeader.CannonRenderPositions[1]")
      .And.NotContain("CommonBaseHeader.AttachmentTable[5]");
  }

  [Fact]
  public async Task CannonPositionEditDoesNotChangeCannonAttachment()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [1] = new(100, 200, 300, 64, 0x80)
      },
      new Dictionary<int, Vector3> { [1] = new(1.25f, 2.5f, 3.75f) });
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root =>
    {
      var helper = root["nodes"]!.AsArray().Single(node =>
        node!["name"]!.GetValue<string>() == "ET_CannonRenderPosition_1")!.AsObject();
      helper["translation"] = new JsonArray(4.5f, 6.5f, -5.5f);
      helper["rotation"] = new JsonArray(0, 0.25f, 0, 0.9682458f);
      helper["scale"] = new JsonArray(2, 3, 4);
    });

    await using var input = new MemoryStream(edited);
    var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    var resultBytes = import.Value!.Asset.GetSerializedRepresentation().ToArray();
    AttachmentAndCannonMshFixture.GetAttachment(resultBytes, 1)
      .Should().Equal(AttachmentAndCannonMshFixture.GetAttachment(sourceBytes, 1));
    var cannon = AttachmentAndCannonMshFixture.GetCannonRenderPosition(resultBytes, 1);
    ReadSingle(cannon, 0).Should().Be(4.5f);
    ReadSingle(cannon, 4).Should().Be(-5.5f);
    ReadSingle(cannon, 8).Should().Be(6.5f);
  }

  [Fact]
  public async Task NonFiniteCannonPositionUsesWarnedFinitePreviewAndRestoresExactBits()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      cannonRenderPositions: new Dictionary<int, Vector3>
      {
        [3] = new(float.NaN, float.PositiveInfinity, float.NegativeInfinity)
      });
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    export.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.CannonRenderPositionPreviewSubstituted
      && diagnostic.Path == "CommonBaseHeader.CannonRenderPositions[3]");
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      json.RootElement.GetProperty("nodes").EnumerateArray().Single(node =>
          node.GetProperty("name").GetString() == "ET_CannonRenderPosition_3")
        .TryGetProperty("translation", out _).Should().BeFalse();
    }
    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    AttachmentAndCannonMshFixture.GetCannonRenderPosition(
      import.Value!.Asset.GetSerializedRepresentation(),
      3).Should().Equal(AttachmentAndCannonMshFixture.GetCannonRenderPosition(sourceBytes, 3));
  }

  [Fact]
  public async Task AttachmentDeletionAdditionAndOccupiedTargetHaveDeterministicOutcomes()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [21] = new(100, 200, 300, 64, 0xA5)
      });
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var rebound = nodes.Single(node =>
        node!["name"]!.GetValue<string>() == GlbDocument.GetAttachmentHelperName(21))!.AsObject();
      rebound["name"] = GlbDocument.GetAttachmentHelperName(22);
      var metadata = JsonNode.Parse(rebound["extras"]!["earthtool"]!.GetValue<string>())!.AsObject();
      metadata["payload"]!["attachment"]!["physicalNumber"] = 22;
      rebound["extras"]!["earthtool"] = metadata.ToJsonString();
      rebound["translation"] = new JsonArray(2, 3, 4);
    });

    await using var input = new MemoryStream(edited);
    var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    import.Value!.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "CommonBaseHeader.AttachmentTable[21]"
      && change.Disposition == PreservationDisposition.Canonicalized
      && change.Reason == "AttachmentDeletion");
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "CommonBaseHeader.AttachmentTable[22]"
      && change.Disposition == PreservationDisposition.Canonicalized
      && change.Reason == "AttachmentAddition");
    var resultBytes = import.Value!.Asset.GetSerializedRepresentation().ToArray();
    AttachmentAndCannonMshFixture.GetAttachment(resultBytes, 21)
      .Should().Equal(0, 128, 0, 128, 0, 128, 0, 0);
    var added = AttachmentAndCannonMshFixture.GetAttachment(resultBytes, 22);
    BinaryPrimitives.ReadInt16LittleEndian(added).Should().Be(512);
    BinaryPrimitives.ReadInt16LittleEndian(added.AsSpan(2)).Should().Be(1024);
    BinaryPrimitives.ReadInt16LittleEndian(added.AsSpan(4)).Should().Be(768);
    added[6].Should().Be(64);
    added[7].Should().Be(0xA5);

    var occupied = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var rootNode = nodes[0]!.AsObject();
      var addedIndex = nodes.Count;
      nodes.Add(new JsonObject { ["name"] = GlbDocument.GetAttachmentHelperName(21) });
      rootNode["children"]!.AsArray().Add(addedIndex);
    });
    await using var occupiedInput = new MemoryStream(occupied);
    var conflict = await interchange.ImportEditGlbAsync(occupiedInput, export.Value.Baseline);
    conflict.Status.Should().Be(OperationStatus.Failed);
    conflict.Value.Should().BeNull();
    conflict.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == "ETG2012");
  }

  [Fact]
  public async Task AttachmentPitchOrRollBlocksWithoutReturningAnAsset()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [49] = new(256, 512, 768, 64, 0x91)
      });
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    foreach (var rotation in new[]
    {
      new Quaternion(MathF.Sin(0.125f), 0, 0, MathF.Cos(0.125f)),
      new Quaternion(0, 0, MathF.Sin(0.125f), MathF.Cos(0.125f))
    })
    {
      var edited = RewriteJson(glb.ToArray(), root =>
      {
        var helper = root["nodes"]!.AsArray().Single(node =>
          node!["name"]!.GetValue<string>() == GlbDocument.GetAttachmentHelperName(49))!.AsObject();
        helper["rotation"] = new JsonArray(rotation.X, rotation.Y, rotation.Z, rotation.W);
      });

      await using var input = new MemoryStream(edited);
      var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);

      import.Status.Should().Be(OperationStatus.Failed);
      import.Value.Should().BeNull();
      import.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == "ETG1002");
    }
  }

  [Fact]
  public async Task AttachmentCopyRequiresForkedIdentityAndAFreePhysicalTarget()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [21] = new(256, 512, 768, 64, 0x93)
      });
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var duplicateIdentity = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var copy = nodes.Single(node =>
        node!["name"]!.GetValue<string>() == GlbDocument.GetAttachmentHelperName(21))!.DeepClone();
      nodes.Add(copy);
      root["nodes"]![0]!["children"]!.AsArray().Add(nodes.Count - 1);
    });
    await using var duplicateInput = new MemoryStream(duplicateIdentity);

    var duplicate = await interchange.ImportEditGlbAsync(duplicateInput, export.Value!.Baseline);

    duplicate.Status.Should().Be(OperationStatus.Failed);
    duplicate.Value.Should().BeNull();
    duplicate.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.DuplicateScopeIdentity);

    var forked = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var copy = nodes.Single(node =>
        node!["name"]!.GetValue<string>() == GlbDocument.GetAttachmentHelperName(21))!.DeepClone().AsObject();
      copy.Remove("extras");
      copy["name"] = GlbDocument.GetAttachmentHelperName(22);
      nodes.Add(copy);
      root["nodes"]![0]!["children"]!.AsArray().Add(nodes.Count - 1);
    });
    await using var forkedInput = new MemoryStream(forked);
    var fork = await interchange.ImportEditGlbAsync(forkedInput, export.Value.Baseline);

    fork.Status.Should().Be(OperationStatus.Succeeded);
    var forkedBytes = fork.Value!.Asset.GetSerializedRepresentation().ToArray();
    AttachmentAndCannonMshFixture.GetAttachment(forkedBytes, 21)
      .Should().Equal(AttachmentAndCannonMshFixture.GetAttachment(sourceBytes, 21));
    var copiedRecord = AttachmentAndCannonMshFixture.GetAttachment(forkedBytes, 22);
    copiedRecord.Take(7).Should().Equal(
      AttachmentAndCannonMshFixture.GetAttachment(sourceBytes, 21).Take(7));
    copiedRecord[7].Should().Be(0x80);
  }

  [Fact]
  public async Task AttachmentSentinelCollisionAndSignedFixedPointOverflowBlock()
  {
    var asset = await ReadAssetAsync(AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [49] = new(1, 2, 3, 64, 0x80)
      }));
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    foreach (var x in new[] { -128f, 128f })
    {
      var edited = RewriteJson(glb.ToArray(), root =>
      {
        var helper = root["nodes"]!.AsArray().Single(node =>
          node!["name"]!.GetValue<string>() == GlbDocument.GetAttachmentHelperName(49))!.AsObject();
        helper["translation"] = new JsonArray(x, 0, 0);
      });
      await using var input = new MemoryStream(edited);
      var import = await interchange.ImportEditGlbAsync(input, export.Value!.Baseline);
      import.Status.Should().Be(OperationStatus.Failed);
      import.Value.Should().BeNull();
      import.Diagnostics.Should().ContainSingle(diagnostic => diagnostic.Code == "ETG1002");
    }
  }

  [Fact]
  public async Task GenericNamedHelpersAuthorAttachmentsAndCannonPositions()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [47] = new(256, 512, -768, 192, 0x80)
      },
      new Dictionary<int, Vector3> { [2] = new(1.25f, -2.5f, 3.75f) });
    var asset = await ReadAssetAsync(sourceBytes);
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      asset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    imported.Diagnostics.Should().NotContain(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.SceneLightIgnored);
    var resultBytes = imported.Value!.Asset.GetSerializedRepresentation().ToArray();
    AttachmentAndCannonMshFixture.GetAttachment(resultBytes, 47)
      .Should().Equal(AttachmentAndCannonMshFixture.GetAttachment(sourceBytes, 47));
    AttachmentAndCannonMshFixture.GetCannonRenderPosition(resultBytes, 2)
      .Should().Equal(AttachmentAndCannonMshFixture.GetCannonRenderPosition(sourceBytes, 2));
  }

  [Fact]
  public async Task TypedHelperAndLightBindingsDoNotDependOnDisplayNames()
  {
    var sourceBytes = StaticLightMshFixture.Create(
      new Dictionary<int, StaticLightMshFixture.SpotRecord>
      {
        [2] = new(Vector3.Zero, Vector3.One, 0, 0, [0, 0, 0], 0.2f, 5, 0.25f, 4)
      },
      new Dictionary<int, StaticLightMshFixture.OmniRecord>(),
      activeSpots: [2]);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var exported = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(asset, exported, new GltfExportOptions(LineageId, DocumentId));
    GltfNodeHandle lightNode = default;
    var generic = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      var nodes = root["nodes"]!.AsArray();
      var nodeIndex = nodes.Select((node, index) => (node, index)).Single(item =>
        item.node!["name"]!.GetValue<string>() == "ET_SpotLight_2_Attachment_14").index;
      lightNode = new GltfNodeHandle(nodeIndex + 1);
      nodes[nodeIndex]!["name"] = "Artist Key Light";
      var light = root["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!.AsObject();
      light["name"] = "Display Light";
      light.Remove("range");
    });
    await using var source = new MemoryStream(generic);

    var imported = await interchange.ImportNewModelGlbAsync(
      source,
      new GltfNewModelImportOptions(
        helperBindings: new Dictionary<GltfNodeHandle, GltfNewModelHelperBinding>
        {
          [lightNode] = new(GltfNewModelHelperKind.SpotLight, 2)
        },
        staticLightOptions: new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>
        {
          [new GltfLightHandle(1)] = new(targetDistance: 7)
        }));

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    imported.Diagnostics.Should().NotContain(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.SceneLightIgnored);
    ReadSingle(StaticLightMshFixture.GetSpot(
      imported.Value!.Asset.GetSerializedRepresentation().ToArray(), 2), 0x18).Should().Be(7);
  }

  [Fact]
  public async Task SeparateGltfPreservesAttachmentAndCannonRecords()
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [1] = new(111, -222, 333, 255, 0xA7),
        [49] = new(444, short.MinValue, -555, 0, 0xE1)
      },
      new Dictionary<int, Vector3> { [4] = new(7.25f, -8.5f, 9.75f) });
    var asset = await ReadAssetAsync(sourceBytes);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);

      import.Status.Should().Be(OperationStatus.Succeeded);
      import.Value!.Asset.GetSerializedRepresentation().Should().Equal(sourceBytes);

      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveEarthToolMetadata(root);
      await File.WriteAllTextAsync(path, root.ToJsonString());
      var generic = await interchange.ImportNewModelGltfFileAsync(path);

      generic.Status.Should().Be(OperationStatus.Succeeded);
      var genericBytes = generic.Value!.Asset.GetSerializedRepresentation().ToArray();
      AttachmentAndCannonMshFixture.GetAttachment(genericBytes, 1).Take(7).Should()
        .Equal(AttachmentAndCannonMshFixture.GetAttachment(sourceBytes, 1).Take(7));
      AttachmentAndCannonMshFixture.GetAttachment(genericBytes, 49).Take(7).Should()
        .Equal(AttachmentAndCannonMshFixture.GetAttachment(sourceBytes, 49).Take(7));
      AttachmentAndCannonMshFixture.GetCannonRenderPosition(genericBytes, 4).Should()
        .Equal(AttachmentAndCannonMshFixture.GetCannonRenderPosition(sourceBytes, 4));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task MaximumNonLightAttachmentCapacityRoundTripsWithoutCompaction()
  {
    var attachments = Enumerable.Range(1, 49)
      .Where(number => number is not (>= 13 and <= 20))
      .ToDictionary(
        number => number,
        number => new AttachmentAndCannonMshFixture.AttachmentRecord(
          checked((short)number),
          checked((short)-number),
          checked((short)(number * 2)),
          unchecked((byte)(number * 5)),
          unchecked((byte)(number * 7))));
    var sourceBytes = AttachmentAndCannonMshFixture.Create(attachments);
    var asset = await ReadAssetAsync(sourceBytes);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.CommonBaseHeader.AttachmentTable.Should()
      .Equal(asset.CommonBaseHeader.AttachmentTable);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  [Trait("Category", "BlenderQualification")]
  public async Task BlenderRoundTripPreservesAttachmentAndCannonRecords(bool separate)
  {
    var sourceBytes = AttachmentAndCannonMshFixture.Create(
      new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
      {
        [3] = new(321, -654, 987, 192, 0xB4),
        [48] = new(-123, 456, -789, 1, 0xE7)
      },
      new Dictionary<int, Vector3> { [3] = new(1.125f, -2.25f, 3.5f) });
    var asset = await ReadAssetAsync(sourceBytes);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
      var interchange = new GltfInterchange();
      var inputPath = Path.Combine(directory, separate ? "source.gltf" : "source.glb");
      var outputPath = Path.Combine(directory, separate ? "blender.gltf" : "blender.glb");
      OperationResult<GltfExportReceipt> export = separate
        ? await interchange.ExportGltfFileAsync(
          asset,
          inputPath,
          new GltfExportOptions(LineageId, DocumentId))
        : await interchange.ExportGlbFileAsync(
          asset,
          inputPath,
          new GltfExportOptions(LineageId, DocumentId));
      export.Status.Should().Be(OperationStatus.Succeeded);

      var blenderEvidence = await RoundTripThroughBlenderAsync(inputPath, outputPath, separate);
      OperationResult<GltfEditImportResult> import;
      if (separate)
      {
        import = await interchange.ImportEditGltfFileAsync(outputPath, export.Value!.Baseline);
      }
      else
      {
        await using var blenderGlb = File.OpenRead(outputPath);
        import = await interchange.ImportEditGlbAsync(blenderGlb, export.Value!.Baseline);
      }

      import.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
      import.Value!.Asset.GetSerializedRepresentation().Should().Equal(sourceBytes);
      await RecordBlenderEvidenceAsync(
        blenderEvidence,
        import.Status.ToString(),
        import.Value.Preservation.Changes,
        separate ? "no-edit-separate-gltf" : "no-edit-glb");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Theory]
  [InlineData("hierarchy")]
  [InlineData("geometry")]
  [InlineData("material")]
  [InlineData("animation")]
  [InlineData("attachment")]
  [InlineData("light")]
  [InlineData("metadata-loss")]
  [InlineData("branch")]
  [InlineData("ambiguity")]
  [InlineData("stale")]
  [Trait("Category", "BlenderQualification")]
  public async Task BlenderEditsPassOwnershipAwareOracle(string scenario)
  {
    var sourceBytes = scenario switch
    {
      "hierarchy" or "material" => StaticMeshSequenceFixture.CreateInterleavedWithoutTextures().Data,
      "animation" => StaticAnimationMshFixture.Create(
        0,
        new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
        translations: [Vector3.Zero, Vector3.One],
        matrices: [Matrix4x4.Identity, Matrix4x4.CreateRotationZ(0.25f)]),
      "attachment" => AttachmentAndCannonMshFixture.Create(
        new Dictionary<int, AttachmentAndCannonMshFixture.AttachmentRecord>
        {
          [3] = new(256, -512, 768, 32, 0xA5)
        }),
      "light" => StaticLightMshFixture.Create(
        spots: new Dictionary<int, StaticLightMshFixture.SpotRecord>
        {
          [1] = new(
            Vector3.One,
            new Vector3(0.25f, 0.5f, 0.75f),
            8,
            32,
            [1, 2, 3],
            0.25f,
            4,
            0.5f,
            2)
        },
        activeSpots: [1]),
      _ => OneTriangleMshFixture.Create()
    };
    var asset = await ReadAssetAsync(sourceBytes);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    try
    {
      var sourcePath = Path.Combine(directory, "source.glb");
      var blenderPath = Path.Combine(directory, "blender.glb");
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGlbFileAsync(
        asset,
        sourcePath,
        new GltfExportOptions(LineageId, DocumentId));
      export.Status.Should().Be(OperationStatus.Succeeded);
      var blenderEvidence = await RoundTripThroughBlenderAsync(
        sourcePath,
        blenderPath,
        separate: false,
        scenario);
      var expectedBaseline = scenario == "branch"
        ? new InterchangeBaseline(LineageId, Guid.NewGuid())
        : export.Value!.Baseline;
      await using var blenderGlb = File.OpenRead(blenderPath);
      var import = await interchange.ImportEditGlbAsync(blenderGlb, expectedBaseline);

      if (scenario is "metadata-loss" or "branch" or "ambiguity" or "stale")
      {
        var expectedCode = scenario switch
        {
          "metadata-loss" => GltfDiagnosticCodes.MissingManifest,
          "branch" => GltfDiagnosticCodes.DocumentMismatch,
          "ambiguity" => GltfDiagnosticCodes.DuplicateScopeIdentity,
          _ => GltfDiagnosticCodes.StaleNativeProjection
        };
        import.Status.Should().Be(OperationStatus.Failed);
        import.Value.Should().BeNull();
        import.Diagnostics.Should().Contain(diagnostic => diagnostic.Code == expectedCode);
        await RecordBlenderEvidenceAsync(
          blenderEvidence,
          expectedCode,
          Array.Empty<PreservationChange>(),
          scenario);
        return;
      }

      import.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
      var changes = import.Value!.Preservation.Changes;
      AssertOnlyExpectedBlenderChanges(scenario, changes);
      switch (scenario)
      {
        case "hierarchy":
          changes.Should().Contain(change =>
            change.FieldPath == "StaticRenderObjectSequence"
            && change.Disposition == PreservationDisposition.Regenerated);
          break;
        case "geometry":
          changes.Should().Contain(change =>
            change.FieldPath.EndsWith(".RenderVertices", StringComparison.Ordinal)
            && change.Disposition == PreservationDisposition.Regenerated);
          break;
        case "material":
          import.Value.Asset.StaticRenderObjectSequence.Select(record => record.TexturePathBytes)
            .Should().Equal(asset.StaticRenderObjectSequence.Select(record => record.TexturePathBytes));
          break;
        case "animation":
          changes.Should().Contain(change =>
            change.FieldPath.EndsWith(".AnimationTracks.TranslationFrames", StringComparison.Ordinal)
            && change.Disposition == PreservationDisposition.Regenerated);
          break;
        case "attachment":
          changes.Should().Contain(change =>
            change.FieldPath == "CommonBaseHeader.AttachmentTable[3]"
            && change.Disposition == PreservationDisposition.Regenerated);
          break;
        case "light":
          changes.Should().Contain(change =>
            change.FieldPath.StartsWith("CommonBaseHeader.StaticSpotLights[1]", StringComparison.Ordinal)
            && change.Disposition == PreservationDisposition.Regenerated);
          break;
      }
      await RecordBlenderEvidenceAsync(blenderEvidence, import.Status.ToString(), changes, scenario);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Theory]
  [InlineData(0, "EarthTool A")]
  [InlineData(1, "EarthTool B")]
  [InlineData(2, "EarthTool C")]
  [InlineData(3, "EarthTool D")]
  public async Task EffectiveAnimationClassesExportDenseNativeTrsAt24Fps(
    uint animationClassValue,
    string expectedName)
  {
    var lengths = animationClassValue switch
    {
      0 => new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      1 => new StaticAnimationMshFixture.AnimationLengths(0, 2, 0, 0),
      2 => new StaticAnimationMshFixture.AnimationLengths(0, 0, 2, 0),
      _ => new StaticAnimationMshFixture.AnimationLengths(0, 0, 0, 2)
    };
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      animationClassValue,
      lengths,
      [Vector3.One, new Vector3(2, 3, 4)],
      [new Vector3(1, 2, 3), new Vector3(4, 5, 6)],
      [Matrix4x4.Identity, Matrix4x4.CreateRotationZ(MathF.PI / 2)]));
    await using var glb = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    using var json = ReadGlbJson(glb.ToArray());
    var animations = json.RootElement.GetProperty("animations");
    animations.GetArrayLength().Should().Be(1);
    animations[0].GetProperty("name").GetString().Should().Be(expectedName);
    animations[0].GetProperty("channels").EnumerateArray()
      .Select(channel => channel.GetProperty("target").GetProperty("path").GetString())
      .Should().BeEquivalentTo(["translation", "rotation", "scale"]);
    var timeAccessor = animations[0].GetProperty("samplers")[0].GetProperty("input").GetInt32();
    ReadFloatAccessor(glb.ToArray(), json.RootElement, timeAccessor, 1)
      .Should().Equal(0, 1f / 24f);
    var rotationChannel = animations[0].GetProperty("channels").EnumerateArray()
      .Single(channel => channel.GetProperty("target").GetProperty("path").GetString() == "rotation");
    var rotationSampler = animations[0].GetProperty("samplers")
      [rotationChannel.GetProperty("sampler").GetInt32()];
    var rotations = ReadFloatAccessor(
      glb.ToArray(),
      json.RootElement,
      rotationSampler.GetProperty("output").GetInt32(),
      4);
    for (var frame = 0; frame < 2; frame++)
    {
      var rotation = new Quaternion(
        rotations[frame * 4],
        rotations[frame * 4 + 1],
        rotations[frame * 4 + 2],
        rotations[frame * 4 + 3]);
      rotation.Length().Should().BeApproximately(1, 1e-6f);
      rotation.W.Should().BeGreaterThanOrEqualTo(0);
    }
  }

  [Fact]
  public async Task StoredXAxisAnimationRotationReflectsDecodedYAxisOnExport()
  {
    const float angle = 0.5f;
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(1, 0, 0, 0),
      matrices: [Matrix4x4.CreateRotationX(angle)]));
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();

    var result = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    result.Status.Should().Be(OperationStatus.Succeeded);
    var bytes = glb.ToArray();
    using var json = ReadGlbJson(bytes);
    var animation = json.RootElement.GetProperty("animations")[0];
    var rotationChannel = animation.GetProperty("channels").EnumerateArray()
      .Single(channel => channel.GetProperty("target").GetProperty("path").GetString()
        == "rotation");
    var outputAccessor = animation.GetProperty("samplers")
      [rotationChannel.GetProperty("sampler").GetInt32()].GetProperty("output").GetInt32();
    var rotation = ReadFloatAccessor(bytes, json.RootElement, outputAccessor, 4);

    rotation[0].Should().BeApproximately(-MathF.Sin(angle / 2), 1e-6f);
    rotation[1].Should().BeApproximately(0, 1e-6f);
    rotation[2].Should().BeApproximately(0, 1e-6f);
    rotation[3].Should().BeApproximately(MathF.Cos(angle / 2), 1e-6f);

    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(glb, result.Value!.Baseline);
    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.StaticRenderObjectSequence[0].AnimationTracks.Matrices.Should()
      .Equal(asset.StaticRenderObjectSequence[0].AnimationTracks.Matrices);

    const float editedAngle = 0.75f;
    var editedRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, editedAngle);
    var rotationOffset = GetFloatAccessorOffset(bytes, json.RootElement, outputAccessor);
    var components = new[]
    {
      editedRotation.X,
      editedRotation.Y,
      editedRotation.Z,
      editedRotation.W
    };
    for (var index = 0; index < components.Length; index++)
    {
      BinaryPrimitives.WriteInt32LittleEndian(
        bytes.AsSpan(rotationOffset + index * sizeof(float)),
        BitConverter.SingleToInt32Bits(components[index]));
    }
    await using var editedGlb = new MemoryStream(bytes);
    var editedImport = await interchange.ImportEditGlbAsync(editedGlb, result.Value.Baseline);

    editedImport.Status.Should().Be(OperationStatus.Succeeded);
    var storedMatrix = editedImport.Value!.Asset.StaticRenderObjectSequence[0]
      .AnimationTracks.Matrices.Single();
    Matrix4x4.Decompose(storedMatrix, out _, out var storedRotation, out _).Should().BeTrue();
    storedRotation.X.Should().BeApproximately(-MathF.Sin(editedAngle / 2), 1e-6f);
    storedRotation.Y.Should().BeApproximately(0, 1e-6f);
    storedRotation.Z.Should().BeApproximately(0, 1e-6f);
    storedRotation.W.Should().BeApproximately(MathF.Cos(editedAngle / 2), 1e-6f);
  }

  [Fact]
  public async Task AbsentTracksDoNotEmitAnEmptyAnimation()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(8, 0, 0, 0)));
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    using var json = ReadGlbJson(glb.ToArray());
    json.RootElement.TryGetProperty("animations", out _).Should().BeFalse();
    using var objectMetadata = JsonDocument.Parse(json.RootElement.GetProperty("nodes")[0]
      .GetProperty("extras").GetProperty("earthtool").GetString()!);
    var preservedAnimation = objectMetadata.RootElement.GetProperty("payload")
      .GetProperty("staticAnimation");
    preservedAnimation.GetProperty("status").GetString().Should().Be("absent");
    preservedAnimation.GetProperty("animationClassValue").GetUInt32().Should().Be(0);
    GlbDocument.DecodeBase64Url(preservedAnimation.GetProperty("scaleFrames").GetString()!, int.MaxValue)
      .Should().BeEmpty();
    GlbDocument.DecodeBase64Url(
      preservedAnimation.GetProperty("translationFrames").GetString()!, int.MaxValue)
      .Should().BeEmpty();
    GlbDocument.DecodeBase64Url(preservedAnimation.GetProperty("matrices").GetString()!, int.MaxValue)
      .Should().BeEmpty();
  }

  [Fact]
  public async Task UnrecognizedClassWithoutTracksRemainsWarningBearingMetadata()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      5,
      new StaticAnimationMshFixture.AnimationLengths(0, 8, 0, 0)));
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    export.Diagnostics.Should().ContainSingle().Which.Code.Should()
      .Be(GltfDiagnosticCodes.AnimationClassUnrecognized);
    using var json = ReadGlbJson(glb.ToArray());
    json.RootElement.TryGetProperty("animations", out _).Should().BeFalse();
    using var objectMetadata = JsonDocument.Parse(json.RootElement.GetProperty("nodes")[0]
      .GetProperty("extras").GetProperty("earthtool").GetString()!);
    objectMetadata.RootElement.GetProperty("payload").GetProperty("staticAnimation")
      .GetProperty("animationClassValue").GetUInt32().Should().Be(5);
  }

  [Fact]
  public async Task ZeroLengthPresentTrackProjectsOnlyEffectiveFrameZero()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      default,
      translations: [new Vector3(1, 2, 3)]));
    await using var glb = new MemoryStream();

    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    using var json = ReadGlbJson(glb.ToArray());
    var animation = json.RootElement.GetProperty("animations")[0];
    var timeAccessor = animation.GetProperty("samplers")[0].GetProperty("input").GetInt32();
    ReadFloatAccessor(glb.ToArray(), json.RootElement, timeAccessor, 1).Should().Equal(0);
  }

  [Fact]
  public async Task ConstantPresentTracksRemainExplicitAndRestoreExactly()
  {
    var scales = new[] { Vector3.One, Vector3.One };
    var translations = new[] { new Vector3(2, 3, 4), new Vector3(2, 3, 4) };
    var matrices = new[] { Matrix4x4.Identity, Matrix4x4.Identity };
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      scales,
      translations,
      matrices));
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      json.RootElement.GetProperty("animations")[0].GetProperty("channels").EnumerateArray()
        .Select(channel => channel.GetProperty("target").GetProperty("path").GetString())
        .Should().BeEquivalentTo(["translation", "rotation", "scale"]);
    }
    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);
    var restored = import.Value!.Asset.StaticRenderObjectSequence[0].AnimationTracks;
    restored.ScaleFrames.Should().Equal(scales);
    restored.TranslationFrames.Should().Equal(translations);
    restored.Matrices.Should().Equal(matrices);
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
      translations: [Vector3.Zero]);
    await using var stream = new MemoryStream(source);

    var read = await new MshReader().ReadAsync(stream);

    read.Status.Should().Be(OperationStatus.Failed);
    read.Value.Should().BeNull();
    read.Diagnostics.Should().ContainSingle().Which.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == MshDiagnosticCodes.StructuralHazard
      && diagnostic.Path == "StaticRenderObjectSequence[0].AnimationTracks.TranslationFrames");
  }

  [Fact]
  public async Task NondecomposableAnimationRemainsMetadataOnlyWithDeterministicDiagnostic()
  {
    var shear = Matrix4x4.Identity;
    shear.M12 = 0.5f;
    var source = StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      matrices: [Matrix4x4.Identity, shear]);
    var asset = await ReadAssetAsync(source);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    export.Diagnostics.Should().ContainSingle().Which.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == "ETG1014"
      && diagnostic.Severity == DiagnosticSeverity.Warning
      && diagnostic.Path == "StaticRenderObjectSequence[0].AnimationTracks"
      && diagnostic.Data["class"] == "A"
      && diagnostic.Data["frame"] == "1");
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      json.RootElement.TryGetProperty("animations", out _).Should().BeFalse();
    }

    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);
    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.StaticRenderObjectSequence[0].AnimationTracks.Matrices
      .Should().Equal(asset.StaticRenderObjectSequence[0].AnimationTracks.Matrices);
  }

  [Fact]
  public async Task UnrecognizedClassUsesModuloFourDomainAndLongTailRestoresExactly()
  {
    var translations = new[]
    {
      new Vector3(1, 2, 3),
      new Vector3(4, 5, 6),
      new Vector3(7, 8, 9)
    };
    var source = StaticAnimationMshFixture.Create(
      5,
      new StaticAnimationMshFixture.AnimationLengths(0, 2, 0, 0),
      translations: translations,
      frameIndices: new StaticAnimationMshFixture.AnimationLengths(3, 4, 5, 6));
    var asset = await ReadAssetAsync(source);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    export.Diagnostics.Should().ContainSingle().Which.Code.Should()
      .Be(GltfDiagnosticCodes.AnimationClassUnrecognized);
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      var animation = json.RootElement.GetProperty("animations").EnumerateArray().Single();
      animation.GetProperty("name").GetString().Should().Be("EarthTool B");
      var timeAccessor = animation.GetProperty("samplers")[0].GetProperty("input").GetInt32();
      ReadFloatAccessor(glb.ToArray(), json.RootElement, timeAccessor, 1)
        .Should().Equal(0, 1f / 24f);
      using var objectMetadata = JsonDocument.Parse(json.RootElement.GetProperty("nodes")[0]
        .GetProperty("extras").GetProperty("earthtool").GetString()!);
      var preservedAnimation = objectMetadata.RootElement.GetProperty("payload")
        .GetProperty("staticAnimation");
      preservedAnimation.GetProperty("animationClassValue").GetUInt32().Should().Be(5);
      GlbDocument.DecodeBase64Url(
        preservedAnimation.GetProperty("scaleFrames").GetString()!, int.MaxValue)
        .Should().BeEmpty();
      GlbDocument.DecodeBase64Url(
        preservedAnimation.GetProperty("matrices").GetString()!, int.MaxValue)
        .Should().BeEmpty();
      var preservedTranslations = GlbDocument.DecodeBase64Url(
        preservedAnimation.GetProperty("translationFrames").GetString()!, int.MaxValue).ToArray();
      for (var frame = 0; frame < translations.Length; frame++)
      {
        ReadSingle(preservedTranslations, frame * 12).Should().Be(translations[frame].X);
        ReadSingle(preservedTranslations, frame * 12 + 4).Should().Be(-translations[frame].Y);
        ReadSingle(preservedTranslations, frame * 12 + 8).Should().Be(translations[frame].Z);
      }
    }

    glb.Position = 0;
    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);
    import.Status.Should().Be(OperationStatus.Succeeded);
    var restored = import.Value!.Asset.StaticRenderObjectSequence[0];
    restored.AnimationClassValue.Should().Be(5);
    restored.AnimationTracks.TranslationFrames.Should().Equal(translations);
    import.Value.Asset.CommonBaseHeader.AnimationFrameIndices.Should()
      .Be(new AnimationClassBytes(3, 4, 5, 6));
  }

  [Fact]
  public async Task ChangedNativeAnimationRegeneratesOnlyItsObjectClassTracks()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      translations: [Vector3.Zero, Vector3.One]));
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    export.Status.Should().Be(OperationStatus.Succeeded);
    var edited = glb.ToArray();
    using (var json = ReadGlbJson(edited))
    {
      var animation = json.RootElement.GetProperty("animations")[0];
      var translationChannel = animation.GetProperty("channels").EnumerateArray()
        .Single(channel => channel.GetProperty("target").GetProperty("path").GetString()
          == "translation");
      var samplerIndex = translationChannel.GetProperty("sampler").GetInt32();
      var accessorIndex = animation.GetProperty("samplers")[samplerIndex]
        .GetProperty("output").GetInt32();
      var offset = GetFloatAccessorOffset(edited, json.RootElement, accessorIndex);
      BinaryPrimitives.WriteInt32LittleEndian(
        edited.AsSpan(offset + 3 * sizeof(float)),
        BitConverter.SingleToInt32Bits(0.25f));
    }
    await using var editedStream = new MemoryStream(edited);

    var import = await interchange.ImportEditGlbAsync(editedStream, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var result = import.Value!;
    var record = result.Asset.StaticRenderObjectSequence[0];
    record.AnimationClassValue.Should().Be(0);
    record.AnimationTracks.ScaleFrames.Should().Equal(Vector3.One, Vector3.One);
    record.AnimationTracks.TranslationFrames.Should().Equal(Vector3.Zero, new Vector3(0.25f, 1, 1));
    record.AnimationTracks.Matrices.Should().Equal(Matrix4x4.Identity, Matrix4x4.Identity);
    result.Asset.CommonBaseHeader.AnimationLengths.Should()
      .Be(asset.CommonBaseHeader.AnimationLengths);
    result.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].AnimationTracks.TranslationFrames"
      && change.Disposition == PreservationDisposition.Regenerated);
  }

  [Fact]
  public async Task EditAnimationRegenerationHonorsOutputLimitWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      translations: [Vector3.Zero, Vector3.One]));
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = glb.ToArray();
    using (var json = ReadGlbJson(edited))
    {
      var animation = json.RootElement.GetProperty("animations")[0];
      var translationChannel = animation.GetProperty("channels").EnumerateArray()
        .Single(channel => channel.GetProperty("target").GetProperty("path").GetString()
          == "translation");
      var outputAccessor = animation.GetProperty("samplers")
        [translationChannel.GetProperty("sampler").GetInt32()].GetProperty("output").GetInt32();
      var offset = GetFloatAccessorOffset(edited, json.RootElement, outputAccessor) + 3 * sizeof(float);
      BinaryPrimitives.WriteInt32LittleEndian(
        edited.AsSpan(offset),
        BitConverter.SingleToInt32Bits(0.25f));
    }
    await using var source = new MemoryStream(edited);

    var import = await interchange.ImportEditGlbAsync(
      source,
      export.Value!.Baseline,
      profile: new GltfOperationProfile(maxOutputBytes: asset.SerializedLength + 100));

    import.Status.Should().Be(OperationStatus.Failed);
    import.Value.Should().BeNull();
    import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
  }

  [Fact]
  public async Task EditImportSamplesSparseSubframeStepAtIntegerFrames()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(3, 0, 0, 0),
      translations: [Vector3.Zero, new Vector3(2, 0, 0), new Vector3(4, 0, 0)]));
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root =>
    {
      var animation = root["animations"]![0]!;
      foreach (var sampler in animation["samplers"]!.AsArray())
      {
        sampler!["interpolation"] = "STEP";
      }
    });
    using (var json = ReadGlbJson(edited))
    {
      var animation = json.RootElement.GetProperty("animations")[0];
      var timeAccessor = animation.GetProperty("samplers")[0].GetProperty("input").GetInt32();
      BinaryPrimitives.WriteInt32LittleEndian(
        edited.AsSpan(GetFloatAccessorOffset(edited, json.RootElement, timeAccessor) + sizeof(float)),
        BitConverter.SingleToInt32Bits(0.5f / 24f));
      var translationChannel = animation.GetProperty("channels").EnumerateArray()
        .Single(channel => channel.GetProperty("target").GetProperty("path").GetString()
          == "translation");
      var outputAccessor = animation.GetProperty("samplers")
        [translationChannel.GetProperty("sampler").GetInt32()].GetProperty("output").GetInt32();
      BinaryPrimitives.WriteInt32LittleEndian(
        edited.AsSpan(GetFloatAccessorOffset(edited, json.RootElement, outputAccessor) + 3 * sizeof(float)),
        BitConverter.SingleToInt32Bits(3));
    }
    await using var source = new MemoryStream(edited);

    var import = await interchange.ImportEditGlbAsync(source, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence[0].AnimationTracks.TranslationFrames.Should()
      .Equal(Vector3.Zero, new Vector3(3, 0, 0), new Vector3(4, 0, 0));
  }

  [Fact]
  public async Task DeletingNativeAnimationClearsOnlyItsObjectClassTracks()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      1,
      new StaticAnimationMshFixture.AnimationLengths(0, 2, 0, 0),
      scales: [Vector3.One, Vector3.One],
      translations: [Vector3.Zero, Vector3.One],
      matrices: [Matrix4x4.Identity, Matrix4x4.Identity]));
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteJson(glb.ToArray(), root => root.Remove("animations"));
    await using var source = new MemoryStream(edited);

    var import = await interchange.ImportEditGlbAsync(source, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var result = import.Value!.Asset;
    result.CommonBaseHeader.AnimationLengths.Should().Be(asset.CommonBaseHeader.AnimationLengths);
    result.StaticRenderObjectSequence[0].AnimationClassValue.Should().Be(1);
    result.StaticRenderObjectSequence[0].AnimationTracks.ScaleFrames.Should().BeEmpty();
    result.StaticRenderObjectSequence[0].AnimationTracks.TranslationFrames.Should().BeEmpty();
    result.StaticRenderObjectSequence[0].AnimationTracks.Matrices.Should().BeEmpty();
    result.StaticRenderObjectSequence[0].RenderVertices.Should()
      .BeEquivalentTo(asset.StaticRenderObjectSequence[0].RenderVertices);
  }

  [Fact]
  public async Task EditImportSamplesCubicSplineWithoutPreservingTangents()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(3, 0, 0, 0),
      translations: [Vector3.Zero, Vector3.UnitX, new Vector3(4, 0, 0)]));
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = RewriteGlb(glb.ToArray(), (root, binary) =>
    {
      var animation = root["animations"]![0]!;
      var translationChannel = animation["channels"]!.AsArray().Single(channel =>
        channel!["target"]!["path"]!.GetValue<string>() == "translation");
      var sampler = animation["samplers"]![translationChannel!["sampler"]!.GetValue<int>()]!;
      sampler!["input"] = AppendFloatAccessor(
        root,
        binary,
        [0, 2f / 24f],
        "SCALAR",
        2,
        0,
        2f / 24f);
      sampler["output"] = AppendFloatAccessor(
        root,
        binary,
        [
          0, 0, 0,
          0, 0, 0,
          0, 0, 0,
          0, 0, 0,
          4, 0, 0,
          0, 0, 0
        ],
        "VEC3",
        6);
      sampler["interpolation"] = "CUBICSPLINE";
    });
    await using var source = new MemoryStream(edited);

    var import = await interchange.ImportEditGlbAsync(source, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence[0].AnimationTracks.TranslationFrames.Should()
      .Equal(Vector3.Zero, new Vector3(2, 0, 0), new Vector3(4, 0, 0));
  }

  [Fact]
  public async Task QuaternionSignBoundaryRestoresEquivalentUnchangedTracks()
  {
    var matrices = new[] { Matrix4x4.Identity, Matrix4x4.CreateRotationX(0.75f) };
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      matrices: matrices));
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = glb.ToArray();
    using (var json = ReadGlbJson(edited))
    {
      var animation = json.RootElement.GetProperty("animations")[0];
      var rotationChannel = animation.GetProperty("channels").EnumerateArray()
        .Single(channel => channel.GetProperty("target").GetProperty("path").GetString()
          == "rotation");
      var outputAccessor = animation.GetProperty("samplers")
        [rotationChannel.GetProperty("sampler").GetInt32()].GetProperty("output").GetInt32();
      var offset = GetFloatAccessorOffset(edited, json.RootElement, outputAccessor) + 4 * sizeof(float);
      for (var component = 0; component < 4; component++)
      {
        var value = ReadSingle(edited, offset + component * sizeof(float));
        BinaryPrimitives.WriteInt32LittleEndian(
          edited.AsSpan(offset + component * sizeof(float)),
          BitConverter.SingleToInt32Bits(-value));
      }
    }
    await using var source = new MemoryStream(edited);

    var import = await interchange.ImportEditGlbAsync(source, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.StaticRenderObjectSequence[0].AnimationTracks.Matrices.Should().Equal(matrices);
  }

  [Fact]
  public async Task IsolatedAnimationEditLeavesOtherObjectClassSerializedStateExact()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateTwoAnimationClasses().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var edited = glb.ToArray();
    using (var json = ReadGlbJson(edited))
    {
      var animation = json.RootElement.GetProperty("animations")[0];
      var translationChannel = animation.GetProperty("channels").EnumerateArray()
        .Single(channel => channel.GetProperty("target").GetProperty("path").GetString()
          == "translation");
      var outputAccessor = animation.GetProperty("samplers")
        [translationChannel.GetProperty("sampler").GetInt32()].GetProperty("output").GetInt32();
      var offset = GetFloatAccessorOffset(edited, json.RootElement, outputAccessor);
      BinaryPrimitives.WriteInt32LittleEndian(
        edited.AsSpan(offset),
        BitConverter.SingleToInt32Bits(ReadSingle(edited, offset) + 0.5f));
    }
    await using var source = new MemoryStream(edited);

    var import = await interchange.ImportEditGlbAsync(source, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.StaticRenderObjectSequence[1].GetSerializedRepresentation().Should()
      .Equal(asset.StaticRenderObjectSequence[1].GetSerializedRepresentation());
    import.Value.Preservation.Changes.Should().NotContain(change =>
      change.Disposition != PreservationDisposition.Retained
      && change.FieldPath.StartsWith("StaticRenderObjectSequence[1]", StringComparison.Ordinal));
  }

  [Fact]
  public async Task DuplicateObjectClassClipConflictsWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(1, 0, 0, 0),
      translations: [Vector3.Zero]));
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var ambiguous = RewriteJson(glb.ToArray(), root =>
    {
      var animations = root["animations"]!.AsArray();
      var duplicate = animations[0]!.DeepClone();
      duplicate!["name"] = "Ambiguous duplicate";
      animations.Add(duplicate);
    });
    await using var source = new MemoryStream(ambiguous);

    var import = await interchange.ImportEditGlbAsync(source, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Failed);
    import.Value.Should().BeNull();
    import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.StaleNativeProjection);
  }

  [Fact]
  public async Task TamperedAnimationPreservationGuardConflictsWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(1, 0, 0, 0),
      translations: [Vector3.Zero]));
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var stale = RewriteJson(glb.ToArray(), root =>
    {
      var node = root["nodes"]![0]!.AsObject();
      var metadata = JsonNode.Parse(node["extras"]!["earthtool"]!.GetValue<string>())!.AsObject();
      metadata["payload"]!["staticAnimation"]!["sha256"] = new string('0', 64);
      node["extras"]!["earthtool"] = metadata.ToJsonString();
    });
    await using var source = new MemoryStream(stale);

    var import = await interchange.ImportEditGlbAsync(source, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Failed);
    import.Value.Should().BeNull();
    import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.MalformedMetadata);
  }

  [Fact]
  public async Task AnimationClipDisplayNameIsNotProjectionIdentity()
  {
    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(1, 0, 0, 0),
      matrices: [Matrix4x4.Identity]));
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var renamed = RewriteJson(glb.ToArray(), root =>
      root["animations"]![0]!["name"] = "Artist action");
    await using var renamedStream = new MemoryStream(renamed);

    var import = await interchange.ImportEditGlbAsync(renamedStream, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.StaticRenderObjectSequence[0].AnimationTracks.Matrices
      .Should().Equal(asset.StaticRenderObjectSequence[0].AnimationTracks.Matrices);
  }

  [Fact]
  public async Task AnimationClipOrderAndNamesAreNotProjectionIdentity()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateTwoAnimationClasses().Data);
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      json.RootElement.GetProperty("animations").EnumerateArray()
        .Select(animation => animation.GetProperty("name").GetString()).Should()
        .Equal("EarthTool A", "EarthTool B");
    }
    var reordered = RewriteJson(glb.ToArray(), root =>
    {
      var animations = root["animations"]!.AsArray();
      var first = animations[0]!.DeepClone();
      animations[0] = animations[1]!.DeepClone();
      animations[1] = first;
      animations[0]!["name"] = "Second artist action";
      animations[1]!["name"] = "First artist action";
    });
    await using var reorderedStream = new MemoryStream(reordered);

    var import = await interchange.ImportEditGlbAsync(reorderedStream, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.StaticRenderObjectSequence.Select(record => record.AnimationClassValue)
      .Should().Equal(0, 1);
  }

  [Fact]
  public async Task NondecomposableFrameSuppressesOnlyItsObjectAndClass()
  {
    var shear = Matrix4x4.Identity;
    shear.M21 = 0.5f;
    var asset = await ReadAssetAsync(
      StaticMeshSequenceFixture.CreateTwoAnimationClasses(shear).Data);
    var interchange = new GltfInterchange();
    var first = new MemoryStream();
    var second = new MemoryStream();

    var firstExport = await interchange.ExportGlbAsync(
      asset,
      first,
      new GltfExportOptions(LineageId, DocumentId));
    var secondExport = await interchange.ExportGlbAsync(
      asset,
      second,
      new GltfExportOptions(LineageId, DocumentId));

    firstExport.Status.Should().Be(OperationStatus.Succeeded);
    firstExport.Diagnostics.Should().ContainSingle().Which.Should().Match<OperationDiagnostic>(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.AnimationMetadataOnly
      && diagnostic.Data["class"] == "B"
      && diagnostic.Data["sourceObject"] == "2");
    secondExport.Diagnostics.Should().BeEquivalentTo(firstExport.Diagnostics);
    second.ToArray().Should().Equal(first.ToArray());
    using var json = ReadGlbJson(first.ToArray());
    json.RootElement.GetProperty("animations").EnumerateArray()
      .Select(animation => animation.GetProperty("name").GetString()).Should()
      .Equal("EarthTool A");
  }

  [Fact]
  public async Task GlbAndSeparateGltfAnimationPackagesValidateAndRestoreExactTracks()
  {
    var source = StaticAnimationMshFixture.Create(
      2,
      new StaticAnimationMshFixture.AnimationLengths(0, 0, 2, 0),
      scales: [Vector3.One, new Vector3(2, 2, 2)],
      matrices: [Matrix4x4.Identity, Matrix4x4.CreateRotationX(0.25f)],
      pivot: new Vector3(4, 5, 6),
      frameIndices: new StaticAnimationMshFixture.AnimationLengths(7, 8, 9, 10));
    var asset = await ReadAssetAsync(source);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var separatePath = Path.Combine(directory, "animation.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var options = new GltfExportOptions(LineageId, DocumentId);
      var glbExport = await interchange.ExportGlbAsync(asset, glb, options);
      var separateExport = await interchange.ExportGltfFileAsync(asset, separatePath, options);
      glbExport.Status.Should().Be(OperationStatus.Succeeded);
      separateExport.Status.Should().Be(OperationStatus.Succeeded);
      glb.Position = 0;
      (await interchange.ValidateGlbAsync(glb)).Status.Should().Be(OperationStatus.Succeeded);
      (await interchange.ValidateGltfFileAsync(separatePath)).Status.Should().Be(OperationStatus.Succeeded);

      glb.Position = 0;
      var glbImport = await interchange.ImportEditGlbAsync(glb, glbExport.Value!.Baseline);
      var separateImport = await interchange.ImportEditGltfFileAsync(
        separatePath,
        separateExport.Value!.Baseline);
      foreach (var import in new[] { glbImport, separateImport })
      {
        import.Status.Should().Be(OperationStatus.Succeeded);
        await using var restored = new MemoryStream();
        (await new MshWriter().WriteAsync(import.Value!.Asset, restored)).Status.Should()
          .Be(OperationStatus.Succeeded);
        restored.ToArray().Should().Equal(source);
        import.Value.RestoredSerializedRepresentationPaths.Should().Contain([
          "CommonBaseHeader.AnimationLengths",
          "CommonBaseHeader.AnimationFrameIndices",
          "StaticRenderObjectSequence[0].AnimationClassValue",
          "StaticRenderObjectSequence[0].AnimationTracks.ScaleFrames",
          "StaticRenderObjectSequence[0].AnimationTracks.TranslationFrames",
          "StaticRenderObjectSequence[0].AnimationTracks.Matrices"]);
      }
    }
    finally
    {
      Directory.Delete(directory, true);
    }
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
      new GltfExportOptions(LineageId, DocumentId));

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    using var json = ReadGlbJson(glb.ToArray());
    var root = json.RootElement;
    root.GetProperty("scenes")[0].GetProperty("nodes").EnumerateArray()
      .Select(node => node.GetInt32()).Should().Equal(0);
    root.GetProperty("nodes").GetArrayLength().Should().Be(7);
    root.GetProperty("meshes").GetArrayLength().Should().Be(3);
    root.GetProperty("nodes")[0].GetProperty("children").EnumerateArray()
      .Select(node => node.GetInt32()).Take(2).Should().Equal(1, 2);
    root.GetProperty("nodes")[0].TryGetProperty("translation", out _).Should().BeFalse();
    root.GetProperty("nodes")[1].GetProperty("translation").EnumerateArray()
      .Select(value => value.GetSingle()).Should().Equal(1, 3, -2);
    root.GetProperty("nodes")[2].GetProperty("translation").EnumerateArray()
      .Select(value => value.GetSingle()).Should().Equal(7, 9, -8);
    root.GetProperty("meshes")[0].GetProperty("primitives").GetArrayLength().Should().Be(2);
    root.GetProperty("meshes")[1].GetProperty("primitives").GetArrayLength().Should().Be(1);
    root.GetProperty("meshes")[2].GetProperty("primitives").GetArrayLength().Should().Be(1);
  }

  [Fact]
  public async Task LoadedTextureBindingsExportAsUnlitMaterialsWithoutUsingDisplayNamesAsIdentity()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    using (var json = ReadGlbJson(glb.ToArray()))
    {
      var root = json.RootElement;
      root.GetProperty("extensionsUsed").EnumerateArray()
        .Select(value => value.GetString()).Should().Contain("KHR_materials_unlit");
      var materials = root.GetProperty("materials");
      materials.GetArrayLength().Should().Be(asset.StaticRenderObjectSequence.Count);
      for (var index = 0; index < materials.GetArrayLength(); index++)
      {
        var material = materials[index];
        material.GetProperty("extensions").TryGetProperty("KHR_materials_unlit", out _)
          .Should().BeTrue();
        var metadata = JsonDocument.Parse(
          material.GetProperty("extras").GetProperty("earthtool").GetString()!);
        var localId = metadata.RootElement.GetProperty("id").GetInt32();
        GlbDocument.DecodeBase64Url(metadata.RootElement.GetProperty("payload")
          .GetProperty("textureBinding").GetString()!, int.MaxValue)
          .Should().Equal(asset.StaticRenderObjectSequence.Single(record =>
            record.LocalId == localId).TexturePathBytes);
      }
    }

    var renamed = RewriteJson(glb.ToArray(), root =>
    {
      var materials = root["materials"]!.AsArray();
      for (var index = 0; index < materials.Count; index++)
      {
        materials[index]!["name"] = $"unrelated preview {materials.Count - index}";
      }
    });
    await using var edited = new MemoryStream(renamed);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence
      .Select(record => record.TexturePathBytes.ToArray()).Should()
      .BeEquivalentTo(
        asset.StaticRenderObjectSequence.Select(record => record.TexturePathBytes.ToArray()),
        options => options.WithStrictOrdering());
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].TexturePathBytes"
      && change.Disposition == PreservationDisposition.Retained);
  }

  [Theory]
  [InlineData("")]
  [InlineData("Textures\\authored\\replacement.tex")]
  public async Task ExplicitMaterialBindingEditRegeneratesOnlyTheAssignedTexKey(string replacement)
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var editedBytes = RewriteJson(glb.ToArray(), root =>
    {
      var material = root["materials"]![0]!.AsObject();
      var metadata = JsonNode.Parse(material["extras"]!["earthtool"]!.GetValue<string>())!.AsObject();
      metadata["payload"]!["textureBinding"] = GlbDocument.EncodeBase64Url(
        Encoding.ASCII.GetBytes(replacement));
      material["extras"]!["earthtool"] = metadata.ToJsonString();
    });
    await using var edited = new MemoryStream(editedBytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var changed = import.Value!.Asset.StaticRenderObjectSequence.Single(record => record.LocalId == 1);
    changed.TexturePathBytes.Should().Equal(Encoding.ASCII.GetBytes(replacement));
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].TexturePathBytes"
      && change.Disposition == PreservationDisposition.Regenerated);
    for (var index = 1; index < asset.StaticRenderObjectSequence.Count; index++)
    {
      import.Value.Asset.StaticRenderObjectSequence[index].GetSerializedRepresentation().Should()
        .Equal(asset.StaticRenderObjectSequence[index].GetSerializedRepresentation());
    }
  }

  [Fact]
  public async Task MaterialSharingForkingAndReassignmentCopyOnlyTheExplicitBinding()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var editedBytes = RewriteJson(glb.ToArray(), root =>
    {
      var materials = root["materials"]!.AsArray();
      var fork = materials[1]!.DeepClone().AsObject();
      fork["name"] = "forked display material";
      materials.Add(fork);
      root["meshes"]![0]!["primitives"]![0]!["material"] = materials.Count - 1;
    });
    await using var edited = new MemoryStream(editedBytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Failed);
    import.Value.Should().BeNull();
    import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.DuplicateScopeIdentity);
  }

  [Fact]
  public async Task ReassignmentCanReuseExactLoadedLegacyBindingBytesWithoutCanonicalizingThem()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var legacyBinding = "Legacy??\\root-a.tex"u8.ToArray();
    legacyBinding.Length.Should().Be(19);
    legacyBinding.CopyTo(fixture.Data, fixture.RecordOffsets[0] + 8 + 0xA0 + 8);
    var asset = await ReadAssetAsync(fixture.Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var editedBytes = RewriteJson(glb.ToArray(), root =>
    {
      root["meshes"]![0]!["primitives"]![1]!["material"] = 0;
    });
    await using var edited = new MemoryStream(editedBytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence.Single(record => record.LocalId == 3)
      .TexturePathBytes.Should().Equal(legacyBinding);
  }

  [Fact]
  public async Task PrimitiveReorderingKeepsEachMaterialBindingWithItsSemanticPartition()
  {
    var source = CreateTwoPartitionAsset();
    var bindingEdit = source.Edit();
    bindingEdit.SetTextureResourceBinding(
      source.StaticRenderObjectSequence[0].Id,
      "Textures\\authored\\first.tex");
    bindingEdit.SetTextureResourceBinding(
      source.StaticRenderObjectSequence[1].Id,
      "Textures\\authored\\second.tex");
    var committed = bindingEdit.Commit();
    committed.TryGetValue(out var boundAsset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      boundAsset!,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var reordered = RewriteJson(glb.ToArray(), root =>
    {
      var primitives = root["meshes"]![0]!["primitives"]!.AsArray();
      primitives.Insert(0, primitives[1]!.DeepClone());
      primitives.RemoveAt(2);
    });
    await using var edited = new MemoryStream(reordered);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence.Single(record => record.LocalId == 1)
      .TexturePathBytes.Should().Equal("Textures\\authored\\first.tex"u8.ToArray());
    import.Value.Asset.StaticRenderObjectSequence.Single(record => record.LocalId == 2)
      .TexturePathBytes.Should().Equal("Textures\\authored\\second.tex"u8.ToArray());
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
      CreateRgbaTex(2, 1, [0xFF, 0, 0, 0xFF, 0, 0, 0xFF, 0xFF]));
    try
    {
      var options = new GltfExportOptions(LineageId, DocumentId, [directory]);
      var interchange = new GltfInterchange();
      await using var first = new MemoryStream();
      await using var second = new MemoryStream();

      var firstResult = await interchange.ExportGlbAsync(asset, first, options);
      var secondResult = await interchange.ExportGlbAsync(asset, second, options);

      firstResult.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", firstResult.Diagnostics.Select(diagnostic => diagnostic.Message)));
      secondResult.Status.Should().Be(OperationStatus.Succeeded);
      second.ToArray().Should().Equal(first.ToArray());
      firstResult.Diagnostics.Should().Contain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing
        && diagnostic.Severity == DiagnosticSeverity.Warning);
      using var json = ReadGlbJson(first.ToArray());
      var root = json.RootElement;
      root.GetProperty("images").GetArrayLength().Should().Be(2);
      root.GetProperty("images")[0].GetProperty("mimeType").GetString().Should().Be("image/png");
      root.GetProperty("textures").GetArrayLength().Should().Be(2);
      root.GetProperty("materials")[0].GetProperty("pbrMetallicRoughness")
        .GetProperty("baseColorTexture").GetProperty("index").GetInt32().Should().Be(0);
      await using var withoutPreview = new MemoryStream();
      await interchange.ExportGlbAsync(
        asset,
        withoutPreview,
        new GltfExportOptions(LineageId, DocumentId));
      var imageBufferView = root.GetProperty("images")[0].GetProperty("bufferView").GetInt32();
      var pngLength = root.GetProperty("bufferViews")[imageBufferView]
        .GetProperty("byteLength").GetInt32();
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
          maxPreviewPixels: 16));
      constrainedResult.Status.Should().Be(OperationStatus.Succeeded);
      constrainedResult.Diagnostics.Should().Contain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable);
      constrainedResult.Diagnostics.Should().NotContain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed
          || diagnostic.Code == GltfDiagnosticCodes.TextureDiagnosticPreviewUsed
          || diagnostic.Code == GltfDiagnosticCodes.TextureVariantsNotRepresented);
      using var constrainedJson = ReadGlbJson(constrained.ToArray());
      constrainedJson.RootElement.TryGetProperty("images", out _).Should().BeFalse();
      var metadataFreePreview = RewriteJson(first.ToArray(), RemoveEarthToolMetadata);
      await using var genericSource = new MemoryStream(metadataFreePreview);
      var genericImport = await new GltfInterchange().ImportNewModelGlbAsync(genericSource);
      genericImport.Status.Should().Be(OperationStatus.Failed);
      genericImport.Diagnostics.Should().ContainSingle().Subject.Data.Should()
        .Contain(new KeyValuePair<string, string>("domain", "TexResourceBinding"));
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
      CreateRgbaTex(2, 1, [0xFF, 0, 0, 0xFF, 0, 0, 0xFF, 0xFF]));
    try
    {
      await using var glb = new MemoryStream();
      var result = await new GltfInterchange().ExportGlbAsync(
        asset,
        glb,
        new GltfExportOptions(LineageId, DocumentId, [directory]),
        new GltfOperationProfile(
          maxInputBytes: 32 * 1024 * 1024,
          maxOutputBytes: 32 * 1024 * 1024,
          maxMetadataBytes: 4 * 1024 * 1024,
          maxJsonDepth: 32,
          maxActiveRenderVertices: 65536,
          maxNodes: 4096,
          maxHierarchyDepth: 15,
          maxTextureBytes: 1024,
          maxPreviewPixels: 1));

      result.Status.Should().Be(OperationStatus.Succeeded);
      result.Diagnostics.Should().Contain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable);
      using var json = ReadGlbJson(glb.ToArray());
      json.RootElement.TryGetProperty("images", out _).Should().BeFalse();
      var metadata = JsonDocument.Parse(json.RootElement.GetProperty("materials")[0]
        .GetProperty("extras").GetProperty("earthtool").GetString()!);
      GlbDocument.DecodeBase64Url(metadata.RootElement.GetProperty("payload")
        .GetProperty("textureBinding").GetString()!, int.MaxValue)
        .Should().Equal("Textures\\root-a.tex"u8.ToArray());
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task OmittedLateDefaultPreviewDoesNotReportThatTheFallbackWasUsed()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var edit = asset.Edit();
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[2].Id, null);
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[3].Id, null);
    edit.Commit().TryGetValue(out var twoMaterialAsset).Should().BeTrue();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var options = new GltfExportOptions(LineageId, DocumentId, [directory]);
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "root-a.tex"),
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF]));
    await File.WriteAllBytesAsync(Path.Combine(textureDirectory, "Default.tex"), [1, 2, 3]);

    try
    {
      var interchange = new GltfInterchange();
      await using var firstOnly = new MemoryStream();
      await interchange.ExportGlbAsync(
        twoMaterialAsset!,
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
          1));
      var defaultPixels = new byte[64 * 64 * 4];
      new Random(145).NextBytes(defaultPixels);
      await File.WriteAllBytesAsync(
        Path.Combine(textureDirectory, "Default.tex"),
        CreateRgbaTex(64, 64, defaultPixels));
      await using var constrained = new MemoryStream();

      var result = await interchange.ExportGlbAsync(
        twoMaterialAsset!,
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
          64 * 64 + 1));

      result.Status.Should().Be(OperationStatus.Succeeded);
      result.Diagnostics.Should().Contain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable
        && diagnostic.Path == "StaticRenderObjectSequence[1].TexturePathBytes");
      result.Diagnostics.Should().NotContain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed);
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
      await File.WriteAllBytesAsync(Path.Combine(textureDirectory, name), CreateRgbaTex(2, 1, pixels));
    }

    try
    {
      var options = new GltfExportOptions(LineageId, DocumentId, [directory]);
      var interchange = new GltfInterchange();

      var first = await interchange.ExportGltfFileAsync(asset, firstPath, options);
      var second = await interchange.ExportGltfFileAsync(asset, secondPath, options);

      first.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", first.Diagnostics.Select(diagnostic => diagnostic.Message)));
      second.Status.Should().Be(OperationStatus.Succeeded);
      (await File.ReadAllBytesAsync(secondPath)).Should().Equal(await File.ReadAllBytesAsync(firstPath));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(firstPath));
      var root = json.RootElement;
      var image = root.GetProperty("images").EnumerateArray().Should().ContainSingle().Subject;
      image.TryGetProperty("bufferView", out _).Should().BeFalse();
      var expectedImageName = GetPreviewContentAddress(2, 1, pixels) + ".png";
      image.GetProperty("uri").GetString().Should().Be(expectedImageName);
      File.Exists(Path.Combine(directory, expectedImageName)).Should().BeTrue();
      root.GetProperty("textures").EnumerateArray()
        .Select(texture => texture.GetProperty("source").GetInt32()).Should().Equal(0);
      root.GetProperty("materials").EnumerateArray()
        .Select(material => material.GetProperty("pbrMetallicRoughness")
          .GetProperty("baseColorTexture").GetProperty("index").GetInt32())
        .Should().OnlyContain(index => index == 0);
      Directory.EnumerateFiles(directory, "*.png").Should().ContainSingle();
      Directory.EnumerateFiles(directory, "*.bin").Should().ContainSingle();

      var validation = await interchange.ValidateGltfFileAsync(firstPath);
      validation.Status.Should().Be(OperationStatus.Succeeded);
      await AssertKhronosValidAsync(firstPath);
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
      CreateRgbaTex(1, 1, selectedPixels));
    await File.WriteAllBytesAsync(
      Path.Combine(secondRoot, "Textures", "root-a.tex"),
      CreateRgbaTex(1, 1, shadowedPixels));
    await File.WriteAllBytesAsync(
      Path.Combine(firstRoot, "Textures", "Default.tex"),
      CreateRgbaTex(1, 1, defaultPixels));

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        output,
        new GltfExportOptions(LineageId, DocumentId, [firstRoot, secondRoot]));

      result.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
      result.Diagnostics.Should().ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureResourceShadowed);
      result.Diagnostics.Count(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing).Should().Be(3);
      result.Diagnostics.Count(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed).Should().Be(3);
      result.Diagnostics.Where(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed).Should().OnlyContain(diagnostic =>
          diagnostic.EventId == 1111
          && diagnostic.Severity == DiagnosticSeverity.Warning
          && diagnostic.Path.EndsWith(".TexturePathBytes", StringComparison.Ordinal));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(output));
      var imageUris = json.RootElement.GetProperty("images").EnumerateArray()
        .Select(image => image.GetProperty("uri").GetString()).ToArray();
      imageUris.Should().BeEquivalentTo(
        GetPreviewContentAddress(1, 1, selectedPixels) + ".png",
        GetPreviewContentAddress(1, 1, defaultPixels) + ".png");
      imageUris.Should().NotContain(GetPreviewContentAddress(1, 1, shadowedPixels) + ".png");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task MissingDefaultUsesOneDeterministicDiagnosticPreviewWithoutChangingBindings()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var output = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        output,
        new GltfExportOptions(LineageId, DocumentId, [directory]));

      result.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
      result.Diagnostics.Count(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing).Should().Be(4);
      result.Diagnostics.Count(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureDiagnosticPreviewUsed).Should().Be(4);
      result.Diagnostics.Where(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureDiagnosticPreviewUsed).Should().OnlyContain(diagnostic =>
          diagnostic.EventId == 1112
          && diagnostic.Severity == DiagnosticSeverity.Warning
          && diagnostic.Path.EndsWith(".TexturePathBytes", StringComparison.Ordinal));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(output));
      json.RootElement.GetProperty("images").EnumerateArray().Should().ContainSingle();
      Directory.EnumerateFiles(directory, "*.png").Should().ContainSingle();
      var import = await new GltfInterchange().ImportEditGltfFileAsync(output, result.Value!.Baseline);
      import.Status.Should().Be(OperationStatus.Succeeded);
      import.Value!.Asset.StaticRenderObjectSequence.Select(record => record.TexturePathBytes.ToArray())
        .Should().BeEquivalentTo(
          asset.StaticRenderObjectSequence.Select(record => record.TexturePathBytes.ToArray()),
          options => options.WithStrictOrdering());
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparatePackageCollisionPreflightPreservesManifestAndWritesNothing()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var edit = asset.Edit();
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[0].Id, "Textures\\preview.tex");
    edit.Commit().TryGetValue(out var boundAsset).Should().BeTrue();
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
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF]));

    try
    {
      var options = new GltfExportOptions(LineageId, DocumentId, [directory]);
      var interchange = new GltfInterchange();
      var reference = await interchange.ExportGltfFileAsync(boundAsset!, referencePath, options);
      reference.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", reference.Diagnostics.Select(diagnostic => diagnostic.Message)));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(referencePath));
      var imageName = json.RootElement.GetProperty("images")[0].GetProperty("uri").GetString()!;
      await File.WriteAllBytesAsync(destinationPath, originalManifest);
      await File.WriteAllBytesAsync(Path.Combine(collisionDirectory, imageName), [1, 2, 3]);

      var result = await interchange.ExportGltfFileAsync(boundAsset!, destinationPath, options);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(destinationPath)).Should().Equal(originalManifest);
      (await File.ReadAllBytesAsync(Path.Combine(collisionDirectory, imageName))).Should().Equal(1, 2, 3);
      Directory.EnumerateFiles(collisionDirectory, "*.bin").Should().BeEmpty();
      Directory.EnumerateFiles(collisionDirectory).Should().NotContain(path =>
        path.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparatePackagePreflightsDirectoryCollisionsBeforeWritingAnySidecar()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var edit = asset.Edit();
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[0].Id, "Textures\\preview.tex");
    edit.Commit().TryGetValue(out var boundAsset).Should().BeTrue();
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
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF]));

    try
    {
      var options = new GltfExportOptions(LineageId, DocumentId, [directory]);
      var interchange = new GltfInterchange();
      (await interchange.ExportGltfFileAsync(boundAsset!, referencePath, options)).Status.Should()
        .Be(OperationStatus.Succeeded);
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(referencePath));
      var sidecarNames = new[]
      {
        json.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!,
        json.RootElement.GetProperty("images")[0].GetProperty("uri").GetString()!
      }.OrderBy(name => name, StringComparer.Ordinal).ToArray();
      await File.WriteAllBytesAsync(destinationPath, originalManifest);
      Directory.CreateDirectory(Path.Combine(collisionDirectory, sidecarNames[1]));

      var result = await interchange.ExportGltfFileAsync(boundAsset!, destinationPath, options);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(destinationPath)).Should().Equal(originalManifest);
      Directory.EnumerateFiles(collisionDirectory).Should().ContainSingle();
      Directory.EnumerateFiles(collisionDirectory).Should().NotContain(path =>
        path.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SpecialTexUsesItsFirstImageAndWarnsThatVariantsAreNotRepresented()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var edit = asset.Edit();
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[0].Id, "Textures\\special.tex");
    edit.Commit().TryGetValue(out var boundAsset).Should().BeTrue();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var output = Path.Combine(directory, "model.gltf");
    var pixels = new byte[] { 0x20, 0x40, 0x60, 0x80 };
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "special.tex"),
      CreateContainerTex(1, 1, pixels));

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        boundAsset!,
        output,
        new GltfExportOptions(LineageId, DocumentId, [directory]));

      result.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
      result.Diagnostics.Should().ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureVariantsNotRepresented
        && diagnostic.EventId == 1113
        && diagnostic.Severity == DiagnosticSeverity.Warning
        && diagnostic.Path == "StaticRenderObjectSequence[0].TexturePathBytes");
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(output));
      json.RootElement.GetProperty("images")[0].GetProperty("uri").GetString().Should()
        .Be(GetPreviewContentAddress(1, 1, pixels) + ".png");
      (await new GltfInterchange().ValidateGltfFileAsync(output)).Status.Should()
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

    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var edit = asset.Edit();
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[0].Id, "Textures\\preview.tex");
    edit.Commit().TryGetValue(out var boundAsset).Should().BeTrue();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var output = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(Path.Combine(textureDirectory, "preview.tex"), CreateRgbaTex(1, 1, [1, 2, 3, 4]));
    await File.WriteAllBytesAsync(Path.Combine(textureDirectory, "PREVIEW.TEX"), CreateRgbaTex(1, 1, [5, 6, 7, 8]));
    await File.WriteAllBytesAsync(output, original);

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        boundAsset!,
        output,
        new GltfExportOptions(LineageId, DocumentId, [directory]));

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should()
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

    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var edit = asset.Edit();
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[0].Id, "Textures\\preview.tex");
    edit.Commit().TryGetValue(out var boundAsset).Should().BeTrue();
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
      CreateRgbaTex(1, 1, defaultPixels));
    File.CreateSymbolicLink(Path.Combine(textureDirectory, "preview.tex"), outside);

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        boundAsset!,
        output,
        new GltfExportOptions(LineageId, DocumentId, [root]));

      result.Status.Should().Be(OperationStatus.Succeeded);
      result.Diagnostics.Should().Contain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing);
      result.Diagnostics.Should().Contain(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.TextureDefaultPreviewUsed);
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
  public async Task SeparateGltfRejectsSymlinkedImageSidecarBeforeValidationOrImport()
  {
    if (OperatingSystem.IsWindows())
    {
      return;
    }

    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var edit = asset.Edit();
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[0].Id, "Textures\\preview.tex");
    edit.Commit().TryGetValue(out var boundAsset).Should().BeTrue();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var packageDirectory = Path.Combine(directory, "package");
    var textureDirectory = Path.Combine(directory, "Textures");
    var output = Path.Combine(packageDirectory, "model.gltf");
    var outsideImage = Path.Combine(directory, "outside.png");
    Directory.CreateDirectory(packageDirectory);
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "preview.tex"),
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF]));

    try
    {
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGltfFileAsync(
        boundAsset!,
        output,
        new GltfExportOptions(LineageId, DocumentId, [directory]));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(output));
      var imagePath = Path.Combine(
        packageDirectory,
        json.RootElement.GetProperty("images")[0].GetProperty("uri").GetString()!);
      File.Move(imagePath, outsideImage);
      File.CreateSymbolicLink(imagePath, outsideImage);

      var validation = await interchange.ValidateGltfFileAsync(output);
      var import = await interchange.ImportEditGltfFileAsync(output, export.Value!.Baseline);

      validation.Status.Should().Be(OperationStatus.Failed);
      import.Status.Should().Be(OperationStatus.Failed);
      import.Value.Should().BeNull();
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
      64);

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        output,
        new GltfExportOptions(LineageId, DocumentId, [firstRoot, secondRoot]),
        profile);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should()
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
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var edit = asset.Edit();
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[0].Id, "Textures\\preview.tex");
    edit.Commit().TryGetValue(out var boundAsset).Should().BeTrue();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var output = Path.Combine(directory, "model.gltf");
    var original = new byte[] { 7, 8, 9 };
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "preview.tex"),
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF]));
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
      1);

    try
    {
      var result = await new GltfInterchange().ExportGltfFileAsync(
        boundAsset!,
        output,
        new GltfExportOptions(LineageId, DocumentId, [directory]),
        profile);

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should()
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
  public async Task GlbAndSeparateGltfWithPreviewsRestoreEquivalentMshState()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var edit = asset.Edit();
    edit.SetTextureResourceBinding(asset.StaticRenderObjectSequence[0].Id, "Textures\\preview.tex");
    edit.Commit().TryGetValue(out var boundAsset).Should().BeTrue();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var textureDirectory = Path.Combine(directory, "Textures");
    var separatePath = Path.Combine(directory, "model.gltf");
    var glbPath = Path.Combine(directory, "model.glb");
    Directory.CreateDirectory(textureDirectory);
    await File.WriteAllBytesAsync(
      Path.Combine(textureDirectory, "preview.tex"),
      CreateRgbaTex(1, 1, [0xFF, 0, 0, 0xFF]));

    try
    {
      var options = new GltfExportOptions(LineageId, DocumentId, [directory]);
      var interchange = new GltfInterchange();
      await using var glb = new MemoryStream();
      var glbExport = await interchange.ExportGlbAsync(boundAsset!, glb, options);
      var separateExport = await interchange.ExportGltfFileAsync(boundAsset!, separatePath, options);
      await File.WriteAllBytesAsync(glbPath, glb.ToArray());

      glbExport.Status.Should().Be(OperationStatus.Succeeded);
      separateExport.Status.Should().Be(OperationStatus.Succeeded);
      glb.Position = 0;
      (await interchange.ValidateGlbAsync(glb)).Status.Should().Be(OperationStatus.Succeeded);
      (await interchange.ValidateGltfFileAsync(separatePath)).Status.Should().Be(OperationStatus.Succeeded);
      glb.Position = 0;
      var glbImport = await interchange.ImportEditGlbAsync(glb, glbExport.Value!.Baseline);
      var separateImport = await interchange.ImportEditGltfFileAsync(
        separatePath,
        separateExport.Value!.Baseline);
      await using var glbMsh = new MemoryStream();
      await using var separateMsh = new MemoryStream();
      await new MshWriter().WriteAsync(glbImport.Value!.Asset, glbMsh);
      await new MshWriter().WriteAsync(separateImport.Value!.Asset, separateMsh);
      separateMsh.ToArray().Should().Equal(glbMsh.ToArray());
      await AssertKhronosValidAsync(glbPath);
      await AssertKhronosValidAsync(separatePath);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task NewModelImportAuthorsCanonicalAssetAndUsableFirstMetadataBaseline()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    var result = imported.Value!;
    result.Asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    result.Asset.ArchiveFraming.Declaration.Should().Be(0x20D0A1FF);
    result.Asset.ArchiveFraming.CreationGuid.Should().NotBeNull().And.NotBe(Guid.Empty);
    result.Baseline.AssetLineageId.Should().Be(result.Asset.LineageId.Value);
    result.Baseline.DocumentId.Should().NotBe(Guid.Empty);
    result.Asset.CommonBaseHeader.BoxPresenceMask.Should().Be(0x00008000);
    result.Asset.StaticRenderObjectSequence.Should().ContainSingle();
    result.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "CommonBaseHeader.Footprint"
      && change.Disposition == PreservationDisposition.Canonicalized);

    await using var baseline = new MemoryStream();
    var firstBaseline = await interchange.ExportGlbAsync(
      result.Asset,
      baseline,
      new GltfExportOptions(result.Baseline.AssetLineageId, result.Baseline.DocumentId));
    baseline.Position = 0;
    var editImport = await interchange.ImportEditGlbAsync(baseline, firstBaseline.Value!.Baseline);
    editImport.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public async Task NewModelImportAuthorsUniquelyNamedAnimationClassAsCanonicalDenseTracks()
  {
    var sourceAsset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      2,
      new StaticAnimationMshFixture.AnimationLengths(0, 0, 2, 0),
      scales: [Vector3.One, new Vector3(2, 3, 4)],
      translations: [Vector3.Zero, new Vector3(5, 6, 7)],
      matrices: [Matrix4x4.Identity, Matrix4x4.CreateRotationZ(0.5f)]));
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var asset = imported.Value!.Asset;
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

  [Fact]
  public async Task TypedAnimationClassBindingDoesNotDependOnClipDisplayName()
  {
    var sourceAsset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      2,
      new StaticAnimationMshFixture.AnimationLengths(0, 0, 2, 0),
      translations: [Vector3.Zero, Vector3.One]));
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var generic = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      root["animations"]![0]!["name"] = "Artist Walk Cycle";
    });
    await using var source = new MemoryStream(generic);

    var imported = await interchange.ImportNewModelGlbAsync(
      source,
      new GltfNewModelImportOptions(
        animationClasses: new Dictionary<GltfAnimationHandle, GltfNewModelAnimationClass>
        {
          [new GltfAnimationHandle(1)] = GltfNewModelAnimationClass.C
        }));

    imported.Status.Should().Be(OperationStatus.Succeeded);
    imported.Value!.Asset.CommonBaseHeader.AnimationLengths.Should()
      .Be(new AnimationClassBytes(0, 0, 2, 0));
  }

  [Fact]
  public async Task NewModelImportUsesNodeRestTransformForUnanimatedTrsPaths()
  {
    var sourceAsset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      3,
      new StaticAnimationMshFixture.AnimationLengths(0, 0, 0, 2),
      translations: [Vector3.Zero, new Vector3(3, 0, 0)]));
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
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
    });
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var tracks = imported.Value!.Asset.StaticRenderObjectSequence[0].AnimationTracks;
    tracks.TranslationFrames.Should().Equal(Vector3.Zero, new Vector3(3, 0, 0));
    tracks.ScaleFrames.Should().Equal(new Vector3(2, 4, 3), new Vector3(2, 4, 3));
    tracks.Matrices.Should().Equal(Matrix4x4.Identity, Matrix4x4.Identity);
    imported.Value.Asset.StaticRenderObjectSequence[0].RenderVertices.Select(vertex => vertex.Position)
      .Should().Equal(sourceAsset.StaticRenderObjectSequence[0].RenderVertices.Select(vertex => vertex.Position));
  }

  [Fact]
  public async Task NewModelImportAccumulatesAnimatedTransformOnlyParentOntoMeshTracks()
  {
    var sourceAsset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      translations: [Vector3.Zero, new Vector3(2, 0, 0)]));
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
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
    });
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    imported.Value!.Asset.StaticRenderObjectSequence[0].AnimationTracks.TranslationFrames.Should()
      .Equal(Vector3.Zero, new Vector3(2, 0, 0));
  }

  [Fact]
  public async Task NewModelImportRejectsAccumulatedAnimationWithUnsupportedMatrixComponents()
  {
    var sourceAsset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      translations: [Vector3.Zero, Vector3.UnitX]));
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      RemoveArtistHelperNodes(root);
      var nodes = root["nodes"]!.AsArray();
      nodes[0]!["rotation"] = new JsonArray(0, 0, MathF.Sin(0.25f), MathF.Cos(0.25f));
      nodes.Insert(0, new JsonObject
      {
        ["scale"] = new JsonArray(2, 1, 1),
        ["children"] = new JsonArray(1)
      });
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
    });
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Should().ContainSingle().Subject.Data.Should()
      .Contain(new KeyValuePair<string, string>("domain", "animations"));
  }

  [Fact]
  public async Task NewModelAnimationBytesParticipateInOutputLimitPreflight()
  {
    var sourceAsset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      translations: [Vector3.Zero, Vector3.UnitX]));
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);
    var maximum = OneTriangleMshFixture.Create().Length + 100;

    var imported = await interchange.ImportNewModelGlbAsync(
      source,
      profile: new GltfOperationProfile(maxOutputBytes: maximum));

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
    var sourceAsset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      0,
      new StaticAnimationMshFixture.AnimationLengths(2, 0, 0, 0),
      translations: [Vector3.Zero, Vector3.One]));
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
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
    });
    if (mutation is "fractional-end" or "frame-255")
    {
      using var json = ReadGlbJson(metadataFree);
      var animation = json.RootElement.GetProperty("animations")[0];
      var timeAccessor = animation.GetProperty("samplers")[0].GetProperty("input").GetInt32();
      var endTime = mutation == "fractional-end" ? 1.5f / 24f : 255f / 24f;
      BinaryPrimitives.WriteInt32LittleEndian(
        metadataFree.AsSpan(
          GetFloatAccessorOffset(metadataFree, json.RootElement, timeAccessor) + sizeof(float)),
        BitConverter.SingleToInt32Bits(endTime));
    }
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Should().ContainSingle().Subject.Data.Should()
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
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var canonicalSource = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(
      canonicalSource,
      options: new GltfNewModelImportOptions(new Dictionary<GltfMaterialHandle, string?>
      {
        [new GltfMaterialHandle(1)] = "Textures\\authored\\hull.tex"
      }));

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    imported.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing
      && diagnostic.EventId == 1107
      && diagnostic.Path == "materials[0]");
    imported.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle().Subject
      .TexturePathBytes.Should().Equal("Textures\\authored\\hull.tex"u8.ToArray());

    await using var unsafeSource = new MemoryStream(metadataFree);
    var rejected = await interchange.ImportNewModelGlbAsync(
      unsafeSource,
      options: new GltfNewModelImportOptions(new Dictionary<GltfMaterialHandle, string?>
      {
        [new GltfMaterialHandle(1)] = "..\\outside.tex"
      }));
    rejected.Status.Should().Be(OperationStatus.Failed);
    rejected.Value.Should().BeNull();
    rejected.Diagnostics.Should().ContainSingle().Subject.Data.Should()
      .Contain(new KeyValuePair<string, string>("domain", "TexResourceBinding"));
  }

  [Fact]
  public async Task NewModelImportAppliesTypedSemanticOverridesAndReportsConcreteCanonicalPaths()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      RemoveArtistHelperNodes(root);
      var nodes = root["nodes"]!.AsArray();
      nodes.Add(new JsonObject { ["children"] = new JsonArray(0) });
      root["scenes"]![0]!["nodes"] = new JsonArray(nodes.Count - 1);
    });
    await using var source = new MemoryStream(metadataFree);
    var elevations = Enumerable.Repeat(1.5f, 16).ToArray();

    var imported = await interchange.ImportNewModelGlbAsync(
      source,
      new GltfNewModelImportOptions(
        textureResourceBindings: new Dictionary<GltfMaterialHandle, string?>(),
        footprint: new GltfNewModelFootprint(0x0003, elevations, new byte[16]),
        horizontalExtents: new GltfNewModelHorizontalExtents(2, 3, 4, 5),
        objectRoles: new Dictionary<GltfNodeHandle, GltfNewModelObjectRole>
        {
          [new GltfNodeHandle(2)] = new(
            GltfStaticObjectRoles.ViewerFaced | GltfStaticObjectRoles.Rotor)
        }));

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var asset = imported.Value!.Asset;
    asset.CommonBaseHeader.BoxPresenceMask.Should().Be(3);
    asset.CommonBaseHeader.HorizontalExtents.Should().Equal(
      new byte[] { 0, 2, 0, 3, 0, 4, 0, 5 });
    asset.StaticRenderObjectSequence[0].KnownFlags.Should().Be(
      StaticRenderObjectFlags.ViewerFaced | StaticRenderObjectFlags.Rotor);
    imported.Value.Preservation.Changes.Should().NotContain(change =>
      change.FieldPath.Contains('*', StringComparison.Ordinal));
    imported.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].ObjectFlags"
      && change.Disposition == PreservationDisposition.Canonicalized);

    Action defaultHandle = () => new GltfNewModelImportOptions(
      textureResourceBindings: new Dictionary<GltfMaterialHandle, string?> { [default] = null });
    defaultHandle.Should().Throw<ArgumentOutOfRangeException>();
    Action conflictingMarkers = () => new GltfNewModelObjectRole(
      GltfStaticObjectRoles.MarkerAttachment1 | GltfStaticObjectRoles.MarkerAttachment2);
    conflictingMarkers.Should().Throw<ArgumentOutOfRangeException>();
  }

  [Fact]
  public async Task EquivalentMetadataFreeGlbAndSeparateGltfAuthorEquivalentCanonicalAssets()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exportedGlb = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exportedGlb,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFreeGlb = RewriteJson(exportedGlb.ToArray(), RemoveEarthToolMetadata);
    await using var glbSource = new MemoryStream(metadataFreeGlb);
    var glbImport = await interchange.ImportNewModelGlbAsync(glbSource);

    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var separateExport = await interchange.ExportGltfFileAsync(
        sourceAsset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      separateExport.Status.Should().Be(OperationStatus.Succeeded);
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveEarthToolMetadata(root);
      await File.WriteAllTextAsync(path, root.ToJsonString());

      var separateImport = await interchange.ImportNewModelGltfFileAsync(path);

      glbImport.Status.Should().Be(OperationStatus.Succeeded);
      separateImport.Status.Should().Be(OperationStatus.Succeeded);
      var glbBytes = glbImport.Value!.Asset.GetSerializedRepresentation();
      var separateBytes = separateImport.Value!.Asset.GetSerializedRepresentation();
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
        path,
        new GltfExportOptions(LineageId, DocumentId));
      export.Status.Should().Be(OperationStatus.Succeeded);
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveEarthToolMetadata(root);
      var bufferName = root["buffers"]![0]!["uri"]!.GetValue<string>();
      var assetsDirectory = Path.Combine(directory, "assets");
      Directory.CreateDirectory(assetsDirectory);
      File.Move(Path.Combine(directory, bufferName), Path.Combine(assetsDirectory, bufferName));
      root["buffers"]![0]!["uri"] = $"assets/{bufferName}";
      await File.WriteAllTextAsync(path, root.ToJsonString());

      var imported = await interchange.ImportNewModelGltfFileAsync(path);

      imported.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
      imported.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task NewModelImportCollapsesGroupsAndPreservesCanonicalHierarchyAndPartitionOrder()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      RemoveArtistHelperNodes(root);
      var nodes = root["nodes"]!.AsArray();
      var meshNode = nodes[0]!.AsObject();
      meshNode["translation"] = new JsonArray(2, 3, 4);
      meshNode["children"] = new JsonArray(2, 3);
      nodes.Insert(0, new JsonObject
      {
        ["scale"] = new JsonArray(-1, 1, 1),
        ["children"] = new JsonArray(1)
      });
      nodes.Add(new JsonObject
      {
        ["mesh"] = 0,
        ["translation"] = new JsonArray(5, 0, 0),
        ["scale"] = new JsonArray(10, 10, 10)
      });
      nodes.Add(new JsonObject
      {
        ["mesh"] = 0,
        ["translation"] = new JsonArray(6, 0, 0)
      });
      root["scenes"]![0]!["nodes"] = new JsonArray(0);
      var primitives = root["meshes"]![0]!["primitives"]!.AsArray();
      primitives.Add(primitives[0]!.DeepClone());
    });
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    var asset = imported.Value!.Asset;
    asset.RootSourceObject.Children.Should().HaveCount(2);
    asset.RootSourceObject.Children.Select(child => child.StaticRenderObjectIds.Count).Should().Equal(2, 2);
    asset.StaticRenderObjectSequence.Select(record => record.SourceObjectId.Value).Should()
      .Equal(1, 2, 2, 3, 3, 1);
    asset.StaticRenderObjectSequence[0].Pivot.Should().Be(new Vector3(-2, -4, 3));
    asset.RootSourceObject.Children.Select(child =>
      asset.StaticRenderObjectSequence.Single(record =>
        record.Id.Equals(child.StaticRenderObjectIds[0])).Pivot.X).Should().Equal(-5, -6);
    var rootTriangle = asset.StaticRenderObjectSequence[0].Triangles.Should().ContainSingle().Subject;
    (rootTriangle.Vertex0, rootTriangle.Vertex1, rootTriangle.Vertex2).Should().Be((0, 2, 1));

    var rootVertices = asset.RootSourceObject.StaticRenderObjectIds
      .SelectMany(id => asset.StaticRenderObjectSequence.Single(record => record.Id.Equals(id)).RenderVertices)
      .ToArray();
    BinaryPrimitives.ReadUInt16LittleEndian(
      asset.CommonBaseHeader.HorizontalExtents.Skip(4).Take(2).ToArray()).Should().Be(
      ToUnsignedFixedPoint(Math.Max(0, rootVertices.Max(vertex => vertex.Position.X))));
    BinaryPrimitives.ReadUInt16LittleEndian(
      asset.CommonBaseHeader.HorizontalExtents.Skip(6).Take(2).ToArray()).Should().Be(
      ToUnsignedFixedPoint(-Math.Min(0, rootVertices.Min(vertex => vertex.Position.X))));
    BinaryPrimitives.ReadUInt16LittleEndian(asset.CommonBaseHeader.BoxTopElevations.Take(2).ToArray())
      .Should().Be(ToUnsignedFixedPoint(rootVertices.Max(vertex => vertex.Position.Z)));
  }

  [Fact]
  public async Task NewModelImportRejectsClaimedEarthToolLineage()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var claimed = new MemoryStream();
    var interchange = new GltfInterchange();
    await interchange.ExportGlbAsync(
      sourceAsset,
      claimed,
      new GltfExportOptions(LineageId, DocumentId));
    claimed.Position = 0;

    var imported = await interchange.ImportNewModelGlbAsync(claimed);

    imported.Status.Should().Be(OperationStatus.Failed);
    imported.Value.Should().BeNull();
    imported.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.OrphanEnvelope);
  }

  [Fact]
  public async Task FailedEditImportNeverFallsBackToNewModelImport()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var editSource = new MemoryStream(metadataFree);

    var edit = await interchange.ImportEditGlbAsync(editSource, export.Value!.Baseline);

    edit.Status.Should().Be(OperationStatus.Failed);
    edit.Value.Should().BeNull();
    edit.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.MissingManifest);
    await using var newSource = new MemoryStream(metadataFree);
    var explicitNew = await interchange.ImportNewModelGlbAsync(newSource);
    explicitNew.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public async Task NewModelImportIgnoresInertAdditionalTextureCoordinatesWithWarning()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var sourceBytes = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      var attributes = root["meshes"]![0]!["primitives"]![0]!["attributes"]!.AsObject();
      attributes["TEXCOORD_1"] = attributes["TEXCOORD_0"]!.DeepClone();
    });
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(OperationStatus.Succeeded);
    imported.Diagnostics.Should().ContainSingle(diagnostic =>
      diagnostic.Code == GltfDiagnosticCodes.InertDataIgnored
      && diagnostic.EventId == 1119
      && diagnostic.Path == "meshes[0].primitives[0].attributes.TEXCOORD_1");
  }

  [Fact]
  public async Task NewModelImportAcceptsSparsePositionAccessorRepresentation()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var sourceBytes = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      var primitive = root["meshes"]![0]!["primitives"]![0]!;
      var accessors = root["accessors"]!.AsArray();
      var bufferViews = root["bufferViews"]!.AsArray();
      var position = accessors[primitive["attributes"]!["POSITION"]!.GetValue<int>()]!.AsObject();
      var indices = accessors[primitive["indices"]!.GetValue<int>()]!.AsObject();
      var sparseIndexView = bufferViews[indices["bufferView"]!.GetValue<int>()]!.DeepClone()!.AsObject();
      sparseIndexView.Remove("target");
      sparseIndexView.Remove("byteStride");
      bufferViews.Add(sparseIndexView);
      var sparseValueView = bufferViews[position["bufferView"]!.GetValue<int>()]!.DeepClone()!.AsObject();
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
          ["componentType"] = indices["componentType"]!.DeepClone()
        },
        ["values"] = new JsonObject
        {
          ["bufferView"] = bufferViews.Count - 1,
          ["byteOffset"] = position["byteOffset"]?.DeepClone() ?? 0
        }
      };
    });
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", imported.Diagnostics.Select(diagnostic => diagnostic.Message)));
    imported.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle();
  }

  [Fact]
  public async Task NewModelImportIgnoresSceneOnlyCameraAndUnusedSamplerWithWarnings()
  {
    var sourceAsset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    await interchange.ExportGlbAsync(
      sourceAsset,
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var sourceBytes = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      RemoveArtistHelperNodes(root);
      var nodes = root["nodes"]!.AsArray();
      nodes.Insert(0, new JsonObject
      {
        ["name"] = "Preview Camera Group",
        ["camera"] = 0,
        ["children"] = new JsonArray(1)
      });
      root["scenes"]![0]!["nodes"] = new JsonArray(0);
      root["cameras"] = new JsonArray(new JsonObject
      {
        ["type"] = "perspective",
        ["perspective"] = new JsonObject { ["yfov"] = 0.7, ["znear"] = 0.1 }
      });
      root["samplers"] = new JsonArray(new JsonObject());
    });
    await using var source = new MemoryStream(sourceBytes);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(OperationStatus.Succeeded);
    imported.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle();
    imported.Diagnostics.Where(diagnostic => diagnostic.Code == GltfDiagnosticCodes.InertDataIgnored)
      .Select(diagnostic => diagnostic.Path).Should().Equal("nodes[0].camera", "samplers");
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
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
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
    });
    if (mutation == "invalid-index")
    {
      BinaryPrimitives.WriteUInt16LittleEndian(
        metadataFree.AsSpan(GetBinaryChunkOffset(metadataFree) + 96),
        3);
    }
    else if (mutation == "normal-overflow")
    {
      var binaryOffset = GetBinaryChunkOffset(metadataFree);
      for (var normalOffset = 36; normalOffset <= 60; normalOffset += 12)
      {
        BinaryPrimitives.WriteInt32LittleEndian(
          metadataFree.AsSpan(binaryOffset + normalOffset),
          BitConverter.SingleToInt32Bits(1));
        BinaryPrimitives.WriteInt32LittleEndian(
          metadataFree.AsSpan(binaryOffset + normalOffset + 8),
          0);
      }
    }
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

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
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), RemoveEarthToolMetadata);
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(
      source,
      profile: new GltfOperationProfile(maxOutputBytes: 1));

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
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      RemoveArtistHelperNodes(root);
      var nodes = root["nodes"]!.AsArray();
      nodes.Insert(0, new JsonObject { ["children"] = new JsonArray(1) });
      root["scenes"]![0]!["nodes"] = new JsonArray(0);
    });
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(
      source,
      profile: new GltfOperationProfile(
        maxInputBytes: 32 * 1024 * 1024,
        maxOutputBytes: 32 * 1024 * 1024,
        maxMetadataBytes: 4 * 1024 * 1024,
        maxJsonDepth: 32,
        maxActiveRenderVertices: 65536,
        maxNodes: 4096,
        maxHierarchyDepth: 1));

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
      exported,
      new GltfExportOptions(LineageId, DocumentId));
    var metadataFree = RewriteJson(exported.ToArray(), root =>
    {
      RemoveEarthToolMetadata(root);
      root["accessors"]![3]!["count"] = 3_145_731;
    });
    await using var source = new MemoryStream(metadataFree);

    var imported = await interchange.ImportNewModelGlbAsync(source);

    imported.Status.Should().Be(OperationStatus.Failed);
    var diagnostic = imported.Diagnostics.Should().ContainSingle().Subject;
    diagnostic.Code.Should().Be(GltfDiagnosticCodes.ResourceLimitExceeded);
    diagnostic.Data.Should().Contain(
      new KeyValuePair<string, string>("actual", "1048577"));
  }

  [Fact]
  public async Task UnchangedMultiPartitionGlbImportRestoresExactMshTopologyAndState()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var asset = await ReadAssetAsync(fixture.Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    glb.Position = 0;

    var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    import.Value!.RestoredSerializedRepresentationPaths.Should().Contain(
      "StaticRenderObjectSequence[3]");
    await using var restored = new MemoryStream();
    var write = await new MshWriter().WriteAsync(import.Value.Asset, restored);
    write.Status.Should().Be(OperationStatus.Succeeded);
    restored.ToArray().Should().Equal(fixture.Data);
  }

  [Fact]
  public async Task BufferOnlySeparateGltfPackageValidatesAndRestoresExactMshState()
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    var asset = await ReadAssetAsync(fixture.Data);
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));

      export.Status.Should().Be(OperationStatus.Succeeded);
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(path));
      var buffers = json.RootElement.GetProperty("buffers");
      buffers.GetArrayLength().Should().Be(1);
      var bufferUri = buffers[0].GetProperty("uri").GetString();
      bufferUri.Should().MatchRegex("^[0-9a-f]{64}\\.bin$");
      var bufferPath = Path.Combine(directory, bufferUri!);
      File.Exists(bufferPath).Should().BeTrue();

      var validation = await interchange.ValidateGltfFileAsync(path);
      validation.Status.Should().Be(OperationStatus.Succeeded);

      var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);
      import.Status.Should().Be(OperationStatus.Succeeded);
      await using var restored = new MemoryStream();
      var write = await new MshWriter().WriteAsync(import.Value!.Asset, restored);
      write.Status.Should().Be(OperationStatus.Succeeded);
      restored.ToArray().Should().Equal(fixture.Data);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task ExportEnforcesFiniteGeometryAndActiveRenderVertexLimit()
  {
    var source = OneTriangleMshFixture.Create();
    var recordOffset = 0x14 + 0x368 + sizeof(uint);
    BinaryPrimitives.WriteInt32LittleEndian(
      source.AsSpan(recordOffset + 0x08),
      BitConverter.SingleToInt32Bits(float.NaN));
    var nonFinite = await ReadAssetAsync(source);
    await using var destination = new MemoryStream();

    var invalid = await new GltfInterchange().ExportGlbAsync(nonFinite, destination);
    var limited = await new GltfInterchange().ExportGlbAsync(
      await ReadAssetAsync(OneTriangleMshFixture.Create()),
      destination,
      profile: new GltfOperationProfile(maxActiveRenderVertices: 2));

    invalid.Status.Should().Be(OperationStatus.Failed);
    invalid.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.InvalidGeometry);
    destination.Length.Should().Be(0);
    limited.Status.Should().Be(OperationStatus.Failed);
    limited.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
  }

  [Fact]
  public async Task EditImportIgnoresIndexWidthAndVertexNumberRepacking()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      var json = await File.ReadAllTextAsync(path);
      json = json.Replace("\"componentType\":5123", "\"componentType\":5121", StringComparison.Ordinal);
      await File.WriteAllTextAsync(path, json);
      using var document = JsonDocument.Parse(json);
      var bufferPath = Path.Combine(
        directory,
        document.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!);
      var binary = await File.ReadAllBytesAsync(bufferPath);
      SwapBlocks(binary, 0, 24, 12);
      SwapBlocks(binary, 36, 60, 12);
      SwapBlocks(binary, 72, 88, 8);
      binary[96] = 2;
      binary[97] = 1;
      binary[98] = 0;
      await File.WriteAllBytesAsync(bufferPath, binary);

      var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);

      import.Status.Should().Be(OperationStatus.Succeeded);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task EditImportAcceptsNonIndexedTriangleList()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), "\"indices\":3,", string.Empty);
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    result.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
      .Subject.Triangles.Should().ContainSingle().Which.Should().Be(
        asset.StaticRenderObjectSequence[0].Triangles[0]);
  }

  [Fact]
  public async Task ExportUsesUnsignedIntIndicesForMaximumVertexIndex()
  {
    var vertices = Enumerable.Range(0, 65536)
      .Select(_ => new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero))
      .ToArray();
    var build = StaticMeshBuilder.Create(
        OneTriangleMshFixture.CreationGuid,
        new MeshAssetLineageId(Guid.Parse("99999999-aaaa-bbbb-cccc-dddddddddddd")))
      .SetRenderObject(vertices, [new CanonicalTriangle(0, 1, ushort.MaxValue)])
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    await using var glb = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset!,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    using var json = ReadGlbJson(glb.ToArray());
    json.RootElement.GetProperty("accessors")[3].GetProperty("componentType").GetInt32()
      .Should().Be(5125);
    glb.Position = 0;
    var validation = await new GltfInterchange().ValidateGlbAsync(glb);
    validation.Status.Should().Be(OperationStatus.Succeeded);
  }

  [Fact]
  public async Task EditImportIgnoresTriangleOrderButRetainsWinding()
  {
    var asset = CreateTwoTriangleAsset();
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(path));
      var bufferPath = Path.Combine(
        directory,
        json.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!);
      var original = await File.ReadAllBytesAsync(bufferPath);
      var reordered = (byte[])original.Clone();
      original.AsSpan(128, 6).CopyTo(reordered.AsSpan(134, 6));
      original.AsSpan(134, 6).CopyTo(reordered.AsSpan(128, 6));
      await File.WriteAllBytesAsync(bufferPath, reordered);

      var reorderedImport = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);

      reorderedImport.Status.Should().Be(OperationStatus.Succeeded);

      var reversed = (byte[])original.Clone();
      BinaryPrimitives.WriteUInt16LittleEndian(reversed.AsSpan(130), 2);
      BinaryPrimitives.WriteUInt16LittleEndian(reversed.AsSpan(132), 1);
      await File.WriteAllBytesAsync(bufferPath, reversed);

      var reversedImport = await interchange.ImportEditGltfFileAsync(path, export.Value.Baseline);

      reversedImport.Status.Should().Be(OperationStatus.Succeeded);
      reversedImport.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
        .Subject.Triangles[0].Should().Be(new StaticTriangle(0, 2, 1, 1));
      reversedImport.Value.RestoredSerializedRepresentationPaths.Should().NotContain(
        "StaticRenderObjectSequence[0]");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task PrimitiveSplitRestoresOriginalPartitionBoundaryExactly()
  {
    var asset = CreateTwoTriangleAsset();
    var original = asset.StaticRenderObjectSequence[0].GetSerializedRepresentation();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    const string primitive =
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0}";
    var bytes = RewriteJson(
      glb.ToArray(),
      primitive,
      primitive + "," + primitive.Replace("\"indices\":3", "\"indices\":4", StringComparison.Ordinal));
    bytes = RewriteJson(
      bytes,
      "{\"bufferView\":3,\"componentType\":5123,\"count\":6,\"type\":\"SCALAR\"}",
      "{\"bufferView\":3,\"componentType\":5123,\"count\":3,\"type\":\"SCALAR\"},"
      + "{\"bufferView\":3,\"byteOffset\":6,\"componentType\":5123,\"count\":3,\"type\":\"SCALAR\"}");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    result.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
      .Subject.GetSerializedRepresentation().Should().Equal(original);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task UnchangedImportRestoresGuardedTopologyFromBothPackageForms(bool separate)
  {
    var source = await CreateGuardedTopologyFixtureAsync();
    var asset = await ReadAssetAsync(source);
    var renderObject = asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    renderObject.RenderVertices.Should().HaveCount(5);
    renderObject.RenderVertices[0].Position.Should().Be(renderObject.RenderVertices[3].Position);
    renderObject.Triangles.Should().Contain(triangle => triangle.Vertex1 == triangle.Vertex2);
    renderObject.RenderVertices[1].NormalSharingIndex.Should().Be(0);
    renderObject.VertexBlockPadding.Should().Contain(0x5A);
    renderObject.Triangles[0].TriangleRenderPassFlags.Should().Be(0x1234);
    var interchange = new GltfInterchange();

    StaticMeshAsset restoredAsset;
    if (separate)
    {
      var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
      var path = Path.Combine(directory, "model.gltf");
      Directory.CreateDirectory(directory);
      try
      {
        var export = await interchange.ExportGltfFileAsync(
          asset,
          path,
          new GltfExportOptions(LineageId, DocumentId));
        var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);
        import.Status.Should().Be(OperationStatus.Succeeded);
        restoredAsset = import.Value!.Asset;
      }
      finally
      {
        Directory.Delete(directory, true);
      }
    }
    else
    {
      await using var glb = new MemoryStream();
      var export = await interchange.ExportGlbAsync(
        asset,
        glb,
        new GltfExportOptions(LineageId, DocumentId));
      glb.Position = 0;
      var import = await interchange.ImportEditGlbAsync(glb, export.Value!.Baseline);
      import.Status.Should().Be(OperationStatus.Succeeded);
      restoredAsset = import.Value!.Asset;
    }

    await using var restored = new MemoryStream();
    var write = await new MshWriter().WriteAsync(restoredAsset, restored);
    write.Status.Should().Be(OperationStatus.Succeeded);
    restored.ToArray().Should().Equal(source);
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
        path,
        new GltfExportOptions(LineageId, DocumentId));

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().HaveCount(2);
      Directory.EnumerateFiles(directory).Should().NotContain(file => file.EndsWith(".tmp", StringComparison.Ordinal));
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
    var options = new GltfExportOptions(LineageId, DocumentId);
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);

    try
    {
      var failing = new GltfInterchange(new FailingManifestTransactionalFileSystem());

      var firstFailure = await failing.ExportGltfFileAsync(asset, path, options);
      var repeatedFailure = await failing.ExportGltfFileAsync(asset, path, options);

      repeatedFailure.Diagnostics.Select(diagnostic => (
        diagnostic.Code,
        diagnostic.EventId,
        diagnostic.Severity,
        diagnostic.Path,
        diagnostic.ByteOffset,
        Data: diagnostic.Data.ToArray())).Should().BeEquivalentTo(
          firstFailure.Diagnostics.Select(diagnostic => (
            diagnostic.Code,
            diagnostic.EventId,
            diagnostic.Severity,
            diagnostic.Path,
            diagnostic.ByteOffset,
            Data: diagnostic.Data.ToArray())),
          assertionOptions => assertionOptions.WithStrictOrdering());
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
    var options = new GltfExportOptions(LineageId, DocumentId);
    Directory.CreateDirectory(directory);
    await File.WriteAllBytesAsync(path, original);
    await new GltfInterchange(new FailingManifestTransactionalFileSystem())
      .ExportGltfFileAsync(asset, path, options);

    try
    {
      var retry = await new GltfInterchange().ExportGltfFileAsync(asset, path, options);

      retry.Status.Should().Be(OperationStatus.Succeeded);
      (await new GltfInterchange().ValidateGltfFileAsync(path)).Status.Should()
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
      var result = await new GltfInterchange(new CorruptingSidecarTransactionalFileSystem())
        .ExportGltfFileAsync(asset, path, new GltfExportOptions(LineageId, DocumentId));

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().HaveCount(2);
      Directory.EnumerateFiles(directory).Should().NotContain(file =>
        file.EndsWith(".tmp", StringComparison.Ordinal));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SeparateGltfValidationAndImportRejectMissingBufferWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var interchange = new GltfInterchange();
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(path));
      var bufferPath = Path.Combine(
        directory,
        json.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!);
      File.Delete(bufferPath);

      var validation = await interchange.ValidateGltfFileAsync(path);
      var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);

      validation.Status.Should().Be(OperationStatus.Failed);
      validation.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
      import.Status.Should().Be(OperationStatus.Failed);
      import.Value.Should().BeNull();
      import.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task DeepReparentRegeneratesCanonicalSequenceAndHierarchyState()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var commonHeader = asset.CommonBaseHeader.SerializedRepresentation.ToArray();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var rootChildren = root["nodes"]![0]!["children"]!.AsArray();
      rootChildren.RemoveAt(1);
      root["nodes"]![1]!["children"] = new JsonArray(2);
    });

    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var reconciled = import.Value!.Asset;
    reconciled.StaticRenderObjectSequence.Select(record => record.LocalId).Should().Equal(1, 2, 4, 3);
    reconciled.StaticRenderObjectSequence.Select(record => record.HierarchyUnwindCount).Should()
      .Equal(0, 0, 0, 2);
    reconciled.StaticRenderObjectSequence.Select(record => record.KnownFlags
      .HasFlag(StaticRenderObjectFlags.BeginsNestedSourceObject)).Should()
      .Equal(false, true, true, false);
    reconciled.StaticRenderObjectSequence.Select(record => record.NextRecordMarker).Should()
      .Equal(1, 1, 1, 0);
    reconciled.StoredTrailingHierarchyUnwindCount.Should().Be(1);
    reconciled.RootSourceObject.Children.Should().ContainSingle().Subject.Children.Should()
      .ContainSingle();
    reconciled.CommonBaseHeader.SerializedRepresentation.Should().Equal(commonHeader);
    foreach (var record in reconciled.StaticRenderObjectSequence)
    {
      var original = asset.StaticRenderObjectSequence.Single(item => item.LocalId == record.LocalId);
      record.TexturePathBytes.Should().Equal(original.TexturePathBytes);
      record.RenderVertices.Should().Equal(original.RenderVertices);
      record.Triangles.Should().Equal(original.Triangles);
      record.AnimationTracks.ScaleFrames.Should().Equal(original.AnimationTracks.ScaleFrames);
      record.AnimationTracks.TranslationFrames.Should().Equal(original.AnimationTracks.TranslationFrames);
      record.AnimationTracks.Matrices.Should().Equal(original.AnimationTracks.Matrices);
      (record.ObjectFlags & ~0x000008FFu).Should().Be(original.ObjectFlags & ~0x000008FFu);
    }
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence" &&
      change.Disposition == PreservationDisposition.Regenerated);
  }

  [Fact]
  public async Task TranslationEditRegeneratesOnlyEffectivePivot()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var sourceRecord = asset.StaticRenderObjectSequence[1];
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), "\"translation\":[1,3,-2]", "\"translation\":[2,3,-2]");
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var record = import.Value!.Asset.StaticRenderObjectSequence[1];
    record.Pivot.Should().Be(new Vector3(2, 2, 3));
    record.RenderVertices.Should().Equal(sourceRecord.RenderVertices);
    record.Triangles.Should().Equal(sourceRecord.Triangles);
    record.TexturePathBytes.Should().Equal(sourceRecord.TexturePathBytes);
    record.ObjectFlags.Should().Be(sourceRecord.ObjectFlags);
    record.AnimationTracks.ScaleFrames.Should().Equal(sourceRecord.AnimationTracks.ScaleFrames);
    import.Value.Asset.StaticRenderObjectSequence[0].GetSerializedRepresentation().Should()
      .Equal(asset.StaticRenderObjectSequence[0].GetSerializedRepresentation());
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[1].Pivot" &&
      change.Disposition == PreservationDisposition.Regenerated);
  }

  [Fact]
  public async Task UniqueSourceObjectDeletionRetainsUnrelatedRecordsAndIdentityGaps()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      RemoveNodeAndReferences(root, 2);
      root["meshes"]!.AsArray().RemoveAt(2);
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var reconciled = import.Value!.Asset;
    reconciled.StaticRenderObjectSequence.Select(record => record.LocalId).Should().Equal(1, 2, 3);
    reconciled.StaticRenderObjectSequence.Should().NotContain(record => record.LocalId == 4);
    reconciled.RootSourceObject.Children.Should().ContainSingle().Subject.Id.Value.Should().Be(2);
    reconciled.StaticRenderObjectSequence.Select(record => record.NextRecordMarker).Should()
      .Equal(1, 1, 0);
    reconciled.StoredTrailingHierarchyUnwindCount.Should().Be(1);
    reconciled.CommonBaseHeader.SerializedRepresentation.Should()
      .Equal(asset.CommonBaseHeader.SerializedRepresentation);
  }

  [Fact]
  public async Task ReflectedNodeTransformRegeneratesGeometryAndReversesWindingOnce()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
      root["nodes"]![0]!["scale"] = new JsonArray(-1, 1, 1));
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var record = import.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    record.RenderVertices.Select(vertex => vertex.Position.X).Should().Equal(0, -1, 0);
    record.Triangles.Should().ContainSingle().Subject.Should().Match<StaticTriangle>(triangle =>
      triangle.Vertex0 == 0 && triangle.Vertex1 == 2 && triangle.Vertex2 == 1);
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].RenderVertices" &&
      change.Disposition == PreservationDisposition.Regenerated);
  }

  [Fact]
  public async Task MatrixTransformUsesTheSameGeometryDependencyPathAsTrs()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
      root["nodes"]![0]!["matrix"] = new JsonArray(
        2, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1));
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    import.Value!.Asset.StaticRenderObjectSequence[0].RenderVertices.Should().NotEqual(
      asset.StaticRenderObjectSequence[0].RenderVertices);
    import.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].RenderVertices" &&
      change.Disposition == PreservationDisposition.Regenerated);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task UntaggedCopyExpandsLinkedMeshAndRequiresForkForDuplicateMesh(bool singleUserMesh)
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var copy = nodes[1]!.DeepClone().AsObject();
      copy["name"] = "Copied source object";
      copy.Remove("extras");
      if (singleUserMesh)
      {
        var meshes = root["meshes"]!.AsArray();
        meshes.Add(meshes[1]!.DeepClone());
        copy["mesh"] = meshes.Count - 1;
      }
      nodes.Add(copy);
      root["nodes"]![0]!["children"]!.AsArray().Add(nodes.Count - 1);
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    if (singleUserMesh)
    {
      import.Status.Should().Be(OperationStatus.Failed);
      import.Value.Should().BeNull();
      import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.DuplicateScopeIdentity);
      return;
    }
    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var reconciled = import.Value!.Asset;
    reconciled.RootSourceObject.Children.Select(child => child.Id.Value).Should().Equal(2, 3, 4);
    reconciled.StaticRenderObjectSequence.Select(record => record.LocalId).Should()
      .Equal(1, 2, 4, 5, 3);
    var copied = reconciled.StaticRenderObjectSequence.Single(record => record.LocalId == 5);
    copied.SourceObjectId.Value.Should().Be(4);
    copied.Pivot.Should().Be(new Vector3(1, 2, 3));
    copied.RenderVertices.Select(vertex => vertex.Position).Should().Equal(
      asset.StaticRenderObjectSequence[1].RenderVertices.Select(vertex => vertex.Position));
    copied.RenderVertices.Should().OnlyContain(vertex =>
      vertex.NormalSharingIndex == ushort.MaxValue
      && vertex.PositionSharingIndex == ushort.MaxValue
      && vertex.ReservedTextureComponent == 0);
    copied.TexturePathBytes.Should().Equal(asset.StaticRenderObjectSequence[1].TexturePathBytes);
    copied.KnownFlags.Should().Be(StaticRenderObjectFlags.BeginsNestedSourceObject);
    copied.HierarchyUnwindCount.Should().Be(1);
  }

  [Fact]
  public async Task TransformOnlyScaffoldingCollapsesIntoDescendantLocalTransform()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      nodes.Add(new JsonObject
      {
        ["name"] = "Blender Empty",
        ["translation"] = new JsonArray(1, 0, 0),
        ["children"] = new JsonArray(1)
      });
      root["nodes"]![0]!["children"] = new JsonArray(nodes.Count - 1, 2);
      foreach (var helperIndex in Enumerable.Range(3, 4))
      {
        root["nodes"]![0]!["children"]!.AsArray().Add(helperIndex);
      }
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var reconciled = import.Value!.Asset;
    reconciled.RootSourceObject.Children.Select(child => child.Id.Value).Should().Equal(2, 3);
    reconciled.StaticRenderObjectSequence[1].Pivot.Should().Be(new Vector3(2, 2, 3));
    reconciled.StaticRenderObjectSequence.Select(record => record.LocalId).Should().Equal(1, 2, 3, 4);
    reconciled.StaticRenderObjectSequence[0].GetSerializedRepresentation().Should()
      .Equal(asset.StaticRenderObjectSequence[0].GetSerializedRepresentation());
  }

  [Fact]
  public async Task SeparateGltfHierarchyAndTransformEditUsesSameReconciliationRules()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var export = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      var root = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      var nodes = root["nodes"]!.AsArray();
      nodes.Add(new JsonObject
      {
        ["name"] = "Blender Empty",
        ["translation"] = new JsonArray(1, 0, 0),
        ["children"] = new JsonArray(1)
      });
      root["nodes"]![1]!["translation"] = new JsonArray(2, 3, -2);
      root["nodes"]![0]!["children"] = new JsonArray(nodes.Count - 1);
      foreach (var helperIndex in Enumerable.Range(3, 4))
      {
        root["nodes"]![0]!["children"]!.AsArray().Add(helperIndex);
      }
      root["nodes"]![1]!["children"] = new JsonArray(2);
      await File.WriteAllTextAsync(path, root.ToJsonString());

      var import = await interchange.ImportEditGltfFileAsync(path, export.Value!.Baseline);

      import.Status.Should().Be(
        OperationStatus.Succeeded,
        string.Join("; ", import.Diagnostics.Select(diagnostic => diagnostic.Message)));
      import.Value!.Asset.StaticRenderObjectSequence.Select(record => record.LocalId).Should()
        .Equal(1, 2, 4, 3);
      import.Value.Asset.StaticRenderObjectSequence[1].Pivot.Should().Be(new Vector3(3, 2, 3));
      import.Value.Asset.StoredTrailingHierarchyUnwindCount.Should().Be(1);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task UntaggedObjectWithMissingExpectedScopeIsAmbiguous()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      RemoveNodeAndReferences(root, 2);
      root["meshes"]!.AsArray().RemoveAt(2);
      var unidentified = nodes[1]!.DeepClone().AsObject();
      unidentified.Remove("extras");
      unidentified["name"] = "Unidentified object";
      nodes.Add(unidentified);
      root["nodes"]![0]!["children"]!.AsArray().Add(nodes.Count - 1);
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Failed);
    import.Value.Should().BeNull();
    import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
  }

  [Fact]
  public async Task DuplicateObjectIdentityRequiresExplicitForkResolution()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      nodes.Add(nodes[1]!.DeepClone());
      root["nodes"]![0]!["children"]!.AsArray().Add(nodes.Count - 1);
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Failed);
    import.Value.Should().BeNull();
    import.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.DuplicateScopeIdentity);

    var resolution = new GltfMetadataConflictResolution(
      import.Diagnostics.Single().Data["conflictKey"],
      GltfMetadataConflictActions.ForkScope);
    await using var retried = new MemoryStream(bytes);
    var forked = await interchange.ImportEditGlbWithResolutionsAsync(
      retried,
      export.Value.Baseline,
      options: new GltfEditImportOptions(new[] { resolution }));

    forked.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", forked.Diagnostics.Select(diagnostic => diagnostic.Message)));
    forked.Value!.Asset.RootSourceObject.Children.Select(child => child.Id.Value)
      .Should().Equal(2, 3, 4);
    forked.Value.Asset.StaticRenderObjectSequence.Select(record => record.LocalId)
      .Should().Equal(1, 2, 4, 5, 3);
    forked.Value.AppliedConflictResolutions.Should().ContainSingle().Which.Should().BeSameAs(resolution);
  }

  [Fact]
  public async Task DeletedHighestIdentitiesAreNotReusedAfterReExport()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var firstGlb = new MemoryStream();
    var firstExport = await interchange.ExportGlbAsync(
      asset,
      firstGlb,
      new GltfExportOptions(LineageId, DocumentId));
    var deletedBytes = RewriteJson(firstGlb.ToArray(), root =>
    {
      RemoveNodeAndReferences(root, 2);
      root["meshes"]!.AsArray().RemoveAt(2);
    });
    await using var deletedGlb = new MemoryStream(deletedBytes);
    var deleted = await interchange.ImportEditGlbAsync(deletedGlb, firstExport.Value!.Baseline);
    deleted.Status.Should().Be(OperationStatus.Succeeded);
    await using var secondGlb = new MemoryStream();
    var secondExport = await interchange.ExportGlbAsync(
      deleted.Value!.Asset,
      secondGlb,
      new GltfExportOptions(LineageId, deleted.Value.NextBaseline.DocumentId));
    var copiedBytes = RewriteJson(secondGlb.ToArray(), root =>
    {
      var nodes = root["nodes"]!.AsArray();
      var copy = nodes[1]!.DeepClone().AsObject();
      copy.Remove("extras");
      nodes.Add(copy);
      root["nodes"]![0]!["children"]!.AsArray().Add(nodes.Count - 1);
    });
    await using var copiedGlb = new MemoryStream(copiedBytes);

    var copied = await interchange.ImportEditGlbAsync(copiedGlb, secondExport.Value!.Baseline);

    copied.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", copied.Diagnostics.Select(diagnostic => diagnostic.Message)));
    copied.Value!.Asset.RootSourceObject.Children.Select(child => child.Id.Value).Should().Equal(2, 4);
    copied.Value.Asset.StaticRenderObjectSequence.Select(record => record.LocalId).Should()
      .Equal(1, 2, 5, 3);
  }

  [Fact]
  public async Task SiblingReorderTriggersDeterministicCanonicalSequencing()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(glb.ToArray(), root =>
    {
      var children = root["nodes"]![0]!["children"]!.AsArray();
      var first = children[0]!.GetValue<int>();
      children[0] = children[1]!.GetValue<int>();
      children[1] = first;
    });
    await using var edited = new MemoryStream(bytes);

    var import = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    import.Status.Should().Be(OperationStatus.Succeeded);
    import.Value!.Asset.RootSourceObject.Children.Select(child => child.Id.Value).Should().Equal(2, 3);
    import.Value.Asset.StaticRenderObjectSequence.Select(record => record.LocalId).Should()
      .Equal(1, 2, 4, 3);
    import.Value.Asset.StaticRenderObjectSequence.Select(record => record.NextRecordMarker).Should()
      .Equal(1, 1, 1, 0);
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
        manifestPath,
        new GltfExportOptions(LineageId, DocumentId));
      first.Status.Should().Be(OperationStatus.Succeeded);
      using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(manifestPath));
      var bufferPath = Path.Combine(
        directory,
        json.RootElement.GetProperty("buffers")[0].GetProperty("uri").GetString()!);
      var originalBuffer = await File.ReadAllBytesAsync(bufferPath);

      var collisionPath = Path.Combine(directory, Path.GetFileName(bufferPath).ToUpperInvariant());
      var collision = await interchange.ExportGltfFileAsync(
        asset,
        collisionPath,
        new GltfExportOptions(LineageId, DocumentId));

      collision.Status.Should().Be(OperationStatus.Failed);
      collision.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
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
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    glbExport.Status.Should().Be(OperationStatus.Succeeded);
    glb.Position = 0;
    var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var path = Path.Combine(directory, "model.gltf");
    Directory.CreateDirectory(directory);

    try
    {
      var gltfExport = await interchange.ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      gltfExport.Status.Should().Be(OperationStatus.Succeeded);

      var glbValidation = await interchange.ValidateGlbAsync(glb, profile);
      var gltfValidation = await interchange.ValidateGltfFileAsync(path, profile);

      glbValidation.Status.Should().Be(OperationStatus.Failed);
      glbValidation.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.MetadataResourceLimitExceeded);
      gltfValidation.Status.Should().Be(OperationStatus.Failed);
      gltfValidation.Diagnostics.Should().ContainSingle().Subject.Code.Should()
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
        cancellationToken: cancellation.Token);

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
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
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
        cancellationToken: cancellation.Token);

      result.Status.Should().Be(OperationStatus.Cancelled);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().HaveCount(2);
      Directory.EnumerateFiles(directory).Should().NotContain(file => file.EndsWith(".tmp", StringComparison.Ordinal));
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
      glbResult.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
      glb.Length.Should().Be(0);
      gltfResult.Status.Should().Be(OperationStatus.Failed);
      gltfResult.Diagnostics.Should().ContainSingle().Subject.Code.Should()
        .Be(GltfDiagnosticCodes.ResourceLimitExceeded);
      (await File.ReadAllBytesAsync(path)).Should().Equal(original);
      Directory.EnumerateFiles(directory).Should().ContainSingle();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task IndependentOneTriangleFixtureCompletesUnchangedGlbRoundTripByteExactly()
  {
    var sourceBytes = OneTriangleMshFixture.Create();
    var asset = await ReadAssetAsync(sourceBytes);
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();

    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));

    export.Status.Should().Be(OperationStatus.Succeeded);
    export.Value!.Baseline.AssetLineageId.Should().Be(LineageId);
    export.Value.Baseline.DocumentId.Should().Be(DocumentId);
    export.Value.Fingerprint.Name.Should().Be("static-geometry");
    export.Value.Fingerprint.Version.Should().Be(1);
    var glbBytes = glb.ToArray();
    var binaryChunkOffset = GetBinaryChunkOffset(glbBytes);
    ReadSingle(glbBytes, binaryChunkOffset + 32).Should().Be(-1f);
    ReadSingle(glbBytes, binaryChunkOffset + 40).Should().Be(1f);

    glb.Position = 0;
    var validation = await interchange.ValidateGlbAsync(glb);
    validation.Status.Should().Be(OperationStatus.Succeeded);

    glb.Position = 0;
    var imported = await interchange.ImportEditGlbAsync(glb, export.Value.Baseline);
    imported.Status.Should().Be(OperationStatus.Succeeded);
    imported.Value!.NextBaseline.DocumentId.Should().NotBe(DocumentId);

    await using var msh = new MemoryStream();
    var write = await new MshWriter().WriteAsync(imported.Value.Asset, msh);
    write.Status.Should().Be(OperationStatus.Succeeded);
    msh.ToArray().Should().Equal(sourceBytes);
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
      glb,
      new GltfExportOptions(LineageId, DocumentId));
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

    var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
      3,
      new StaticAnimationMshFixture.AnimationLengths(0, 0, 0, 2),
      translations: [Vector3.Zero, Vector3.One],
      matrices: [Matrix4x4.Identity, Matrix4x4.CreateRotationY(0.5f)]));
    await using var glb = new MemoryStream();
    var export = await new GltfInterchange().ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
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
      var asset = await ReadAssetAsync(StaticAnimationMshFixture.Create(
        1,
        new StaticAnimationMshFixture.AnimationLengths(0, 2, 0, 0),
        scales: [Vector3.One, new Vector3(1, 2, 1)],
        matrices: [Matrix4x4.Identity, Matrix4x4.CreateRotationZ(0.5f)]));
      var export = await new GltfInterchange().ExportGltfFileAsync(
        asset,
        path,
        new GltfExportOptions(LineageId, DocumentId));
      export.Succeeded.Should().BeTrue();

      await AssertKhronosValidAsync(path);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task IsolatedPositionEditRegeneratesAffectedPartition()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    var binaryChunkOffset = GetBinaryChunkOffset(bytes);
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(binaryChunkOffset),
      BitConverter.SingleToInt32Bits(0.25f));
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Succeeded);
    var renderObject = result.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    renderObject.RenderVertices[0].Position.Should().Be(new Vector3(0.25f, 0, 0));
    renderObject.RenderVertices.Should().OnlyContain(vertex =>
      vertex.NormalSharingIndex == ushort.MaxValue
      && vertex.PositionSharingIndex == ushort.MaxValue
      && vertex.ReservedTextureComponent == 0);
    result.Value.RestoredSerializedRepresentationPaths.Should().NotContain(
      "StaticRenderObjectSequence[0]");
    result.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[0].RenderVertices"
      && change.Disposition == PreservationDisposition.Regenerated);
  }

  [Theory]
  [InlineData("position", 0)]
  [InlineData("normal", 36)]
  [InlineData("uv", 72)]
  public async Task IsolatedGeometryChannelEditRegeneratesOnlyAffectedPartition(
    string channel,
    int channelOffset)
  {
    var fixture = StaticMeshSequenceFixture.CreateInterleaved();
    BinaryPrimitives.WriteInt32LittleEndian(
      fixture.Data.AsSpan(fixture.RecordOffsets[2] + 8),
      BitConverter.SingleToInt32Bits(10f));
    var asset = await ReadAssetAsync(fixture.Data);
    var originalRecords = asset.StaticRenderObjectSequence
      .Select(record => record.GetSerializedRepresentation())
      .ToArray();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(GetBinaryChunkOffset(bytes) + channelOffset),
      BitConverter.SingleToInt32Bits(channel == "normal" ? 1f : 0.25f));
    if (channel == "normal")
    {
      BinaryPrimitives.WriteInt32LittleEndian(
        bytes.AsSpan(GetBinaryChunkOffset(bytes) + channelOffset + sizeof(float)),
        BitConverter.SingleToInt32Bits(0));
    }
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var records = result.Value!.Asset.StaticRenderObjectSequence;
    var dependentRecord = (byte[])originalRecords[1].Clone();
    BinaryPrimitives.WriteUInt16LittleEndian(dependentRecord.AsSpan(8 + 0x90), ushort.MaxValue);
    records[1].GetSerializedRepresentation().Should().Equal(dependentRecord);
    for (var index = 2; index < records.Count; index++)
    {
      records[index].GetSerializedRepresentation().Should().Equal(originalRecords[index]);
    }
    records[0].ObjectFlags.Should().Be(asset.StaticRenderObjectSequence[0].ObjectFlags);
    records[0].TexturePathBytes.Should().Equal(asset.StaticRenderObjectSequence[0].TexturePathBytes);
    records[0].VertexBlockPadding.Should().OnlyContain(value => value == 0);
    records[0].RenderVertices.Should().OnlyContain(vertex =>
      vertex.NormalSharingIndex == ushort.MaxValue
      && vertex.PositionSharingIndex == ushort.MaxValue
      && vertex.ReservedTextureComponent == 0);
    if (channel == "position")
    {
      records[0].RenderVertices[0].Position.X.Should().Be(0.25f);
    }
    else if (channel == "normal")
    {
      records[0].RenderVertices[0].Normal.X.Should().Be(1f);
    }
    else
    {
      records[0].RenderVertices[0].TextureCoordinate.X.Should().Be(0.25f);
    }
    result.Value.RestoredSerializedRepresentationPaths.Should().Contain(
      "StaticRenderObjectSequence[2]");
    result.Value.RestoredSerializedRepresentationPaths.Should().NotContain(
      "StaticRenderObjectSequence[0]");
    result.Value.RestoredSerializedRepresentationPaths.Should().NotContain(
      "StaticRenderObjectSequence[1]");
    result.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[1].RenderVertices[0].NormalSharingIndex"
      && change.Disposition == PreservationDisposition.Canonicalized);
  }

  [Fact]
  public async Task DuplicatePartitionDeletionProducesAmbiguousCorrespondenceWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(StaticMeshSequenceFixture.CreateInterleaved().Data);
    var originalIds = asset.StaticRenderObjectSequence.Select(record => record.LocalId).ToArray();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(
      glb.ToArray(),
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0},",
      string.Empty);
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
    asset.StaticRenderObjectSequence.Select(record => record.LocalId).Should().Equal(originalIds);
  }

  [Fact]
  public async Task MultipleStalePartitionsProduceAmbiguousCorrespondence()
  {
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      CreateTwoPartitionAsset(),
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    var binaryOffset = GetBinaryChunkOffset(bytes);
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(binaryOffset),
      BitConverter.SingleToInt32Bits(0.25f));
    BinaryPrimitives.WriteInt32LittleEndian(
      bytes.AsSpan(binaryOffset + 104),
      BitConverter.SingleToInt32Bits(11f));
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should()
      .Be(GltfDiagnosticCodes.AmbiguousPartitionCorrespondence);
  }

  [Fact]
  public async Task UniquePartitionDeletionRetainsUnaffectedPartitionExactly()
  {
    var asset = CreateTwoPartitionAsset();
    var retained = asset.StaticRenderObjectSequence[1];
    var retainedBytes = retained.GetSerializedRepresentation();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(
      glb.ToArray(),
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0},",
      string.Empty);
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var resultRecord = result.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle().Subject;
    resultRecord.Id.Value.Should().Be(retained.Id.Value);
    resultRecord.Id.Lineage.Value.Should().Be(LineageId);
    resultRecord.GetSerializedRepresentation().Should().Equal(retainedBytes);
    result.Value.Asset.RootSourceObject.StaticRenderObjectIds.Select(id => id.Value).Should()
      .Equal(retained.Id.Value);
  }

  [Fact]
  public async Task ReExportAfterDeletionPreservesSparsePartitionIdentity()
  {
    var interchange = new GltfInterchange();
    await using var firstGlb = new MemoryStream();
    var firstExport = await interchange.ExportGlbAsync(
      CreateTwoPartitionAsset(),
      firstGlb,
      new GltfExportOptions(LineageId, DocumentId));
    var deletedBytes = RewriteJson(
      firstGlb.ToArray(),
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0},",
      string.Empty);
    await using var deletedGlb = new MemoryStream(deletedBytes);
    var deleted = await interchange.ImportEditGlbAsync(
      deletedGlb,
      firstExport.Value!.Baseline);
    deleted.Status.Should().Be(OperationStatus.Succeeded);
    var retainedId = deleted.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
      .Subject.LocalId;
    await using var secondGlb = new MemoryStream();
    var secondExport = await interchange.ExportGlbAsync(
      deleted.Value.Asset,
      secondGlb,
      new GltfExportOptions(LineageId, deleted.Value.NextBaseline.DocumentId));
    secondGlb.Position = 0;

    var secondImport = await interchange.ImportEditGlbAsync(
      secondGlb,
      secondExport.Value!.Baseline);

    secondImport.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", secondImport.Diagnostics.Select(diagnostic => diagnostic.Message)));
    secondImport.Value!.Asset.StaticRenderObjectSequence.Should().ContainSingle()
      .Subject.LocalId.Should().Be(retainedId);
  }

  [Fact]
  public async Task UniquePartitionCopyCreatesCanonicalForkWithFreshIdentity()
  {
    var source = CreateTwoPartitionAsset();
    var bindingEdit = source.Edit()
      .SetTextureResourceBinding(
        source.StaticRenderObjectSequence[0].Id,
        "Textures\\authored\\shared.tex")
      .Commit();
    bindingEdit.TryGetValue(out var editedAsset).Should().BeTrue();
    var asset = editedAsset!;
    var originalIds = asset.StaticRenderObjectSequence.Select(record => record.LocalId).ToArray();
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    const string firstPrimitive =
      "{\"attributes\":{\"POSITION\":0,\"NORMAL\":1,\"TEXCOORD_0\":2},\"indices\":3,\"mode\":4,\"material\":0}";
    var bytes = RewriteJson(
      glb.ToArray(),
      firstPrimitive + ",",
      firstPrimitive + "," + firstPrimitive + ",");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(
      OperationStatus.Succeeded,
      string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    var records = result.Value!.Asset.StaticRenderObjectSequence;
    records.Should().HaveCount(3);
    records.Take(2).Select(record => record.LocalId).Should().Equal(originalIds);
    records[2].LocalId.Should().BeGreaterThan(originalIds.Max());
    records[2].RenderVertices.Select(vertex => vertex.Position).Should().Equal(
      records[0].RenderVertices.Select(vertex => vertex.Position));
    records[2].RenderVertices.Should().OnlyContain(vertex =>
      vertex.NormalSharingIndex == ushort.MaxValue
      && vertex.PositionSharingIndex == ushort.MaxValue
      && vertex.ReservedTextureComponent == 0);
    records[2].TexturePathBytes.Should().Equal("Textures\\authored\\shared.tex"u8.ToArray());
    result.Value.Preservation.Changes.Should().Contain(change =>
      change.FieldPath == "StaticRenderObjectSequence[2].TexturePathBytes"
      && change.Disposition == PreservationDisposition.Canonicalized);
  }

  [Fact]
  public async Task EditImportRejectsForeignLineageWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    var interchange = new GltfInterchange();
    await using var glb = new MemoryStream();
    await interchange.ExportGlbAsync(asset, glb, new GltfExportOptions(LineageId, DocumentId));
    glb.Position = 0;

    var result = await interchange.ImportEditGlbAsync(
      glb,
      new InterchangeBaseline(Guid.NewGuid(), DocumentId));

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.AssetLineageMismatch);
  }

  [Fact]
  public async Task EditImportRejectsDetachedMeshNodeWithoutPartialAsset()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    ReplaceFirst(bytes, "\"nodes\":[0]", "\"nodes\":[] ");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.UnsupportedDomain);
  }

  [Fact]
  public async Task EditImportRejectsUnsupportedFingerprintProjection()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    ReplaceFirst(bytes, "static-geometry", "static-geometrx");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.UnsupportedGuard);
  }

  [Fact]
  public async Task EditImportRejectsUnsupportedPrimitiveAttribute()
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = RewriteJson(
      glb.ToArray(),
      "\"TEXCOORD_0\":2}",
      "\"TEXCOORD_0\":2,\"COLOR_0\":1}");
    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.UnsupportedDomain);
  }

  [Theory]
  [InlineData("missing-uv")]
  [InlineData("non-triangle")]
  [InlineData("invalid-index")]
  public async Task EditImportRejectsInvalidGeometryWithoutPartialAsset(string mutation)
  {
    var asset = await ReadAssetAsync(OneTriangleMshFixture.Create());
    await using var glb = new MemoryStream();
    var interchange = new GltfInterchange();
    var export = await interchange.ExportGlbAsync(
      asset,
      glb,
      new GltfExportOptions(LineageId, DocumentId));
    var bytes = glb.ToArray();
    if (mutation == "missing-uv")
    {
      bytes = RewriteJson(bytes, ",\"TEXCOORD_0\":2", string.Empty);
    }
    else if (mutation == "non-triangle")
    {
      ReplaceFirst(bytes, "\"mode\":4", "\"mode\":1");
    }
    else
    {
      BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(GetBinaryChunkOffset(bytes) + 96), 3);
    }

    await using var edited = new MemoryStream(bytes);

    var result = await interchange.ImportEditGlbAsync(edited, export.Value!.Baseline);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle();
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
        path,
        new GltfExportOptions(LineageId, DocumentId));

      result.Status.Should().Be(OperationStatus.Failed);
      result.Diagnostics.Should().ContainSingle().Subject.Code.Should().Be(GltfDiagnosticCodes.IoFailure);
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
        cancellationToken: cancellation.Token);

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
      typeof(GltfInterchange).Assembly.ExportedTypes.Where(type => type.Namespace == "EarthTool.GLTF"));
  }

  private static async Task<StaticMeshAsset> ReadAssetAsync(byte[] source)
  {
    await using var stream = new MemoryStream(source);
    var result = await new MshReader().ReadAsync(stream);
    return result.Value.Should().BeOfType<StaticMeshAsset>().Subject;
  }

  private static StaticMeshAsset CreateTwoTriangleAsset()
  {
    var build = StaticMeshBuilder.Create(
        OneTriangleMshFixture.CreationGuid,
        new MeshAssetLineageId(Guid.Parse("99999999-8888-7777-6666-555555555555")))
      .SetRenderObject(
        [
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
          new CanonicalStaticVertex(Vector3.One, Vector3.UnitZ, Vector2.One)
        ],
        [new CanonicalTriangle(0, 1, 2), new CanonicalTriangle(2, 1, 3)])
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static StaticMeshAsset CreateTwoPartitionAsset()
  {
    var vertices = new[]
    {
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY)
    };
    var translated = vertices.Select(vertex => new CanonicalStaticVertex(
      vertex.Position + new Vector3(10, 0, 0),
      vertex.Normal,
      vertex.TextureCoordinate));
    var build = StaticMeshBuilder.Create(
        OneTriangleMshFixture.CreationGuid,
        new MeshAssetLineageId(Guid.Parse("88888888-9999-aaaa-bbbb-cccccccccccc")))
      .SetRootSourceObject(new CanonicalStaticSourceObject(
      [
        new CanonicalStaticRenderObject(vertices, [new CanonicalTriangle(0, 1, 2)]),
        new CanonicalStaticRenderObject(translated, [new CanonicalTriangle(0, 1, 2)])
      ]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static async Task<byte[]> CreateGuardedTopologyFixtureAsync()
  {
    var build = StaticMeshBuilder.Create(
        OneTriangleMshFixture.CreationGuid,
        new MeshAssetLineageId(Guid.Parse("77777777-8888-9999-aaaa-bbbbbbbbbbbb")))
      .SetRenderObject(
        [
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
          new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
          new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
          new CanonicalStaticVertex(Vector3.One, Vector3.UnitZ, Vector2.One)
        ],
        [new CanonicalTriangle(0, 1, 2), new CanonicalTriangle(3, 1, 1)])
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
      0);
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

  private static void RemoveArtistHelperNodes(JsonObject root)
  {
    var nodes = root["nodes"]!.AsArray();
    var helperIndices = nodes.Select((node, index) => (node, index))
      .Where(item => item.node?["name"]?.GetValue<string>() is string name
        && (name.StartsWith("ET_Attachment_", StringComparison.Ordinal)
          || name.StartsWith("ET_CannonRenderPosition_", StringComparison.Ordinal)))
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

  private static void AssertOnlyExpectedBlenderChanges(
    string scenario,
    IReadOnlyList<PreservationChange> changes)
  {
    changes.Where(change =>
      change.Disposition != PreservationDisposition.Retained
      && !IsExpectedBlenderChange(scenario, change.FieldPath)).Should().BeEmpty();
  }

  private static bool IsExpectedBlenderChange(string scenario, string path)
  {
    return scenario switch
    {
      "hierarchy" => path is "StaticRenderObjectSequence"
          or "RootSourceObject"
          or "StoredTrailingHierarchyUnwindCount"
        || path.StartsWith("StaticRenderObjectSequence[", StringComparison.Ordinal)
        && (path.EndsWith(".ObjectFlags", StringComparison.Ordinal)
          || path.EndsWith(".NextRecordMarker", StringComparison.Ordinal)
          || path.EndsWith(".HierarchyUnwindCount", StringComparison.Ordinal)),
      "geometry" => path is "StaticRenderObjectSequence[0].RenderVertices"
        or "StaticRenderObjectSequence[0].Triangles"
        or "StaticRenderObjectSequence[0].VertexBlockPadding",
      "material" => false,
      "animation" => path.StartsWith(
        "StaticRenderObjectSequence[0].AnimationTracks.",
        StringComparison.Ordinal),
      "attachment" => path == "CommonBaseHeader.AttachmentTable[3]",
      "light" => path is "CommonBaseHeader.AttachmentTable[13]"
        or "CommonBaseHeader.StaticSpotLights[1].Position"
        or "CommonBaseHeader.StaticSpotLights[1].TerrainLightAmplitude",
      _ => false
    };
  }

  private static async Task AssertKhronosValidAsync(string path)
  {
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
    var startInfo = new ProcessStartInfo(
      "node",
      $"\"{Path.Combine(root, "test-tools", "validate-glb.mjs")}\" \"{path}\"")
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };
    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Node did not start.");
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    process.ExitCode.Should().Be(0, $"validator stdout: {output} stderr: {error}");
    output.Should().Contain("\"errors\":0");
    output.Should().Contain("\"warnings\":0");
  }

  private static async Task<BlenderOutputEvidence> RoundTripThroughBlenderAsync(
    string inputPath,
    string outputPath,
    bool separate,
    string scenario = "none")
  {
    var scriptPath = Path.Combine(Path.GetDirectoryName(outputPath)!, "round-trip.py");
    await File.WriteAllTextAsync(scriptPath, """
      import bpy
      import json
      import sys

      args = sys.argv[sys.argv.index("--") + 1:]
      bpy.ops.wm.read_factory_settings(use_empty=True)
      bpy.ops.import_scene.gltf(
          filepath=args[0],
          merge_vertices=False,
          import_scene_extras=True)
      scenario = args[3]
      mesh_objects = [item for item in bpy.context.scene.objects if item.type == 'MESH']
      if scenario == 'hierarchy':
          if len(mesh_objects) < 3:
              raise RuntimeError('hierarchy scenario requires three mesh objects')
          mesh_objects[-1].parent = mesh_objects[-2]
      elif scenario == 'geometry':
          mesh_objects[0].data.vertices[0].co.x += 0.25
      elif scenario == 'material':
          bpy.data.materials[0].name = 'Artist material'
      elif scenario == 'animation':
          action = list(bpy.data.actions)[0]
          if hasattr(action, 'fcurves'):
              curves = list(action.fcurves)
          else:
              curves = [curve for layer in action.layers
                        for strip in layer.strips
                        for channelbag in strip.channelbags
                        for curve in channelbag.fcurves]
          location_curves = [curve for curve in curves if curve.data_path == 'location']
          if not location_curves:
              raise RuntimeError('animation scenario requires a location curve')
          location_curves[0].keyframe_points[-1].co.y += 0.25
      elif scenario == 'attachment':
          attachments = [item for item in bpy.context.scene.objects
                         if 'earthtool' in item and
                         'attachment' in json.loads(item['earthtool']).get('payload', {})]
          if len(attachments) != 1:
              raise RuntimeError('attachment scenario requires one attachment')
          attachments[0].location.x += 0.25
      elif scenario == 'light':
          lights = [item for item in bpy.context.scene.objects if item.type == 'LIGHT']
          if len(lights) != 1:
              raise RuntimeError('light scenario requires one light')
          lights[0].location.x += 0.25
          lights[0].data.energy *= 1.25
      elif scenario == 'metadata-loss':
          del bpy.context.scene['earthtool']
      elif scenario == 'stale':
          metadata = json.loads(mesh_objects[0].data['earthtool'])
          metadata['guards']['nativeProjection']['digest'] = 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'
          mesh_objects[0].data['earthtool'] = json.dumps(metadata, separators=(',', ':'))
      elif scenario == 'ambiguity':
          copy = mesh_objects[0].copy()
          copy.data = mesh_objects[0].data.copy()
          bpy.context.scene.collection.objects.link(copy)
          copy.parent = mesh_objects[0]
      bpy.context.scene.render.fps = 24
      bpy.context.scene.render.fps_base = 1
      bpy.context.scene.frame_step = 1
      bpy.ops.export_scene.gltf(
          filepath=args[1],
          export_format=args[2],
          export_extras=True,
          export_attributes=True,
          export_lights=True,
          export_yup=True)
      """);
    var blenderExecutable = Environment.GetEnvironmentVariable("EARTHTOOL_BLENDER_EXECUTABLE")
      ?? "blender";
    var startInfo = new ProcessStartInfo(blenderExecutable)
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };
    foreach (var argument in new[]
    {
      "--background",
      "--factory-startup",
      "--python",
      scriptPath,
      "--",
      inputPath,
      outputPath,
      separate ? "GLTF_SEPARATE" : "GLB",
      scenario
    })
    {
      startInfo.ArgumentList.Add(argument);
    }
    using var process = Process.Start(startInfo)
      ?? throw new InvalidOperationException("Blender did not start.");
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    process.ExitCode.Should().Be(0, $"Blender stdout: {output} stderr: {error}");
    File.Exists(outputPath).Should().BeTrue($"Blender stdout: {output} stderr: {error}");

    if (Environment.GetEnvironmentVariable("EARTHTOOL_RUN_KHRONOS_VALIDATOR") == "1")
    {
      await AssertKhronosValidAsync(outputPath);
    }
    var outputSha256 = await ComputeBlenderPackageHashAsync(outputPath, separate);
    return new BlenderOutputEvidence(
      separate ? "gltf" : "glb",
      outputSha256,
      Environment.GetEnvironmentVariable("EARTHTOOL_RUN_KHRONOS_VALIDATOR") == "1"
        ? "passed"
        : "not-run");
  }

  private static async Task RecordBlenderEvidenceAsync(
    BlenderOutputEvidence output,
    string earthToolOutcome,
    IEnumerable<PreservationChange> changes,
    params string[] domains)
  {
    var evidencePath = Environment.GetEnvironmentVariable("EARTHTOOL_BLENDER_EVIDENCE_EVENTS");
    if (string.IsNullOrWhiteSpace(evidencePath))
    {
      return;
    }
    foreach (var domain in domains)
    {
      var json = JsonSerializer.Serialize(new
      {
        domain,
        package = output.Package,
        outputSha256 = output.OutputSha256,
        sharpGltfValidation = "passed",
        khronosValidation = output.KhronosValidation,
        earthToolOutcome,
        options = new
        {
          import = new[]
          {
            "import_merge_vertices=false",
            "import_scene_extras=true"
          },
          export = new[]
          {
            output.Package == "glb" ? "export_format=GLB" : "export_format=GLTF_SEPARATE",
            "export_extras=true",
            "export_attributes=true",
            "export_lights=true",
            "export_yup=true",
            "scene.render.fps=24",
            "scene.render.fps_base=1",
            "scene.frame_step=1"
          }
        },
        preservation = changes.Select(change => new
        {
          fieldPath = change.FieldPath,
          disposition = change.Disposition.ToString()
        })
      });
      await File.AppendAllTextAsync(evidencePath, json + Environment.NewLine);
    }
  }

  private static async Task<string> ComputeBlenderPackageHashAsync(string outputPath, bool separate)
  {
    if (!separate)
    {
      await using var output = File.OpenRead(outputPath);
      return Convert.ToHexString(await SHA256.HashDataAsync(output)).ToLowerInvariant();
    }

    using var manifest = JsonDocument.Parse(await File.ReadAllBytesAsync(outputPath));
    var directory = Path.GetDirectoryName(outputPath)!;
    var resources = new[] { Path.GetFileName(outputPath) }
      .Concat(manifest.RootElement.TryGetProperty("buffers", out var buffers)
        ? buffers.EnumerateArray().Select(buffer => buffer.GetProperty("uri").GetString()!)
        : [])
      .Concat(manifest.RootElement.TryGetProperty("images", out var images)
        ? images.EnumerateArray()
          .Where(image => image.TryGetProperty("uri", out var uri)
            && !uri.GetString()!.StartsWith("data:", StringComparison.Ordinal))
          .Select(image => image.GetProperty("uri").GetString()!)
        : [])
      .Select(Uri.UnescapeDataString)
      .Distinct(StringComparer.Ordinal)
      .Order(StringComparer.Ordinal);
    using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    foreach (var resource in resources)
    {
      hash.AppendData(Encoding.UTF8.GetBytes(resource));
      hash.AppendData([0]);
      hash.AppendData(await File.ReadAllBytesAsync(Path.Combine(directory, resource)));
      hash.AppendData([0]);
    }
    return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
  }

  private sealed record BlenderOutputEvidence(
    string Package,
    string OutputSha256,
    string KhronosValidation);

  private static float ReadSingle(byte[] data, int offset)
  {
    return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset)));
  }

  private static float[] ReadFloatAccessor(
    byte[] glb,
    JsonElement root,
    int accessorIndex,
    int dimensions)
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
          offset + element * stride + component * sizeof(float));
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
      + (accessor.TryGetProperty("byteOffset", out var accessorOffset) ? accessorOffset.GetInt32() : 0);
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

  private static void ReplaceFirst(byte[] data, string oldValue, string newValue)
  {
    var oldBytes = Encoding.UTF8.GetBytes(oldValue);
    var newBytes = Encoding.UTF8.GetBytes(newValue);
    newBytes.Length.Should().Be(oldBytes.Length);
    var offset = data.AsSpan().IndexOf(oldBytes);
    offset.Should().BeGreaterThanOrEqualTo(0, $"'{oldValue}' should exist in the GLB");
    newBytes.CopyTo(data, offset);
  }

  private static byte[] RewriteJson(byte[] glb, string oldValue, string newValue)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var json = Encoding.UTF8.GetString(glb, 20, jsonLength).TrimEnd();
    json.Should().Contain(oldValue);
    var rewrittenJson = Encoding.UTF8.GetBytes(json.Replace(oldValue, newValue, StringComparison.Ordinal));
    return RewriteJsonChunk(glb, rewrittenJson);
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
    float? maximum = null)
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
    views.Add(new JsonObject
    {
      ["buffer"] = 0,
      ["byteOffset"] = offset,
      ["byteLength"] = values.Count * sizeof(float)
    });
    var accessor = new JsonObject
    {
      ["bufferView"] = viewIndex,
      ["componentType"] = 5126,
      ["count"] = count,
      ["type"] = type
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
      if (owner["extras"] is JsonObject extras)
      {
        extras.Remove("earthtool");
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

  private sealed class FailingTransactionalFileSystem : EarthTool.GLTF.Internal.ITransactionalFileSystem
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
