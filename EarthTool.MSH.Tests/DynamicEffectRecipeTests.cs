using AwesomeAssertions;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Numerics;

namespace EarthTool.MSH.Tests;

public class DynamicEffectRecipeTests
{
  private static readonly Guid _creationGuid = new("12345678-9abc-def0-1234-56789abcdef0");
  private static readonly MeshAssetLineageId _lineageId = new(
    new Guid("11111111-2222-3333-4444-555555555555"));
  private static readonly CanonicalDynamicFrameSequence _frames = new(2, 3, 4);
  private static readonly CanonicalDynamicSpriteSheet _sprite = new(_frames, 5, 2);
  private static readonly CanonicalDynamicEffectShape _shape = new(
    new EffectRectangle(3, 2, -1, -2),
    new EffectRectangle(-3, 4, 5, -6),
    -0.75f);
  private static readonly CanonicalDynamicAlpha _alpha = new(
    0.8f,
    0.2f,
    DynamicAlphaTiming.LifetimeProgress);
  private static readonly CanonicalDynamicTerrainLight _light = new(
    DynamicLightType.Trapezium,
    new Vector3(0.1f, 0.2f, 0.3f));

  [Fact]
  public async Task EveryKnownDynamicEffectHasAnExplicitCanonicalRecipe()
  {
    var recipes = new[]
    {
      DynamicEffectRecipes.Group(),
      DynamicEffectRecipes.Explosion(_sprite, _shape, "Textures\\fx\\explosion.tex",
        new Vector3(0.4f, 0.5f, 0.6f), _alpha, true, _light),
      DynamicEffectRecipes.Track(_frames, _shape.StartEffectRectangle, _shape.EndEffectRectangle,
        "Textures\\fx\\track.tex", _alpha, true),
      DynamicEffectRecipes.ScalableObject(_frames, "Objects\\effect.msh", "Textures\\fx\\object.tex",
        0.5f, 2f, new Vector3(0.4f, 0.5f, 0.6f), _alpha, true, _light),
      DynamicEffectRecipes.MappedExplosion(_frames, _shape.StartEffectRectangle, _shape.EndEffectRectangle,
        "Textures\\fx\\mapped.tex", new Vector3(0.4f, 0.5f, 0.6f), _alpha, true, _light),
      DynamicEffectRecipes.FlatExplosion(_sprite, _shape, "Textures\\fx\\flat.tex",
        new Vector3(0.4f, 0.5f, 0.6f), _alpha, true, _light),
      DynamicEffectRecipes.Laser(_sprite, -0.25f, "Textures\\fx\\laser.tex",
        new Vector3(0.4f, 0.5f, 0.6f), _alpha, true, _light),
      DynamicEffectRecipes.LaserWall(_sprite, 0.25f, "Textures\\fx\\wall.tex",
        new Vector3(0.4f, 0.5f, 0.6f), _alpha, true, new Vector3(0.1f, 0.2f, 0.3f)),
      DynamicEffectRecipes.Shockwave(_sprite, _shape, "Textures\\fx\\shockwave.tex",
        new Vector3(0.4f, 0.5f, 0.6f), 0.7f, 0.8f, 0.2f, true),
      DynamicEffectRecipes.Line(_sprite, _shape, "Textures\\fx\\line.tex",
        new Vector3(0.4f, 0.5f, 0.6f), 0.7f, 0.8f, 0.2f, true),
      DynamicEffectRecipes.Sphere("Textures\\fx\\sphere.tex", new Vector3(0.4f, 0.5f, 0.6f), true),
      DynamicEffectRecipes.ElectricalCannon(_sprite, 0.25f, "Textures\\fx\\electrical.tex",
        new Vector3(0.4f, 0.5f, 0.6f), _alpha, true),
      DynamicEffectRecipes.Lightning(_sprite, -0.25f, "Textures\\fx\\lightning.tex",
        new Vector3(0.4f, 0.5f, 0.6f), _alpha, true, _light),
      DynamicEffectRecipes.Smoke(_sprite, _shape, "Textures\\fx\\smoke.tex",
        new Vector3(0.4f, 0.5f, 0.6f), 0.7f, _alpha, true),
      DynamicEffectRecipes.Keelwater(_sprite, _shape, "Textures\\fx\\keelwater.tex", 0.8f, 0.2f, true)
    };

    recipes.Select(recipe => recipe.EffectType).Should().Equal(Enum.GetValues<DynamicEffectType>());
    foreach (var recipe in recipes)
    {
      var firstBuild = Build(recipe);
      var secondBuild = Build(recipe);
      firstBuild.TryGetValue(out var first).Should().BeTrue();
      secondBuild.TryGetValue(out var second).Should().BeTrue();
      firstBuild.Diagnostics.Should().BeEmpty();
      secondBuild.Diagnostics.Should().BeEmpty();
      (await WriteAsync(first!)).Should().Equal(await WriteAsync(second!));
    }
  }

  [Fact]
  public async Task RecipeProducesCoherentDeterministicSerializedRepresentations()
  {
    var child = DynamicEffectRecipes.ScalableObject(
      _frames,
      "Objects\\effect.msh",
      "Textures\\fx\\object.tex",
      0.5f,
      2f,
      new Vector3(0.4f, 0.5f, 0.6f),
      _alpha,
      true,
      _light)
      .SetChildTranslation(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
    var root = DynamicEffectRecipes.Lightning(
      _sprite,
      -0.25f,
      "Textures\\fx\\lightning.tex",
      new Vector3(0.4f, 0.5f, 0.6f),
      _alpha,
      true,
      _light,
      [child]);

    var firstBuild = Build(root);
    var secondBuild = Build(root);
    firstBuild.TryGetValue(out var first).Should().BeTrue();
    secondBuild.TryGetValue(out var second).Should().BeTrue();
    var firstExtension = first!.RootDynamicObject.Extension;
    var childExtension = first.RootDynamicObject.Children.Should().ContainSingle().Subject.Extension;

    firstExtension.KnownEffectType.Should().Be(DynamicEffectType.Lightning);
    firstExtension.KnownLightType.Should().Be(DynamicLightType.Trapezium);
    firstExtension.FirstSourceFrame.Should().Be(2);
    firstExtension.FrameCount.Should().Be(3);
    firstExtension.SpriteSheetColumnCount.Should().Be(5);
    firstExtension.SpriteSheetRowCount.Should().Be(2);
    firstExtension.FramePeriodTicks.Should().Be(4);
    firstExtension.ReciprocalColumnCount.Should().Be(0.2f);
    firstExtension.ReciprocalRowCount.Should().Be(0.5f);
    firstExtension.RibbonHalfWidth.Should().Be(-0.25f);
    firstExtension.UsesAdditiveBlending.Should().BeTrue();
    firstExtension.KnownAlphaTiming.Should().Be(DynamicAlphaTiming.LifetimeProgress);
    firstExtension.TexturePathBytes.Should().Equal(System.Text.Encoding.ASCII.GetBytes("Textures\\fx\\lightning.tex"));
    childExtension.MeshNameBytes.Should().Equal(System.Text.Encoding.ASCII.GetBytes("Objects\\effect.msh"));
    childExtension.StartModelScale.Should().Be(0.5f);
    childExtension.EndModelScale.Should().Be(2f);
    childExtension.ChildStartTranslation.Should().Be(new Vector3(1, 2, 3));
    childExtension.ChildEndTranslation.Should().Be(new Vector3(4, 5, 6));
    (await WriteAsync(first)).Should().Equal(await WriteAsync(second!));
  }

  [Fact]
  public async Task RecipeWritesDocumentedDynamicWireLayout()
  {
    var root = DynamicEffectRecipes.Lightning(
      _sprite,
      -0.25f,
      "Textures\\fx\\lightning.tex",
      new Vector3(0.4f, 0.5f, 0.6f),
      _alpha,
      true,
      _light,
      [DynamicEffectRecipes.Group()]);
    var build = Build(root);
    build.TryGetValue(out var asset).Should().BeTrue();
    var bytes = await WriteAsync(asset!);
    const int rootOffset = 0x18;

    BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(rootOffset + 0x368)).Should().Be(12);
    BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(rootOffset + 0x36C)).Should().Be(2);
    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(rootOffset + 0x370)).Should().Be(2);
    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(rootOffset + 0x374)).Should().Be(3);
    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(rootOffset + 0x378)).Should().Be(5);
    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(rootOffset + 0x37C)).Should().Be(2);
    ReadSingle(bytes, rootOffset + 0x384).Should().Be(0.2f);
    ReadSingle(bytes, rootOffset + 0x388).Should().Be(0.5f);
    ReadSingle(bytes, rootOffset + 0x38C).Should().Be(-0.25f);
    ReadSingle(bytes, rootOffset + 0x39C).Should().Be(-0.25f);
    ReadSingle(bytes, rootOffset + 0x3B0).Should().Be(-0.25f);
    BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(rootOffset + 0x3B8)).Should().Be(1);
    ReadSingle(bytes, rootOffset + 0x3BC).Should().Be(0.1f);
    ReadSingle(bytes, rootOffset + 0x3C8).Should().Be(0.4f);
    BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(rootOffset + 0x3D8)).Should().Be(1);
    ReadSingle(bytes, rootOffset + 0x3DC).Should().Be(0.2f);
    ReadSingle(bytes, rootOffset + 0x3E0).Should().Be(0.8f);
    BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(rootOffset + 0x404)).Should().Be(0);
    var textureBytes = System.Text.Encoding.ASCII.GetBytes("Textures\\fx\\lightning.tex");
    BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(rootOffset + 0x408))
      .Should().Be((uint)textureBytes.Length);
    bytes.AsSpan(rootOffset + 0x40C, textureBytes.Length).ToArray().Should().Equal(textureBytes);
    var childCountOffset = rootOffset + 0x40C + textureBytes.Length;
    BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(childCountOffset)).Should().Be(1);
    bytes.AsSpan(childCountOffset + 4, 4).ToArray().Should().Equal("MESH"u8.ToArray());
  }

  [Fact]
  public void CanonicalRecipesRejectInvalidSemanticInputsWithoutPartialAssets()
  {
    var invalidSprite = new CanonicalDynamicSpriteSheet(
      new CanonicalDynamicFrameSequence(9, 2, 0),
      2,
      5);
    var invalidRibbon = DynamicEffectRecipes.Laser(
      _sprite,
      0,
      "Textures\\fx\\laser.tex",
      Vector3.One,
      _alpha,
      false,
      _light);
    var invalidRootTranslation = DynamicEffectRecipes.Group()
      .SetChildTranslation(Vector3.One, Vector3.One);

    Build(DynamicEffectRecipes.Explosion(invalidSprite, _shape, "Textures\\fx\\bad.tex",
      Vector3.One, _alpha, false, _light)).TryGetValue(out _).Should().BeFalse();
    Build(invalidRibbon).TryGetValue(out _).Should().BeFalse();
    Build(invalidRootTranslation).TryGetValue(out _).Should().BeFalse();
    Build(DynamicEffectRecipes.Sphere(
      "Textures\\fx\\sphere?variant.tex",
      Vector3.One,
      false)).TryGetValue(out _).Should().BeFalse();
  }

  private static MshBuildResult<DynamicMeshAsset> Build(CanonicalDynamicObject root)
  {
    return DynamicMeshBuilder.Create(_creationGuid, _lineageId).SetRoot(root).Build();
  }

  private static async Task<byte[]> WriteAsync(MeshAsset asset)
  {
    await using var destination = new MemoryStream();
    var result = await new MshWriter().WriteAsync(asset, destination);
    result.Succeeded.Should().BeTrue();
    return destination.ToArray();
  }

  private static float ReadSingle(byte[] data, int offset)
  {
    return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset)));
  }
}
