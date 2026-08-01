#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.MSH.Operations
{
  /// <summary>Defines stable diagnostics emitted by safe MSH operations.</summary>
  public static class MshDiagnosticCodes
  {
    /// <summary>Invalid archive framing.</summary>
    public const string InvalidFraming = "ETM1000";
    /// <summary>Unsupported MSH version.</summary>
    public const string UnsupportedVersion = "ETM1001";
    /// <summary>Unsupported root mesh kind.</summary>
    public const string UnsupportedMeshKind = "ETM1002";
    /// <summary>Unsafe or ambiguous serialized structure.</summary>
    public const string StructuralHazard = "ETM1003";
    /// <summary>Configured resource limit exceeded.</summary>
    public const string ResourceLimitExceeded = "ETM1004";
    /// <summary>Domain assigned to a later implementation slice.</summary>
    public const string UnsupportedDomain = "ETM1005";
    /// <summary>Independent MSH validation failure.</summary>
    public const string ValidationFailed = "ETM1006";
    /// <summary>MSH input or output failure.</summary>
    public const string IoFailure = "ETM1007";
    /// <summary>MSH operation cancellation.</summary>
    public const string Cancelled = "ETM1008";
    /// <summary>Safely preserved noncanonical serialized representation.</summary>
    public const string CompatibilityAnomaly = "ETM1009";
    /// <summary>Additional diagnostics were suppressed by the operation profile.</summary>
    public const string DiagnosticsTruncated = "ETM1010";
    /// <summary>Canonical semantic input cannot be represented safely.</summary>
    public const string InvalidAuthoringInput = "ETM1011";
    /// <summary>A requested edit cannot produce one coherent snapshot.</summary>
    public const string InvalidEdit = "ETM1012";
  }

  /// <summary>Defines finite resource limits for one MSH operation.</summary>
  public sealed class MshOperationProfile
  {
    /// <summary>Gets the default finite profile.</summary>
    public static MshOperationProfile Default { get; } = new MshOperationProfile();

    /// <summary>Gets the maximum accepted input size in bytes.</summary>
    public int MaxInputBytes { get; }

    /// <summary>Gets the maximum emitted output size in bytes.</summary>
    public int MaxOutputBytes { get; }

    /// <summary>Gets the maximum retained operation diagnostics.</summary>
    public int MaxDiagnostics { get; }

    /// <summary>Gets the maximum accepted opaque root trailing-byte count.</summary>
    public int MaxRootTrailingBytes { get; }

    /// <summary>Gets the maximum dynamic-object nesting depth, including the root.</summary>
    public int MaxDynamicDepth { get; }

    /// <summary>Gets the maximum total dynamic-object count.</summary>
    public int MaxDynamicObjects { get; }

    /// <summary>Gets the maximum direct child count of one dynamic object.</summary>
    public int MaxDynamicChildrenPerObject { get; }

    /// <summary>Gets the maximum total dynamic string bytes.</summary>
    public int MaxDynamicStringBytes { get; }

    /// <summary>Initializes finite MSH operation limits.</summary>
    public MshOperationProfile(
      int maxInputBytes = 16 * 1024 * 1024,
      int maxOutputBytes = 16 * 1024 * 1024,
      int maxDiagnostics = 128,
      int maxRootTrailingBytes = 1024 * 1024,
      int maxDynamicDepth = 64,
      int maxDynamicObjects = 4096,
      int maxDynamicChildrenPerObject = 1024,
      int maxDynamicStringBytes = 1024 * 1024)
    {
      MaxInputBytes = RequirePositive(maxInputBytes, nameof(maxInputBytes));
      MaxOutputBytes = RequirePositive(maxOutputBytes, nameof(maxOutputBytes));
      MaxDiagnostics = RequirePositive(maxDiagnostics, nameof(maxDiagnostics));
      MaxRootTrailingBytes = RequireNonNegative(maxRootTrailingBytes, nameof(maxRootTrailingBytes));
      MaxDynamicDepth = RequirePositive(maxDynamicDepth, nameof(maxDynamicDepth));
      MaxDynamicObjects = RequirePositive(maxDynamicObjects, nameof(maxDynamicObjects));
      MaxDynamicChildrenPerObject = RequireNonNegative(
        maxDynamicChildrenPerObject,
        nameof(maxDynamicChildrenPerObject));
      MaxDynamicStringBytes = RequireNonNegative(maxDynamicStringBytes, nameof(maxDynamicStringBytes));
    }

    private static int RequirePositive(int value, string parameterName)
    {
      return value > 0 ? value : throw new ArgumentOutOfRangeException(parameterName);
    }

    private static int RequireNonNegative(int value, string parameterName)
    {
      return value >= 0 ? value : throw new ArgumentOutOfRangeException(parameterName);
    }
  }

  /// <summary>Reads framed MSH input without exposing a partial asset.</summary>
  public interface IMshReader
  {
    /// <summary>Reads one caller-owned stream.</summary>
    Task<OperationResult<MeshAsset>> ReadAsync(
      Stream source,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default);

    /// <summary>Reads one file.</summary>
    Task<OperationResult<MeshAsset>> ReadFileAsync(
      string path,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default);
  }

  /// <summary>Writes immutable MSH assets.</summary>
  public interface IMshWriter
  {
    /// <summary>Writes to a caller-owned stream.</summary>
    Task<OperationResult> WriteAsync(
      MeshAsset asset,
      Stream destination,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default);

    /// <summary>Transactionally replaces one destination file after complete validation.</summary>
    Task<OperationResult> WriteFileAsync(
      MeshAsset asset,
      string destinationPath,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default);
  }

  /// <summary>Validates an immutable MSH asset independently of writing it.</summary>
  public interface IMshValidator
  {
    /// <summary>Validates one asset under finite operation limits.</summary>
    Task<OperationResult> ValidateAsync(
      MeshAsset asset,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default);
  }
}
