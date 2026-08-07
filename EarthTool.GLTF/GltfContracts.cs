#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.GLTF
{
  /// <summary>Defines stable diagnostics emitted by the glTF interchange facade.</summary>
  public static class GltfDiagnosticCodes
  {
    /// <summary>Invalid GLB structure or content.</summary>
    public const string InvalidGlb = "ETG1000";

    /// <summary>Configured resource limit exceeded.</summary>
    public const string ResourceLimitExceeded = "ETG1001";

    /// <summary>Domain assigned to a later implementation slice.</summary>
    public const string UnsupportedDomain = "ETG1002";

    /// <summary>SharpGLTF strict validation failure.</summary>
    public const string StrictValidationFailed = "ETG1003";

    /// <summary>glTF input or output failure.</summary>
    public const string IoFailure = "ETG1004";

    /// <summary>glTF operation cancellation.</summary>
    public const string Cancelled = "ETG1005";

    /// <summary>Static geometry cannot be represented safely.</summary>
    public const string InvalidGeometry = "ETG1006";

    /// <summary>An explicit TEX resource could not be resolved from the configured roots.</summary>
    public const string TextureResourceMissing = "ETG1007";

    /// <summary>TEX resource resolution was ambiguous in the winning root.</summary>
    public const string AmbiguousTextureResource = "ETG1008";

    /// <summary>An explicit TEX resource could not be decoded into a bounded preview.</summary>
    public const string TexturePreviewUnavailable = "ETG1009";

    /// <summary>A later TEX root contains a resource shadowed by the winning root.</summary>
    public const string TextureResourceShadowed = "ETG1010";

    /// <summary>A missing TEX resource uses the runtime default preview.</summary>
    public const string TextureDefaultPreviewUsed = "ETG1011";

    /// <summary>A missing TEX resource uses EarthTool's deterministic diagnostic preview.</summary>
    public const string TextureDiagnosticPreviewUsed = "ETG1012";

    /// <summary>A representative preview omits special TEX variant behavior.</summary>
    public const string TextureVariantsNotRepresented = "ETG1013";

    /// <summary>An unrecognized serialized animation-class value uses modulo-four projection.</summary>
    public const string AnimationClassUnrecognized = "ETG1015";

    /// <summary>A non-finite cannon render-position component uses a finite native preview.</summary>
    public const string CannonRenderPositionPreviewSubstituted = "ETG1016";

    /// <summary>An anomalous static-light field uses a finite native preview.</summary>
    public const string StaticLightPreviewSubstituted = "ETG1017";

    /// <summary>An untagged noncanonical punctual light remains scene-only artist lighting.</summary>
    public const string SceneLightIgnored = "ETG1018";

    /// <summary>Inert native glTF data was deliberately excluded from canonical MSH state.</summary>
    public const string InertDataIgnored = "ETG1019";

    /// <summary>An explicit MSH resource could not be resolved from the configured roots.</summary>
    public const string MeshResourceMissing = "ETG1020";

    /// <summary>MSH resource resolution was ambiguous in the winning root.</summary>
    public const string AmbiguousMeshResource = "ETG1021";

    /// <summary>A referenced MSH resource could not be decoded into a bounded preview.</summary>
    public const string MeshPreviewUnavailable = "ETG1022";

    /// <summary>A later MSH root contains a resource shadowed by the winning root.</summary>
    public const string MeshResourceShadowed = "ETG1023";

    /// <summary>An unresolved MSH resource uses EarthTool's deterministic diagnostic preview.</summary>
    public const string MeshDiagnosticPreviewUsed = "ETG1024";

    /// <summary>A referenced MSH is valid but has an unsupported dynamic payload.</summary>
    public const string UnsupportedMeshResource = "ETG1025";

    /// <summary>A referenced dynamic MSH resource chain contains a cycle.</summary>
    public const string MeshResourceCycle = "ETG1026";

    /// <summary>An emitter helper cannot be nested under its marker-attachment source object.</summary>
    public const string EmitterHierarchyFallback = "ETG1027";

    /// <summary>New-model photometric intensity was not used as terrain-light amplitude.</summary>
    public const string NewModelPhotometricIntensityIgnored = "ETG1028";

    /// <summary>A textured new-model material requires a typed canonical TEX resource binding.</summary>
    public const string TextureResourceBindingRequired = "ETG1029";

    /// <summary>An accepted MSH serialized representation is not retained for later canonical creation.</summary>
    public const string SourceRepresentationNotPreserved = "ETG1030";

    /// <summary>Canonical authoring metadata exceeds its finite operation profile.</summary>
    public const string MetadataResourceLimitExceeded = "ETG2005";

    /// <summary>The import plan is malformed or contains forbidden state.</summary>
    public const string MalformedImportPlan = "ETG3000";

    /// <summary>The import-plan protocol version is unsupported.</summary>
    public const string UnsupportedImportPlanVersion = "ETG3001";

    /// <summary>The import plan exceeds its finite operation profile.</summary>
    public const string ImportPlanResourceLimitExceeded = "ETG3002";

    /// <summary>The plan does not match the selected import or source package.</summary>
    public const string ImportPlanMismatch = "ETG3004";

    /// <summary>The import plan contains an input removed from the current protocol.</summary>
    public const string RemovedImportPlanMember = "ETG3005";

    /// <summary>An optional typed authoring value used its canonical default.</summary>
    public const string AuthoringValueDefaulted = "ETG4000";

    /// <summary>A canonical authoring identifier is declared more than once.</summary>
    public const string DuplicateAuthoringOwner = "ETG4001";

    /// <summary>A required typed authoring value has no safe canonical default.</summary>
    public const string RequiredAuthoringValueMissing = "ETG4002";

    /// <summary>The bounded typed-authoring warning inventory was truncated.</summary>
    public const string AuthoringDiagnosticsTruncated = "ETG4003";
  }

  /// <summary>Defines finite limits for referenced MSH preview lookup.</summary>
  public sealed class GltfMeshResourceLimits
  {
    /// <summary>Gets the default finite referenced MSH limits.</summary>
    public static GltfMeshResourceLimits Default { get; } = new GltfMeshResourceLimits();

    /// <summary>Gets the maximum aggregate referenced MSH bytes.</summary>
    public int MaxResourceBytes { get; }

    /// <summary>Gets the maximum ordered search roots.</summary>
    public int MaxSearchRoots { get; }

    /// <summary>Gets the maximum examined directory entries.</summary>
    public int MaxDirectoryEntries { get; }

    /// <summary>Gets the maximum resolved resources.</summary>
    public int MaxResources { get; }

    /// <summary>Gets the maximum aggregate emitted preview vertices.</summary>
    public int MaxPreviewVertices { get; }

    /// <summary>Gets the maximum dynamic resource-chain traversal depth.</summary>
    public int MaxDepth { get; }

    /// <summary>Initializes finite referenced MSH preview limits.</summary>
    public GltfMeshResourceLimits(
      int maxResourceBytes = 16 * 1024 * 1024,
      int maxSearchRoots = 64,
      int maxDirectoryEntries = 65536,
      int maxResources = 256,
      int maxPreviewVertices = 262144,
      int maxDepth = 8
    )
    {
      MaxResourceBytes = RequirePositive(maxResourceBytes, nameof(maxResourceBytes));
      MaxSearchRoots = RequirePositive(maxSearchRoots, nameof(maxSearchRoots));
      MaxDirectoryEntries = RequirePositive(maxDirectoryEntries, nameof(maxDirectoryEntries));
      MaxResources = RequirePositive(maxResources, nameof(maxResources));
      MaxPreviewVertices = RequirePositive(maxPreviewVertices, nameof(maxPreviewVertices));
      MaxDepth = RequirePositive(maxDepth, nameof(maxDepth));
    }

    private static int RequirePositive(int value, string parameterName)
    {
      return value > 0 ? value : throw new ArgumentOutOfRangeException(parameterName);
    }
  }

  /// <summary>Defines finite resource limits for one glTF operation.</summary>
  public sealed class GltfOperationProfile
  {
    /// <summary>Gets the default finite operation profile.</summary>
    public static GltfOperationProfile Default { get; } = new GltfOperationProfile();

    /// <summary>Gets the maximum accepted input size in bytes.</summary>
    public int MaxInputBytes { get; }

    /// <summary>Gets the maximum emitted output size in bytes.</summary>
    public int MaxOutputBytes { get; }

    /// <summary>Gets the maximum size of one canonical authoring envelope in bytes.</summary>
    public int MaxMetadataBytes { get; }

    /// <summary>Gets the maximum accepted JSON depth.</summary>
    public int MaxJsonDepth { get; }

    /// <summary>Gets the maximum active render vertices accepted in one partition.</summary>
    public int MaxActiveRenderVertices { get; }

    /// <summary>Gets the maximum number of nodes accepted in the glTF graph.</summary>
    public int MaxNodes { get; }

    /// <summary>Gets the maximum node hierarchy depth, including the scene root.</summary>
    public int MaxHierarchyDepth { get; }

    /// <summary>Gets the maximum aggregate TEX resource bytes read.</summary>
    public int MaxTextureBytes { get; }

    /// <summary>Gets the maximum aggregate decoded preview pixels.</summary>
    public int MaxPreviewPixels { get; }

    /// <summary>Gets the maximum ordered TEX search roots.</summary>
    public int MaxTextureSearchRoots { get; }

    /// <summary>Gets the maximum directory entries examined during TEX lookup.</summary>
    public int MaxTextureDirectoryEntries { get; }

    /// <summary>Gets the maximum aggregate referenced MSH resource bytes read.</summary>
    public int MaxMeshResourceBytes { get; }

    /// <summary>Gets the maximum ordered MSH resource search roots.</summary>
    public int MaxMeshResourceSearchRoots { get; }

    /// <summary>Gets the maximum directory entries examined during MSH lookup.</summary>
    public int MaxMeshResourceDirectoryEntries { get; }

    /// <summary>Gets the maximum referenced MSH resources resolved by one operation.</summary>
    public int MaxMeshResources { get; }

    /// <summary>Gets the maximum aggregate emitted referenced-MSH preview vertices.</summary>
    public int MaxMeshPreviewVertices { get; }

    /// <summary>Gets the maximum referenced dynamic MSH traversal depth.</summary>
    public int MaxMeshResourceDepth { get; }

    /// <summary>Gets the cumulative canonical authoring envelope byte limit.</summary>
    public int MaxTotalMetadataBytes { get; }

    /// <summary>Gets the maximum number of canonical authoring envelopes.</summary>
    public int MaxMetadataEnvelopes { get; }

    /// <summary>Gets the cumulative canonical authoring envelope element limit.</summary>
    public int MaxMetadataElements { get; }

    /// <summary>Gets the cumulative unsupported canonical authoring member limit.</summary>
    public int MaxUnknownMetadataMembers { get; }

    internal int MaxAuthoringDiagnostics { get; }

    /// <summary>Initializes finite glTF operation limits.</summary>
    public GltfOperationProfile(
      int maxInputBytes = 32 * 1024 * 1024,
      int maxOutputBytes = 32 * 1024 * 1024,
      int maxMetadataBytes = 4 * 1024 * 1024,
      int maxJsonDepth = 32,
      int maxActiveRenderVertices = 65536
    )
      : this(
        GltfMeshResourceLimits.Default,
        maxInputBytes,
        maxOutputBytes,
        maxMetadataBytes,
        maxJsonDepth,
        maxActiveRenderVertices
      )
    { }

    /// <summary>Initializes finite glTF operation limits including referenced MSH previews.</summary>
    public GltfOperationProfile(
      GltfMeshResourceLimits meshResourceLimits,
      int maxInputBytes = 32 * 1024 * 1024,
      int maxOutputBytes = 32 * 1024 * 1024,
      int maxMetadataBytes = 4 * 1024 * 1024,
      int maxJsonDepth = 32,
      int maxActiveRenderVertices = 65536
    )
      : this(
        maxInputBytes,
        maxOutputBytes,
        maxMetadataBytes,
        maxJsonDepth,
        maxActiveRenderVertices,
        4096,
        15,
        16 * 1024 * 1024,
        16 * 1024 * 1024,
        64,
        65536,
        32 * 1024 * 1024,
        262144,
        4194304,
        262144,
        1024,
        meshResourceLimits
      )
    { }

    /// <summary>Initializes finite glTF operation limits including graph depth.</summary>
    public GltfOperationProfile(
      int maxInputBytes,
      int maxOutputBytes,
      int maxMetadataBytes,
      int maxJsonDepth,
      int maxActiveRenderVertices,
      int maxNodes,
      int maxHierarchyDepth
    )
      : this(
        maxInputBytes,
        maxOutputBytes,
        maxMetadataBytes,
        maxJsonDepth,
        maxActiveRenderVertices,
        maxNodes,
        maxHierarchyDepth,
        16 * 1024 * 1024,
        16 * 1024 * 1024,
        64,
        65536,
        32 * 1024 * 1024,
        262144,
        4194304,
        262144
      )
    { }

    /// <summary>Initializes all finite glTF operation limits including TEX previews.</summary>
    public GltfOperationProfile(
      int maxInputBytes,
      int maxOutputBytes,
      int maxMetadataBytes,
      int maxJsonDepth,
      int maxActiveRenderVertices,
      int maxNodes,
      int maxHierarchyDepth,
      int maxTextureBytes,
      int maxPreviewPixels
    )
      : this(
        maxInputBytes,
        maxOutputBytes,
        maxMetadataBytes,
        maxJsonDepth,
        maxActiveRenderVertices,
        maxNodes,
        maxHierarchyDepth,
        maxTextureBytes,
        maxPreviewPixels,
        64,
        65536,
        32 * 1024 * 1024,
        262144,
        4194304,
        262144
      )
    { }

    /// <summary>Initializes all finite glTF operation and TEX lookup limits.</summary>
    public GltfOperationProfile(
      int maxInputBytes,
      int maxOutputBytes,
      int maxMetadataBytes,
      int maxJsonDepth,
      int maxActiveRenderVertices,
      int maxNodes,
      int maxHierarchyDepth,
      int maxTextureBytes,
      int maxPreviewPixels,
      int maxTextureSearchRoots,
      int maxTextureDirectoryEntries,
      int maxTotalMetadataBytes = 32 * 1024 * 1024,
      int maxMetadataEnvelopes = 262144,
      int maxMetadataElements = 4194304,
      int maxUnknownMetadataMembers = 262144
    )
      : this(
        maxInputBytes,
        maxOutputBytes,
        maxMetadataBytes,
        maxJsonDepth,
        maxActiveRenderVertices,
        maxNodes,
        maxHierarchyDepth,
        maxTextureBytes,
        maxPreviewPixels,
        maxTextureSearchRoots,
        maxTextureDirectoryEntries,
        maxTotalMetadataBytes,
        maxMetadataEnvelopes,
        maxMetadataElements,
        maxUnknownMetadataMembers,
        1024,
        GltfMeshResourceLimits.Default
      )
    { }

    /// <summary>Initializes every finite glTF operation and resource lookup limit.</summary>
    internal GltfOperationProfile(
      int maxInputBytes,
      int maxOutputBytes,
      int maxMetadataBytes,
      int maxJsonDepth,
      int maxActiveRenderVertices,
      int maxNodes,
      int maxHierarchyDepth,
      int maxTextureBytes,
      int maxPreviewPixels,
      int maxTextureSearchRoots,
      int maxTextureDirectoryEntries,
      int maxTotalMetadataBytes,
      int maxMetadataEnvelopes,
      int maxMetadataElements,
      int maxUnknownMetadataMembers,
      int maxAuthoringDiagnostics,
      GltfMeshResourceLimits meshResourceLimits
    )
    {
      if (meshResourceLimits is null)
      {
        throw new ArgumentNullException(nameof(meshResourceLimits));
      }
      MaxInputBytes = RequirePositive(maxInputBytes, nameof(maxInputBytes));
      MaxOutputBytes = RequirePositive(maxOutputBytes, nameof(maxOutputBytes));
      MaxMetadataBytes = RequirePositive(maxMetadataBytes, nameof(maxMetadataBytes));
      MaxJsonDepth = RequirePositive(maxJsonDepth, nameof(maxJsonDepth));
      MaxActiveRenderVertices = maxActiveRenderVertices is > 0 and <= 65536
        ? maxActiveRenderVertices
        : throw new ArgumentOutOfRangeException(nameof(maxActiveRenderVertices));
      MaxNodes = RequirePositive(maxNodes, nameof(maxNodes));
      MaxHierarchyDepth = RequirePositive(maxHierarchyDepth, nameof(maxHierarchyDepth));
      MaxTextureBytes = RequirePositive(maxTextureBytes, nameof(maxTextureBytes));
      MaxPreviewPixels = RequirePositive(maxPreviewPixels, nameof(maxPreviewPixels));
      MaxTextureSearchRoots = RequirePositive(maxTextureSearchRoots, nameof(maxTextureSearchRoots));
      MaxTextureDirectoryEntries = RequirePositive(
        maxTextureDirectoryEntries,
        nameof(maxTextureDirectoryEntries)
      );
      MaxMeshResourceBytes = meshResourceLimits.MaxResourceBytes;
      MaxMeshResourceSearchRoots = meshResourceLimits.MaxSearchRoots;
      MaxMeshResourceDirectoryEntries = meshResourceLimits.MaxDirectoryEntries;
      MaxMeshResources = meshResourceLimits.MaxResources;
      MaxMeshPreviewVertices = meshResourceLimits.MaxPreviewVertices;
      MaxMeshResourceDepth = meshResourceLimits.MaxDepth;
      MaxTotalMetadataBytes = RequirePositive(maxTotalMetadataBytes, nameof(maxTotalMetadataBytes));
      MaxMetadataEnvelopes = RequirePositive(maxMetadataEnvelopes, nameof(maxMetadataEnvelopes));
      MaxMetadataElements = RequirePositive(maxMetadataElements, nameof(maxMetadataElements));
      MaxUnknownMetadataMembers = RequirePositive(
        maxUnknownMetadataMembers,
        nameof(maxUnknownMetadataMembers)
      );
      MaxAuthoringDiagnostics = RequirePositive(
        maxAuthoringDiagnostics,
        nameof(maxAuthoringDiagnostics)
      );
    }

    private static int RequirePositive(int value, string parameterName)
    {
      return value > 0 ? value : throw new ArgumentOutOfRangeException(parameterName);
    }
  }

  /// <summary>Controls resource preview lookup and artist-facing naming for glTF export.</summary>
  public sealed class GltfExportOptions
  {
    /// <summary>Gets ordered absolute roots used only to resolve decoded TEX previews.</summary>
    public IReadOnlyList<string> TextureSearchRoots { get; }

    /// <summary>Gets ordered absolute roots used only to resolve referenced MSH previews.</summary>
    public IReadOnlyList<string> MeshResourceSearchRoots { get; }

    /// <summary>Gets the optional source-file basename used for artist-facing object names.</summary>
    public string? SourceBaseName { get; }

    /// <summary>Initializes export options for previews and artist-facing source naming.</summary>
    public GltfExportOptions(
      IEnumerable<string>? textureSearchRoots = null,
      IEnumerable<string>? meshResourceSearchRoots = null,
      string? sourceBaseName = null
    )
    {
      var roots = (textureSearchRoots ?? Array.Empty<string>()).ToArray();
      if (
        roots.Any(root =>
          string.IsNullOrWhiteSpace(root) || !System.IO.Path.IsPathFullyQualified(root)
        )
      )
      {
        throw new ArgumentException(
          "TEX search roots must be absolute paths.",
          nameof(textureSearchRoots)
        );
      }
      TextureSearchRoots = Array.AsReadOnly(roots.Select(System.IO.Path.GetFullPath).ToArray());
      var meshRoots = (meshResourceSearchRoots ?? Array.Empty<string>()).ToArray();
      if (
        meshRoots.Any(root =>
          string.IsNullOrWhiteSpace(root) || !System.IO.Path.IsPathFullyQualified(root)
        )
      )
      {
        throw new ArgumentException(
          "MSH resource search roots must be absolute paths.",
          nameof(meshResourceSearchRoots)
        );
      }
      MeshResourceSearchRoots = Array.AsReadOnly(
        meshRoots.Select(System.IO.Path.GetFullPath).ToArray()
      );
      if (sourceBaseName is not null && string.IsNullOrWhiteSpace(sourceBaseName))
      {
        throw new ArgumentException(
          "Source basename must contain a visible character.",
          nameof(sourceBaseName)
        );
      }
      SourceBaseName = sourceBaseName;
    }
  }

  /// <summary>Identifies one source node by its one-based document-local authoring order.</summary>
  public readonly struct GltfNodeHandle : IEquatable<GltfNodeHandle>
  {
    /// <summary>Gets the one-based document-local value.</summary>
    public int Value { get; }

    /// <summary>Initializes a document-local node handle.</summary>
    public GltfNodeHandle(int value)
    {
      if (value <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(value));
      }
      Value = value;
    }

    /// <inheritdoc />
    public bool Equals(GltfNodeHandle other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GltfNodeHandle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value;
  }

  /// <summary>Identifies one source material by its one-based document-local authoring order.</summary>
  public readonly struct GltfMaterialHandle : IEquatable<GltfMaterialHandle>
  {
    /// <summary>Gets the one-based document-local value.</summary>
    public int Value { get; }

    /// <summary>Initializes a document-local material handle.</summary>
    public GltfMaterialHandle(int value)
    {
      if (value <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(value));
      }
      Value = value;
    }

    /// <inheritdoc />
    public bool Equals(GltfMaterialHandle other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GltfMaterialHandle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value;
  }

  /// <summary>Identifies one punctual-light definition by one-based document-local order.</summary>
  public readonly struct GltfLightHandle : IEquatable<GltfLightHandle>
  {
    /// <summary>Gets the one-based document-local value.</summary>
    public int Value { get; }

    /// <summary>Initializes a document-local light handle.</summary>
    public GltfLightHandle(int value)
    {
      if (value <= 0)
      {
        throw new ArgumentOutOfRangeException(nameof(value));
      }
      Value = value;
    }

    /// <inheritdoc />
    public bool Equals(GltfLightHandle other) => Value == other.Value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is GltfLightHandle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value;
  }

  /// <summary>Supplies MSH-only semantic values for one generic punctual light.</summary>
  public sealed class GltfNewModelStaticLightOptions
  {
    /// <summary>Gets the optional positive spot target distance.</summary>
    public float? TargetDistance { get; }

    /// <summary>Gets the optional terrain-light amplitude.</summary>
    public float? TerrainLightAmplitude { get; }

    /// <summary>Initializes supported semantic light values.</summary>
    public GltfNewModelStaticLightOptions(
      float? targetDistance = null,
      float? terrainLightAmplitude = null
    )
    {
      if (
        targetDistance.HasValue
        && (!float.IsFinite(targetDistance.Value) || targetDistance.Value <= 0)
      )
      {
        throw new ArgumentOutOfRangeException(nameof(targetDistance));
      }
      if (
        terrainLightAmplitude.HasValue
        && (!float.IsFinite(terrainLightAmplitude.Value) || terrainLightAmplitude.Value < 0)
      )
      {
        throw new ArgumentOutOfRangeException(nameof(terrainLightAmplitude));
      }
      TargetDistance = targetDistance;
      TerrainLightAmplitude = terrainLightAmplitude;
    }
  }

  /// <summary>Names supported semantic roles for a newly authored source object.</summary>
  [Flags]
  public enum GltfStaticObjectRoles
  {
    /// <summary>No recognized role.</summary>
    None = 0,

    /// <summary>Faces the active viewer.</summary>
    ViewerFaced = 1,

    /// <summary>Uses the barrel transform role.</summary>
    Barrel = 2,

    /// <summary>Uses the rotor transform role.</summary>
    Rotor = 4,
  }

  /// <summary>Supplies supported semantic role values for one source object.</summary>
  public sealed class GltfNewModelObjectRole
  {
    /// <summary>Gets the supported role set.</summary>
    public GltfStaticObjectRoles Roles { get; }

    /// <summary>Gets the barrel maximum raise-angle byte.</summary>
    public byte BarrelMaximumAngle { get; }

    /// <summary>Initializes a supported semantic role override.</summary>
    public GltfNewModelObjectRole(GltfStaticObjectRoles roles, byte barrelMaximumAngle = 0)
    {
      var allowed =
        GltfStaticObjectRoles.ViewerFaced
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
      Roles = roles;
      BarrelMaximumAngle = barrelMaximumAngle;
    }

    internal CanonicalStaticObjectRole ToCanonical()
    {
      var flags = StaticRenderObjectFlags.None;
      if ((Roles & GltfStaticObjectRoles.ViewerFaced) != 0)
        flags |= StaticRenderObjectFlags.ViewerFaced;
      if ((Roles & GltfStaticObjectRoles.Barrel) != 0)
        flags |= StaticRenderObjectFlags.Barrel;
      if ((Roles & GltfStaticObjectRoles.Rotor) != 0)
        flags |= StaticRenderObjectFlags.Rotor;
      return new CanonicalStaticObjectRole(flags, BarrelMaximumAngle);
    }
  }

  /// <summary>Supplies one explicit semantic 4x4 footprint.</summary>
  public sealed class GltfNewModelFootprint
  {
    /// <summary>Gets the low-16 logical occupied-cell mask.</summary>
    public ushort PresenceMask { get; }

    /// <summary>Gets 16 logical unsigned top elevations.</summary>
    public IReadOnlyList<float> TopElevations { get; }

    /// <summary>Gets 16 logical four-bit corner-passage values.</summary>
    public IReadOnlyList<byte> CornerPassageFlags { get; }

    /// <summary>Initializes an explicit semantic footprint.</summary>
    public GltfNewModelFootprint(
      ushort presenceMask,
      IEnumerable<float> topElevations,
      IEnumerable<byte> cornerPassageFlags
    )
    {
      var canonical = new CanonicalStaticFootprint(presenceMask, topElevations, cornerPassageFlags);
      PresenceMask = canonical.PresenceMask;
      TopElevations = canonical.TopElevations;
      CornerPassageFlags = canonical.CornerPassageFlags;
    }

    internal CanonicalStaticFootprint ToCanonical() =>
      new(PresenceMask, TopElevations, CornerPassageFlags);
  }

  /// <summary>Supplies explicit semantic horizontal extent magnitudes.</summary>
  public sealed class GltfNewModelHorizontalExtents
  {
    /// <summary>Gets the positive-Y extent.</summary>
    public float PositiveY { get; }

    /// <summary>Gets the negative-Y extent magnitude.</summary>
    public float NegativeY { get; }

    /// <summary>Gets the positive-X extent.</summary>
    public float PositiveX { get; }

    /// <summary>Gets the negative-X extent magnitude.</summary>
    public float NegativeX { get; }

    /// <summary>Initializes explicit semantic horizontal extents.</summary>
    public GltfNewModelHorizontalExtents(
      float positiveY,
      float negativeY,
      float positiveX,
      float negativeX
    )
    {
      var canonical = new CanonicalHorizontalExtents(positiveY, negativeY, positiveX, negativeX);
      PositiveY = canonical.PositiveY;
      NegativeY = canonical.NegativeY;
      PositiveX = canonical.PositiveX;
      NegativeX = canonical.NegativeX;
    }

    internal CanonicalHorizontalExtents ToCanonical() =>
      new(PositiveY, NegativeY, PositiveX, NegativeX);
  }

  /// <summary>Supplies closed semantic overrides for generic new-model authoring.</summary>
  public sealed class GltfNewModelImportOptions
  {
    /// <summary>Gets material-handle bindings; null clears an untextured material binding.</summary>
    public IReadOnlyDictionary<GltfMaterialHandle, string?> TextureResourceBindings { get; }

    /// <summary>Gets dynamic mesh-resource bindings keyed by owning node handle.</summary>
    public IReadOnlyDictionary<GltfNodeHandle, string> MeshResourceBindings { get; }

    /// <summary>Gets the optional explicit footprint.</summary>
    public GltfNewModelFootprint? Footprint { get; }

    /// <summary>Gets the optional explicit horizontal extents.</summary>
    public GltfNewModelHorizontalExtents? HorizontalExtents { get; }

    /// <summary>Gets supported role overrides keyed by node handle.</summary>
    public IReadOnlyDictionary<GltfNodeHandle, GltfNewModelObjectRole> ObjectRoles { get; }

    /// <summary>Gets MSH-only semantic light values keyed by light handle.</summary>
    public IReadOnlyDictionary<
      GltfLightHandle,
      GltfNewModelStaticLightOptions
    > StaticLightOptions
    { get; }

    /// <summary>Initializes closed semantic new-model overrides.</summary>
    public GltfNewModelImportOptions(
      IReadOnlyDictionary<GltfMaterialHandle, string?>? textureResourceBindings = null,
      GltfNewModelFootprint? footprint = null,
      GltfNewModelHorizontalExtents? horizontalExtents = null,
      IReadOnlyDictionary<GltfNodeHandle, GltfNewModelObjectRole>? objectRoles = null,
      IReadOnlyDictionary<GltfLightHandle, GltfNewModelStaticLightOptions>? staticLightOptions =
        null,
      IReadOnlyDictionary<GltfNodeHandle, string>? meshResourceBindings = null
    )
    {
      var bindings =
        textureResourceBindings?.ToDictionary(pair => pair.Key, pair => pair.Value)
        ?? new Dictionary<GltfMaterialHandle, string?>();
      var roles =
        objectRoles?.ToDictionary(pair => pair.Key, pair => pair.Value)
        ?? new Dictionary<GltfNodeHandle, GltfNewModelObjectRole>();
      var lights =
        staticLightOptions?.ToDictionary(pair => pair.Key, pair => pair.Value)
        ?? new Dictionary<GltfLightHandle, GltfNewModelStaticLightOptions>();
      var meshes =
        meshResourceBindings?.ToDictionary(pair => pair.Key, pair => pair.Value)
        ?? new Dictionary<GltfNodeHandle, string>();
      if (
        bindings.Keys.Any(handle => handle.Value <= 0)
        || roles.Keys.Any(handle => handle.Value <= 0)
        || lights.Keys.Any(handle => handle.Value <= 0)
        || meshes.Keys.Any(handle => handle.Value <= 0)
      )
      {
        throw new ArgumentOutOfRangeException(
          nameof(textureResourceBindings),
          "A document-local handle must be positive."
        );
      }
      if (
        roles.Values.Any(role => role is null)
        || lights.Values.Any(light => light is null)
        || meshes.Values.Any(mesh => mesh is null)
      )
      {
        throw new ArgumentException("New-model semantic overrides cannot contain null values.");
      }
      TextureResourceBindings = new ReadOnlyDictionary<GltfMaterialHandle, string?>(bindings);
      MeshResourceBindings = new ReadOnlyDictionary<GltfNodeHandle, string>(meshes);
      Footprint = footprint;
      HorizontalExtents = horizontalExtents;
      ObjectRoles = new ReadOnlyDictionary<GltfNodeHandle, GltfNewModelObjectRole>(roles);
      StaticLightOptions = new ReadOnlyDictionary<GltfLightHandle, GltfNewModelStaticLightOptions>(
        lights
      );
    }
  }

}
