#nullable enable

using EarthTool.MSH.Assets;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace EarthTool.MSH.Authoring
{
  /// <summary>Describes canonical frame selection without assuming a sprite-sheet layout.</summary>
  public readonly struct CanonicalDynamicFrameSequence
  {
    /// <summary>Gets the first source frame.</summary>
    public int FirstSourceFrame { get; }
    /// <summary>Gets the number of source frames.</summary>
    public int FrameCount { get; }
    /// <summary>Gets the simulation-tick period per frame, or zero for lifetime progression.</summary>
    public int FramePeriodTicks { get; }

    /// <summary>Initializes canonical frame-selection input.</summary>
    public CanonicalDynamicFrameSequence(int firstSourceFrame, int frameCount, int framePeriodTicks)
    {
      FirstSourceFrame = firstSourceFrame;
      FrameCount = frameCount;
      FramePeriodTicks = framePeriodTicks;
    }
  }

  /// <summary>Describes a canonical sprite sheet and its frame-selection domain.</summary>
  public readonly struct CanonicalDynamicSpriteSheet
  {
    /// <summary>Gets frame-selection input.</summary>
    public CanonicalDynamicFrameSequence Frames { get; }
    /// <summary>Gets the sprite-sheet column count.</summary>
    public int ColumnCount { get; }
    /// <summary>Gets the sprite-sheet row count.</summary>
    public int RowCount { get; }

    /// <summary>Initializes canonical sprite-sheet input.</summary>
    public CanonicalDynamicSpriteSheet(
      CanonicalDynamicFrameSequence frames,
      int columnCount,
      int rowCount)
    {
      Frames = frames;
      ColumnCount = columnCount;
      RowCount = rowCount;
    }
  }

  /// <summary>Describes independent canonical start and end effect rectangles and depth.</summary>
  public readonly struct CanonicalDynamicEffectShape
  {
    /// <summary>Gets the start effect rectangle.</summary>
    public EffectRectangle StartEffectRectangle { get; }
    /// <summary>Gets the end effect rectangle.</summary>
    public EffectRectangle EndEffectRectangle { get; }
    /// <summary>Gets the effect-specific depth offset.</summary>
    public float EffectDepthOffset { get; }

    /// <summary>Initializes canonical effect-shape input without sorting rectangle lanes.</summary>
    public CanonicalDynamicEffectShape(
      EffectRectangle startEffectRectangle,
      EffectRectangle endEffectRectangle,
      float effectDepthOffset)
    {
      StartEffectRectangle = startEffectRectangle;
      EndEffectRectangle = endEffectRectangle;
      EffectDepthOffset = effectDepthOffset;
    }
  }

  /// <summary>Describes canonical alpha endpoints and their explicit timing mode.</summary>
  public readonly struct CanonicalDynamicAlpha
  {
    /// <summary>Gets the start alpha.</summary>
    public float StartAlpha { get; }
    /// <summary>Gets the end alpha.</summary>
    public float EndAlpha { get; }
    /// <summary>Gets the alpha timing mode.</summary>
    public DynamicAlphaTiming Timing { get; }

    /// <summary>Initializes canonical alpha input.</summary>
    public CanonicalDynamicAlpha(float startAlpha, float endAlpha, DynamicAlphaTiming timing)
    {
      StartAlpha = startAlpha;
      EndAlpha = endAlpha;
      Timing = timing;
    }
  }

  /// <summary>Describes a canonical terrain-light mode and final serialized RGB value.</summary>
  public readonly struct CanonicalDynamicTerrainLight
  {
    /// <summary>Gets the terrain-light intensity mode.</summary>
    public DynamicLightType LightType { get; }
    /// <summary>Gets the final terrain-light RGB value.</summary>
    public Vector3 Color { get; }

    /// <summary>Initializes canonical terrain-light input.</summary>
    public CanonicalDynamicTerrainLight(DynamicLightType lightType, Vector3 color)
    {
      LightType = lightType;
      Color = color;
    }
  }

  internal sealed class CanonicalDynamicRecipe
  {
    internal DynamicEffectType EffectType { get; set; }
    internal DynamicLightType LightType { get; set; }
    internal int FirstSourceFrame { get; set; }
    internal int FrameCount { get; set; }
    internal int SpriteSheetColumnCount { get; set; }
    internal int SpriteSheetRowCount { get; set; }
    internal int FramePeriodTicks { get; set; }
    internal EffectRectangle StartEffectRectangle { get; set; } = DynamicEffectRecipes.DefaultRectangle;
    internal EffectRectangle EndEffectRectangle { get; set; } = DynamicEffectRecipes.DefaultRectangle;
    internal float EffectDepthOffset { get; set; } = 0.25f;
    internal float RibbonHalfWidth { get; set; } = 0.25f;
    internal bool Additive { get; set; }
    internal Vector3 TerrainLightColor { get; set; }
    internal Vector3 VisibleEffectColor { get; set; } = Vector3.One;
    internal float VisibleTerrainLightGain { get; set; } = 1f;
    internal DynamicAlphaTiming AlphaTiming { get; set; }
    internal float StartAlpha { get; set; } = 1f;
    internal float EndAlpha { get; set; } = 1f;
    internal float StartModelScale { get; set; }
    internal float EndModelScale { get; set; }
    internal Vector3 ChildStartTranslation { get; set; }
    internal Vector3 ChildEndTranslation { get; set; }
    internal string MeshResourceKey { get; set; } = string.Empty;
    internal string TextureResourceKey { get; set; } = string.Empty;
  }

  /// <summary>Creates coherent canonical dynamic objects for every recognized effect.</summary>
  public static class DynamicEffectRecipes
  {
    internal static EffectRectangle DefaultRectangle { get; } =
      new EffectRectangle(-0.25f, 0.25f, 0.25f, -0.25f);

    /// <summary>Creates a child-container effect with no own primary geometry or terrain light.</summary>
    public static CanonicalDynamicObject Group(
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return Create(DynamicEffectType.Group, children);
    }

    /// <summary>Creates a camera-facing explosion and its radial terrain light.</summary>
    public static CanonicalDynamicObject Explosion(
      CanonicalDynamicSpriteSheet sprite,
      CanonicalDynamicEffectShape shape,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      CanonicalDynamicTerrainLight terrainLight,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return SpriteShape(DynamicEffectType.Explosion, sprite, shape, textureResourceKey,
        visibleEffectColor, alpha, additive, terrainLight, children);
    }

    /// <summary>Creates a terrain-cell track decal.</summary>
    public static CanonicalDynamicObject Track(
      CanonicalDynamicFrameSequence frames,
      EffectRectangle startEffectRectangle,
      EffectRectangle endEffectRectangle,
      string textureResourceKey,
      CanonicalDynamicAlpha alpha,
      bool additive,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      var recipe = Base(DynamicEffectType.Track, textureResourceKey, Vector3.One, alpha, additive);
      ApplyFrames(recipe, frames);
      recipe.StartEffectRectangle = startEffectRectangle;
      recipe.EndEffectRectangle = endEffectRectangle;
      return new CanonicalDynamicObject(recipe, children);
    }

    /// <summary>Creates a referenced mesh with interpolated uniform scale.</summary>
    public static CanonicalDynamicObject ScalableObject(
      CanonicalDynamicFrameSequence frames,
      string meshResourceKey,
      string textureResourceKey,
      float startModelScale,
      float endModelScale,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      CanonicalDynamicTerrainLight terrainLight,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      var recipe = Base(DynamicEffectType.ScalableObject, textureResourceKey,
        visibleEffectColor, alpha, additive);
      ApplyFrames(recipe, frames);
      ApplyLight(recipe, terrainLight);
      recipe.MeshResourceKey = meshResourceKey ?? throw new ArgumentNullException(nameof(meshResourceKey));
      recipe.StartModelScale = startModelScale;
      recipe.EndModelScale = endModelScale;
      return new CanonicalDynamicObject(recipe, children);
    }

    /// <summary>Creates a mapped terrain explosion and its radial terrain light.</summary>
    public static CanonicalDynamicObject MappedExplosion(
      CanonicalDynamicFrameSequence frames,
      EffectRectangle startEffectRectangle,
      EffectRectangle endEffectRectangle,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      CanonicalDynamicTerrainLight terrainLight,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      var recipe = Base(DynamicEffectType.MappedExplosion, textureResourceKey,
        visibleEffectColor, alpha, additive);
      ApplyFrames(recipe, frames);
      ApplyLight(recipe, terrainLight);
      recipe.StartEffectRectangle = startEffectRectangle;
      recipe.EndEffectRectangle = endEffectRectangle;
      return new CanonicalDynamicObject(recipe, children);
    }

    /// <summary>Creates a local-plane explosion and its radial terrain light.</summary>
    public static CanonicalDynamicObject FlatExplosion(
      CanonicalDynamicSpriteSheet sprite,
      CanonicalDynamicEffectShape shape,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      CanonicalDynamicTerrainLight terrainLight,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return SpriteShape(DynamicEffectType.FlatExplosion, sprite, shape, textureResourceKey,
        visibleEffectColor, alpha, additive, terrainLight, children);
    }

    /// <summary>Creates an endpoint ribbon with a modulated terrain-light line.</summary>
    public static CanonicalDynamicObject Laser(
      CanonicalDynamicSpriteSheet sprite,
      float ribbonHalfWidth,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      CanonicalDynamicTerrainLight terrainLight,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return Ribbon(DynamicEffectType.Laser, sprite, ribbonHalfWidth, textureResourceKey,
        visibleEffectColor, alpha, additive, terrainLight, children);
    }

    /// <summary>Creates an endpoint ribbon with an unmodulated terrain-light line.</summary>
    public static CanonicalDynamicObject LaserWall(
      CanonicalDynamicSpriteSheet sprite,
      float ribbonHalfWidth,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      Vector3 terrainLightColor,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      var recipe = RibbonRecipe(DynamicEffectType.LaserWall, sprite, ribbonHalfWidth,
        textureResourceKey, visibleEffectColor, alpha, additive);
      recipe.TerrainLightColor = terrainLightColor;
      return new CanonicalDynamicObject(recipe, children);
    }

    /// <summary>Creates a generic attached-particle shockwave recipe.</summary>
    public static CanonicalDynamicObject Shockwave(
      CanonicalDynamicSpriteSheet sprite,
      CanonicalDynamicEffectShape shape,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      float visibleTerrainLightGain,
      float startAlpha,
      float endAlpha,
      bool additive,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return Attached(DynamicEffectType.Shockwave, sprite, shape, textureResourceKey,
        visibleEffectColor, visibleTerrainLightGain,
        new CanonicalDynamicAlpha(startAlpha, endAlpha, DynamicAlphaTiming.FramePhase),
        additive, children);
    }

    /// <summary>Creates a generic attached-particle line recipe.</summary>
    public static CanonicalDynamicObject Line(
      CanonicalDynamicSpriteSheet sprite,
      CanonicalDynamicEffectShape shape,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      float visibleTerrainLightGain,
      float startAlpha,
      float endAlpha,
      bool additive,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return Attached(DynamicEffectType.Line, sprite, shape, textureResourceKey,
        visibleEffectColor, visibleTerrainLightGain,
        new CanonicalDynamicAlpha(startAlpha, endAlpha, DynamicAlphaTiming.FramePhase),
        additive, children);
    }

    /// <summary>Creates the dedicated built-in sphere effect.</summary>
    public static CanonicalDynamicObject Sphere(
      string textureResourceKey,
      Vector3 visibleEffectColor,
      bool additive,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      var recipe = new CanonicalDynamicRecipe
      {
        EffectType = DynamicEffectType.Sphere,
        TextureResourceKey = textureResourceKey ?? throw new ArgumentNullException(nameof(textureResourceKey)),
        VisibleEffectColor = visibleEffectColor,
        Additive = additive
      };
      return new CanonicalDynamicObject(recipe, children);
    }

    /// <summary>Creates an adaptive jagged endpoint ribbon.</summary>
    public static CanonicalDynamicObject ElectricalCannon(
      CanonicalDynamicSpriteSheet sprite,
      float ribbonHalfWidth,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return new CanonicalDynamicObject(
        RibbonRecipe(DynamicEffectType.ElectricalCannon, sprite, ribbonHalfWidth,
          textureResourceKey, visibleEffectColor, alpha, additive),
        children);
    }

    /// <summary>Creates a fixed-segment jagged ribbon and terrain-light line.</summary>
    public static CanonicalDynamicObject Lightning(
      CanonicalDynamicSpriteSheet sprite,
      float ribbonHalfWidth,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      CanonicalDynamicTerrainLight terrainLight,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return Ribbon(DynamicEffectType.Lightning, sprite, ribbonHalfWidth, textureResourceKey,
        visibleEffectColor, alpha, additive, terrainLight, children);
    }

    /// <summary>Creates a terrain-light-modulated smoke billboard.</summary>
    public static CanonicalDynamicObject Smoke(
      CanonicalDynamicSpriteSheet sprite,
      CanonicalDynamicEffectShape shape,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      float visibleTerrainLightGain,
      CanonicalDynamicAlpha alpha,
      bool additive,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return Attached(DynamicEffectType.Smoke, sprite, shape, textureResourceKey,
        visibleEffectColor, visibleTerrainLightGain, alpha, additive, children);
    }

    /// <summary>Creates a dedicated water-colored attached billboard.</summary>
    public static CanonicalDynamicObject Keelwater(
      CanonicalDynamicSpriteSheet sprite,
      CanonicalDynamicEffectShape shape,
      string textureResourceKey,
      float startAlpha,
      float endAlpha,
      bool additive,
      IEnumerable<CanonicalDynamicObject>? children = null)
    {
      return Attached(DynamicEffectType.Keelwater, sprite, shape, textureResourceKey,
        Vector3.One, 1f,
        new CanonicalDynamicAlpha(startAlpha, endAlpha, DynamicAlphaTiming.FramePhase),
        additive, children);
    }

    private static CanonicalDynamicObject Create(
      DynamicEffectType effectType,
      IEnumerable<CanonicalDynamicObject>? children)
    {
      return new CanonicalDynamicObject(new CanonicalDynamicRecipe { EffectType = effectType }, children);
    }

    private static CanonicalDynamicObject SpriteShape(
      DynamicEffectType effectType,
      CanonicalDynamicSpriteSheet sprite,
      CanonicalDynamicEffectShape shape,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      CanonicalDynamicTerrainLight terrainLight,
      IEnumerable<CanonicalDynamicObject>? children)
    {
      var recipe = Base(effectType, textureResourceKey, visibleEffectColor, alpha, additive);
      ApplySprite(recipe, sprite);
      ApplyShape(recipe, shape);
      ApplyLight(recipe, terrainLight);
      return new CanonicalDynamicObject(recipe, children);
    }

    private static CanonicalDynamicObject Ribbon(
      DynamicEffectType effectType,
      CanonicalDynamicSpriteSheet sprite,
      float ribbonHalfWidth,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive,
      CanonicalDynamicTerrainLight terrainLight,
      IEnumerable<CanonicalDynamicObject>? children)
    {
      var recipe = RibbonRecipe(effectType, sprite, ribbonHalfWidth,
        textureResourceKey, visibleEffectColor, alpha, additive);
      ApplyLight(recipe, terrainLight);
      return new CanonicalDynamicObject(recipe, children);
    }

    private static CanonicalDynamicRecipe RibbonRecipe(
      DynamicEffectType effectType,
      CanonicalDynamicSpriteSheet sprite,
      float ribbonHalfWidth,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive)
    {
      var recipe = Base(effectType, textureResourceKey, visibleEffectColor, alpha, additive);
      ApplySprite(recipe, sprite);
      recipe.RibbonHalfWidth = ribbonHalfWidth;
      return recipe;
    }

    private static CanonicalDynamicObject Attached(
      DynamicEffectType effectType,
      CanonicalDynamicSpriteSheet sprite,
      CanonicalDynamicEffectShape shape,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      float visibleTerrainLightGain,
      CanonicalDynamicAlpha alpha,
      bool additive,
      IEnumerable<CanonicalDynamicObject>? children)
    {
      var recipe = Base(effectType, textureResourceKey, visibleEffectColor, alpha, additive);
      ApplySprite(recipe, sprite);
      ApplyShape(recipe, shape);
      recipe.VisibleTerrainLightGain = visibleTerrainLightGain;
      return new CanonicalDynamicObject(recipe, children);
    }

    private static CanonicalDynamicRecipe Base(
      DynamicEffectType effectType,
      string textureResourceKey,
      Vector3 visibleEffectColor,
      CanonicalDynamicAlpha alpha,
      bool additive)
    {
      return new CanonicalDynamicRecipe
      {
        EffectType = effectType,
        TextureResourceKey = textureResourceKey ?? throw new ArgumentNullException(nameof(textureResourceKey)),
        VisibleEffectColor = visibleEffectColor,
        AlphaTiming = alpha.Timing,
        StartAlpha = alpha.StartAlpha,
        EndAlpha = alpha.EndAlpha,
        Additive = additive
      };
    }

    private static void ApplyFrames(CanonicalDynamicRecipe recipe, CanonicalDynamicFrameSequence frames)
    {
      recipe.FirstSourceFrame = frames.FirstSourceFrame;
      recipe.FrameCount = frames.FrameCount;
      recipe.FramePeriodTicks = frames.FramePeriodTicks;
    }

    private static void ApplySprite(CanonicalDynamicRecipe recipe, CanonicalDynamicSpriteSheet sprite)
    {
      ApplyFrames(recipe, sprite.Frames);
      recipe.SpriteSheetColumnCount = sprite.ColumnCount;
      recipe.SpriteSheetRowCount = sprite.RowCount;
    }

    private static void ApplyShape(CanonicalDynamicRecipe recipe, CanonicalDynamicEffectShape shape)
    {
      recipe.StartEffectRectangle = shape.StartEffectRectangle;
      recipe.EndEffectRectangle = shape.EndEffectRectangle;
      recipe.EffectDepthOffset = shape.EffectDepthOffset;
    }

    private static void ApplyLight(CanonicalDynamicRecipe recipe, CanonicalDynamicTerrainLight terrainLight)
    {
      recipe.LightType = terrainLight.LightType;
      recipe.TerrainLightColor = terrainLight.Color;
    }
  }
}
