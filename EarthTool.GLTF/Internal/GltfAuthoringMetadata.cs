#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace EarthTool.GLTF.Internal
{
  internal enum CanonicalAuthoringOwnerKind
  {
    StaticSource,
    Attachment,
    Cannon,
    StaticLight,
    Animation,
    DynamicObject
  }

  internal readonly struct CanonicalAuthoringOwner : IEquatable<CanonicalAuthoringOwner>
  {
    internal CanonicalAuthoringOwnerKind Kind { get; }

    internal int Number { get; }

    internal string CanonicalName { get; }

    internal DynamicEffectType? EffectType { get; }

    private CanonicalAuthoringOwner(
      CanonicalAuthoringOwnerKind kind,
      int number,
      string canonicalName,
      DynamicEffectType? effectType = null)
    {
      Kind = kind;
      Number = number;
      CanonicalName = canonicalName;
      EffectType = effectType;
    }

    internal static CanonicalAuthoringOwner Parse(string name)
    {
      return TryParse(name, out var owner)
        ? owner
        : throw new ArgumentException("The name is not a canonical authoring identifier.", nameof(name));
    }

    internal static bool TryParse(string? name, out CanonicalAuthoringOwner owner)
    {
      owner = default;
      if (name is null)
      {
        return false;
      }

      if (TryParsePositiveSuffix(name, "ET_Static_", out var number))
      {
        owner = new CanonicalAuthoringOwner(
          CanonicalAuthoringOwnerKind.StaticSource,
          number,
          name);
        return true;
      }
      if (GlbDocument.TryParseCannonHelperName(name, out number))
      {
        owner = new CanonicalAuthoringOwner(CanonicalAuthoringOwnerKind.Cannon, number, name);
        return true;
      }
      if (GlbDocument.TryParseAttachmentHelperName(name, out number))
      {
        owner = new CanonicalAuthoringOwner(CanonicalAuthoringOwnerKind.Attachment, number, name);
        return true;
      }
      if (GlbDocument.TryParseStaticLightHelperName(name, out _, out number))
      {
        owner = new CanonicalAuthoringOwner(CanonicalAuthoringOwnerKind.StaticLight, number, name);
        return true;
      }
      for (var classIndex = 0; classIndex < 4; classIndex++)
      {
        var animationName = $"EarthTool {(char)('A' + classIndex)}";
        if (string.Equals(name, animationName, StringComparison.Ordinal))
        {
          owner = new CanonicalAuthoringOwner(
            CanonicalAuthoringOwnerKind.Animation,
            classIndex + 1,
            name);
          return true;
        }
      }
      if (TryParseDynamic(name, out number, out var effectType))
      {
        owner = new CanonicalAuthoringOwner(
          CanonicalAuthoringOwnerKind.DynamicObject,
          number,
          name,
          effectType);
        return true;
      }
      return false;
    }

    public bool Equals(CanonicalAuthoringOwner other)
    {
      return Kind == other.Kind
        && Number == other.Number
        && string.Equals(CanonicalName, other.CanonicalName, StringComparison.Ordinal)
        && EffectType == other.EffectType;
    }

    public override bool Equals(object? obj)
    {
      return obj is CanonicalAuthoringOwner other && Equals(other);
    }

    public override int GetHashCode()
    {
      return (Kind, Number, CanonicalName, EffectType).GetHashCode();
    }

    private static bool TryParseDynamic(
      string name,
      out int number,
      out DynamicEffectType effectType)
    {
      number = 0;
      effectType = default;
      const string prefix = "ET_Dynamic_";
      if (!name.StartsWith(prefix, StringComparison.Ordinal))
      {
        return false;
      }
      var separator = name.IndexOf('_', prefix.Length);
      if (separator < 0 || !TryParsePositiveNumber(name[prefix.Length..separator], out number))
      {
        return false;
      }
      var effectName = name[(separator + 1)..];
      return Enum.TryParse(effectName, ignoreCase: false, out effectType)
        && Enum.IsDefined(typeof(DynamicEffectType), effectType)
        && string.Equals(effectType.ToString(), effectName, StringComparison.Ordinal);
    }

    private static bool TryParsePositiveSuffix(string name, string prefix, out int number)
    {
      number = 0;
      return name.StartsWith(prefix, StringComparison.Ordinal)
        && TryParsePositiveNumber(name[prefix.Length..], out number);
    }

    private static bool TryParsePositiveNumber(string text, out int number)
    {
      number = 0;
      return text.Length != 0
        && text[0] != '0'
        && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number)
        && number > 0
        && string.Equals(number.ToString(CultureInfo.InvariantCulture), text, StringComparison.Ordinal);
    }
  }

  internal sealed class AuthoringMetadataCarrier
  {
    internal string Path { get; }

    internal string Name { get; }

    internal string? Metadata { get; }

    internal AuthoringMetadataCarrier(string path, string name, string? metadata)
    {
      Path = path ?? throw new ArgumentNullException(nameof(path));
      Name = name ?? throw new ArgumentNullException(nameof(name));
      Metadata = metadata;
    }
  }

  internal abstract class AuthoringMetadataValues { }

  internal sealed class EmptyAuthoringValues : AuthoringMetadataValues
  {
    internal static EmptyAuthoringValues Instance { get; } = new EmptyAuthoringValues();

    private EmptyAuthoringValues() { }
  }

  internal sealed class StaticSourceAuthoringValues : AuthoringMetadataValues
  {
    internal CanonicalStaticFootprint? Footprint { get; }

    internal CanonicalHorizontalExtents? HorizontalExtents { get; }

    internal GltfStaticObjectRoles Roles { get; }

    internal byte BarrelMaximumAngle { get; }

    internal StaticSourceAuthoringValues(
      CanonicalStaticFootprint? footprint = null,
      CanonicalHorizontalExtents? horizontalExtents = null,
      GltfStaticObjectRoles roles = GltfStaticObjectRoles.None,
      byte barrelMaximumAngle = 0)
    {
      var allowed = GltfStaticObjectRoles.ViewerFaced
        | GltfStaticObjectRoles.Barrel
        | GltfStaticObjectRoles.Rotor;
      if ((roles & ~allowed) != 0)
      {
        throw new ArgumentOutOfRangeException(nameof(roles));
      }
      if ((roles & GltfStaticObjectRoles.Barrel) == 0 && barrelMaximumAngle != 0)
      {
        throw new ArgumentOutOfRangeException(nameof(barrelMaximumAngle));
      }
      Footprint = footprint;
      HorizontalExtents = horizontalExtents;
      Roles = roles;
      BarrelMaximumAngle = barrelMaximumAngle;
    }
  }

  internal sealed class CannonAuthoringValues : AuthoringMetadataValues
  {
    internal byte YawHalfRange { get; }

    internal CannonAuthoringValues(byte yawHalfRange = 0x80)
    {
      YawHalfRange = yawHalfRange;
    }
  }

  internal sealed class StaticLightAuthoringValues : AuthoringMetadataValues
  {
    internal float TerrainLightAmplitude { get; }

    internal float? TargetDistance { get; }

    internal StaticLightAuthoringValues(
      float terrainLightAmplitude = 1,
      float? targetDistance = null
    )
    {
      if (!float.IsFinite(terrainLightAmplitude) || terrainLightAmplitude < 0)
      {
        throw new ArgumentOutOfRangeException(nameof(terrainLightAmplitude));
      }
      if (
        targetDistance.HasValue
        && (!float.IsFinite(targetDistance.Value) || targetDistance.Value <= 0)
      )
      {
        throw new ArgumentOutOfRangeException(nameof(targetDistance));
      }
      TerrainLightAmplitude = terrainLightAmplitude;
      TargetDistance = targetDistance;
    }
  }

  internal sealed class DynamicAuthoringValues : AuthoringMetadataValues
  {
    internal CanonicalDynamicFrameSequence? Frames { get; }

    internal CanonicalDynamicSpriteSheet? SpriteSheet { get; }

    internal EffectRectangle EndEffectRectangle { get; }

    internal CanonicalDynamicTerrainLight TerrainLight { get; }

    internal float VisibleTerrainLightGain { get; }

    internal DynamicAlphaTiming AlphaTiming { get; }

    internal float EndAlpha { get; }

    internal bool Additive { get; }

    internal string? MeshResourceKey { get; }

    internal DynamicAuthoringValues(
      EffectRectangle? endEffectRectangle = null,
      CanonicalDynamicTerrainLight? terrainLight = null,
      float visibleTerrainLightGain = 1,
      DynamicAlphaTiming alphaTiming = DynamicAlphaTiming.FramePhase,
      float endAlpha = 1,
      bool additive = false)
      : this(
        null,
        null,
        endEffectRectangle,
        terrainLight,
        visibleTerrainLightGain,
        alphaTiming,
        endAlpha,
        additive)
    { }

    internal DynamicAuthoringValues(
      CanonicalDynamicFrameSequence frames,
      EffectRectangle? endEffectRectangle = null,
      CanonicalDynamicTerrainLight? terrainLight = null,
      float visibleTerrainLightGain = 1,
      DynamicAlphaTiming alphaTiming = DynamicAlphaTiming.FramePhase,
      float endAlpha = 1,
      bool additive = false)
      : this(
        frames,
        null,
        endEffectRectangle,
        terrainLight,
        visibleTerrainLightGain,
        alphaTiming,
        endAlpha,
        additive)
    { }

    internal DynamicAuthoringValues(
      CanonicalDynamicSpriteSheet spriteSheet,
      EffectRectangle? endEffectRectangle = null,
      CanonicalDynamicTerrainLight? terrainLight = null,
      float visibleTerrainLightGain = 1,
      DynamicAlphaTiming alphaTiming = DynamicAlphaTiming.FramePhase,
      float endAlpha = 1,
      bool additive = false)
      : this(
        spriteSheet.Frames,
        spriteSheet,
        endEffectRectangle,
        terrainLight,
        visibleTerrainLightGain,
        alphaTiming,
        endAlpha,
        additive)
    { }

    private DynamicAuthoringValues(
      CanonicalDynamicFrameSequence? frames,
      CanonicalDynamicSpriteSheet? spriteSheet,
      EffectRectangle? endEffectRectangle,
      CanonicalDynamicTerrainLight? terrainLight,
      float visibleTerrainLightGain,
      DynamicAlphaTiming alphaTiming,
      float endAlpha,
      bool additive,
      string? meshResourceKey = null)
    {
      Frames = frames;
      SpriteSheet = spriteSheet;
      EndEffectRectangle = endEffectRectangle ?? DefaultRectangle;
      TerrainLight = terrainLight
        ?? new CanonicalDynamicTerrainLight(DynamicLightType.Constant, Vector3.Zero);
      VisibleTerrainLightGain = visibleTerrainLightGain;
      AlphaTiming = alphaTiming;
      EndAlpha = endAlpha;
      Additive = additive;
      MeshResourceKey = meshResourceKey;
    }

    internal static DynamicAuthoringValues Defaults { get; } = new DynamicAuthoringValues(
      null,
      null,
      null,
      null,
      1,
      DynamicAlphaTiming.FramePhase,
      1,
      false);

    internal static DynamicAuthoringValues Create(
      CanonicalDynamicFrameSequence? frames,
      CanonicalDynamicSpriteSheet? spriteSheet,
      EffectRectangle endEffectRectangle,
      CanonicalDynamicTerrainLight terrainLight,
      float visibleTerrainLightGain,
      DynamicAlphaTiming alphaTiming,
      float endAlpha,
      bool additive,
      string? meshResourceKey = null)
    {
      return new DynamicAuthoringValues(
        frames,
        spriteSheet,
        endEffectRectangle,
        terrainLight,
        visibleTerrainLightGain,
        alphaTiming,
        endAlpha,
        additive,
        meshResourceKey);
    }

    private static EffectRectangle DefaultRectangle => new(-0.25f, 0.25f, 0.25f, -0.25f);
  }

  internal sealed class CanonicalAuthoringMetadataDocument
  {
    private readonly IReadOnlyDictionary<CanonicalAuthoringOwner, AuthoringMetadataValues> _values;

    internal CanonicalAuthoringMetadataDocument(
      IDictionary<CanonicalAuthoringOwner, AuthoringMetadataValues> values)
    {
      _values = new ReadOnlyDictionary<CanonicalAuthoringOwner, AuthoringMetadataValues>(
        new Dictionary<CanonicalAuthoringOwner, AuthoringMetadataValues>(values));
    }

    internal T Get<T>(CanonicalAuthoringOwner owner)
      where T : AuthoringMetadataValues
    {
      return _values.TryGetValue(owner, out var value) && value is T typed
        ? typed
        : throw new KeyNotFoundException(owner.CanonicalName);
    }
  }

  internal static class GltfAuthoringMetadataDiagnosticCodes
  {
    internal const string OptionalValueDefaulted = GltfDiagnosticCodes.AuthoringValueDefaulted;
    internal const string DuplicateOwner = GltfDiagnosticCodes.DuplicateAuthoringOwner;
    internal const string RequiredValueMissing = GltfDiagnosticCodes.RequiredAuthoringValueMissing;
    internal const string DiagnosticsTruncated = GltfDiagnosticCodes.AuthoringDiagnosticsTruncated;
  }

  internal static class CanonicalAuthoringMetadata
  {
    internal const string Format = "earthtool.msh.authoring";
    internal const int Version = 1;

    internal const string MaterialFormat = "earthtool.msh.material-authoring";
    internal const int MaterialVersion = 1;

    private static readonly HashSet<string> _rootProperties = new(StringComparer.Ordinal)
    {
      "format",
      "version",
      "values"
    };

    /// <summary>Writes a minimal material envelope carrying a canonical TEX resource key.</summary>
    internal static string WriteMaterial(string textureResourceKey, GltfOperationProfile profile)
    {
      if (textureResourceKey is null)
      {
        throw new ArgumentNullException(nameof(textureResourceKey));
      }
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        writer.WriteStartObject();
        writer.WriteString("format", MaterialFormat);
        writer.WriteNumber("version", MaterialVersion);
        writer.WriteString("textureResourceKey", textureResourceKey);
        writer.WriteEndObject();
      }
      if (stream.Length > profile.MaxMetadataBytes)
      {
        throw new InvalidDataException("The material authoring metadata envelope exceeds its byte limit.");
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Reads the canonical TEX resource key from a material envelope, when present.</summary>
    internal static string? ReadMaterialTextureResourceKey(string? metadata)
    {
      if (string.IsNullOrEmpty(metadata))
      {
        return null;
      }
      try
      {
        using var document = JsonDocument.Parse(metadata, new JsonDocumentOptions
        {
          MaxDepth = GltfOperationProfile.Default.MaxJsonDepth
        });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
          || !TryString(root, "format", out var format)
          || !string.Equals(format, MaterialFormat, StringComparison.Ordinal)
          || !TryInt32(root, "version", out var version)
          || version != MaterialVersion
          || !TryString(root, "textureResourceKey", out var resourceKey))
        {
          return null;
        }
        return resourceKey;
      }
      catch (JsonException)
      {
        return null;
      }
    }

    internal static OperationResult<CanonicalAuthoringMetadataDocument> Read(
      IEnumerable<AuthoringMetadataCarrier> carriers,
      GltfOperationProfile profile)
    {
      if (carriers is null)
      {
        throw new ArgumentNullException(nameof(carriers));
      }
      if (profile is null)
      {
        throw new ArgumentNullException(nameof(profile));
      }

      var input = carriers.ToArray();
      if (input.Any(carrier => carrier is null))
      {
        throw new ArgumentException("Metadata carriers cannot contain null values.", nameof(carriers));
      }
      if (input.Count(carrier => carrier.Metadata is not null) > profile.MaxMetadataEnvelopes)
      {
        return Failed<CanonicalAuthoringMetadataDocument>(MetadataLimit("metadata"));
      }

      long totalBytes = 0;
      var totalElements = 0;
      foreach (var carrier in input.Where(carrier => carrier.Metadata is not null))
      {
        var byteCount = Encoding.UTF8.GetByteCount(carrier.Metadata!);
        totalBytes = checked(totalBytes + byteCount);
        if (byteCount > profile.MaxMetadataBytes || totalBytes > profile.MaxTotalMetadataBytes)
        {
          return Failed<CanonicalAuthoringMetadataDocument>(MetadataLimit(carrier.Path));
        }
        try
        {
          totalElements = checked(
            totalElements + CountJsonElements(
              carrier.Metadata!,
              profile,
              profile.MaxMetadataElements - totalElements
            )
          );
        }
        catch (MetadataElementLimitException)
        {
          return Failed<CanonicalAuthoringMetadataDocument>(MetadataLimit(carrier.Path));
        }
      }

      var warnings = new WarningCollector(profile.MaxAuthoringDiagnostics);
      var parsed = new List<(AuthoringMetadataCarrier Carrier, CanonicalAuthoringOwner Owner)>();
      foreach (var carrier in input)
      {
        if (CanonicalAuthoringOwner.TryParse(carrier.Name, out var owner))
        {
          parsed.Add((carrier, owner));
          continue;
        }
        if (carrier.Name.StartsWith("ET_Dynamic_", StringComparison.OrdinalIgnoreCase))
        {
          return Failed<CanonicalAuthoringMetadataDocument>(
            Required(carrier.Path, "A dynamic object requires an exact recognized canonical name."));
        }
        if (carrier.Metadata is not null)
        {
          warnings.Add(Defaulted(carrier.Path, "Metadata on a noncanonical owner was ignored."));
        }
      }

      var duplicate = parsed
        .GroupBy(item => item.Owner.CanonicalName, StringComparer.Ordinal)
        .Where(group => group.Count() > 1)
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .FirstOrDefault();
      if (duplicate is not null)
      {
        var paths = duplicate.Select(item => item.Carrier.Path)
          .OrderBy(path => path, StringComparer.Ordinal)
          .ToArray();
        return Failed<CanonicalAuthoringMetadataDocument>(
          new OperationDiagnostic(
            GltfAuthoringMetadataDiagnosticCodes.DuplicateOwner,
            4001,
            DiagnosticSeverity.Error,
            paths[0],
            "A canonical authoring identifier is declared more than once.",
            data: new Dictionary<string, string> { ["paths"] = string.Join(',', paths) }));
      }

      var values = new Dictionary<CanonicalAuthoringOwner, AuthoringMetadataValues>();
      var errors = new List<OperationDiagnostic>();
      var unknownMembers = 0;
      foreach (var item in parsed.OrderBy(item => item.Carrier.Path, StringComparer.Ordinal))
      {
        var value = ReadOwner(item, profile, warnings, ref unknownMembers);
        values.Add(item.Owner, value);
        if (item.Owner.Kind == CanonicalAuthoringOwnerKind.DynamicObject)
        {
          var requiredFailure = ValidateRequiredDynamicValues(
            item.Owner,
            (DynamicAuthoringValues)value,
            item.Carrier.Path);
          if (requiredFailure is not null)
          {
            errors.Add(requiredFailure);
          }
        }
      }
      if (unknownMembers > profile.MaxUnknownMetadataMembers)
      {
        return Failed<CanonicalAuthoringMetadataDocument>(MetadataLimit("metadata"));
      }
      if (errors.Count != 0)
      {
        return new OperationResult<CanonicalAuthoringMetadataDocument>(
          OperationStatus.Failed,
          diagnostics: errors.Concat(warnings.Diagnostics));
      }
      return new OperationResult<CanonicalAuthoringMetadataDocument>(
        OperationStatus.Succeeded,
        new CanonicalAuthoringMetadataDocument(values),
        warnings.Diagnostics);
    }

    internal static string Write(
      CanonicalAuthoringOwner owner,
      AuthoringMetadataValues values,
      GltfOperationProfile profile)
    {
      if (values is null)
      {
        throw new ArgumentNullException(nameof(values));
      }
      if (profile is null)
      {
        throw new ArgumentNullException(nameof(profile));
      }
      EnsureCompatible(owner, values);
      if (values is DynamicAuthoringValues dynamicValues)
      {
        var failure = ValidateRequiredDynamicValues(owner, dynamicValues, owner.CanonicalName);
        if (failure is not null)
        {
          throw new InvalidDataException(failure.Message);
        }
      }

      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream))
      {
        writer.WriteStartObject();
        writer.WriteString("format", Format);
        writer.WriteNumber("version", Version);
        writer.WriteStartObject("values");
        WriteValues(writer, owner, values);
        writer.WriteEndObject();
        writer.WriteEndObject();
      }
      if (stream.Length > profile.MaxMetadataBytes)
      {
        throw new InvalidDataException("The authoring metadata envelope exceeds its byte limit.");
      }
      return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static AuthoringMetadataValues ReadOwner(
      (AuthoringMetadataCarrier Carrier, CanonicalAuthoringOwner Owner) item,
      GltfOperationProfile profile,
      WarningCollector warnings,
      ref int unknownMembers)
    {
      var defaults = CreateDefaults(item.Owner);
      if (item.Carrier.Metadata is null)
      {
        if (HasTypedValues(item.Owner))
        {
          warnings.Add(Defaulted(item.Carrier.Path, "Local typed metadata is absent; canonical defaults were used."));
        }
        return defaults;
      }

      JsonDocument document;
      try
      {
        ValidateJsonBounds(item.Carrier.Metadata, profile, profile.MaxMetadataElements);
        document = JsonDocument.Parse(
          item.Carrier.Metadata,
          new JsonDocumentOptions { MaxDepth = profile.MaxJsonDepth });
      }
      catch (Exception ex) when (ex is JsonException or InvalidDataException)
      {
        warnings.Add(Defaulted(item.Carrier.Path, "Local typed metadata is malformed; canonical defaults were used."));
        return defaults;
      }
      using (document)
      {
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
          || !TryString(root, "format", out var format)
          || !string.Equals(format, Format, StringComparison.Ordinal)
          || !TryInt32(root, "version", out var version)
          || version != Version
          || !root.TryGetProperty("values", out var jsonValues)
          || jsonValues.ValueKind != JsonValueKind.Object)
        {
          warnings.Add(Defaulted(item.Carrier.Path, "Local typed metadata format is unsupported; canonical defaults were used."));
          return defaults;
        }
        AddUnknownWarnings(
          root,
          _rootProperties,
          item.Carrier.Path,
          warnings,
          ref unknownMembers);
        return ReadValues(item.Owner, jsonValues, item.Carrier.Path, warnings, ref unknownMembers);
      }
    }

    private static AuthoringMetadataValues ReadValues(
      CanonicalAuthoringOwner owner,
      JsonElement values,
      string path,
      WarningCollector warnings,
      ref int unknownMembers)
    {
      return owner.Kind switch
      {
        CanonicalAuthoringOwnerKind.StaticSource => ReadStaticSource(
          values,
          path,
          warnings,
          ref unknownMembers),
        CanonicalAuthoringOwnerKind.Cannon => ReadCannon(
          values,
          path,
          warnings,
          ref unknownMembers),
        CanonicalAuthoringOwnerKind.StaticLight => ReadStaticLight(
          values,
          path,
          warnings,
          ref unknownMembers),
        CanonicalAuthoringOwnerKind.DynamicObject => ReadDynamic(
          owner,
          values,
          path,
          warnings,
          ref unknownMembers),
        _ => ReadEmpty(values, path, warnings, ref unknownMembers)
      };
    }

    private static StaticSourceAuthoringValues ReadStaticSource(
      JsonElement values,
      string path,
      WarningCollector warnings,
      ref int unknownMembers)
    {
      var known = new HashSet<string>(StringComparer.Ordinal)
      {
        "footprint",
        "horizontalExtents",
        "role"
      };
      AddUnknownWarnings(values, known, path, warnings, ref unknownMembers);
      AddNestedUnknownWarnings(
        values,
        "footprint",
        new[] { "presenceMask", "topElevations", "cornerPassageFlags" },
        path,
        warnings,
        ref unknownMembers);
      AddNestedUnknownWarnings(
        values,
        "horizontalExtents",
        new[] { "positiveY", "negativeY", "positiveX", "negativeX" },
        path,
        warnings,
        ref unknownMembers);
      AddNestedUnknownWarnings(
        values,
        "role",
        new[] { "viewerFaced", "barrel", "rotor", "barrelMaximumAngle" },
        path,
        warnings,
        ref unknownMembers);
      var footprint = ReadFootprint(values, path, warnings);
      var extents = ReadExtents(values, path, warnings);
      if (!values.TryGetProperty("footprint", out _))
      {
        warnings.Add(Defaulted(path + ".values.footprint", "The footprint is absent; its geometry-derived canonical default will be used."));
      }
      if (!values.TryGetProperty("horizontalExtents", out _))
      {
        warnings.Add(Defaulted(path + ".values.horizontalExtents", "Horizontal extents are absent; their geometry-derived canonical default will be used."));
      }
      var roles = GltfStaticObjectRoles.None;
      byte angle = 0;
      if (values.TryGetProperty("role", out var role))
      {
        if (role.ValueKind != JsonValueKind.Object
          || !TryBoolean(role, "viewerFaced", out var viewerFaced)
          || !TryBoolean(role, "barrel", out var barrel)
          || !TryBoolean(role, "rotor", out var rotor)
          || !TryByte(role, "barrelMaximumAngle", out angle)
          || (!barrel && angle != 0))
        {
          warnings.Add(Defaulted(path + ".values.role", "The source-object role is invalid; its canonical default was used."));
          angle = 0;
        }
        else
        {
          if (viewerFaced)
            roles |= GltfStaticObjectRoles.ViewerFaced;
          if (barrel)
            roles |= GltfStaticObjectRoles.Barrel;
          if (rotor)
            roles |= GltfStaticObjectRoles.Rotor;
        }
      }
      else
      {
        warnings.Add(Defaulted(path + ".values.role", "The source-object role is absent; its canonical default was used."));
      }
      return new StaticSourceAuthoringValues(footprint, extents, roles, angle);
    }

    private static CannonAuthoringValues ReadCannon(
      JsonElement values,
      string path,
      WarningCollector warnings,
      ref int unknownMembers)
    {
      var known = new HashSet<string>(StringComparer.Ordinal) { "cannonYawHalfRange" };
      AddUnknownWarnings(values, known, path, warnings, ref unknownMembers);
      if (!values.TryGetProperty("cannonYawHalfRange", out var property))
      {
        warnings.Add(Defaulted(path + ".values.cannonYawHalfRange", "The cannon yaw half-range is absent; its canonical default was used."));
        return new CannonAuthoringValues();
      }
      if (!TryByte(property, out var value))
      {
        warnings.Add(Defaulted(path + ".values.cannonYawHalfRange", "The cannon yaw half-range is invalid; its canonical default was used."));
        return new CannonAuthoringValues();
      }
      return new CannonAuthoringValues(value);
    }

    private static StaticLightAuthoringValues ReadStaticLight(
      JsonElement values,
      string path,
      WarningCollector warnings,
      ref int unknownMembers)
    {
      var known = new HashSet<string>(StringComparer.Ordinal)
      {
        "targetDistance",
        "terrainLightAmplitude"
      };
      AddUnknownWarnings(values, known, path, warnings, ref unknownMembers);
      float? targetDistance = null;
      if (values.TryGetProperty("targetDistance", out var targetProperty))
      {
        if (!TryFiniteSingle(targetProperty, out var targetValue) || targetValue <= 0)
        {
          warnings.Add(Defaulted(path + ".values.targetDistance", "The spot target distance is invalid; native range evidence or the required-value validation will apply."));
        }
        else
        {
          targetDistance = targetValue;
        }
      }
      if (!values.TryGetProperty("terrainLightAmplitude", out var property))
      {
        warnings.Add(Defaulted(path + ".values.terrainLightAmplitude", "The terrain-light amplitude is absent; its canonical default was used."));
        return new StaticLightAuthoringValues(targetDistance: targetDistance);
      }
      if (!TryFiniteSingle(property, out var value) || value < 0)
      {
        warnings.Add(Defaulted(path + ".values.terrainLightAmplitude", "The terrain-light amplitude is invalid; its canonical default was used."));
        return new StaticLightAuthoringValues(targetDistance: targetDistance);
      }
      return new StaticLightAuthoringValues(value, targetDistance);
    }

    private static DynamicAuthoringValues ReadDynamic(
      CanonicalAuthoringOwner owner,
      JsonElement values,
      string path,
      WarningCollector warnings,
      ref int unknownMembers)
    {
      var effectType = owner.EffectType!.Value;
      var known = new HashSet<string>(StringComparer.Ordinal)
      {
        "frames",
        "spriteSheet",
        "endEffectRectangle",
        "terrainLight",
        "visibleTerrainLightGain",
        "alphaTiming",
        "endAlpha",
        "additive",
        "meshResourceKey"
      };
      AddUnknownWarnings(values, known, path, warnings, ref unknownMembers);
      AddNestedUnknownWarnings(
        values,
        "frames",
        new[] { "first", "count", "periodTicks" },
        path,
        warnings,
        ref unknownMembers);
      AddNestedUnknownWarnings(
        values,
        "spriteSheet",
        new[] { "columns", "rows" },
        path,
        warnings,
        ref unknownMembers);
      AddNestedUnknownWarnings(
        values,
        "endEffectRectangle",
        new[] { "x0", "y1", "x1", "y0" },
        path,
        warnings,
        ref unknownMembers);
      AddNestedUnknownWarnings(
        values,
        "terrainLight",
        DynamicEffectBehavior.ConsumesRepresentation(effectType, DynamicRepresentationUse.LightType)
          ? new[] { "mode", "red", "green", "blue" }
          : new[] { "red", "green", "blue" },
        path,
        warnings,
        ref unknownMembers);

      var requirements = DynamicEffectBehavior.GetAuthoringRequirements(effectType);
      var requiresFrames = (requirements & DynamicAuthoringRequirement.Frames) != 0;
      var requiresSprite = (requirements & DynamicAuthoringRequirement.SpriteSheet) != 0;

      CanonicalDynamicFrameSequence? frames = null;
      if (values.TryGetProperty("frames", out var frameProperty))
      {
        if (TryReadFrames(frameProperty, out var parsedFrames))
        {
          frames = parsedFrames;
        }
        else
        {
          warnings.Add(Defaulted(path + ".values.frames", "Dynamic frame values are invalid."));
        }
      }
      if (values.TryGetProperty("frames", out _) && !requiresFrames)
      {
        warnings.Add(Defaulted(path + ".values.frames", "This effect does not support local frame metadata; the value was ignored."));
      }
      CanonicalDynamicSpriteSheet? sprite = null;
      if (values.TryGetProperty("spriteSheet", out var spriteProperty))
      {
        if (frames.HasValue
          && spriteProperty.ValueKind == JsonValueKind.Object
          && TryPositiveInt32(spriteProperty, "columns", out var columns)
          && TryPositiveInt32(spriteProperty, "rows", out var rows)
          && FramesFit(frames.Value, columns, rows))
        {
          sprite = new CanonicalDynamicSpriteSheet(frames.Value, columns, rows);
        }
        else
        {
          warnings.Add(Defaulted(path + ".values.spriteSheet", "Dynamic sprite-sheet values are invalid."));
        }
      }
      if (values.TryGetProperty("spriteSheet", out _) && !requiresSprite)
      {
        warnings.Add(Defaulted(path + ".values.spriteSheet", "This effect does not support local sprite-sheet metadata; the value was ignored."));
      }

      WarnMissingDynamicMember(effectType, DynamicRepresentationUse.EffectRectangles,
        "endEffectRectangle", values, path, warnings);
      WarnMissingDynamicMember(effectType, DynamicRepresentationUse.TerrainLightColor,
        "terrainLight", values, path, warnings);
      WarnMissingDynamicMember(effectType, DynamicRepresentationUse.VisibleTerrainLightGain,
        "visibleTerrainLightGain", values, path, warnings);
      WarnMissingDynamicMember(effectType, DynamicRepresentationUse.AlphaTiming,
        "alphaTiming", values, path, warnings);
      WarnMissingDynamicMember(effectType, DynamicRepresentationUse.AlphaEndpoints,
        "endAlpha", values, path, warnings);
      WarnMissingDynamicMember(effectType, DynamicRepresentationUse.AdditiveFlag,
        "additive", values, path, warnings);

      var endRectangle = ReadRectangle(values, "endEffectRectangle", path, warnings)
        ?? new EffectRectangle(-0.25f, 0.25f, 0.25f, -0.25f);
      var terrainLight = ReadTerrainLight(effectType, values, path, warnings);
      var visibleGain = ReadOptionalFinite(
        values,
        "visibleTerrainLightGain",
        1,
        path,
        warnings);
      var endAlpha = ReadOptionalFinite(values, "endAlpha", 1, path, warnings);
      var additive = ReadOptionalBoolean(values, "additive", false, path, warnings);
      var alphaTiming = DynamicAlphaTiming.FramePhase;
      if (values.TryGetProperty("alphaTiming", out var timing))
      {
        if (timing.ValueKind != JsonValueKind.String
          || !Enum.TryParse(timing.GetString(), ignoreCase: false, out alphaTiming)
          || !Enum.IsDefined(typeof(DynamicAlphaTiming), alphaTiming))
        {
          warnings.Add(Defaulted(path + ".values.alphaTiming", "The alpha timing is invalid; its canonical default was used."));
          alphaTiming = DynamicAlphaTiming.FramePhase;
        }
      }
      string? meshResourceKey = null;
      if (values.TryGetProperty("meshResourceKey", out var meshResource))
      {
        if (meshResource.ValueKind == JsonValueKind.String
          && !string.IsNullOrEmpty(meshResource.GetString()))
        {
          meshResourceKey = meshResource.GetString();
        }
        else
        {
          warnings.Add(Defaulted(path + ".values.meshResourceKey", "The mesh resource key is invalid; its required-value validation will apply."));
        }
      }

      var result = DynamicAuthoringValues.Create(
        frames,
        sprite,
        endRectangle,
        terrainLight,
        visibleGain,
        alphaTiming,
        endAlpha,
        additive,
        meshResourceKey);
      return ApplyDynamicApplicability(effectType, result, values, path, warnings);
    }

    private static DynamicAuthoringValues ApplyDynamicApplicability(
      DynamicEffectType effectType,
      DynamicAuthoringValues values,
      JsonElement json,
      string path,
      WarningCollector warnings)
    {
      WarnUnsupportedDynamicMember(
        effectType,
        DynamicRepresentationUse.EffectRectangles,
        "endEffectRectangle",
        json,
        path,
        warnings);
      WarnUnsupportedDynamicMember(
        effectType,
        DynamicRepresentationUse.VisibleTerrainLightGain,
        "visibleTerrainLightGain",
        json,
        path,
        warnings);
      WarnUnsupportedDynamicMember(
        effectType,
        DynamicRepresentationUse.AlphaTiming,
        "alphaTiming",
        json,
        path,
        warnings);
      WarnUnsupportedDynamicMember(
        effectType,
        DynamicRepresentationUse.AlphaEndpoints,
        "endAlpha",
        json,
        path,
        warnings);
      WarnUnsupportedDynamicMember(
        effectType,
        DynamicRepresentationUse.AdditiveFlag,
        "additive",
        json,
        path,
        warnings);
      var consumesTerrain = DynamicEffectBehavior.ConsumesRepresentation(
        effectType,
        DynamicRepresentationUse.TerrainLightColor);
      if (json.TryGetProperty("terrainLight", out _) && !consumesTerrain)
      {
        warnings.Add(Defaulted(path + ".values.terrainLight", "This effect does not support local terrain-light metadata; the value was ignored."));
      }
      return DynamicAuthoringValues.Create(
        (DynamicEffectBehavior.GetAuthoringRequirements(effectType)
          & DynamicAuthoringRequirement.Frames) != 0
            ? values.Frames
            : null,
        (DynamicEffectBehavior.GetAuthoringRequirements(effectType)
          & DynamicAuthoringRequirement.SpriteSheet) != 0
            ? values.SpriteSheet
            : null,
        DynamicEffectBehavior.ConsumesRepresentation(
          effectType,
          DynamicRepresentationUse.EffectRectangles)
            ? values.EndEffectRectangle
            : DynamicAuthoringValues.Defaults.EndEffectRectangle,
        consumesTerrain
          ? values.TerrainLight
          : DynamicAuthoringValues.Defaults.TerrainLight,
        DynamicEffectBehavior.ConsumesRepresentation(
          effectType,
          DynamicRepresentationUse.VisibleTerrainLightGain)
            ? values.VisibleTerrainLightGain
            : DynamicAuthoringValues.Defaults.VisibleTerrainLightGain,
        DynamicEffectBehavior.ConsumesRepresentation(
          effectType,
          DynamicRepresentationUse.AlphaTiming)
            ? values.AlphaTiming
            : DynamicAuthoringValues.Defaults.AlphaTiming,
        DynamicEffectBehavior.ConsumesRepresentation(
          effectType,
          DynamicRepresentationUse.AlphaEndpoints)
            ? values.EndAlpha
            : DynamicAuthoringValues.Defaults.EndAlpha,
        DynamicEffectBehavior.ConsumesRepresentation(
          effectType,
          DynamicRepresentationUse.AdditiveFlag)
            ? values.Additive
            : DynamicAuthoringValues.Defaults.Additive,
        DynamicEffectBehavior.ConsumesRepresentation(
          effectType,
          DynamicRepresentationUse.MeshResourceKey)
            ? values.MeshResourceKey
            : null);
    }

    private static EmptyAuthoringValues ReadEmpty(
      JsonElement values,
      string path,
      WarningCollector warnings,
      ref int unknownMembers)
    {
      AddUnknownWarnings(
        values,
        new HashSet<string>(StringComparer.Ordinal),
        path,
        warnings,
        ref unknownMembers);
      return EmptyAuthoringValues.Instance;
    }

    private static CanonicalStaticFootprint? ReadFootprint(
      JsonElement values,
      string path,
      WarningCollector warnings)
    {
      if (!values.TryGetProperty("footprint", out var footprint))
      {
        return null;
      }
      try
      {
        if (footprint.ValueKind != JsonValueKind.Object
          || !TryUInt16(footprint, "presenceMask", out var mask)
          || !TrySingleArray(footprint, "topElevations", 16, out var elevations)
          || !TryByteArray(footprint, "cornerPassageFlags", 16, 0x0F, out var flags))
        {
          throw new InvalidDataException();
        }
        return new CanonicalStaticFootprint(mask, elevations, flags);
      }
      catch (Exception ex) when (ex is ArgumentException or InvalidDataException)
      {
        warnings.Add(Defaulted(path + ".values.footprint", "The footprint is invalid; its geometry-derived canonical default will be used."));
        return null;
      }
    }

    private static CanonicalHorizontalExtents? ReadExtents(
      JsonElement values,
      string path,
      WarningCollector warnings)
    {
      if (!values.TryGetProperty("horizontalExtents", out var extents))
      {
        return null;
      }
      try
      {
        if (extents.ValueKind != JsonValueKind.Object
          || !TryFiniteSingle(extents, "positiveY", out var positiveY)
          || !TryFiniteSingle(extents, "negativeY", out var negativeY)
          || !TryFiniteSingle(extents, "positiveX", out var positiveX)
          || !TryFiniteSingle(extents, "negativeX", out var negativeX))
        {
          throw new InvalidDataException();
        }
        return new CanonicalHorizontalExtents(positiveY, negativeY, positiveX, negativeX);
      }
      catch (Exception ex) when (ex is ArgumentException or InvalidDataException)
      {
        warnings.Add(Defaulted(path + ".values.horizontalExtents", "The horizontal extents are invalid; their geometry-derived canonical default will be used."));
        return null;
      }
    }

    private static EffectRectangle? ReadRectangle(
      JsonElement values,
      string name,
      string path,
      WarningCollector warnings)
    {
      if (!values.TryGetProperty(name, out var rectangle))
      {
        return null;
      }
      if (rectangle.ValueKind == JsonValueKind.Object
        && TryFiniteSingle(rectangle, "x0", out var x0)
        && TryFiniteSingle(rectangle, "y1", out var y1)
        && TryFiniteSingle(rectangle, "x1", out var x1)
        && TryFiniteSingle(rectangle, "y0", out var y0))
      {
        return new EffectRectangle(x0, y1, x1, y0);
      }
      warnings.Add(Defaulted(path + ".values." + name, "The effect rectangle is invalid; its canonical default was used."));
      return null;
    }

    private static CanonicalDynamicTerrainLight ReadTerrainLight(
      DynamicEffectType effectType,
      JsonElement values,
      string path,
      WarningCollector warnings)
    {
      var result = new CanonicalDynamicTerrainLight(DynamicLightType.Constant, Vector3.Zero);
      if (!values.TryGetProperty("terrainLight", out var light))
      {
        return result;
      }
      var needsMode = DynamicEffectBehavior.ConsumesRepresentation(
        effectType,
        DynamicRepresentationUse.LightType);
      var lightType = DynamicLightType.Constant;
      if (light.ValueKind == JsonValueKind.Object
        && (!needsMode
          || TryString(light, "mode", out var mode)
            && Enum.TryParse(mode, ignoreCase: false, out lightType)
            && Enum.IsDefined(typeof(DynamicLightType), lightType))
        && TryFiniteSingle(light, "red", out var red)
        && TryFiniteSingle(light, "green", out var green)
        && TryFiniteSingle(light, "blue", out var blue))
      {
        return new CanonicalDynamicTerrainLight(lightType, new Vector3(red, green, blue));
      }
      warnings.Add(Defaulted(path + ".values.terrainLight", "The terrain-light values are invalid; their canonical defaults were used."));
      return result;
    }

    private static void WriteValues(
      Utf8JsonWriter writer,
      CanonicalAuthoringOwner owner,
      AuthoringMetadataValues values)
    {
      switch (values)
      {
        case StaticSourceAuthoringValues source:
          WriteStaticSource(writer, source);
          break;
        case CannonAuthoringValues cannon:
          writer.WriteNumber("cannonYawHalfRange", cannon.YawHalfRange);
          break;
        case StaticLightAuthoringValues light:
          if (light.TargetDistance.HasValue)
          {
            writer.WriteNumber("targetDistance", light.TargetDistance.Value);
          }
          writer.WriteNumber("terrainLightAmplitude", light.TerrainLightAmplitude);
          break;
        case DynamicAuthoringValues dynamicValues:
          WriteDynamic(writer, owner.EffectType!.Value, dynamicValues);
          break;
      }
    }

    private static void WriteStaticSource(Utf8JsonWriter writer, StaticSourceAuthoringValues values)
    {
      if (values.Footprint is not null)
      {
        writer.WriteStartObject("footprint");
        writer.WriteNumber("presenceMask", values.Footprint.PresenceMask);
        WriteArray(writer, "topElevations", values.Footprint.TopElevations);
        WriteArray(writer, "cornerPassageFlags", values.Footprint.CornerPassageFlags);
        writer.WriteEndObject();
      }
      if (values.HorizontalExtents is not null)
      {
        writer.WriteStartObject("horizontalExtents");
        writer.WriteNumber("positiveY", values.HorizontalExtents.PositiveY);
        writer.WriteNumber("negativeY", values.HorizontalExtents.NegativeY);
        writer.WriteNumber("positiveX", values.HorizontalExtents.PositiveX);
        writer.WriteNumber("negativeX", values.HorizontalExtents.NegativeX);
        writer.WriteEndObject();
      }
      writer.WriteStartObject("role");
      writer.WriteBoolean(
        "viewerFaced",
        (values.Roles & GltfStaticObjectRoles.ViewerFaced) != 0);
      writer.WriteBoolean("barrel", (values.Roles & GltfStaticObjectRoles.Barrel) != 0);
      writer.WriteBoolean("rotor", (values.Roles & GltfStaticObjectRoles.Rotor) != 0);
      writer.WriteNumber("barrelMaximumAngle", values.BarrelMaximumAngle);
      writer.WriteEndObject();
    }

    private static void WriteDynamic(
      Utf8JsonWriter writer,
      DynamicEffectType effectType,
      DynamicAuthoringValues values)
    {
      var requirements = DynamicEffectBehavior.GetAuthoringRequirements(effectType);
      if ((requirements & DynamicAuthoringRequirement.Frames) != 0)
      {
        var frames = values.Frames!.Value;
        writer.WriteStartObject("frames");
        writer.WriteNumber("first", frames.FirstSourceFrame);
        writer.WriteNumber("count", frames.FrameCount);
        writer.WriteNumber("periodTicks", frames.FramePeriodTicks);
        writer.WriteEndObject();
      }
      if ((requirements & DynamicAuthoringRequirement.SpriteSheet) != 0)
      {
        var sprite = values.SpriteSheet!.Value;
        writer.WriteStartObject("spriteSheet");
        writer.WriteNumber("columns", sprite.ColumnCount);
        writer.WriteNumber("rows", sprite.RowCount);
        writer.WriteEndObject();
      }
      if (DynamicEffectBehavior.ConsumesRepresentation(effectType, DynamicRepresentationUse.EffectRectangles))
      {
        WriteRectangle(writer, "endEffectRectangle", values.EndEffectRectangle);
      }
      if (DynamicEffectBehavior.ConsumesRepresentation(
        effectType,
        DynamicRepresentationUse.TerrainLightColor))
      {
        writer.WriteStartObject("terrainLight");
        if (DynamicEffectBehavior.ConsumesRepresentation(
          effectType,
          DynamicRepresentationUse.LightType))
        {
          writer.WriteString("mode", values.TerrainLight.LightType.ToString());
        }
        writer.WriteNumber("red", values.TerrainLight.Color.X);
        writer.WriteNumber("green", values.TerrainLight.Color.Y);
        writer.WriteNumber("blue", values.TerrainLight.Color.Z);
        writer.WriteEndObject();
      }
      if (DynamicEffectBehavior.ConsumesRepresentation(effectType, DynamicRepresentationUse.VisibleTerrainLightGain))
      {
        writer.WriteNumber("visibleTerrainLightGain", values.VisibleTerrainLightGain);
      }
      if (DynamicEffectBehavior.ConsumesRepresentation(effectType, DynamicRepresentationUse.AlphaTiming))
      {
        writer.WriteString("alphaTiming", values.AlphaTiming.ToString());
      }
      if (DynamicEffectBehavior.ConsumesRepresentation(effectType, DynamicRepresentationUse.AlphaEndpoints))
      {
        writer.WriteNumber("endAlpha", values.EndAlpha);
      }
      if (DynamicEffectBehavior.ConsumesRepresentation(effectType, DynamicRepresentationUse.AdditiveFlag))
      {
        writer.WriteBoolean("additive", values.Additive);
      }
      if (DynamicEffectBehavior.ConsumesRepresentation(effectType, DynamicRepresentationUse.MeshResourceKey)
        && values.MeshResourceKey is not null)
      {
        writer.WriteString("meshResourceKey", values.MeshResourceKey);
      }
    }

    private static OperationDiagnostic? ValidateRequiredDynamicValues(
      CanonicalAuthoringOwner owner,
      DynamicAuthoringValues values,
      string path)
    {
      var requirements = DynamicEffectBehavior.GetAuthoringRequirements(owner.EffectType!.Value);
      if ((requirements & DynamicAuthoringRequirement.Frames) != 0
        && (!values.Frames.HasValue || !ValidFrames(values.Frames.Value)))
      {
        return Required(path + ".values.frames", "The effect requires supported frame values and has no safe canonical default.");
      }
      if ((requirements & DynamicAuthoringRequirement.SpriteSheet) != 0
        && (!values.SpriteSheet.HasValue
          || values.SpriteSheet.Value.ColumnCount <= 0
          || values.SpriteSheet.Value.RowCount <= 0
          || !FramesFit(
            values.SpriteSheet.Value.Frames,
            values.SpriteSheet.Value.ColumnCount,
            values.SpriteSheet.Value.RowCount)))
      {
        return Required(path + ".values.spriteSheet", "The effect requires a supported sprite-sheet domain and has no safe canonical default.");
      }
      if (!float.IsFinite(values.VisibleTerrainLightGain)
        || !float.IsFinite(values.EndAlpha)
        || !Enum.IsDefined(typeof(DynamicAlphaTiming), values.AlphaTiming)
        || !Enum.IsDefined(typeof(DynamicLightType), values.TerrainLight.LightType)
        || !IsFinite(values.EndEffectRectangle)
        || !IsFinite(values.TerrainLight.Color))
      {
        return Required(path + ".values", "Dynamic typed authoring values must be finite and supported.");
      }
      return null;
    }

    private static AuthoringMetadataValues CreateDefaults(CanonicalAuthoringOwner owner)
    {
      return owner.Kind switch
      {
        CanonicalAuthoringOwnerKind.StaticSource => new StaticSourceAuthoringValues(),
        CanonicalAuthoringOwnerKind.Cannon => new CannonAuthoringValues(),
        CanonicalAuthoringOwnerKind.StaticLight => new StaticLightAuthoringValues(),
        CanonicalAuthoringOwnerKind.DynamicObject => DynamicAuthoringValues.Defaults,
        _ => EmptyAuthoringValues.Instance
      };
    }

    private static bool HasTypedValues(CanonicalAuthoringOwner owner)
    {
      return owner.Kind is CanonicalAuthoringOwnerKind.StaticSource
        or CanonicalAuthoringOwnerKind.Cannon
        or CanonicalAuthoringOwnerKind.StaticLight
        || owner.Kind == CanonicalAuthoringOwnerKind.DynamicObject
          && owner.EffectType != DynamicEffectType.Group;
    }

    private static void EnsureCompatible(
      CanonicalAuthoringOwner owner,
      AuthoringMetadataValues values)
    {
      var compatible = owner.Kind switch
      {
        CanonicalAuthoringOwnerKind.StaticSource => values is StaticSourceAuthoringValues,
        CanonicalAuthoringOwnerKind.Cannon => values is CannonAuthoringValues,
        CanonicalAuthoringOwnerKind.StaticLight => values is StaticLightAuthoringValues,
        CanonicalAuthoringOwnerKind.DynamicObject => values is DynamicAuthoringValues,
        _ => values is EmptyAuthoringValues
      };
      if (!compatible)
      {
        throw new ArgumentException("Typed values do not match their canonical named owner.", nameof(values));
      }
    }

    private static void AddUnknownWarnings(
      JsonElement value,
      ISet<string> known,
      string path,
      WarningCollector warnings,
      ref int unknownMembers)
    {
      foreach (var property in value.EnumerateObject())
      {
        if (known.Contains(property.Name))
        {
          continue;
        }
        unknownMembers = checked(unknownMembers + 1);
        warnings.Add(Defaulted(
          path + "." + property.Name,
          "An unsupported local metadata member was ignored."));
      }
    }

    private static void AddNestedUnknownWarnings(
      JsonElement values,
      string name,
      IEnumerable<string> knownNames,
      string path,
      WarningCollector warnings,
      ref int unknownMembers)
    {
      if (!values.TryGetProperty(name, out var nested) || nested.ValueKind != JsonValueKind.Object)
      {
        return;
      }
      AddUnknownWarnings(
        nested,
        new HashSet<string>(knownNames, StringComparer.Ordinal),
        path + ".values." + name,
        warnings,
        ref unknownMembers);
    }

    private static void WarnUnsupportedDynamicMember(
      DynamicEffectType effectType,
      DynamicRepresentationUse representation,
      string name,
      JsonElement values,
      string path,
      WarningCollector warnings)
    {
      if (values.TryGetProperty(name, out _)
        && !DynamicEffectBehavior.ConsumesRepresentation(effectType, representation))
      {
        warnings.Add(Defaulted(
          path + ".values." + name,
          "This effect does not support the local typed value; its canonical default was used."));
      }
    }

    private static void WarnMissingDynamicMember(
      DynamicEffectType effectType,
      DynamicRepresentationUse representation,
      string name,
      JsonElement values,
      string path,
      WarningCollector warnings)
    {
      if (!values.TryGetProperty(name, out _)
        && DynamicEffectBehavior.ConsumesRepresentation(effectType, representation))
      {
        warnings.Add(Defaulted(
          path + ".values." + name,
          "The optional local typed value is absent; its canonical default was used."));
      }
    }

    private static int ValidateJsonBounds(
      string json,
      GltfOperationProfile profile,
      int maximumElements)
    {
      var reader = new Utf8JsonReader(
        Encoding.UTF8.GetBytes(json),
        new JsonReaderOptions { MaxDepth = profile.MaxJsonDepth });
      var objects = new Stack<HashSet<string>>();
      var elements = 0;
      while (reader.Read())
      {
        elements = checked(elements + 1);
        if (elements > maximumElements)
        {
          throw new MetadataElementLimitException();
        }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
          objects.Push(new HashSet<string>(StringComparer.Ordinal));
        }
        else if (reader.TokenType == JsonTokenType.EndObject)
        {
          objects.Pop();
        }
        else if (reader.TokenType == JsonTokenType.PropertyName
          && (objects.Count == 0 || !objects.Peek().Add(reader.GetString()!)))
        {
          throw new InvalidDataException("A metadata object contains a duplicate member.");
        }
      }
      return elements;
    }

    private static int CountJsonElements(
      string json,
      GltfOperationProfile profile,
      int maximumElements)
    {
      var reader = new Utf8JsonReader(
        Encoding.UTF8.GetBytes(json),
        new JsonReaderOptions { MaxDepth = profile.MaxJsonDepth });
      var elements = 0;
      try
      {
        while (reader.Read())
        {
          elements = checked(elements + 1);
          if (elements > maximumElements)
          {
            throw new MetadataElementLimitException();
          }
        }
      }
      catch (JsonException)
      {
        return elements;
      }
      return elements;
    }

    private sealed class MetadataElementLimitException : Exception { }

    private static bool TryReadFrames(JsonElement value, out CanonicalDynamicFrameSequence frames)
    {
      frames = default;
      if (value.ValueKind != JsonValueKind.Object
        || !TryInt32(value, "first", out var first)
        || !TryInt32(value, "count", out var count)
        || !TryInt32(value, "periodTicks", out var period))
      {
        return false;
      }
      frames = new CanonicalDynamicFrameSequence(first, count, period);
      return ValidFrames(frames);
    }

    private static bool ValidFrames(CanonicalDynamicFrameSequence frames)
    {
      return frames.FirstSourceFrame >= 0 && frames.FrameCount > 0 && frames.FramePeriodTicks >= 0;
    }

    private static bool FramesFit(
      CanonicalDynamicFrameSequence frames,
      int columns,
      int rows)
    {
      try
      {
        return ValidFrames(frames)
          && checked(frames.FirstSourceFrame + frames.FrameCount) <= checked(columns * rows);
      }
      catch (OverflowException)
      {
        return false;
      }
    }

    private static float ReadOptionalFinite(
      JsonElement values,
      string name,
      float fallback,
      string path,
      WarningCollector warnings)
    {
      if (!values.TryGetProperty(name, out var property))
      {
        return fallback;
      }
      if (TryFiniteSingle(property, out var value))
      {
        return value;
      }
      warnings.Add(Defaulted(path + ".values." + name, "The local typed value is invalid; its canonical default was used."));
      return fallback;
    }

    private static bool ReadOptionalBoolean(
      JsonElement values,
      string name,
      bool fallback,
      string path,
      WarningCollector warnings)
    {
      if (!values.TryGetProperty(name, out var property))
      {
        return fallback;
      }
      if (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
      {
        return property.GetBoolean();
      }
      warnings.Add(Defaulted(path + ".values." + name, "The local typed value is invalid; its canonical default was used."));
      return fallback;
    }

    private static bool TryString(JsonElement value, string name, out string result)
    {
      result = string.Empty;
      return value.TryGetProperty(name, out var property)
        && property.ValueKind == JsonValueKind.String
        && (result = property.GetString() ?? string.Empty).Length != 0;
    }

    private static bool TryInt32(JsonElement value, string name, out int result)
    {
      result = 0;
      return value.TryGetProperty(name, out var property)
        && property.ValueKind == JsonValueKind.Number
        && property.TryGetInt32(out result);
    }

    private static bool TryPositiveInt32(JsonElement value, string name, out int result)
    {
      return TryInt32(value, name, out result) && result > 0;
    }

    private static bool TryUInt16(JsonElement value, string name, out ushort result)
    {
      result = 0;
      return value.TryGetProperty(name, out var property)
        && property.ValueKind == JsonValueKind.Number
        && property.TryGetUInt16(out result);
    }

    private static bool TryByte(JsonElement value, string name, out byte result)
    {
      result = 0;
      return value.TryGetProperty(name, out var property) && TryByte(property, out result);
    }

    private static bool TryByte(JsonElement value, out byte result)
    {
      result = 0;
      return value.ValueKind == JsonValueKind.Number && value.TryGetByte(out result);
    }

    private static bool TryBoolean(JsonElement value, string name, out bool result)
    {
      result = false;
      if (!value.TryGetProperty(name, out var property)
        || property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
      {
        return false;
      }
      result = property.GetBoolean();
      return true;
    }

    private static bool TryFiniteSingle(JsonElement value, string name, out float result)
    {
      result = 0;
      return value.TryGetProperty(name, out var property) && TryFiniteSingle(property, out result);
    }

    private static bool TryFiniteSingle(JsonElement value, out float result)
    {
      result = 0;
      return value.ValueKind == JsonValueKind.Number
        && value.TryGetSingle(out result)
        && float.IsFinite(result);
    }

    private static bool TrySingleArray(
      JsonElement value,
      string name,
      int count,
      out float[] result)
    {
      result = Array.Empty<float>();
      if (!value.TryGetProperty(name, out var property)
        || property.ValueKind != JsonValueKind.Array
        || property.GetArrayLength() != count)
      {
        return false;
      }
      var values = new float[count];
      var index = 0;
      foreach (var element in property.EnumerateArray())
      {
        if (!TryFiniteSingle(element, out values[index]))
        {
          return false;
        }
        index++;
      }
      result = values;
      return true;
    }

    private static bool TryByteArray(
      JsonElement value,
      string name,
      int count,
      byte maximum,
      out byte[] result)
    {
      result = Array.Empty<byte>();
      if (!value.TryGetProperty(name, out var property)
        || property.ValueKind != JsonValueKind.Array
        || property.GetArrayLength() != count)
      {
        return false;
      }
      var values = new byte[count];
      var index = 0;
      foreach (var element in property.EnumerateArray())
      {
        if (!TryByte(element, out values[index]) || values[index] > maximum)
        {
          return false;
        }
        index++;
      }
      result = values;
      return true;
    }

    private static void WriteArray(
      Utf8JsonWriter writer,
      string name,
      IEnumerable<float> values)
    {
      writer.WriteStartArray(name);
      foreach (var value in values)
      {
        writer.WriteNumberValue(value);
      }
      writer.WriteEndArray();
    }

    private static void WriteArray(
      Utf8JsonWriter writer,
      string name,
      IEnumerable<byte> values)
    {
      writer.WriteStartArray(name);
      foreach (var value in values)
      {
        writer.WriteNumberValue(value);
      }
      writer.WriteEndArray();
    }

    private static void WriteRectangle(
      Utf8JsonWriter writer,
      string name,
      EffectRectangle rectangle)
    {
      writer.WriteStartObject(name);
      writer.WriteNumber("x0", rectangle.X0);
      writer.WriteNumber("y1", rectangle.Y1);
      writer.WriteNumber("x1", rectangle.X1);
      writer.WriteNumber("y0", rectangle.Y0);
      writer.WriteEndObject();
    }

    private static bool IsFinite(EffectRectangle value)
    {
      return float.IsFinite(value.X0)
        && float.IsFinite(value.Y1)
        && float.IsFinite(value.X1)
        && float.IsFinite(value.Y0);
    }

    private static bool IsFinite(Vector3 value)
    {
      return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }

    private static OperationDiagnostic Defaulted(string path, string message)
    {
      return new OperationDiagnostic(
        GltfAuthoringMetadataDiagnosticCodes.OptionalValueDefaulted,
        4000,
        DiagnosticSeverity.Warning,
        path,
        message);
    }

    private static OperationDiagnostic Required(string path, string message)
    {
      return new OperationDiagnostic(
        GltfAuthoringMetadataDiagnosticCodes.RequiredValueMissing,
        4002,
        DiagnosticSeverity.Error,
        path,
        message);
    }

    private static OperationDiagnostic MetadataLimit(string path)
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.MetadataResourceLimitExceeded,
        2005,
        DiagnosticSeverity.Error,
        path,
        "Canonical authoring metadata exceeds a finite operation limit.");
    }

    private static OperationResult<T> Failed<T>(OperationDiagnostic diagnostic)
      where T : class
    {
      return new OperationResult<T>(OperationStatus.Failed, diagnostics: new[] { diagnostic });
    }

    private sealed class WarningCollector
    {
      private readonly int _maximum;
      private readonly List<OperationDiagnostic> _diagnostics = new();
      private bool _truncated;

      internal IReadOnlyList<OperationDiagnostic> Diagnostics => _diagnostics.AsReadOnly();

      internal WarningCollector(int maximum)
      {
        _maximum = maximum;
      }

      internal void Add(OperationDiagnostic diagnostic)
      {
        if (_truncated)
        {
          return;
        }
        if (_diagnostics.Count < _maximum - 1)
        {
          _diagnostics.Add(diagnostic);
          return;
        }
        if (_diagnostics.Count < _maximum)
        {
          _diagnostics.Add(new OperationDiagnostic(
            GltfAuthoringMetadataDiagnosticCodes.DiagnosticsTruncated,
            4003,
            DiagnosticSeverity.Warning,
            "metadata",
            "Additional canonical authoring metadata warnings were truncated."));
        }
        _truncated = true;
      }
    }
  }
}
