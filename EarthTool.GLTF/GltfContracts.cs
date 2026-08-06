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

    /// <summary>The import plan is malformed or contains forbidden state.</summary>
    public const string MalformedImportPlan = "ETG3000";

    /// <summary>The import-plan protocol version is unsupported.</summary>
    public const string UnsupportedImportPlanVersion = "ETG3001";

    /// <summary>The import plan exceeds its finite operation profile.</summary>
    public const string ImportPlanResourceLimitExceeded = "ETG3002";

    /// <summary>A conflict action no longer matches the current conflict inventory.</summary>
    public const string StaleImportPlan = "ETG3003";

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

  /// <summary>Defines the closed version-1 metadata conflict action identifiers.</summary>
  internal static class GltfMetadataConflictActions
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
  internal static class GltfMetadataConflictCatalog
  {
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ActionsByCode { get; } =
      new ReadOnlyDictionary<string, IReadOnlyList<string>>(
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
          [GltfDiagnosticCodes.MissingManifest] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage
          ),
          [GltfDiagnosticCodes.InvalidSceneContract] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RepairNativeExternally
          ),
          [GltfDiagnosticCodes.InvalidMetadataCarrier] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage
          ),
          [GltfDiagnosticCodes.MalformedMetadata] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.DiscardLineage
          ),
          [GltfDiagnosticCodes.UnsupportedMetadataVersion] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage
          ),
          [GltfDiagnosticCodes.MetadataResourceLimitExceeded] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata
          ),
          [GltfDiagnosticCodes.AssetLineageMismatch] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.AdoptAsNew,
            GltfMetadataConflictActions.DiscardLineage
          ),
          [GltfDiagnosticCodes.DocumentMismatch] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.AcceptBranch
          ),
          [GltfDiagnosticCodes.KindCarrierMismatch] = ScopeActions(),
          [GltfDiagnosticCodes.DuplicateScopeIdentity] = ScopeActions(),
          [GltfDiagnosticCodes.MissingExpectedScope] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.AcceptDeletion
          ),
          [GltfDiagnosticCodes.OrphanEnvelope] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.ForkScope,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.DiscardLineage
          ),
          [GltfDiagnosticCodes.AmbiguousPartitionCorrespondence] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.ForkScope,
            GltfMetadataConflictActions.RepairNativeExternally
          ),
          [GltfDiagnosticCodes.DanglingMetadataReference] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.DiscardAffectedState
          ),
          [GltfDiagnosticCodes.MissingRequiredGuard] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RegenerateDerivedState,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.RepairNativeExternally
          ),
          [GltfDiagnosticCodes.UnsupportedGuard] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardAffectedState
          ),
          [GltfDiagnosticCodes.StaleNativeProjection] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RegenerateDerivedState,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.RepairNativeExternally
          ),
          [GltfDiagnosticCodes.ProvenanceMismatch] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.AcceptBranch,
            GltfMetadataConflictActions.DiscardAffectedState
          ),
          [GltfDiagnosticCodes.UnknownRequiredSemantics] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardAffectedState,
            GltfMetadataConflictActions.DiscardLineage
          ),
          [GltfDiagnosticCodes.TooManyMetadataConflicts] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata
          ),
          [GltfDiagnosticCodes.InvalidManifestInventory] = Actions(
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage
          ),
        }
      );

    private static IReadOnlyList<string> ScopeActions()
    {
      return Actions(
        GltfMetadataConflictActions.Abort,
        GltfMetadataConflictActions.MapScope,
        GltfMetadataConflictActions.ForkScope,
        GltfMetadataConflictActions.DiscardAffectedState
      );
    }

    private static IReadOnlyList<string> Actions(params string[] actions)
    {
      return Array.AsReadOnly(actions);
    }
  }

  /// <summary>Identifies how a successful edit import treated its metadata lineage.</summary>
  internal enum GltfMetadataLineageDisposition
  {
    /// <summary>The expected lineage and document branch were retained.</summary>
    Retained,

    /// <summary>A different document branch of the expected lineage was explicitly accepted.</summary>
    BranchAccepted,

    /// <summary>Native content was adopted into a new lineage.</summary>
    AdoptedAsNew,

    /// <summary>The claimed metadata lineage was discarded before native content was imported.</summary>
    Discarded,
  }

  /// <summary>Selects one allowed action for one exact metadata conflict.</summary>
  internal sealed class GltfMetadataConflictResolution
  {
    /// <summary>Gets the deterministic key of the exact conflict being resolved.</summary>
    public string ConflictKey { get; }

    /// <summary>Gets the selected closed version-1 action identifier.</summary>
    public string Action { get; }

    /// <summary>Gets the optional native carrier path used by scope mapping.</summary>
    public string? TargetNativePath { get; }

    /// <summary>Initializes one exact conflict resolution.</summary>
    public GltfMetadataConflictResolution(
      string conflictKey,
      string action,
      string? targetNativePath = null
    )
    {
      if (string.IsNullOrWhiteSpace(conflictKey))
      {
        throw new ArgumentException("A conflict key is required.", nameof(conflictKey));
      }
      if (
        !GltfMetadataConflictCatalog
          .ActionsByCode.Values.SelectMany(actions => actions)
          .Contains(action, StringComparer.Ordinal)
      )
      {
        throw new ArgumentOutOfRangeException(nameof(action));
      }
      if (
        action == GltfMetadataConflictActions.MapScope
        && string.IsNullOrWhiteSpace(targetNativePath)
      )
      {
        throw new ArgumentException(
          "Scope mapping requires a target native path.",
          nameof(targetNativePath)
        );
      }
      if (action != GltfMetadataConflictActions.MapScope && targetNativePath is not null)
      {
        throw new ArgumentException(
          "Only scope mapping accepts a target native path.",
          nameof(targetNativePath)
        );
      }

      ConflictKey = conflictKey;
      Action = action;
      TargetNativePath = targetNativePath;
    }
  }

  /// <summary>Supplies operation-scoped metadata conflict resolutions for edit import.</summary>
  internal sealed class GltfEditImportOptions
  {
    /// <summary>Gets the exact conflict resolutions applied as one transaction.</summary>
    public IReadOnlyList<GltfMetadataConflictResolution> ConflictResolutions { get; }

    /// <summary>Initializes edit-import options.</summary>
    public GltfEditImportOptions(
      IEnumerable<GltfMetadataConflictResolution>? conflictResolutions = null
    )
    {
      var resolutions =
        conflictResolutions?.ToArray() ?? Array.Empty<GltfMetadataConflictResolution>();
      if (resolutions.Any(resolution => resolution is null))
      {
        throw new ArgumentException(
          "Conflict resolutions cannot contain null values.",
          nameof(conflictResolutions)
        );
      }
      if (
        resolutions
          .Select(resolution => resolution.ConflictKey)
          .Distinct(StringComparer.Ordinal)
          .Count() != resolutions.Length
      )
      {
        throw new ArgumentException(
          "A conflict can be resolved only once.",
          nameof(conflictResolutions)
        );
      }
      if (
        resolutions.Count(resolution =>
          resolution.Action == GltfMetadataConflictActions.AdoptAsNew
          || resolution.Action == GltfMetadataConflictActions.DiscardLineage
        ) > 1
      )
      {
        throw new ArgumentException(
          "A transaction can contain only one whole-lineage action.",
          nameof(conflictResolutions)
        );
      }

      ConflictResolutions = Array.AsReadOnly(resolutions);
    }
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

    /// <summary>Gets the maximum metadata conflicts returned by one operation.</summary>
    public int MaxMetadataConflicts { get; }

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
        64,
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
        262144,
        64,
        1024
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
        262144,
        64,
        1024
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
      int maxUnknownMetadataMembers = 262144,
      int maxMetadataGuards = 64,
      int maxMetadataConflicts = 1024
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
        maxMetadataGuards,
        maxMetadataConflicts,
        GltfMeshResourceLimits.Default
      )
    { }

    /// <summary>Initializes every finite glTF operation and resource lookup limit.</summary>
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
      int maxTotalMetadataBytes,
      int maxMetadataEnvelopes,
      int maxMetadataElements,
      int maxUnknownMetadataMembers,
      int maxMetadataGuards,
      int maxMetadataConflicts,
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
      MaxMetadataGuards = RequirePositive(maxMetadataGuards, nameof(maxMetadataGuards));
      MaxMetadataConflicts = RequirePositive(maxMetadataConflicts, nameof(maxMetadataConflicts));
    }

    private static int RequirePositive(int value, string parameterName)
    {
      return value > 0 ? value : throw new ArgumentOutOfRangeException(parameterName);
    }
  }

  /// <summary>Identifies one asset lineage and one emitted interchange baseline.</summary>
  internal sealed class InterchangeBaseline
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
        : throw new ArgumentException(
          "Asset lineage identity must be a version-4 UUID.",
          nameof(assetLineageId)
        );
      DocumentId = GltfMetadataIdentity.IsVersion4(documentId)
        ? documentId
        : throw new ArgumentException(
          "Document identity must be a version-4 UUID.",
          nameof(documentId)
        );
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

  internal sealed class GltfArtistObjectLocalIds
  {
    internal IReadOnlyDictionary<int, int> Attachments { get; }
    internal IReadOnlyDictionary<int, int> Cannons { get; }
    internal IReadOnlyDictionary<int, int> StaticLightInstancesByDefinitionLocalId { get; }

    internal GltfArtistObjectLocalIds(
      IReadOnlyDictionary<int, int>? attachments = null,
      IReadOnlyDictionary<int, int>? cannons = null,
      IReadOnlyDictionary<int, int>? staticLights = null
    )
    {
      Attachments = new ReadOnlyDictionary<int, int>(
        attachments?.ToDictionary(pair => pair.Key, pair => pair.Value)
          ?? new Dictionary<int, int>()
      );
      Cannons = new ReadOnlyDictionary<int, int>(
        cannons?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<int, int>()
      );
      StaticLightInstancesByDefinitionLocalId = new ReadOnlyDictionary<int, int>(
        staticLights?.ToDictionary(pair => pair.Key, pair => pair.Value)
          ?? new Dictionary<int, int>()
      );
    }
  }

  /// <summary>Controls identities assigned to a GLB export.</summary>
  public sealed class GltfExportOptions
  {
    /// <summary>Gets an optional caller-supplied asset-lineage identity.</summary>
    internal Guid? AssetLineageId { get; }

    /// <summary>Gets an optional caller-supplied document identity.</summary>
    internal Guid? DocumentId { get; }

    /// <summary>Gets ordered absolute roots used only to resolve decoded TEX previews.</summary>
    public IReadOnlyList<string> TextureSearchRoots { get; }

    /// <summary>Gets ordered absolute roots used only to resolve referenced MSH previews.</summary>
    public IReadOnlyList<string> MeshResourceSearchRoots { get; }

    /// <summary>Gets the optional source-file basename used for artist-facing object names.</summary>
    public string? SourceBaseName { get; }

    /// <summary>Gets exact unknown additive metadata tokens to carry into the next baseline.</summary>
    internal IReadOnlyDictionary<string, string> PreservedUnknownMetadata { get; }

    internal IReadOnlyDictionary<string, int> MetadataNextIds { get; }
    internal GltfArtistObjectLocalIds ArtistObjectLocalIds { get; }
    internal IReadOnlyList<int> DynamicObjectIds { get; private set; }
    internal Internal.GltfStaticIdentityMap? StaticIdentityMap { get; private set; }

    /// <summary>Initializes export options for previews and artist-facing source naming.</summary>
    public GltfExportOptions(
      IEnumerable<string>? textureSearchRoots = null,
      IEnumerable<string>? meshResourceSearchRoots = null,
      string? sourceBaseName = null
    )
      : this(null, null, textureSearchRoots, null, meshResourceSearchRoots, sourceBaseName)
    { }

    internal GltfExportOptions(
      Guid? assetLineageId,
      Guid? documentId,
      IEnumerable<string>? textureSearchRoots = null,
      IReadOnlyDictionary<string, string>? preservedUnknownMetadata = null
    )
      : this(assetLineageId, documentId, textureSearchRoots, preservedUnknownMetadata, null, null)
    { }

    internal GltfExportOptions(
      Guid? assetLineageId,
      Guid? documentId,
      IEnumerable<string>? textureSearchRoots,
      IReadOnlyDictionary<string, string>? preservedUnknownMetadata,
      IEnumerable<string>? meshResourceSearchRoots
    )
      : this(
        assetLineageId,
        documentId,
        textureSearchRoots,
        preservedUnknownMetadata,
        meshResourceSearchRoots,
        null
      )
    { }

    internal GltfExportOptions(
      Guid? assetLineageId,
      Guid? documentId,
      IEnumerable<string>? textureSearchRoots,
      IReadOnlyDictionary<string, string>? preservedUnknownMetadata,
      IEnumerable<string>? meshResourceSearchRoots,
      string? sourceBaseName
    )
    {
      if (assetLineageId.HasValue && !GltfMetadataIdentity.IsVersion4(assetLineageId.Value))
      {
        throw new ArgumentException(
          "Asset lineage identity must be a version-4 UUID.",
          nameof(assetLineageId)
        );
      }

      if (documentId.HasValue && !GltfMetadataIdentity.IsVersion4(documentId.Value))
      {
        throw new ArgumentException(
          "Document identity must be a version-4 UUID.",
          nameof(documentId)
        );
      }

      AssetLineageId = assetLineageId;
      DocumentId = documentId;
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
      var unknownMetadata =
        preservedUnknownMetadata?.ToDictionary(
          pair => pair.Key,
          pair => pair.Value,
          StringComparer.Ordinal
        ) ?? new Dictionary<string, string>(StringComparer.Ordinal);
      foreach (var member in unknownMetadata)
      {
        ValidateUnknownMetadata(member.Key, member.Value, nameof(preservedUnknownMetadata));
      }
      PreservedUnknownMetadata = new ReadOnlyDictionary<string, string>(unknownMetadata);
      MetadataNextIds = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>());
      ArtistObjectLocalIds = new GltfArtistObjectLocalIds();
      DynamicObjectIds = Array.Empty<int>();
      StaticIdentityMap = null;
    }

    internal GltfExportOptions(
      Guid assetLineageId,
      Guid documentId,
      IReadOnlyDictionary<string, string> preservedUnknownMetadata,
      IReadOnlyDictionary<string, int> metadataNextIds,
      GltfArtistObjectLocalIds? artistObjectLocalIds = null,
      Internal.GltfStaticIdentityMap? staticIdentityMap = null
    )
      : this(assetLineageId, documentId, preservedUnknownMetadata: preservedUnknownMetadata)
    {
      MetadataNextIds = new ReadOnlyDictionary<string, int>(
        metadataNextIds.ToDictionary(pair => pair.Key, pair => pair.Value)
      );
      ArtistObjectLocalIds = artistObjectLocalIds ?? new GltfArtistObjectLocalIds();
      StaticIdentityMap = staticIdentityMap;
    }

    internal GltfExportOptions(
      Guid assetLineageId,
      Guid documentId,
      IReadOnlyList<int> dynamicObjectIds
    )
      : this(assetLineageId, documentId)
    {
      DynamicObjectIds = Array.AsReadOnly(dynamicObjectIds.ToArray());
    }

    private static void ValidateUnknownMetadata(string key, string value, string parameterName)
    {
      var firstSeparator = key.IndexOf(':');
      var secondSeparator = firstSeparator < 0 ? -1 : key.IndexOf(':', firstSeparator + 1);
      if (
        firstSeparator <= 0
        || secondSeparator <= firstSeparator + 1
        || !int.TryParse(
          key.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1),
          System.Globalization.NumberStyles.None,
          System.Globalization.CultureInfo.InvariantCulture,
          out var localId
        )
      )
      {
        throw new ArgumentException(
          "Unknown metadata keys must contain a scope kind, local ID, and JSON Pointer.",
          parameterName
        );
      }
      var scopeKind = key.Substring(0, firstSeparator);
      var localIdText = key.Substring(firstSeparator + 1, secondSeparator - firstSeparator - 1);
      var path = key.Substring(secondSeparator + 1);
      if (
        scopeKind is not ("manifest" or "object" or "mesh" or "material" or "light")
        || localIdText != localId.ToString(System.Globalization.CultureInfo.InvariantCulture)
        || (scopeKind == "manifest" ? localId != 0 : localId <= 0)
        || !Internal.GlbDocument.IsSupportedUnknownMetadataPath(scopeKind, path)
      )
      {
        throw new ArgumentException(
          "Unknown metadata key does not identify an additive version-1 member.",
          parameterName
        );
      }
      try
      {
        using var _ = JsonDocument.Parse(
          value,
          new JsonDocumentOptions
          {
            MaxDepth = int.MaxValue,
            CommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
          }
        );
      }
      catch (Exception ex) when (ex is JsonException || ex is ArgumentException)
      {
        throw new ArgumentException(
          "Unknown metadata values must contain one valid JSON token.",
          parameterName,
          ex
        );
      }
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

  /// <summary>Reports the baseline and native projection emitted by an export.</summary>
  public sealed class GltfExportReceipt
  {
    /// <summary>Gets the emitted interchange baseline.</summary>
    internal InterchangeBaseline Baseline { get; }

    /// <summary>Gets the emitted native projection fingerprint.</summary>
    public NativeProjectionFingerprint Fingerprint { get; }

    internal GltfExportReceipt(
      InterchangeBaseline baseline,
      NativeProjectionFingerprint fingerprint
    )
    {
      Baseline = baseline;
      Fingerprint = fingerprint;
    }
  }

  /// <summary>Reports one immutable mesh asset created from glTF.</summary>
  public sealed class GltfMeshCreationResult
  {
    /// <summary>Gets the created immutable static or dynamic mesh asset.</summary>
    public MeshAsset Asset { get; }

    /// <summary>Gets the retained, regenerated, and canonicalized MSH paths.</summary>
    public PreservationReport Preservation { get; }

    internal GltfMeshCreationResult(MeshAsset asset, PreservationReport preservation)
    {
      Asset = asset;
      Preservation = preservation;
    }
  }

  /// <summary>Reports a successful reconciled edit import.</summary>
  internal sealed class GltfEditImportResult
  {
    /// <summary>Gets the restored immutable static mesh asset.</summary>
    public StaticMeshAsset Asset { get; }

    /// <summary>Gets the retained lineage and rotated document baseline.</summary>
    public InterchangeBaseline NextBaseline { get; }

    /// <summary>Gets the fingerprint that proved metadata applicability, or null after lineage discard.</summary>
    public NativeProjectionFingerprint? AppliedFingerprint { get; }

    /// <summary>Gets the serialized representation paths restored from applicable metadata.</summary>
    public IReadOnlyList<string> RestoredSerializedRepresentationPaths { get; }

    /// <summary>Gets exact retained, regenerated, invalidated, and canonicalized MSH paths.</summary>
    public PreservationReport Preservation { get; }

    /// <summary>Gets exact raw JSON tokens for unknown additive version-1 members.</summary>
    public IReadOnlyDictionary<string, string> PreservedUnknownMetadata { get; }

    /// <summary>Gets export options that retain the next baseline and unknown additive tokens.</summary>
    public GltfExportOptions NextExportOptions { get; }

    /// <summary>Gets how the successful transaction treated metadata lineage.</summary>
    public GltfMetadataLineageDisposition LineageDisposition { get; }

    /// <summary>Gets the exact conflict resolutions applied by the successful transaction.</summary>
    public IReadOnlyList<GltfMetadataConflictResolution> AppliedConflictResolutions { get; }

    internal PreservationReport CreationPreservation { get; }

    internal GltfEditImportResult(
      StaticMeshAsset asset,
      InterchangeBaseline nextBaseline,
      NativeProjectionFingerprint? appliedFingerprint,
      PreservationReport preservation,
      IEnumerable<string> restoredSerializedRepresentationPaths,
      IReadOnlyDictionary<string, string>? preservedUnknownMetadata = null,
      IReadOnlyDictionary<string, int>? metadataNextIds = null,
      GltfArtistObjectLocalIds? artistObjectLocalIds = null,
      GltfMetadataLineageDisposition lineageDisposition = GltfMetadataLineageDisposition.Retained,
      IEnumerable<GltfMetadataConflictResolution>? appliedConflictResolutions = null,
      PreservationReport? creationPreservation = null,
      Internal.GltfStaticIdentityMap? staticIdentityMap = null
    )
    {
      Asset = asset;
      NextBaseline = nextBaseline;
      AppliedFingerprint = appliedFingerprint;
      Preservation = preservation;
      RestoredSerializedRepresentationPaths = Array.AsReadOnly(
        new List<string>(restoredSerializedRepresentationPaths).ToArray()
      );
      PreservedUnknownMetadata = new ReadOnlyDictionary<string, string>(
        preservedUnknownMetadata?.ToDictionary(pair => pair.Key, pair => pair.Value)
          ?? new Dictionary<string, string>()
      );
      NextExportOptions = new GltfExportOptions(
        nextBaseline.AssetLineageId,
        nextBaseline.DocumentId,
        PreservedUnknownMetadata,
        metadataNextIds ?? new Dictionary<string, int>(),
        artistObjectLocalIds,
        staticIdentityMap
      );
      LineageDisposition = lineageDisposition;
      AppliedConflictResolutions = Array.AsReadOnly(
        appliedConflictResolutions?.ToArray() ?? Array.Empty<GltfMetadataConflictResolution>()
      );
      CreationPreservation = creationPreservation ?? preservation;
    }
  }

  /// <summary>Reports a successful reconciled dynamic edit import.</summary>
  internal sealed class GltfDynamicEditImportResult
  {
    /// <summary>Gets the restored immutable dynamic mesh asset.</summary>
    public DynamicMeshAsset Asset { get; }

    /// <summary>Gets the retained lineage and rotated document baseline.</summary>
    public InterchangeBaseline NextBaseline { get; }

    /// <summary>Gets the fingerprint that proved metadata applicability.</summary>
    public NativeProjectionFingerprint AppliedFingerprint { get; }

    /// <summary>Gets exact retained and regenerated MSH paths.</summary>
    public PreservationReport Preservation { get; }

    /// <summary>Gets the serialized representation paths restored from metadata.</summary>
    public IReadOnlyList<string> RestoredSerializedRepresentationPaths { get; }

    /// <summary>Gets export options that retain the next baseline.</summary>
    public GltfExportOptions NextExportOptions { get; }

    internal GltfDynamicEditImportResult(
      DynamicMeshAsset asset,
      InterchangeBaseline nextBaseline,
      NativeProjectionFingerprint appliedFingerprint,
      PreservationReport preservation,
      IEnumerable<string> restoredSerializedRepresentationPaths,
      IReadOnlyList<int> dynamicObjectIds
    )
    {
      Asset = asset;
      NextBaseline = nextBaseline;
      AppliedFingerprint = appliedFingerprint;
      Preservation = preservation;
      RestoredSerializedRepresentationPaths = Array.AsReadOnly(
        restoredSerializedRepresentationPaths.ToArray()
      );
      NextExportOptions = new GltfExportOptions(
        nextBaseline.AssetLineageId,
        nextBaseline.DocumentId,
        dynamicObjectIds
      );
    }
  }

  /// <summary>Reports a successful edit import without weakening kind-specific APIs.</summary>
  internal sealed class GltfMeshEditImportResult
  {
    /// <summary>Gets the restored immutable static or dynamic mesh asset.</summary>
    public MeshAsset Asset { get; }

    /// <summary>Gets the retained lineage and rotated document baseline.</summary>
    public InterchangeBaseline NextBaseline { get; }

    /// <summary>Gets the fingerprint that proved metadata applicability.</summary>
    public NativeProjectionFingerprint? AppliedFingerprint { get; }

    /// <summary>Gets exact retained and regenerated MSH paths.</summary>
    public PreservationReport Preservation { get; }

    /// <summary>Gets serialized representation paths restored from metadata.</summary>
    public IReadOnlyList<string> RestoredSerializedRepresentationPaths { get; }

    /// <summary>Gets how the successful transaction treated metadata lineage.</summary>
    public GltfMetadataLineageDisposition LineageDisposition { get; }

    /// <summary>Gets conflict resolutions applied by the successful transaction.</summary>
    public IReadOnlyList<GltfMetadataConflictResolution> AppliedConflictResolutions { get; }

    internal GltfMeshEditImportResult(
      MeshAsset asset,
      InterchangeBaseline nextBaseline,
      NativeProjectionFingerprint? appliedFingerprint,
      PreservationReport preservation,
      IEnumerable<string> restoredSerializedRepresentationPaths,
      GltfMetadataLineageDisposition lineageDisposition,
      IEnumerable<GltfMetadataConflictResolution>? appliedConflictResolutions = null
    )
    {
      Asset = asset;
      NextBaseline = nextBaseline;
      AppliedFingerprint = appliedFingerprint;
      Preservation = preservation;
      RestoredSerializedRepresentationPaths = Array.AsReadOnly(
        restoredSerializedRepresentationPaths.ToArray()
      );
      LineageDisposition = lineageDisposition;
      AppliedConflictResolutions = Array.AsReadOnly(
        appliedConflictResolutions?.ToArray() ?? Array.Empty<GltfMetadataConflictResolution>()
      );
    }
  }

  /// <summary>Reports a successful new-model import and its first interchange baseline.</summary>
  internal sealed class GltfNewModelImportResult
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
      PreservationReport preservation
    )
    {
      Asset = asset;
      Baseline = baseline;
      Preservation = preservation;
    }
  }
}
