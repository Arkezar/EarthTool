#nullable enable

using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;
using System;
using System.Collections.Generic;
using System.Threading;

namespace EarthTool.MSH.Expert
{
  /// <summary>Constructs exact accepted serialized state without weakening structural validation.</summary>
  public static class MshExpert
  {
    /// <summary>Creates a static asset from a complete exact framed representation.</summary>
    public static MshBuildResult<StaticMeshAsset> CreateStatic(
      IEnumerable<byte> serializedRepresentation,
      MshOperationProfile? profile = null
    )
    {
      return Create<StaticMeshAsset>(serializedRepresentation, profile);
    }

    /// <summary>Creates a dynamic asset from a complete exact framed representation.</summary>
    public static MshBuildResult<DynamicMeshAsset> CreateDynamic(
      IEnumerable<byte> serializedRepresentation,
      MshOperationProfile? profile = null
    )
    {
      return Create<DynamicMeshAsset>(serializedRepresentation, profile);
    }

    private static MshBuildResult<T> Create<T>(
      IEnumerable<byte> serializedRepresentation,
      MshOperationProfile? profile
    )
      where T : MeshAsset
    {
      if (serializedRepresentation is null)
      {
        throw new ArgumentNullException(nameof(serializedRepresentation));
      }

      profile ??= MshOperationProfile.Default;
      var bytes = MaterializeBounded(
        serializedRepresentation,
        profile.MaxInputBytes,
        out var exceededLimit
      );
      if (exceededLimit)
      {
        return new MshBuildResult<T>(
          false,
          null,
          new[]
          {
            AuthoringValidation.ResourceLimit(
              (long)profile.MaxInputBytes + 1,
              profile.MaxInputBytes
            ),
          }
        );
      }

      try
      {
        var decoded = MshV1Decoder.Decode(
          bytes,
          profile,
          CancellationToken.None,
          MeshAssetOrigin.Expert
        );
        if (decoded.Asset is not T asset)
        {
          return new MshBuildResult<T>(
            false,
            null,
            new[]
            {
              new EarthTool.Common.Operations.OperationDiagnostic(
                MshDiagnosticCodes.UnsupportedMeshKind,
                1002,
                EarthTool.Common.Operations.DiagnosticSeverity.Error,
                "BaseHeader.MeshKind",
                "The exact representation does not contain the requested mesh kind."
              ),
            }
          );
        }

        return new MshBuildResult<T>(true, asset, decoded.Diagnostics);
      }
      catch (MshContentException ex)
      {
        return new MshBuildResult<T>(false, null, new[] { ex.Diagnostic });
      }
    }

    private static byte[] MaterializeBounded(
      IEnumerable<byte> source,
      int maximum,
      out bool exceededLimit
    )
    {
      var bytes = new List<byte>(Math.Min(maximum, 4096));
      foreach (var value in source)
      {
        if (bytes.Count == maximum)
        {
          exceededLimit = true;
          return bytes.ToArray();
        }

        bytes.Add(value);
      }

      exceededLimit = false;
      return bytes.ToArray();
    }
  }
}
