using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;

namespace EarthTool.MSH.Tests;

public sealed class CanonicalDynamicGltfCreationTests
{
  [Fact]
  public async Task ChildTranslationAnimationRegeneratesStartAndEndFromGltfHierarchy()
  {
    var child = DynamicEffectRecipes.Group().SetChildTranslation(
      new Vector3(1, 2, 3),
      new Vector3(4, 5, 6)
    );
    var source = DynamicMeshBuilder.Create()
      .SetRoot(DynamicEffectRecipes.Group([child]))
      .Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(sourceAsset!, exported);
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    var sourceFree = RewriteGlb(exported.ToArray(), RemoveSourceMetadata);
    await using var input = new MemoryStream(sourceFree);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    AssertEqualExceptCreationGuid(
      sourceAsset!.GetSerializedRepresentation(),
      result.Value!.Asset.GetSerializedRepresentation()
    );
  }

  [Fact]
  public async Task DuplicateDynamicIdentifiersFailWithoutAnAsset()
  {
    var source = DynamicMeshBuilder.Create()
      .SetRoot(DynamicEffectRecipes.Group([DynamicEffectRecipes.Group()]))
      .Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    var sourceFree = await CreateSourceFreeGlbAsync(sourceAsset!, metadata: null);
    sourceFree = RewriteGlb(sourceFree, root =>
      root["nodes"]![2]!["name"] = root["nodes"]![1]!["name"]!.GetValue<string>()
    );
    await using var input = new MemoryStream(sourceFree);

    var result = await new GltfInterchange().CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.DuplicateOwner
    );
  }

  [Fact]
  public async Task MissingRequiredTypedValuesFailWithoutUsingPreviewOrImageIdentity()
  {
    var frames = new CanonicalDynamicFrameSequence(0, 1, 1);
    const string textureKey = "Textures\\effects\\required.tex";
    var source = DynamicMeshBuilder.Create()
      .SetRoot(
        DynamicEffectRecipes.Track(
          frames,
          new EffectRectangle(-1, 1, 1, -1),
          new EffectRectangle(-1, 1, 1, -1),
          textureKey,
          new CanonicalDynamicAlpha(1, 1, DynamicAlphaTiming.FramePhase),
          false
        )
      )
      .Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    var sourceFree = await CreateSourceFreeGlbAsync(sourceAsset!, metadata: null);
    var options = new GltfNewModelImportOptions(
      textureResourceBindings: new Dictionary<GltfMaterialHandle, string?>
      {
        [new GltfMaterialHandle(1)] = textureKey,
      }
    );
    await using var input = new MemoryStream(sourceFree);

    var result = await new GltfInterchange().CreateMeshAsync(input, options);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().Contain(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.RequiredValueMissing
    );
  }

  [Fact]
  public async Task OptionalUnsupportedTypedValuesDefaultWithWarnings()
  {
    var source = DynamicMeshBuilder.Create().SetRoot(DynamicEffectRecipes.Group()).Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    const string metadata =
      "{\"format\":\"earthtool.msh.authoring\",\"version\":1,\"values\":{"
      + "\"additive\":true,\"endAlpha\":0.25}}";
    var sourceFree = await CreateSourceFreeGlbAsync(sourceAsset!, metadata);
    await using var input = new MemoryStream(sourceFree);

    var result = await new GltfInterchange().CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result.Value!.Asset.Should().BeOfType<DynamicMeshAsset>();
    result.Diagnostics.Should().HaveCount(2);
    result.Diagnostics.Should().OnlyContain(item =>
      item.Code == GltfAuthoringMetadataDiagnosticCodes.OptionalValueDefaulted
      && item.Severity == DiagnosticSeverity.Warning
    );
  }

  [Fact]
  public async Task MissingExplicitTextureBindingFailsWithoutInferringImageIdentity()
  {
    var authored = CreateEffect(DynamicEffectType.Sphere);
    var source = DynamicMeshBuilder.Create().SetRoot(authored.Object).Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    var metadata = CanonicalAuthoringMetadata.Write(
      CanonicalAuthoringOwner.Parse("ET_Dynamic_1_Sphere"),
      authored.Values,
      GltfOperationProfile.Default
    );
    var sourceFree = await CreateSourceFreeGlbAsync(sourceAsset!, metadata);
    await using var input = new MemoryStream(sourceFree);

    var result = await new GltfInterchange().CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Failed);
    result.Value.Should().BeNull();
    result.Diagnostics.Should().ContainSingle(item =>
      item.Code == GltfDiagnosticCodes.TextureResourceBindingRequired
    );
  }

  [Theory]
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
  public async Task EverySupportedVisibleEffectRegeneratesWithoutSourceMsh(
    DynamicEffectType effectType
  )
  {
    var authored = CreateEffect(effectType);
    var source = DynamicMeshBuilder.Create().SetRoot(authored.Object).Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    var owner = CanonicalAuthoringOwner.Parse($"ET_Dynamic_1_{effectType}");
    var metadata = CanonicalAuthoringMetadata.Write(
      owner,
      authored.Values,
      GltfOperationProfile.Default
    );
    var sourceFree = await CreateSourceFreeGlbAsync(sourceAsset!, metadata);
    var options = new GltfNewModelImportOptions(
      textureResourceBindings: new Dictionary<GltfMaterialHandle, string?>
      {
        [new GltfMaterialHandle(1)] = authored.TextureKey,
      },
      meshResourceBindings: authored.MeshKey is null
        ? null
        : new Dictionary<GltfNodeHandle, string>
        {
          [new GltfNodeHandle(2)] = authored.MeshKey,
        }
    );
    await using var input = new MemoryStream(sourceFree);

    var result = await new GltfInterchange().CreateMeshAsync(input, options);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var actual = result.Value!.Asset.Should().BeOfType<DynamicMeshAsset>().Subject;
    AssertEqualExceptCreationGuid(
      sourceAsset!.GetSerializedRepresentation(),
      actual.GetSerializedRepresentation()
    );
  }

  [Fact]
  public async Task EquivalentGlbAndSeparateGltfProduceIdenticalPayloadOutsideCreationGuid()
  {
    var source = DynamicMeshBuilder.Create()
      .SetRoot(DynamicEffectRecipes.Group([DynamicEffectRecipes.Group()]))
      .Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var glbExport = new MemoryStream();
    var glbResult = await interchange.ExportGlbAsync(sourceAsset!, glbExport);
    glbResult.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(glbResult.Diagnostics));
    var sourceFreeGlb = RewriteGlb(glbExport.ToArray(), RemoveSourceMetadata);
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-canonical-dynamic-{Guid.NewGuid():N}");
    var path = Path.Combine(directory, "effect.gltf");
    Directory.CreateDirectory(directory);
    try
    {
      var gltfResult = await interchange.ExportGltfFileAsync(sourceAsset!, path);
      gltfResult.Status.Should().Be(
        OperationStatus.Succeeded,
        Diagnostics(gltfResult.Diagnostics)
      );
      var separateRoot = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
      RemoveSourceMetadata(separateRoot);
      await File.WriteAllTextAsync(path, separateRoot.ToJsonString());
      await using var glbInput = new MemoryStream(sourceFreeGlb);

      var glbCreated = await interchange.CreateMeshAsync(glbInput);
      var gltfCreated = await interchange.CreateMeshFileAsync(path);

      glbCreated.Status.Should().Be(
        OperationStatus.Succeeded,
        Diagnostics(glbCreated.Diagnostics)
      );
      gltfCreated.Status.Should().Be(
        OperationStatus.Succeeded,
        Diagnostics(gltfCreated.Diagnostics)
      );
      AssertEqualExceptCreationGuid(
        glbCreated.Value!.Asset.GetSerializedRepresentation(),
        gltfCreated.Value!.Asset.GetSerializedRepresentation()
      );
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SourceFreeTrackSolvesVisibleFramePhaseBackToAuthoredStartValues()
  {
    var frames = new CanonicalDynamicFrameSequence(0, 2, 0);
    var start = new EffectRectangle(0, 0, 0, 0);
    var end = new EffectRectangle(1, 1, 1, 1);
    var alpha = new CanonicalDynamicAlpha(0, 1, DynamicAlphaTiming.FramePhase);
    const string textureKey = "Textures\\effects\\track.tex";
    var source = DynamicMeshBuilder.Create()
      .SetRoot(
        DynamicEffectRecipes.Track(frames, start, end, textureKey, alpha, false)
      )
      .Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    var metadata = CanonicalAuthoringMetadata.Write(
      CanonicalAuthoringOwner.Parse("ET_Dynamic_1_Track"),
      new DynamicAuthoringValues(
        frames,
        end,
        alphaTiming: alpha.Timing,
        endAlpha: alpha.EndAlpha
      ),
      GltfOperationProfile.Default
    );
    var sourceFree = await CreateSourceFreeGlbAsync(sourceAsset!, metadata);
    var options = new GltfNewModelImportOptions(
      textureResourceBindings: new Dictionary<GltfMaterialHandle, string?>
      {
        [new GltfMaterialHandle(1)] = textureKey,
      }
    );
    await using var input = new MemoryStream(sourceFree);

    var result = await new GltfInterchange().CreateMeshAsync(input, options);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var actual = result.Value!.Asset.Should().BeOfType<DynamicMeshAsset>().Subject;
    AssertEqualExceptCreationGuid(
      sourceAsset!.GetSerializedRepresentation(),
      actual.GetSerializedRepresentation()
    );
  }

  [Fact]
  public async Task SourceFreeExplosionRegeneratesVisibleTypedAndResourceSemantics()
  {
    var frames = new CanonicalDynamicFrameSequence(2, 4, 3);
    var sprite = new CanonicalDynamicSpriteSheet(frames, 3, 2);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-2, 3, 4, -5),
      new EffectRectangle(-1, 2, 3, -4),
      0.75f
    );
    var alpha = new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.LifetimeProgress);
    var light = new CanonicalDynamicTerrainLight(
      DynamicLightType.Pyramid,
      new Vector3(0.1f, 0.2f, 0.3f)
    );
    const string textureKey = "Textures\\effects\\blast.tex";
    var source = DynamicMeshBuilder.Create()
      .SetRoot(
        DynamicEffectRecipes.Explosion(
          sprite,
          shape,
          textureKey,
          new Vector3(0.25f, 0.5f, 0.75f),
          alpha,
          true,
          light
        )
      )
      .Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    var metadata = CanonicalAuthoringMetadata.Write(
      CanonicalAuthoringOwner.Parse("ET_Dynamic_1_Explosion"),
      new DynamicAuthoringValues(
        sprite,
        shape.EndEffectRectangle,
        light,
        1,
        alpha.Timing,
        alpha.EndAlpha,
        true
      ),
      GltfOperationProfile.Default
    );
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(sourceAsset!, exported);
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    var sourceFree = RewriteGlb(exported.ToArray(), root =>
    {
      root["scenes"]![0]!.AsObject().Remove("extras");
      root["nodes"]![1]!["extras"] = new JsonObject { ["earthtool"] = metadata };
    });
    var options = new GltfNewModelImportOptions(
      textureResourceBindings: new Dictionary<GltfMaterialHandle, string?>
      {
        [new GltfMaterialHandle(1)] = textureKey,
      }
    );
    await using var input = new MemoryStream(sourceFree);

    var result = await interchange.CreateMeshAsync(input, options);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var actual = result.Value!.Asset.Should().BeOfType<DynamicMeshAsset>().Subject;
    actual.Origin.Should().Be(MeshAssetOrigin.Canonical);
    AssertEqualExceptCreationGuid(
      sourceAsset!.GetSerializedRepresentation(),
      actual.GetSerializedRepresentation()
    );
  }

  [Fact]
  public async Task SourceFreeGroupHierarchyUsesNamesAndGltfChildOrder()
  {
    var source = DynamicMeshBuilder.Create()
      .SetRoot(
        DynamicEffectRecipes.Group(
          [DynamicEffectRecipes.Group(), DynamicEffectRecipes.Group()]
        )
      )
      .Build();
    source.TryGetValue(out var sourceAsset).Should().BeTrue();
    var interchange = new GltfInterchange();
    await using var exported = new MemoryStream();
    var export = await interchange.ExportGlbAsync(sourceAsset!, exported);
    export.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(export.Diagnostics));
    var sourceFree = RewriteGlb(exported.ToArray(), root =>
    {
      root["scenes"]![0]!.AsObject().Remove("extras");
      var nodes = root["nodes"]!.AsArray();
      for (var index = 1; index < nodes.Count; index++)
      {
        nodes[index]!.AsObject().Remove("extras");
      }
      nodes[1]!["name"] = "ET_Dynamic_90_Group";
      nodes[2]!["name"] = "ET_Dynamic_7_Group";
      nodes[3]!["name"] = "ET_Dynamic_2_Group";
      nodes[1]!["children"] = new JsonArray(3, 2);
    });
    await using var input = new MemoryStream(sourceFree);

    var result = await interchange.CreateMeshAsync(input);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var asset = result.Value!.Asset.Should().BeOfType<DynamicMeshAsset>().Subject;
    asset.Origin.Should().Be(MeshAssetOrigin.Canonical);
    asset.RootDynamicObject.Extension.KnownEffectType.Should().Be(DynamicEffectType.Group);
    asset.RootDynamicObject.Children.Should().HaveCount(2);
    asset.RootDynamicObject.Children.Select(child => child.Extension.KnownEffectType)
      .Should().Equal(DynamicEffectType.Group, DynamicEffectType.Group);
    asset.RootDynamicObject.CommonBaseHeader.SerializedRepresentation.Should().Equal(
      asset.RootDynamicObject.Children[0].CommonBaseHeader.SerializedRepresentation
    );
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

  private static async Task<byte[]> CreateSourceFreeGlbAsync(
    DynamicMeshAsset source,
    string? metadata
  )
  {
    await using var exported = new MemoryStream();
    var result = await new GltfInterchange().ExportGlbAsync(source, exported);
    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    return RewriteGlb(exported.ToArray(), root =>
    {
      root["scenes"]![0]!.AsObject().Remove("extras");
      if (metadata is null)
      {
        root["nodes"]![1]!.AsObject().Remove("extras");
      }
      else
      {
        root["nodes"]![1]!["extras"] = new JsonObject { ["earthtool"] = metadata };
      }
    });
  }

  private static void RemoveSourceMetadata(JsonObject root)
  {
    root["scenes"]![0]!.AsObject().Remove("extras");
    foreach (var node in root["nodes"]!.AsArray().Skip(1))
    {
      node!.AsObject().Remove("extras");
    }
  }

  private static string Diagnostics(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return string.Join(Environment.NewLine, diagnostics.Select(item => item.ToString()));
  }

  private static void AssertEqualExceptCreationGuid(byte[] expected, byte[] actual)
  {
    actual[..8].Should().Equal(expected[..8]);
    actual[24..].Should().Equal(expected[24..]);
  }

  private static AuthoredEffect CreateEffect(DynamicEffectType effectType)
  {
    var frames = new CanonicalDynamicFrameSequence(0, 1, 2);
    var sprite = new CanonicalDynamicSpriteSheet(frames, 1, 1);
    var startRectangle = new EffectRectangle(-0.5f, 0.75f, 1, -1.25f);
    var endRectangle = new EffectRectangle(-0.25f, 0.5f, 0.75f, -1);
    var shape = new CanonicalDynamicEffectShape(startRectangle, endRectangle, 0.5f);
    var alpha = new CanonicalDynamicAlpha(0.25f, 0.75f, DynamicAlphaTiming.FramePhase);
    var terrain = new CanonicalDynamicTerrainLight(
      DynamicLightType.Pyramid,
      new Vector3(0.1f, 0.2f, 0.3f)
    );
    var color = new Vector3(0.4f, 0.6f, 0.8f);
    const float gain = 0.5f;
    var textureKey = $"Textures\\effects\\{effectType}.tex";
    const string meshKey = "Objects\\effects\\scalable.msh";
    var values = effectType switch
    {
      DynamicEffectType.ScalableObject or DynamicEffectType.MappedExplosion =>
        new DynamicAuthoringValues(
          frames,
          endRectangle,
          terrain,
          gain,
          alpha.Timing,
          alpha.EndAlpha,
          true
        ),
      DynamicEffectType.Sphere => new DynamicAuthoringValues(additive: true),
      _ => new DynamicAuthoringValues(
        sprite,
        endRectangle,
        terrain,
        gain,
        alpha.Timing,
        alpha.EndAlpha,
        true
      ),
    };
    var effect = effectType switch
    {
      DynamicEffectType.ScalableObject => DynamicEffectRecipes.ScalableObject(
        frames,
        meshKey,
        textureKey,
        0.5f,
        0.5f,
        color,
        alpha,
        true,
        terrain
      ),
      DynamicEffectType.MappedExplosion => DynamicEffectRecipes.MappedExplosion(
        frames,
        startRectangle,
        endRectangle,
        textureKey,
        color,
        alpha,
        true,
        terrain
      ),
      DynamicEffectType.FlatExplosion => DynamicEffectRecipes.FlatExplosion(
        sprite,
        shape,
        textureKey,
        color,
        alpha,
        true,
        terrain
      ),
      DynamicEffectType.Laser => DynamicEffectRecipes.Laser(
        sprite,
        -0.5f,
        textureKey,
        color,
        alpha,
        true,
        terrain
      ),
      DynamicEffectType.LaserWall => DynamicEffectRecipes.LaserWall(
        sprite,
        0.25f,
        textureKey,
        color,
        alpha,
        true,
        terrain.Color
      ),
      DynamicEffectType.Shockwave => DynamicEffectRecipes.Shockwave(
        sprite,
        shape,
        textureKey,
        color,
        gain,
        alpha.StartAlpha,
        alpha.EndAlpha,
        true
      ),
      DynamicEffectType.Line => DynamicEffectRecipes.Line(
        sprite,
        shape,
        textureKey,
        color,
        gain,
        alpha.StartAlpha,
        alpha.EndAlpha,
        true
      ),
      DynamicEffectType.Sphere => DynamicEffectRecipes.Sphere(textureKey, color, true),
      DynamicEffectType.ElectricalCannon => DynamicEffectRecipes.ElectricalCannon(
        sprite,
        -0.75f,
        textureKey,
        color,
        alpha,
        true
      ),
      DynamicEffectType.Lightning => DynamicEffectRecipes.Lightning(
        sprite,
        1,
        textureKey,
        color,
        alpha,
        true,
        terrain
      ),
      DynamicEffectType.Smoke => DynamicEffectRecipes.Smoke(
        sprite,
        shape,
        textureKey,
        color,
        gain,
        alpha,
        true
      ),
      DynamicEffectType.Keelwater => DynamicEffectRecipes.Keelwater(
        sprite,
        shape,
        textureKey,
        alpha.StartAlpha,
        alpha.EndAlpha,
        true
      ),
      _ => throw new ArgumentOutOfRangeException(nameof(effectType)),
    };
    return new AuthoredEffect(
      effect,
      values,
      textureKey,
      effectType == DynamicEffectType.ScalableObject ? meshKey : null
    );
  }

  private sealed class AuthoredEffect
  {
    internal CanonicalDynamicObject Object { get; }
    internal DynamicAuthoringValues Values { get; }
    internal string TextureKey { get; }
    internal string? MeshKey { get; }

    internal AuthoredEffect(
      CanonicalDynamicObject @object,
      DynamicAuthoringValues values,
      string textureKey,
      string? meshKey
    )
    {
      Object = @object;
      Values = values;
      TextureKey = textureKey;
      MeshKey = meshKey;
    }
  }
}
