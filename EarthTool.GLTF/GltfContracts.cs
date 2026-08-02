#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace EarthTool.GLTF
{
  internal static class GltfMetadataIdentity
  {
    internal static bool IsVersion4(Guid value)
    {
      var bytes = value.ToByteArray();
      return value != Guid.Empty && bytes[7] >> 4 == 4 && (bytes[8] & 0xC0) == 0x80;
    }
  }

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
    /// <summary>Source animation remains exact metadata because native TRS cannot represent it.</summary>
    public const string AnimationMetadataOnly = "ETG1014";
    /// <summary>An unrecognized serialized animation-class value uses modulo-four projection.</summary>
    public const string AnimationClassUnrecognized = "ETG1015";
    /// <summary>A non-finite cannon render-position component uses a finite native preview.</summary>
    public const string CannonRenderPositionPreviewSubstituted = "ETG1016";
    /// <summary>An anomalous static-light field uses a finite native preview.</summary>
    public const string StaticLightPreviewSubstituted = "ETG1017";
    /// <summary>An untagged noncanonical punctual light remains scene-only artist lighting.</summary>
    public const string SceneLightIgnored = "ETG1018";
    /// <summary>Required EarthTool manifest metadata is absent.</summary>
    public const string MissingManifest = "ETG2000";
    /// <summary>The edit-import scene contract is invalid.</summary>
    public const string InvalidSceneContract = "ETG2001";
    /// <summary>A reserved EarthTool metadata carrier is invalid.</summary>
    public const string InvalidMetadataCarrier = "ETG2002";
    /// <summary>EarthTool metadata is malformed.</summary>
    public const string MalformedMetadata = "ETG2003";
    /// <summary>EarthTool metadata version is unsupported.</summary>
    public const string UnsupportedMetadataVersion = "ETG2004";
    /// <summary>The metadata graph exceeds its finite operation profile.</summary>
    public const string MetadataResourceLimitExceeded = "ETG2005";
    /// <summary>Asset lineage differs from the expected baseline.</summary>
    public const string AssetLineageMismatch = "ETG2006";
    /// <summary>Document identity differs from the expected baseline.</summary>
    public const string DocumentMismatch = "ETG2007";
    /// <summary>An envelope kind does not match its glTF carrier.</summary>
    public const string KindCarrierMismatch = "ETG2008";
    /// <summary>More than one envelope claims the same scope identity.</summary>
    public const string DuplicateScopeIdentity = "ETG2009";
    /// <summary>Expected local metadata scope is absent.</summary>
    public const string MissingExpectedScope = "ETG2010";
    /// <summary>An envelope is not associated with a reachable native scope.</summary>
    public const string OrphanEnvelope = "ETG2011";
    /// <summary>Native geometry cannot be associated with one unique preserved partition set.</summary>
    public const string AmbiguousPartitionCorrespondence = "ETG2012";
    /// <summary>A metadata reference has no matching scope.</summary>
    public const string DanglingMetadataReference = "ETG2013";
    /// <summary>A required native projection guard is absent.</summary>
    public const string MissingRequiredGuard = "ETG2014";
    /// <summary>A guard projection or version is unsupported.</summary>
    public const string UnsupportedGuard = "ETG2015";
    /// <summary>Native projection no longer matches preservation metadata.</summary>
    public const string StaleNativeProjection = "ETG2016";
    /// <summary>Informational provenance does not match the claimed source.</summary>
    public const string ProvenanceMismatch = "ETG2017";
    /// <summary>The graph contains unknown semantics required for safe interpretation.</summary>
    public const string UnknownRequiredSemantics = "ETG2018";
    /// <summary>The bounded conflict inventory was truncated.</summary>
    public const string TooManyMetadataConflicts = "ETG2019";
    /// <summary>The manifest inventory or identity high-water marks are invalid.</summary>
    public const string InvalidManifestInventory = "ETG2020";
  }

  /// <summary>Defines the closed version-1 metadata conflict action identifiers.</summary>
  public static class GltfMetadataConflictActions
  {
    public const string Abort = "abort";
    public const string RetryWithMetadata = "retryWithMetadata";
    public const string AcceptBranch = "acceptBranch";
    public const string MapScope = "mapScope";
    public const string AcceptDeletion = "acceptDeletion";
    public const string AdoptAsNew = "adoptAsNew";
    public const string ForkScope = "forkScope";
    public const string DiscardAffectedState = "discardAffectedState";
    public const string RegenerateDerivedState = "regenerateDerivedState";
    public const string RepairNativeExternally = "repairNativeExternally";
    public const string DiscardLineage = "discardLineage";
  }

  /// <summary>Exposes the complete allowed-action set for each version-1 metadata conflict.</summary>
  public static class GltfMetadataConflictCatalog
  {
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ActionsByCode { get; } =
      new ReadOnlyDictionary<string, IReadOnlyList<string>>(
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
          [GltfDiagnosticCodes.MissingManifest] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage),
          [GltfDiagnosticCodes.InvalidSceneContract] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RepairNativeExternally),
          [GltfDiagnosticCodes.InvalidMetadataCarrier] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage),
          [GltfDiagnosticCodes.MalformedMetadata] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.DiscardLineage),
          [GltfDiagnosticCodes.UnsupportedMetadataVersion] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage),
          [GltfDiagnosticCodes.MetadataResourceLimitExceeded] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata),
          [GltfDiagnosticCodes.AssetLineageMismatch] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.AdoptAsNew,
            GltfMetadataConflictActions.DiscardLineage),
          [GltfDiagnosticCodes.DocumentMismatch] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.AcceptBranch),
          [GltfDiagnosticCodes.KindCarrierMismatch] = ScopeActions(),
          [GltfDiagnosticCodes.DuplicateScopeIdentity] = ScopeActions(),
          [GltfDiagnosticCodes.MissingExpectedScope] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.AcceptDeletion),
          [GltfDiagnosticCodes.OrphanEnvelope] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.ForkScope,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.DiscardLineage),
          [GltfDiagnosticCodes.AmbiguousPartitionCorrespondence] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.ForkScope,
            GltfMetadataConflictActions.RepairNativeExternally),
          [GltfDiagnosticCodes.DanglingMetadataReference] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.DiscardAffectedState),
          [GltfDiagnosticCodes.MissingRequiredGuard] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RegenerateDerivedState,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.RepairNativeExternally),
          [GltfDiagnosticCodes.UnsupportedGuard] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardAffectedState),
          [GltfDiagnosticCodes.StaleNativeProjection] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RegenerateDerivedState,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.RepairNativeExternally),
          [GltfDiagnosticCodes.ProvenanceMismatch] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.AcceptBranch,
            GltfMetadataConflictActions.DiscardAffectedState),
          [GltfDiagnosticCodes.UnknownRequiredSemantics] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.DiscardLineage),
          [GltfDiagnosticCodes.TooManyMetadataConflicts] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata),
          [GltfDiagnosticCodes.InvalidManifestInventory] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage)
        });

    private static IReadOnlyList<string> ScopeActions()
    {
      return Actions(
        GltfMetadataConflictActions.Abort,
        GltfMetadataConflictActions.MapScope,
        GltfMetadataConflictActions.ForkScope,
        GltfMetadataConflictActions.DiscardAffectedState);
    }

    private static IReadOnlyList<string> Actions(params string[] actions)
    {
      return Array.AsReadOnly(actions);
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

    /// <summary>Gets the maximum metadata envelope size in bytes.</summary>
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

    /// <summary>Gets the cumulative decoded metadata byte limit.</summary>
    public int MaxTotalMetadataBytes { get; }

    /// <summary>Gets the maximum number of metadata envelopes.</summary>
    public int MaxMetadataEnvelopes { get; }

    /// <summary>Gets the cumulative non-bulk metadata element limit.</summary>
    public int MaxMetadataElements { get; }

    /// <summary>Gets the cumulative unknown additive member limit.</summary>
    public int MaxUnknownMetadataMembers { get; }

    /// <summary>Gets the maximum guards accepted in one envelope.</summary>
    public int MaxMetadataGuards { get; }

    /// <summary>Initializes finite glTF operation limits.</summary>
    public GltfOperationProfile(
      int maxInputBytes = 32 * 1024 * 1024,
      int maxOutputBytes = 32 * 1024 * 1024,
      int maxMetadataBytes = 4 * 1024 * 1024,
      int maxJsonDepth = 32,
      int maxActiveRenderVertices = 65536)
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
        64)
    {
    }

    /// <summary>Initializes finite glTF operation limits including graph depth.</summary>
    public GltfOperationProfile(
      int maxInputBytes,
      int maxOutputBytes,
      int maxMetadataBytes,
      int maxJsonDepth,
      int maxActiveRenderVertices,
      int maxNodes,
      int maxHierarchyDepth)
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
        262144,
        64)
    {
    }

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
      int maxPreviewPixels)
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
        262144,
        64)
    {
    }

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
      int maxUnknownMetadataMembers = 262144,
      int maxMetadataGuards = 64)
    {
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
        nameof(maxTextureDirectoryEntries));
      MaxTotalMetadataBytes = RequirePositive(maxTotalMetadataBytes, nameof(maxTotalMetadataBytes));
      MaxMetadataEnvelopes = RequirePositive(maxMetadataEnvelopes, nameof(maxMetadataEnvelopes));
      MaxMetadataElements = RequirePositive(maxMetadataElements, nameof(maxMetadataElements));
      MaxUnknownMetadataMembers = RequirePositive(
        maxUnknownMetadataMembers,
        nameof(maxUnknownMetadataMembers));
      MaxMetadataGuards = RequirePositive(maxMetadataGuards, nameof(maxMetadataGuards));
    }

    private static int RequirePositive(int value, string parameterName)
    {
      return value > 0 ? value : throw new ArgumentOutOfRangeException(parameterName);
    }
  }

  /// <summary>Identifies one asset lineage and one emitted interchange baseline.</summary>
  public sealed class InterchangeBaseline
  {
    /// <summary>Gets the persistent asset-lineage identity.</summary>
    public Guid AssetLineageId { get; }

    /// <summary>Gets the identity of one emitted interchange document.</summary>
    public Guid DocumentId { get; }

    /// <summary>Initializes an interchange baseline.</summary>
    public InterchangeBaseline(Guid assetLineageId, Guid documentId)
    {
      AssetLineageId = GltfMetadataIdentity.IsVersion4(assetLineageId)
        ? assetLineageId
        : throw new ArgumentException("Asset lineage identity must be a version-4 UUID.", nameof(assetLineageId));
      DocumentId = GltfMetadataIdentity.IsVersion4(documentId)
        ? documentId
        : throw new ArgumentException("Document identity must be a version-4 UUID.", nameof(documentId));
    }

  }

  /// <summary>Describes a named and versioned SHA-256 native projection fingerprint.</summary>
  public sealed class NativeProjectionFingerprint
  {
    /// <summary>Gets the projection name.</summary>
    public string Name { get; }

    /// <summary>Gets the projection version.</summary>
    public int Version { get; }

    /// <summary>Gets the lowercase SHA-256 digest.</summary>
    public string Sha256 { get; }

    internal NativeProjectionFingerprint(string name, int version, string sha256)
    {
      Name = name;
      Version = version;
      Sha256 = sha256;
    }
  }

  /// <summary>Controls identities assigned to a GLB export.</summary>
  public sealed class GltfExportOptions
  {
    /// <summary>Gets an optional caller-supplied asset-lineage identity.</summary>
    public Guid? AssetLineageId { get; }

    /// <summary>Gets an optional caller-supplied document identity.</summary>
    public Guid? DocumentId { get; }

    /// <summary>Gets ordered absolute roots used only to resolve decoded TEX previews.</summary>
    public IReadOnlyList<string> TextureSearchRoots { get; }

    /// <summary>Gets exact unknown additive metadata tokens to carry into the next baseline.</summary>
    public IReadOnlyDictionary<string, string> PreservedUnknownMetadata { get; }

    internal IReadOnlyDictionary<string, int> MetadataNextIds { get; }

    /// <summary>Initializes GLB export options.</summary>
    public GltfExportOptions(
      Guid? assetLineageId = null,
      Guid? documentId = null,
      IEnumerable<string>? textureSearchRoots = null,
      IReadOnlyDictionary<string, string>? preservedUnknownMetadata = null)
    {
      if (assetLineageId.HasValue && !GltfMetadataIdentity.IsVersion4(assetLineageId.Value))
      {
        throw new ArgumentException("Asset lineage identity must be a version-4 UUID.", nameof(assetLineageId));
      }

      if (documentId.HasValue && !GltfMetadataIdentity.IsVersion4(documentId.Value))
      {
        throw new ArgumentException("Document identity must be a version-4 UUID.", nameof(documentId));
      }

      AssetLineageId = assetLineageId;
      DocumentId = documentId;
      var roots = (textureSearchRoots ?? Array.Empty<string>()).ToArray();
      if (roots.Any(root => string.IsNullOrWhiteSpace(root) || !System.IO.Path.IsPathRooted(root)))
      {
        throw new ArgumentException("TEX search roots must be absolute paths.", nameof(textureSearchRoots));
      }
      TextureSearchRoots = Array.AsReadOnly(roots.Select(System.IO.Path.GetFullPath).ToArray());
      var unknownMetadata = preservedUnknownMetadata?.ToDictionary(
        pair => pair.Key,
        pair => pair.Value,
        StringComparer.Ordinal) ?? new Dictionary<string, string>(StringComparer.Ordinal);
      foreach (var member in unknownMetadata)
      {
        ValidateUnknownMetadata(member.Key, member.Value, nameof(preservedUnknownMetadata));
      }
      PreservedUnknownMetadata = new ReadOnlyDictionary<string, string>(unknownMetadata);
      MetadataNextIds = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>());
    }

    internal GltfExportOptions(
      Guid assetLineageId,
      Guid documentId,
      IReadOnlyDictionary<string, string> preservedUnknownMetadata,
      IReadOnlyDictionary<string, int> metadataNextIds)
      : this(assetLineageId, documentId, preservedUnknownMetadata: preservedUnknownMetadata)
    {
      MetadataNextIds = new ReadOnlyDictionary<string, int>(
        metadataNextIds.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private static void ValidateUnknownMetadata(string key, string value, string parameterName)
    {
      var firstSeparator = key.IndexOf(':');
      var secondSeparator = firstSeparator < 0 ? -1 : key.IndexOf(':', firstSeparator + 1);
      if (firstSeparator <= 0 || secondSeparator <= firstSeparator + 1
        || !int.TryParse(
          key.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1),
          System.Globalization.NumberStyles.None,
          System.Globalization.CultureInfo.InvariantCulture,
          out var localId))
      {
        throw new ArgumentException("Unknown metadata keys must contain a scope kind, local ID, and JSON Pointer.",
          parameterName);
      }
      var scopeKind = key.Substring(0, firstSeparator);
      var localIdText = key.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1);
      var path = key.Substring(secondSeparator + 1);
      if (scopeKind is not ("manifest" or "object" or "mesh" or "material" or "light")
        || localIdText != localId.ToString(System.Globalization.CultureInfo.InvariantCulture)
        || (scopeKind == "manifest" ? localId != 0 : localId <= 0)
        || !Internal.GlbDocument.IsSupportedUnknownMetadataPath(scopeKind, path))
      {
        throw new ArgumentException("Unknown metadata key does not identify an additive version-1 member.",
          parameterName);
      }
      try
      {
        using var _ = JsonDocument.Parse(value, new JsonDocumentOptions
        {
          MaxDepth = int.MaxValue,
          CommentHandling = JsonCommentHandling.Disallow,
          AllowTrailingCommas = false
        });
      }
      catch (Exception ex) when (ex is JsonException || ex is ArgumentException)
      {
        throw new ArgumentException("Unknown metadata values must contain one valid JSON token.", parameterName, ex);
      }
    }

  }

  /// <summary>Supplies explicit game-authoritative TEX bindings for generic material indices.</summary>
  public sealed class GltfNewModelImportOptions
  {
    /// <summary>Gets material-index bindings; a null value explicitly clears the binding.</summary>
    public IReadOnlyDictionary<int, string?> TextureResourceBindings { get; }

    /// <summary>Initializes explicit generic material bindings.</summary>
    public GltfNewModelImportOptions(
      IReadOnlyDictionary<int, string?>? textureResourceBindings = null)
    {
      var bindings = textureResourceBindings?.ToDictionary(pair => pair.Key, pair => pair.Value)
        ?? new Dictionary<int, string?>();
      if (bindings.Keys.Any(index => index < 0))
      {
        throw new ArgumentOutOfRangeException(nameof(textureResourceBindings));
      }
      TextureResourceBindings = new ReadOnlyDictionary<int, string?>(bindings);
    }
  }

  /// <summary>Reports the baseline and native projection emitted by an export.</summary>
  public sealed class GltfExportReceipt
  {
    /// <summary>Gets the emitted interchange baseline.</summary>
    public InterchangeBaseline Baseline { get; }

    /// <summary>Gets the emitted native projection fingerprint.</summary>
    public NativeProjectionFingerprint Fingerprint { get; }

    internal GltfExportReceipt(InterchangeBaseline baseline, NativeProjectionFingerprint fingerprint)
    {
      Baseline = baseline;
      Fingerprint = fingerprint;
    }
  }

  /// <summary>Reports a successful reconciled edit import.</summary>
  public sealed class GltfEditImportResult
  {
    /// <summary>Gets the restored immutable static mesh asset.</summary>
    public StaticMeshAsset Asset { get; }

    /// <summary>Gets the retained lineage and rotated document baseline.</summary>
    public InterchangeBaseline NextBaseline { get; }

    /// <summary>Gets the fingerprint that proved metadata applicability.</summary>
    public NativeProjectionFingerprint AppliedFingerprint { get; }

    /// <summary>Gets the serialized representation paths restored from applicable metadata.</summary>
    public IReadOnlyList<string> RestoredSerializedRepresentationPaths { get; }

    /// <summary>Gets exact retained, regenerated, invalidated, and canonicalized MSH paths.</summary>
    public PreservationReport Preservation { get; }

    /// <summary>Gets exact raw JSON tokens for unknown additive version-1 members.</summary>
    public IReadOnlyDictionary<string, string> PreservedUnknownMetadata { get; }

    /// <summary>Gets export options that retain the next baseline and unknown additive tokens.</summary>
    public GltfExportOptions NextExportOptions { get; }

    internal GltfEditImportResult(
      StaticMeshAsset asset,
      InterchangeBaseline nextBaseline,
      NativeProjectionFingerprint appliedFingerprint,
      PreservationReport preservation,
      IEnumerable<string> restoredSerializedRepresentationPaths,
      IReadOnlyDictionary<string, string>? preservedUnknownMetadata = null,
      IReadOnlyDictionary<string, int>? metadataNextIds = null)
    {
      Asset = asset;
      NextBaseline = nextBaseline;
      AppliedFingerprint = appliedFingerprint;
      Preservation = preservation;
      RestoredSerializedRepresentationPaths = Array.AsReadOnly(
        new List<string>(restoredSerializedRepresentationPaths).ToArray());
      PreservedUnknownMetadata = new ReadOnlyDictionary<string, string>(
        preservedUnknownMetadata?.ToDictionary(pair => pair.Key, pair => pair.Value)
        ?? new Dictionary<string, string>());
      NextExportOptions = new GltfExportOptions(
        nextBaseline.AssetLineageId,
        nextBaseline.DocumentId,
        PreservedUnknownMetadata,
        metadataNextIds ?? new Dictionary<string, int>());
    }
  }

  /// <summary>Reports a successful new-model import and its first interchange baseline.</summary>
  public sealed class GltfNewModelImportResult
  {
    /// <summary>Gets the immutable canonical authored static mesh asset.</summary>
    public StaticMeshAsset Asset { get; }

    /// <summary>Gets the initial lineage and document identity for the authored asset.</summary>
    public InterchangeBaseline Baseline { get; }

    /// <summary>Gets the serialized representation paths canonicalized during authoring.</summary>
    public PreservationReport Preservation { get; }

    internal GltfNewModelImportResult(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      PreservationReport preservation)
    {
      Asset = asset;
      Baseline = baseline;
      Preservation = preservation;
    }
  }
}
