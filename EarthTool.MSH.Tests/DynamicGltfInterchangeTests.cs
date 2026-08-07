using AwesomeAssertions;
using EarthTool.Common.Operations;
using EarthTool.GLTF;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Expert;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;

namespace EarthTool.MSH.Tests;

public class DynamicGltfInterchangeTests
{
  [Fact]
  public async Task GroupAndExplosionExportAsAnOrderedNativePreview()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateAsset(),
      destination,
      new GltfExportOptions(sourceBaseName: "EDBBPP")
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    using var json = ReadGlbJson(destination.ToArray());
    var nodes = json.RootElement.GetProperty("nodes");
    nodes.GetArrayLength().Should().Be(4);
    nodes[0]
      .GetProperty("children")
      .EnumerateArray()
      .Select(item => item.GetInt32())
      .Should()
      .Equal(1);
    nodes[0].TryGetProperty("mesh", out _).Should().BeFalse();
    nodes[0].GetProperty("name").GetString().Should().Be("EDBBPP");
    nodes[0]
      .GetProperty("extras")
      .GetProperty("earthtoolPlacementRoot")
      .GetBoolean()
      .Should()
      .BeTrue();
    nodes[1].GetProperty("name").GetString().Should().Be("ET_Dynamic_1_Group");
    nodes[1]
      .GetProperty("children")
      .EnumerateArray()
      .Select(item => item.GetInt32())
      .Should()
      .Equal(2, 3);
    nodes[2].GetProperty("name").GetString().Should().Be("ET_Dynamic_2_Explosion");
    nodes[3].GetProperty("name").GetString().Should().Be("ET_Dynamic_3_Explosion");
    nodes[2].GetProperty("mesh").GetInt32().Should().Be(0);
    nodes[3].GetProperty("mesh").GetInt32().Should().Be(1);
    json.RootElement.GetProperty("meshes").GetArrayLength().Should().Be(2);
    json.RootElement.GetProperty("meshes")[0]
      .GetProperty("name")
      .GetString()
      .Should()
      .Be("EDBBPP_2_Explosion_Mesh");
    json.RootElement.GetProperty("meshes")[1]
      .GetProperty("name")
      .GetString()
      .Should()
      .Be("EDBBPP_3_Explosion_Mesh");
    json.RootElement.GetProperty("images").GetArrayLength().Should().Be(1);
    json.RootElement.GetProperty("images")[0]
      .GetProperty("mimeType")
      .GetString()
      .Should()
      .Be("image/png");
    json.RootElement.GetProperty("images")[0]
      .GetProperty("bufferView")
      .GetInt32()
      .Should()
      .BeGreaterThan(0);
    result
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.TextureResourceMissing)
      .And.Contain(GltfDiagnosticCodes.TextureDiagnosticPreviewUsed);
  }

  [Fact]
  public async Task GroupOnlyExportHasNoSyntheticEffectGeometry()
  {
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([DynamicEffectRecipes.Group()]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(asset!, destination);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    using var json = ReadGlbJson(destination.ToArray());
    json.RootElement.GetProperty("nodes").GetArrayLength().Should().Be(3);
    json.RootElement.TryGetProperty("meshes", out _).Should().BeFalse();
    json.RootElement.TryGetProperty("materials", out _).Should().BeFalse();
  }

  [Theory]
  [InlineData(3, 15)]
  [InlineData(4096, 2)]
  public async Task DynamicExportLimitsIncludeThePlacementRoot(int maxNodes, int maxHierarchyDepth)
  {
    await using var destination = new MemoryStream();
    var profile = new GltfOperationProfile(
      32 * 1024 * 1024,
      32 * 1024 * 1024,
      4 * 1024 * 1024,
      32,
      65536,
      maxNodes,
      maxHierarchyDepth
    );

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateAsset(),
      destination,
      profile: profile
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Should()
      .ContainSingle(diagnostic => diagnostic.Code == GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task SpriteEffectsExportThroughThePublicGlbSeam()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateSpriteEffectsAsset(),
      destination
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result
      .Diagnostics.Should()
      .NotContain(item => item.Code == GltfDiagnosticCodes.UnsupportedDomain);
    result
      .Diagnostics.Count(item => item.Code == GltfDiagnosticCodes.TextureResourceMissing)
      .Should()
      .Be(4);
    using var json = ReadGlbJson(destination.ToArray());
    json.RootElement.GetProperty("nodes").GetArrayLength().Should().Be(6);
    json.RootElement.GetProperty("meshes").GetArrayLength().Should().Be(4);
  }

  [Fact]
  public async Task RibbonEffectsExportThroughThePublicGlbSeam()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      destination
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result
      .Diagnostics.Should()
      .NotContain(item => item.Code == GltfDiagnosticCodes.UnsupportedDomain);
    result
      .Diagnostics.Count(item => item.Code == GltfDiagnosticCodes.TextureResourceMissing)
      .Should()
      .Be(4);
    using var json = ReadGlbJson(destination.ToArray());
    json.RootElement.GetProperty("nodes").GetArrayLength().Should().Be(6);
    json.RootElement.GetProperty("meshes").GetArrayLength().Should().Be(4);
  }

  [Fact]
  public async Task AttachedAndProceduralEffectsExportNativePreviews()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateAttachedAndProceduralEffectsAsset(),
      destination
    );

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    result
      .Diagnostics.Should()
      .NotContain(item => item.Code == GltfDiagnosticCodes.UnsupportedDomain);
    result
      .Diagnostics.Count(item => item.Code == GltfDiagnosticCodes.TextureResourceMissing)
      .Should()
      .Be(4);
    using var json = ReadGlbJson(destination.ToArray());
    var root = json.RootElement;
    root.GetProperty("nodes").GetArrayLength().Should().Be(6);
    root.GetProperty("meshes").GetArrayLength().Should().Be(4);
    root.GetProperty("accessors")[8].GetProperty("count").GetInt32().Should().BeGreaterThan(4);
    root.GetProperty("nodes")[3]
      .GetProperty("translation")
      .EnumerateArray()
      .Select(item => item.GetSingle())
      .Should()
      .Equal(2, 4, -3);
  }

  [Fact]
  public async Task RibbonPreviewRetainsHalfWidthSignTextureSideAndWinding()
  {
    await using var destination = new MemoryStream();
    var result = await new GltfInterchange().ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      destination
    );
    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var glb = destination.ToArray();
    using var json = ReadGlbJson(glb);
    var binary = ReadGlbBinary(glb);
    var expected = new[]
    {
      (RibbonHalfWidth: 0.5f, Vertices: 4),
      (RibbonHalfWidth: -0.25f, Vertices: 42),
      (RibbonHalfWidth: 1f, Vertices: 4),
      (RibbonHalfWidth: -0.75f, Vertices: 62),
    };

    for (var meshIndex = 0; meshIndex < expected.Length; meshIndex++)
    {
      var primitive = json.RootElement.GetProperty("meshes")[meshIndex].GetProperty("primitives")[
        0
      ];
      var positions = ReadVector3Accessor(
        json.RootElement,
        binary,
        primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32()
      );
      var textureCoordinates = ReadVector2Accessor(
        json.RootElement,
        binary,
        primitive.GetProperty("attributes").GetProperty("TEXCOORD_0").GetInt32()
      );
      var indices = ReadUInt16Accessor(
        json.RootElement,
        binary,
        primitive.GetProperty("indices").GetInt32()
      );
      positions.Should().HaveCount(expected[meshIndex].Vertices);
      Vector3
        .Distance(positions[0], positions[1])
        .Should()
        .BeApproximately(Math.Abs(expected[meshIndex].RibbonHalfWidth) * 2, 0.0001f);
      textureCoordinates[0].X.Should().BeLessThan(textureCoordinates[1].X);
      indices.Take(6).Should().Equal(0, 2, 1, 1, 2, 3);
      var winding = Vector3
        .Cross(
          positions[indices[1]] - positions[indices[0]],
          positions[indices[2]] - positions[indices[0]]
        )
        .Z;
      Math.Sign(winding).Should().Be(-Math.Sign(expected[meshIndex].RibbonHalfWidth));
    }
  }

  [Theory]
  [InlineData(0)]
  [InlineData(float.NaN)]
  [InlineData(float.PositiveInfinity)]
  public async Task InvalidSerializedRibbonHalfWidthsFailWithoutPartialOutput(float ribbonHalfWidth)
  {
    var bytes = CreateRibbonEffectsAsset().GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    WriteSingle(bytes, firstChildOffset + 0x3B0, ribbonHalfWidth);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var asset).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(asset!, destination);

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "DynamicObjectScopes[2].Extension.RibbonHalfWidth"
      );
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task RibbonPreviewVertexLimitFailsWithoutPartialOutput()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateRibbonEffectsAsset(),
      destination,
      profile: new GltfOperationProfile(maxActiveRenderVertices: 32)
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task InvalidSpritePreviewDomainsFailWithoutPartialOutput()
  {
    var frames = new CanonicalDynamicFrameSequence(0, 1, 1);
    var sprite = new CanonicalDynamicSpriteSheet(frames, 1, 1);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 1, 1, -1),
      new EffectRectangle(-2, 2, 2, -2),
      0.25f
    );
    var alpha = new CanonicalDynamicAlpha(1, 0, DynamicAlphaTiming.FramePhase);
    var light = new CanonicalDynamicTerrainLight(DynamicLightType.Constant, Vector3.One);
    var cases = new (DynamicMeshAsset Asset, int Offset, int Value)[]
    {
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.Track(
            frames,
            shape.StartEffectRectangle,
            shape.EndEffectRectangle,
            "Textures\\fx\\track.tex",
            alpha,
            false
          )
        ),
        0x370,
        -1
      ),
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.MappedExplosion(
            frames,
            shape.StartEffectRectangle,
            shape.EndEffectRectangle,
            "Textures\\fx\\mapped.tex",
            Vector3.One,
            alpha,
            false,
            light
          )
        ),
        0x370,
        int.MaxValue
      ),
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.FlatExplosion(
            sprite,
            shape,
            "Textures\\fx\\flat.tex",
            Vector3.One,
            alpha,
            false,
            light
          )
        ),
        0x378,
        0
      ),
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.FlatExplosion(
            sprite,
            shape,
            "Textures\\fx\\flat.tex",
            Vector3.One,
            alpha,
            false,
            light
          )
        ),
        0x37C,
        0
      ),
      (
        CreateSingleEffectAsset(
          DynamicEffectRecipes.Smoke(
            sprite,
            shape,
            "Textures\\fx\\smoke.tex",
            Vector3.One,
            1,
            alpha,
            false
          )
        ),
        0x38C,
        BitConverter.SingleToInt32Bits(float.NaN)
      ),
    };

    foreach (var item in cases)
    {
      var bytes = item.Asset.GetSerializedRepresentation();
      const int firstChildOffset = 0x18 + 0x410;
      BinaryPrimitives.WriteInt32LittleEndian(
        bytes.AsSpan(firstChildOffset + item.Offset),
        item.Value
      );
      var expert = MshExpert.CreateDynamic(bytes);
      expert.TryGetValue(out var malformed).Should().BeTrue();
      await using var destination = new MemoryStream();

      var result = await new GltfInterchange().ExportGlbAsync(malformed!, destination);

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
          && diagnostic.Path.StartsWith(
            "DynamicObjectScopes[2].Extension",
            StringComparison.Ordinal
          )
        );
      destination.Length.Should().Be(0);
    }
  }

  [Fact]
  public async Task InvalidAttachedFrameAndFiniteDomainsFailWithoutPartialOutput()
  {
    var cases = new (int Offset, int Value)[]
    {
      (0x374, 0),
      (0x3D4, BitConverter.SingleToInt32Bits(float.NaN)),
    };
    foreach (var item in cases)
    {
      var bytes = CreateAttachedAndProceduralEffectsAsset().GetSerializedRepresentation();
      const int firstChildOffset = 0x18 + 0x410;
      BinaryPrimitives.WriteInt32LittleEndian(
        bytes.AsSpan(firstChildOffset + item.Offset),
        item.Value
      );
      var expert = MshExpert.CreateDynamic(bytes);
      expert.TryGetValue(out var malformed).Should().BeTrue();
      await using var destination = new MemoryStream();

      var result = await new GltfInterchange().ExportGlbAsync(malformed!, destination);

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Should()
        .ContainSingle(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
          && diagnostic.Path.StartsWith(
            "DynamicObjectScopes[2].Extension",
            StringComparison.Ordinal
          )
        );
      destination.Length.Should().Be(0);
    }
  }

  [Theory]
  [InlineData(DynamicEffectType.Shockwave)]
  [InlineData(DynamicEffectType.Line)]
  [InlineData(DynamicEffectType.Keelwater)]
  public async Task EveryAttachedEffectRejectsInvalidFrameDomains(DynamicEffectType effectType)
  {
    var bytes = CreateSingleAttachedEffectAsset(effectType).GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(firstChildOffset + 0x374), 0);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var malformed).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(malformed!, destination);

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path == "DynamicObjectScopes[2].Extension.Frames"
      );
    destination.Length.Should().Be(0);
  }

  [Theory]
  [InlineData(DynamicEffectType.Shockwave, 0x3D4)]
  [InlineData(DynamicEffectType.Line, 0x3D4)]
  [InlineData(DynamicEffectType.Sphere, 0x3C8)]
  [InlineData(DynamicEffectType.Keelwater, 0x3E0)]
  public async Task EveryNewEffectRejectsNonFiniteActivePreviewValues(
    DynamicEffectType effectType,
    int fieldOffset
  )
  {
    var source =
      effectType == DynamicEffectType.Sphere
        ? CreateSingleEffectAsset(
          DynamicEffectRecipes.Sphere(
            "Textures\\fx\\sphere.tex",
            new Vector3(0.4f, 0.5f, 0.6f),
            true
          )
        )
        : CreateSingleAttachedEffectAsset(effectType);
    var bytes = source.GetSerializedRepresentation();
    const int firstChildOffset = 0x18 + 0x410;
    WriteSingle(bytes, firstChildOffset + fieldOffset, float.NaN);
    var expert = MshExpert.CreateDynamic(bytes);
    expert.TryGetValue(out var malformed).Should().BeTrue();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(malformed!, destination);

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Should()
      .ContainSingle(diagnostic =>
        diagnostic.Code == GltfDiagnosticCodes.InvalidGeometry
        && diagnostic.Path.StartsWith("DynamicObjectScopes[2].Extension", StringComparison.Ordinal)
      );
    destination.Length.Should().Be(0);
  }

  [Theory]
  [InlineData(DynamicEffectType.Shockwave)]
  [InlineData(DynamicEffectType.Line)]
  [InlineData(DynamicEffectType.Sphere)]
  [InlineData(DynamicEffectType.Keelwater)]
  public async Task EveryNewEffectHonorsTransactionalOutputLimits(DynamicEffectType effectType)
  {
    var asset =
      effectType == DynamicEffectType.Sphere
        ? CreateSingleEffectAsset(
          DynamicEffectRecipes.Sphere(
            "Textures\\fx\\sphere.tex",
            new Vector3(0.4f, 0.5f, 0.6f),
            true
          )
        )
        : CreateSingleAttachedEffectAsset(effectType);
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      asset,
      destination,
      profile: new GltfOperationProfile(maxOutputBytes: 256)
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task AttachedAndProceduralEffectsBindRealTexPreviews()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-attached-tex-{Guid.NewGuid():N}");
    var textureDirectory = Path.Combine(directory, "Textures", "fx");
    Directory.CreateDirectory(textureDirectory);
    try
    {
      var tex = CreateRgbaTex([0x11, 0x22, 0x33, 0xFF]);
      foreach (var name in new[] { "shockwave.tex", "line.tex", "sphere.tex", "keelwater.tex" })
      {
        await File.WriteAllBytesAsync(Path.Combine(textureDirectory, name), tex);
      }
      await using var destination = new MemoryStream();

      var result = await new GltfInterchange().ExportGlbAsync(
        CreateAttachedAndProceduralEffectsAsset(),
        destination,
        new GltfExportOptions(textureSearchRoots: [directory])
      );

      result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
      result
        .Diagnostics.Should()
        .NotContain(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.TextureResourceMissing
          || diagnostic.Code == GltfDiagnosticCodes.TexturePreviewUnavailable
        );
      using var json = ReadGlbJson(destination.ToArray());
      json.RootElement.GetProperty("images").GetArrayLength().Should().Be(1);
      json.RootElement.GetProperty("materials")
        .EnumerateArray()
        .Should()
        .AllSatisfy(material =>
          material
            .GetProperty("pbrMetallicRoughness")
            .GetProperty("baseColorTexture")
            .GetProperty("index")
            .GetInt32()
            .Should()
            .Be(0)
        );
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task SpherePreviewVertexLimitFailsWithoutPartialOutput()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateAttachedAndProceduralEffectsAsset(),
      destination,
      profile: new GltfOperationProfile(maxActiveRenderVertices: 100)
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Select(diagnostic => diagnostic.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task SpriteEffectsHonorTransactionalOutputLimit()
  {
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateSpriteEffectsAsset(),
      destination,
      profile: new GltfOperationProfile(maxOutputBytes: 1024)
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Select(item => item.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task ScalableObjectUsesAReferencedStaticMeshPreview()
  {
    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-{Guid.NewGuid():N}");
    var meshes = Path.Combine(directory, "mEsHeS", "EfFeCtS");
    Directory.CreateDirectory(meshes);
    try
    {
      await File.WriteAllBytesAsync(
        Path.Combine(meshes, "PrEvIeW.MsH"),
        CreateReferencedStaticAsset().GetSerializedRepresentation()
      );
      await using var package = new MemoryStream();

      var result = await new GltfInterchange().ExportGlbAsync(
        CreateScalableAsset("effects\\preview", 2, 5),
        package,
        new GltfExportOptions(meshResourceSearchRoots: [directory])
      );

      result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
      result.Diagnostics.Should().NotContain(item => item.Code == GltfDiagnosticCodes.UnsupportedDomain);
      using var json = ReadGlbJson(package.ToArray());
      var scalableNode = json.RootElement.GetProperty("nodes")[2];
      scalableNode
        .GetProperty("scale")
        .EnumerateArray()
        .Select(item => item.GetSingle())
        .Should()
        .OnlyContain(item => Math.Abs(item - 2.03f) < 0.0001f);
      var primitive = json.RootElement.GetProperty("meshes")[0].GetProperty("primitives")[0];
      var positionAccessor = primitive.GetProperty("attributes").GetProperty("POSITION").GetInt32();
      json.RootElement.GetProperty("accessors")[positionAccessor]
        .GetProperty("count")
        .GetInt32()
        .Should()
        .Be(3);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task MissingAndShadowedScalableResourcesAreDiagnosed()
  {
    var firstRoot = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-scalable-first-{Guid.NewGuid():N}"
    );
    var secondRoot = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-scalable-second-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(Path.Combine(firstRoot, "Meshes"));
    Directory.CreateDirectory(Path.Combine(secondRoot, "Meshes"));
    try
    {
      var referenced = CreateReferencedStaticAsset().GetSerializedRepresentation();
      await File.WriteAllBytesAsync(Path.Combine(firstRoot, "Meshes", "shared.msh"), referenced);
      await File.WriteAllBytesAsync(Path.Combine(secondRoot, "Meshes", "SHARED.MSH"), referenced);

      var shadowedResult = await new GltfInterchange().ExportGlbAsync(
        CreateScalableAsset("shared", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(meshResourceSearchRoots: [firstRoot, secondRoot])
      );
      var missingResult = await new GltfInterchange().ExportGlbAsync(
        CreateScalableAsset("..\\outside", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(meshResourceSearchRoots: [firstRoot])
      );

      shadowedResult
        .Status.Should()
        .Be(OperationStatus.Succeeded, Diagnostics(shadowedResult.Diagnostics));
      shadowedResult
        .Diagnostics.Should()
        .ContainSingle(item => item.Code == GltfDiagnosticCodes.MeshResourceShadowed);
      missingResult
        .Status.Should()
        .Be(OperationStatus.Succeeded, Diagnostics(missingResult.Diagnostics));
      missingResult
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.MeshPreviewUnavailable)
        .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
    }
    finally
    {
      Directory.Delete(firstRoot, true);
      Directory.Delete(secondRoot, true);
    }
  }

  [Fact]
  public async Task AmbiguousAndDynamicScalableResourcesUseDeterministicPlaceholders()
  {
    var root = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-hazards-{Guid.NewGuid():N}");
    var meshes = Path.Combine(root, "Meshes");
    Directory.CreateDirectory(meshes);
    try
    {
      var staticBytes = CreateReferencedStaticAsset().GetSerializedRepresentation();
      await File.WriteAllBytesAsync(Path.Combine(meshes, "ambiguous.msh"), staticBytes);
      await File.WriteAllBytesAsync(Path.Combine(meshes, "AMBIGUOUS.MSH"), staticBytes);
      var supportsCaseDistinctFiles =
        Directory
          .EnumerateFiles(meshes)
          .Count(path =>
            string.Equals(
              Path.GetFileName(path),
              "ambiguous.msh",
              StringComparison.OrdinalIgnoreCase
            )
          ) == 2;
      await File.WriteAllBytesAsync(
        Path.Combine(meshes, "dynamic.msh"),
        CreateAsset().GetSerializedRepresentation()
      );
      var interchange = new GltfInterchange();

      var ambiguous = await interchange.ExportGlbAsync(
        CreateScalableAsset("ambiguous", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(meshResourceSearchRoots: [root])
      );
      var dynamic = await interchange.ExportGlbAsync(
        CreateScalableAsset("dynamic", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(meshResourceSearchRoots: [root])
      );

      ambiguous.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(ambiguous.Diagnostics));
      if (supportsCaseDistinctFiles)
      {
        ambiguous
          .Diagnostics.Select(item => item.Code)
          .Should()
          .Contain(GltfDiagnosticCodes.AmbiguousMeshResource)
          .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
      }
      else
      {
        ambiguous
          .Diagnostics.Select(item => item.Code)
          .Should()
          .NotContain(GltfDiagnosticCodes.AmbiguousMeshResource)
          .And.NotContain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
      }
      dynamic.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(dynamic.Diagnostics));
      dynamic
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.UnsupportedMeshResource)
        .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
    }
    finally
    {
      Directory.Delete(root, true);
    }
  }

  [Fact]
  public async Task CyclicScalableResourceChainsAreBoundedAndDiagnosed()
  {
    var root = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-cycle-{Guid.NewGuid():N}");
    var meshes = Path.Combine(root, "Meshes");
    Directory.CreateDirectory(meshes);
    try
    {
      await File.WriteAllBytesAsync(
        Path.Combine(meshes, "first.msh"),
        CreateScalableAsset("second", 1, 2).GetSerializedRepresentation()
      );
      await File.WriteAllBytesAsync(
        Path.Combine(meshes, "second.msh"),
        CreateScalableAsset("first", 1, 2).GetSerializedRepresentation()
      );

      var cyclic = await new GltfInterchange().ExportGlbAsync(
        CreateScalableAsset("first", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(meshResourceSearchRoots: [root])
      );
      await using var limitedOutput = new MemoryStream();
      var limited = await new GltfInterchange().ExportGlbAsync(
        CreateScalableAsset("first", 1, 2),
        limitedOutput,
        new GltfExportOptions(meshResourceSearchRoots: [root]),
        new GltfOperationProfile(new GltfMeshResourceLimits(maxDepth: 1))
      );

      cyclic.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(cyclic.Diagnostics));
      cyclic
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.MeshResourceCycle)
        .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
      limited.Status.Should().Be(OperationStatus.Failed);
      limited
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
      limitedOutput.Length.Should().Be(0);
    }
    finally
    {
      Directory.Delete(root, true);
    }
  }

  [Fact]
  public async Task ScalableResourceLimitsFailWithoutPartialOutput()
  {
    var roots = new[]
    {
      Path.GetFullPath(Path.Combine(Path.GetTempPath(), "earthtool-root-a")),
      Path.GetFullPath(Path.Combine(Path.GetTempPath(), "earthtool-root-b")),
    };
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateScalableAsset("preview", 1, 2),
      destination,
      new GltfExportOptions(meshResourceSearchRoots: roots),
      new GltfOperationProfile(new GltfMeshResourceLimits(maxSearchRoots: 1))
    );

    result.Status.Should().Be(OperationStatus.Failed);
    result
      .Diagnostics.Select(item => item.Code)
      .Should()
      .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task AggregateScalablePreviewVertexLimitCountsEveryEmittedScope()
  {
    var root = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-vertices-{Guid.NewGuid():N}");
    Directory.CreateDirectory(Path.Combine(root, "Meshes"));
    try
    {
      await File.WriteAllBytesAsync(
        Path.Combine(root, "Meshes", "preview.msh"),
        CreateReferencedStaticAsset().GetSerializedRepresentation()
      );
      var build = DynamicMeshBuilder
        .Create()
        .SetRoot(
          DynamicEffectRecipes.Group([
            CreateScalableRecipe("preview", 1, 2),
            CreateScalableRecipe("preview", 1, 2),
          ])
        )
        .Build();
      build.TryGetValue(out var asset).Should().BeTrue();
      await using var destination = new MemoryStream();

      var result = await new GltfInterchange().ExportGlbAsync(
        asset!,
        destination,
        new GltfExportOptions(meshResourceSearchRoots: [root]),
        new GltfOperationProfile(new GltfMeshResourceLimits(maxPreviewVertices: 5))
      );

      result.Status.Should().Be(OperationStatus.Failed);
      result
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.ResourceLimitExceeded);
      destination.Length.Should().Be(0);
    }
    finally
    {
      Directory.Delete(root, true);
    }
  }

  [Fact]
  public async Task ScalableLookupRejectsRelativeRootsAndLinkedComponents()
  {
    var createRelativeOptions = () => new GltfExportOptions(meshResourceSearchRoots: ["relative"]);
    createRelativeOptions.Should().Throw<ArgumentException>();

    var directory = Path.Combine(Path.GetTempPath(), $"earthtool-scalable-link-{Guid.NewGuid():N}");
    var outside = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-scalable-outside-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(directory);
    Directory.CreateDirectory(Path.Combine(outside, "Meshes"));
    try
    {
      await File.WriteAllBytesAsync(
        Path.Combine(outside, "Meshes", "preview.msh"),
        CreateReferencedStaticAsset().GetSerializedRepresentation()
      );
      Directory.CreateSymbolicLink(
        Path.Combine(directory, "Meshes"),
        Path.Combine(outside, "Meshes")
      );

      var result = await new GltfInterchange().ExportGlbAsync(
        CreateScalableAsset("preview", 1, 2),
        new MemoryStream(),
        new GltfExportOptions(meshResourceSearchRoots: [directory])
      );

      result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
      result
        .Diagnostics.Select(item => item.Code)
        .Should()
        .Contain(GltfDiagnosticCodes.MeshResourceMissing)
        .And.Contain(GltfDiagnosticCodes.MeshDiagnosticPreviewUsed);
    }
    finally
    {
      Directory.Delete(directory, true);
      Directory.Delete(outside, true);
    }
  }

  [Fact]
  public async Task ScalableObjectExportsLifetimeTranslationAndScaleAnimation()
  {
    var asset = CreateSingleEffectAsset(
      CreateScalableRecipe("preview", 2, 5)
        .SetChildTranslation(new Vector3(1, 2, 3), new Vector3(4, 5, 6))
    );
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(asset, destination);

    result.Status.Should().Be(OperationStatus.Succeeded, Diagnostics(result.Diagnostics));
    var glb = destination.ToArray();
    using var json = ReadGlbJson(glb);
    var root = json.RootElement;
    var animation = root.GetProperty("animations").EnumerateArray().Single();
    animation.GetProperty("name").GetString().Should().Be("EarthTool Dynamic Preview");
    var channels = animation.GetProperty("channels").EnumerateArray().ToArray();
    channels
      .Select(channel => channel.GetProperty("target").GetProperty("path").GetString())
      .Should()
      .BeEquivalentTo(["translation", "scale"]);
    channels
      .Select(channel => channel.GetProperty("target").GetProperty("node").GetInt32())
      .Should()
      .OnlyContain(node => node == 2);
    var samplers = animation.GetProperty("samplers");
    var inputAccessor = samplers[0].GetProperty("input").GetInt32();
    ReadFloatAccessor(root, ReadGlbBinary(glb), inputAccessor).Should().Equal(0, 5);
    var node = root.GetProperty("nodes")[2];
    var restTranslation = node.GetProperty("translation")
      .EnumerateArray()
      .Select(value => value.GetSingle())
      .ToArray();
    var restScale = node.GetProperty("scale")
      .EnumerateArray()
      .Select(value => value.GetSingle())
      .ToArray();
    foreach (var channel in channels)
    {
      var path = channel.GetProperty("target").GetProperty("path").GetString();
      var sampler = samplers[channel.GetProperty("sampler").GetInt32()];
      sampler.GetProperty("input").GetInt32().Should().Be(inputAccessor);
      sampler.GetProperty("interpolation").GetString().Should().Be("LINEAR");
      var values = ReadVector3Accessor(
        root,
        ReadGlbBinary(glb),
        sampler.GetProperty("output").GetInt32()
      );
      if (path == "translation")
      {
        values.Should().Equal(new Vector3(1, 3, -2), new Vector3(4, 6, -5));
        values[0]
          .Should()
          .Be(new Vector3(restTranslation[0], restTranslation[1], restTranslation[2]));
      }
      else
      {
        values[0].Should().Be(new Vector3(restScale[0], restScale[1], restScale[2]));
        values[1].Should().Be(new Vector3(5));
      }
    }
  }

  [Fact]
  public async Task CancelledDynamicExportWritesNoOutput()
  {
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    await using var destination = new MemoryStream();

    var result = await new GltfInterchange().ExportGlbAsync(
      CreateAsset(),
      destination,
      cancellationToken: cancellation.Token
    );

    result.Status.Should().Be(OperationStatus.Cancelled);
    result
      .Diagnostics.Should()
      .ContainSingle(item => item.Code == GltfDiagnosticCodes.Cancelled);
    destination.Length.Should().Be(0);
  }

  [Fact]
  public async Task SeparateManifestFailureRemovesNewDynamicSidecar()
  {
    var directory = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-dynamic-transaction-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(directory);
    try
    {
      var destination = Path.Combine(directory, "effect.gltf");
      var interchange = new GltfInterchange(new ManifestFailingFileSystem());

      var result = await interchange.ExportGltfFileAsync(CreateAsset(), destination);

      result.Status.Should().Be(OperationStatus.Failed);
      Directory.EnumerateFileSystemEntries(directory).Should().BeEmpty();
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [Fact]
  public async Task DynamicPackagesPassKhronosValidation()
  {
    var directory = Path.Combine(
      Path.GetTempPath(),
      $"earthtool-dynamic-validation-{Guid.NewGuid():N}"
    );
    Directory.CreateDirectory(directory);
    try
    {
      var glbPath = Path.Combine(directory, "effect.glb");
      var gltfPath = Path.Combine(directory, "effect.gltf");
      var ribbonGlbPath = Path.Combine(directory, "ribbons.glb");
      var ribbonGltfPath = Path.Combine(directory, "ribbons.gltf");
      var attachedGlbPath = Path.Combine(directory, "attached.glb");
      var attachedGltfPath = Path.Combine(directory, "attached.gltf");
      var scalableGlbPath = Path.Combine(directory, "scalable.glb");
      var scalableGltfPath = Path.Combine(directory, "scalable.gltf");
      var groupPath = Path.Combine(directory, "group.glb");
      var asset = CreateSpriteEffectsAsset();
      var interchange = new GltfInterchange();
      (await interchange.ExportGlbFileAsync(asset, glbPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (await interchange.ExportGltfFileAsync(asset, gltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (await interchange.ExportGlbFileAsync(CreateRibbonEffectsAsset(), ribbonGlbPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (await interchange.ExportGltfFileAsync(CreateRibbonEffectsAsset(), ribbonGltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (
        await interchange.ExportGlbFileAsync(
          CreateAttachedAndProceduralEffectsAsset(),
          attachedGlbPath
        )
      )
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (
        await interchange.ExportGltfFileAsync(
          CreateAttachedAndProceduralEffectsAsset(),
          attachedGltfPath
        )
      )
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      Directory.CreateDirectory(Path.Combine(directory, "Meshes"));
      await File.WriteAllBytesAsync(
        Path.Combine(directory, "Meshes", "preview.msh"),
        CreateReferencedStaticAsset().GetSerializedRepresentation()
      );
      var scalableOptions = new GltfExportOptions(meshResourceSearchRoots: [directory]);
      (
        await interchange.ExportGlbFileAsync(
          CreateScalableAsset("preview", -2, 3),
          scalableGlbPath,
          scalableOptions
        )
      )
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      (
        await interchange.ExportGltfFileAsync(
          CreateScalableAsset("preview", -2, 3),
          scalableGltfPath,
          scalableOptions
        )
      )
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      var groupBuild = DynamicMeshBuilder
        .Create()
        .SetRoot(DynamicEffectRecipes.Group([DynamicEffectRecipes.Group()]))
        .Build();
      groupBuild.TryGetValue(out var group).Should().BeTrue();
      (await interchange.ExportGlbFileAsync(group!, groupPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);

      await using (var glb = File.OpenRead(glbPath))
      {
        (await interchange.ValidateGlbAsync(glb)).Status.Should().Be(OperationStatus.Succeeded);
      }
      (await interchange.ValidateGltfFileAsync(gltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      await using (var ribbonGlb = File.OpenRead(ribbonGlbPath))
      {
        (await interchange.ValidateGlbAsync(ribbonGlb))
          .Status.Should()
          .Be(OperationStatus.Succeeded);
      }
      (await interchange.ValidateGltfFileAsync(ribbonGltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      await using (var attachedGlb = File.OpenRead(attachedGlbPath))
      {
        (await interchange.ValidateGlbAsync(attachedGlb))
          .Status.Should()
          .Be(OperationStatus.Succeeded);
      }
      (await interchange.ValidateGltfFileAsync(attachedGltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);
      await using (var scalableGlb = File.OpenRead(scalableGlbPath))
      {
        (await interchange.ValidateGlbAsync(scalableGlb))
          .Status.Should()
          .Be(OperationStatus.Succeeded);
      }
      (await interchange.ValidateGltfFileAsync(scalableGltfPath))
        .Status.Should()
        .Be(OperationStatus.Succeeded);

      await AssertKhronosValidAsync(glbPath);
      await AssertKhronosValidAsync(gltfPath);
      await AssertKhronosValidAsync(ribbonGlbPath);
      await AssertKhronosValidAsync(ribbonGltfPath);
      await AssertKhronosValidAsync(attachedGlbPath);
      await AssertKhronosValidAsync(attachedGltfPath);
      await AssertKhronosValidAsync(scalableGlbPath);
      await AssertKhronosValidAsync(scalableGltfPath);
      await AssertKhronosValidAsync(groupPath);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  private static DynamicMeshAsset CreateAsset()
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 3, 4), 5, 2);
    var alpha = new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.LifetimeProgress);
    var light = new CanonicalDynamicTerrainLight(
      DynamicLightType.Trapezium,
      new Vector3(0.1f, 0.2f, 0.3f)
    );
    var first = DynamicEffectRecipes
      .Explosion(
        sprite,
        new CanonicalDynamicEffectShape(
          new EffectRectangle(-1, 2, 3, -4),
          new EffectRectangle(-5, 6, 7, -8),
          0.25f
        ),
        "Textures\\fx\\first.tex",
        new Vector3(0.4f, 0.5f, 0.6f),
        alpha,
        true,
        light
      )
      .SetChildTranslation(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
    var second = DynamicEffectRecipes.Explosion(
      sprite,
      new CanonicalDynamicEffectShape(
        new EffectRectangle(-2, 3, 4, -5),
        new EffectRectangle(-6, 7, 8, -9),
        0.5f
      ),
      "Textures\\fx\\second.tex",
      new Vector3(0.7f, 0.8f, 0.9f),
      alpha,
      false,
      light
    );
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([first, second]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateScalableAsset(
    string meshResourceKey,
    float startScale,
    float endScale
  )
  {
    return CreateSingleEffectAsset(CreateScalableRecipe(meshResourceKey, startScale, endScale));
  }

  private static CanonicalDynamicObject CreateScalableRecipe(
    string meshResourceKey,
    float startScale,
    float endScale
  )
  {
    return DynamicEffectRecipes.ScalableObject(
      new CanonicalDynamicFrameSequence(0, 1, 0),
      meshResourceKey,
      "Textures\\fx\\scalable.tex",
      startScale,
      endScale,
      new Vector3(0.4f, 0.5f, 0.6f),
      new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.FramePhase),
      false,
      new CanonicalDynamicTerrainLight(DynamicLightType.Constant, Vector3.Zero)
    );
  }

  private static StaticMeshAsset CreateReferencedStaticAsset()
  {
    var vertices = new[]
    {
      new CanonicalStaticVertex(Vector3.Zero, Vector3.UnitZ, Vector2.Zero),
      new CanonicalStaticVertex(Vector3.UnitX, Vector3.UnitZ, Vector2.UnitX),
      new CanonicalStaticVertex(Vector3.UnitY, Vector3.UnitZ, Vector2.UnitY),
    };
    var build = StaticMeshBuilder
      .Create()
      .SetRootSourceObject(
        new CanonicalStaticSourceObject([
          new CanonicalStaticRenderObject(vertices, [new CanonicalTriangle(0, 1, 2)]),
        ])
      )
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateSpriteEffectsAsset()
  {
    var frames = new CanonicalDynamicFrameSequence(2, 3, 4);
    var sprite = new CanonicalDynamicSpriteSheet(frames, 5, 2);
    var alpha = new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.LifetimeProgress);
    var light = new CanonicalDynamicTerrainLight(
      DynamicLightType.Trapezium,
      new Vector3(0.1f, 0.2f, 0.3f)
    );
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 2, 3, -4),
      new EffectRectangle(-5, 6, 7, -8),
      0.25f
    );
    var smoke = DynamicEffectRecipes.Smoke(
      sprite,
      shape,
      "Textures\\fx\\smoke.tex",
      new Vector3(0.4f, 0.5f, 0.6f),
      0.5f,
      alpha,
      true
    );
    var track = DynamicEffectRecipes.Track(
      frames,
      shape.StartEffectRectangle,
      shape.EndEffectRectangle,
      "Textures\\fx\\track.tex",
      alpha,
      false,
      [smoke]
    );
    var flat = DynamicEffectRecipes.FlatExplosion(
      sprite,
      shape,
      "Textures\\fx\\flat.tex",
      new Vector3(0.7f, 0.8f, 0.9f),
      alpha,
      false,
      light
    );
    var mapped = DynamicEffectRecipes.MappedExplosion(
      frames,
      shape.StartEffectRectangle,
      shape.EndEffectRectangle,
      "Textures\\fx\\mapped.tex",
      new Vector3(0.2f, 0.3f, 0.4f),
      alpha,
      true,
      light,
      [flat]
    );
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([track, mapped]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateRibbonEffectsAsset()
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 3, 4), 5, 2);
    var alpha = new CanonicalDynamicAlpha(0.8f, 0.2f, DynamicAlphaTiming.LifetimeProgress);
    var light = new CanonicalDynamicTerrainLight(
      DynamicLightType.Trapezium,
      new Vector3(0.1f, 0.2f, 0.3f)
    );
    var electrical = DynamicEffectRecipes.ElectricalCannon(
      sprite,
      -0.25f,
      "Textures\\fx\\electrical.tex",
      new Vector3(0.2f, 0.3f, 0.4f),
      alpha,
      true
    );
    var laser = DynamicEffectRecipes.Laser(
      sprite,
      0.5f,
      "Textures\\fx\\laser.tex",
      new Vector3(0.4f, 0.5f, 0.6f),
      alpha,
      false,
      light,
      [electrical]
    );
    var lightning = DynamicEffectRecipes.Lightning(
      sprite,
      -0.75f,
      "Textures\\fx\\lightning.tex",
      new Vector3(0.7f, 0.8f, 0.9f),
      alpha,
      true,
      light
    );
    var laserWall = DynamicEffectRecipes.LaserWall(
      sprite,
      1,
      "Textures\\fx\\laser-wall.tex",
      new Vector3(0.3f, 0.6f, 0.9f),
      alpha,
      false,
      new Vector3(0.9f, 0.6f, 0.3f),
      [lightning]
    );
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([laser, laserWall]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateAttachedAndProceduralEffectsAsset()
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 3, 4), 5, 2);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 2, 3, -4),
      new EffectRectangle(-5, 6, 7, -8),
      0.25f
    );
    var line = DynamicEffectRecipes
      .Line(
        sprite,
        shape,
        "Textures\\fx\\line.tex",
        new Vector3(0.2f, 0.3f, 0.4f),
        0.5f,
        0.8f,
        0.2f,
        true
      )
      .SetChildTranslation(new Vector3(2, 3, 4), new Vector3(5, 6, 7));
    var shockwave = DynamicEffectRecipes.Shockwave(
      sprite,
      shape,
      "Textures\\fx\\shockwave.tex",
      new Vector3(0.4f, 0.5f, 0.6f),
      0.5f,
      0.8f,
      0.2f,
      false,
      [line]
    );
    var keelwater = DynamicEffectRecipes.Keelwater(
      sprite,
      shape,
      "Textures\\fx\\keelwater.tex",
      0.8f,
      0.2f,
      false
    );
    var sphere = DynamicEffectRecipes.Sphere(
      "Textures\\fx\\sphere.tex",
      new Vector3(0.7f, 0.8f, 0.9f),
      true,
      [keelwater]
    );
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([shockwave, sphere]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateSingleEffectAsset(CanonicalDynamicObject effect)
  {
    var build = DynamicMeshBuilder
      .Create(Guid.Parse("12345678-9abc-4ef0-9234-56789abcdef0"))
      .SetRoot(DynamicEffectRecipes.Group([effect]))
      .Build();
    build.TryGetValue(out var asset).Should().BeTrue();
    return asset!;
  }

  private static DynamicMeshAsset CreateSingleAttachedEffectAsset(DynamicEffectType effectType)
  {
    var sprite = new CanonicalDynamicSpriteSheet(new CanonicalDynamicFrameSequence(2, 3, 4), 5, 2);
    var shape = new CanonicalDynamicEffectShape(
      new EffectRectangle(-1, 2, 3, -4),
      new EffectRectangle(-5, 6, 7, -8),
      0.25f
    );
    var effect = effectType switch
    {
      DynamicEffectType.Shockwave => DynamicEffectRecipes.Shockwave(
        sprite,
        shape,
        "Textures\\fx\\shockwave.tex",
        new Vector3(0.4f, 0.5f, 0.6f),
        0.5f,
        0.8f,
        0.2f,
        false
      ),
      DynamicEffectType.Line => DynamicEffectRecipes.Line(
        sprite,
        shape,
        "Textures\\fx\\line.tex",
        new Vector3(0.2f, 0.3f, 0.4f),
        0.5f,
        0.8f,
        0.2f,
        true
      ),
      DynamicEffectType.Keelwater => DynamicEffectRecipes.Keelwater(
        sprite,
        shape,
        "Textures\\fx\\keelwater.tex",
        0.8f,
        0.2f,
        false
      ),
      _ => throw new ArgumentOutOfRangeException(nameof(effectType)),
    };
    return CreateSingleEffectAsset(
      effect.SetChildTranslation(new Vector3(1, 2, 3), new Vector3(4, 5, 6))
    );
  }

  private static JsonDocument ReadGlbJson(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    return JsonDocument.Parse(glb.AsMemory(20, jsonLength));
  }

  private static byte[] ReadGlbBinary(byte[] glb)
  {
    var jsonLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(12));
    var binaryHeader = 20 + jsonLength;
    var binaryLength = BinaryPrimitives.ReadInt32LittleEndian(glb.AsSpan(binaryHeader));
    return glb.AsSpan(binaryHeader + 8, binaryLength).ToArray();
  }

  private static Vector3[] ReadVector3Accessor(JsonElement root, byte[] binary, int accessorIndex)
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    var offset = view.GetProperty("byteOffset").GetInt32();
    return Enumerable
      .Range(0, accessor.GetProperty("count").GetInt32())
      .Select(index => new Vector3(
        BitConverter.ToSingle(binary, offset + index * 12),
        BitConverter.ToSingle(binary, offset + index * 12 + 4),
        BitConverter.ToSingle(binary, offset + index * 12 + 8)
      ))
      .ToArray();
  }

  private static float[] ReadFloatAccessor(JsonElement root, byte[] binary, int accessorIndex)
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    var offset = view.GetProperty("byteOffset").GetInt32();
    return Enumerable
      .Range(0, accessor.GetProperty("count").GetInt32())
      .Select(index => BitConverter.ToSingle(binary, offset + index * sizeof(float)))
      .ToArray();
  }

  private static Vector2[] ReadVector2Accessor(JsonElement root, byte[] binary, int accessorIndex)
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    var offset = view.GetProperty("byteOffset").GetInt32();
    return Enumerable
      .Range(0, accessor.GetProperty("count").GetInt32())
      .Select(index => new Vector2(
        BitConverter.ToSingle(binary, offset + index * 8),
        BitConverter.ToSingle(binary, offset + index * 8 + 4)
      ))
      .ToArray();
  }

  private static ushort[] ReadUInt16Accessor(JsonElement root, byte[] binary, int accessorIndex)
  {
    var accessor = root.GetProperty("accessors")[accessorIndex];
    var view = root.GetProperty("bufferViews")[accessor.GetProperty("bufferView").GetInt32()];
    var offset = view.GetProperty("byteOffset").GetInt32();
    return Enumerable
      .Range(0, accessor.GetProperty("count").GetInt32())
      .Select(index => BinaryPrimitives.ReadUInt16LittleEndian(binary.AsSpan(offset + index * 2)))
      .ToArray();
  }

  private static void WriteSingle(byte[] destination, int offset, float value)
  {
    BinaryPrimitives.WriteInt32LittleEndian(
      destination.AsSpan(offset),
      BitConverter.SingleToInt32Bits(value)
    );
  }

  private static byte[] CreateRgbaTex(byte[] pixels)
  {
    pixels.Length.Should().Be(4);
    var result = new byte[24 + pixels.Length];
    "TEX\0\x01\0\0\0"u8.CopyTo(result);
    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), 0x03000012);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(12), 0x8888);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(16), 1);
    BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(20), 1);
    pixels.CopyTo(result, 24);
    return result;
  }

  private static string Diagnostics(IEnumerable<OperationDiagnostic> diagnostics)
  {
    return string.Join(
      "; ",
      diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Path}: {diagnostic.Message}")
    );
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
    output.Should().Contain("\"errors\":0").And.Contain("\"warnings\":0");
  }

  private sealed class ManifestFailingFileSystem : ITransactionalFileSystem
  {
    private int _commitCount;

    public string GetTemporaryPath(string destinationPath)
    {
      return destinationPath + ".tmp";
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      _commitCount++;
      if (_commitCount == 2)
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
}
