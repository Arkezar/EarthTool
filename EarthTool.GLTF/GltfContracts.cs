#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using System;
using System.Collections.Generic;

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
    /// <summary>Required EarthTool manifest metadata is absent.</summary>
    public const string MissingManifest = "ETG2000";
    /// <summary>EarthTool metadata is malformed.</summary>
    public const string MalformedMetadata = "ETG2001";
    /// <summary>EarthTool metadata version is unsupported.</summary>
    public const string UnsupportedMetadataVersion = "ETG2002";
    /// <summary>Asset lineage differs from the expected baseline.</summary>
    public const string AssetLineageMismatch = "ETG2003";
    /// <summary>Document identity differs from the expected baseline.</summary>
    public const string DocumentMismatch = "ETG2004";
    /// <summary>Expected local metadata scope is absent.</summary>
    public const string MissingExpectedScope = "ETG2005";
    /// <summary>Native projection no longer matches preservation metadata.</summary>
    public const string StaleNativeProjection = "ETG2008";
    /// <summary>EarthTool metadata appears on an unsupported carrier.</summary>
    public const string MisplacedMetadata = "ETG2009";
    /// <summary>Native geometry cannot be associated with one unique preserved partition set.</summary>
    public const string AmbiguousPartitionCorrespondence = "ETG2012";
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

    /// <summary>Initializes finite glTF operation limits.</summary>
    public GltfOperationProfile(
      int maxInputBytes = 32 * 1024 * 1024,
      int maxOutputBytes = 32 * 1024 * 1024,
      int maxMetadataBytes = 4 * 1024 * 1024,
      int maxJsonDepth = 32,
      int maxActiveRenderVertices = 65536)
    {
      MaxInputBytes = RequirePositive(maxInputBytes, nameof(maxInputBytes));
      MaxOutputBytes = RequirePositive(maxOutputBytes, nameof(maxOutputBytes));
      MaxMetadataBytes = RequirePositive(maxMetadataBytes, nameof(maxMetadataBytes));
      MaxJsonDepth = RequirePositive(maxJsonDepth, nameof(maxJsonDepth));
      MaxActiveRenderVertices = maxActiveRenderVertices is > 0 and <= 65536
        ? maxActiveRenderVertices
        : throw new ArgumentOutOfRangeException(nameof(maxActiveRenderVertices));
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
      AssetLineageId = assetLineageId != Guid.Empty
        ? assetLineageId
        : throw new ArgumentException("Asset lineage identity cannot be empty.", nameof(assetLineageId));
      DocumentId = documentId != Guid.Empty
        ? documentId
        : throw new ArgumentException("Document identity cannot be empty.", nameof(documentId));
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

    /// <summary>Initializes GLB export options.</summary>
    public GltfExportOptions(Guid? assetLineageId = null, Guid? documentId = null)
    {
      if (assetLineageId == Guid.Empty)
      {
        throw new ArgumentException("Asset lineage identity cannot be empty.", nameof(assetLineageId));
      }

      if (documentId == Guid.Empty)
      {
        throw new ArgumentException("Document identity cannot be empty.", nameof(documentId));
      }

      AssetLineageId = assetLineageId;
      DocumentId = documentId;
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

    internal GltfEditImportResult(
      StaticMeshAsset asset,
      InterchangeBaseline nextBaseline,
      NativeProjectionFingerprint appliedFingerprint,
      PreservationReport preservation,
      IEnumerable<string> restoredSerializedRepresentationPaths)
    {
      Asset = asset;
      NextBaseline = nextBaseline;
      AppliedFingerprint = appliedFingerprint;
      Preservation = preservation;
      RestoredSerializedRepresentationPaths = Array.AsReadOnly(
        new List<string>(restoredSerializedRepresentationPaths).ToArray());
    }
  }
}
