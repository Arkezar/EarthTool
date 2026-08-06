#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace EarthTool.MSH.Internal
{
  internal sealed class MshContentException : Exception
  {
    internal OperationDiagnostic Diagnostic { get; }

    internal MshContentException(OperationDiagnostic diagnostic)
      : base(diagnostic.Message)
    {
      Diagnostic = diagnostic;
    }
  }

  internal sealed class MshDecodeResult
  {
    internal MeshAsset Asset { get; }
    internal IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    internal MshDecodeResult(MeshAsset asset, IReadOnlyList<OperationDiagnostic> diagnostics)
    {
      Asset = asset;
      Diagnostics = diagnostics;
    }
  }

  internal static class MshV1Decoder
  {
    private const uint ArchiveSignature = 0x00D0A1FF;
    private const uint ArchiveTypeFlag = 0x10000000;
    private const uint CreationGuidFlag = 0x20000000;
    private const uint KnownDeclarationBits = 0x30FFFFFF;

    internal static MshDecodeResult Decode(
      byte[] source,
      MshOperationProfile profile,
      CancellationToken cancellationToken,
      MeshAssetOrigin origin = MeshAssetOrigin.Loaded
    )
    {
      var context = new MshDecodeContext(source, profile, cancellationToken);
      context.ThrowIfCancellationRequested();
      var data = context.Data;
      if (data.Length < sizeof(uint))
      {
        throw context.Failure(
          MshDiagnosticCodes.InvalidFraming,
          1000,
          "ArchiveFraming.Declaration",
          0,
          "The archive framing declaration is truncated."
        );
      }

      var declaration = context.ReadUInt32(0);
      if ((declaration & 0x00FFFFFF) != ArchiveSignature)
      {
        throw context.Failure(
          MshDiagnosticCodes.InvalidFraming,
          1000,
          "ArchiveFraming.Declaration",
          0,
          "The archive framing signature is invalid."
        );
      }

      var unknownDeclarationBits = declaration & ~KnownDeclarationBits;
      if (unknownDeclarationBits != 0)
      {
        context.AddDiagnostic(
          context.Compatibility(
            "ArchiveFraming.Declaration",
            0,
            "Unknown archive declaration bits were preserved.",
            new Dictionary<string, string> { ["unknownBits"] = $"0x{unknownDeclarationBits:X8}" }
          )
        );
      }

      var cursor = sizeof(uint);
      uint? archiveType = null;
      if ((declaration & ArchiveTypeFlag) != 0)
      {
        context.Ensure(cursor, sizeof(uint), "ArchiveFraming.ArchiveType");
        archiveType = context.ReadUInt32(cursor);
        cursor += sizeof(uint);
      }

      Guid? creationGuid = null;
      if ((declaration & CreationGuidFlag) != 0)
      {
        context.Ensure(cursor, 16, "ArchiveFraming.CreationGuid");
        creationGuid = new Guid(data.Slice(cursor, 16).ToArray());
        cursor += 16;
      }

      var baseOffset = cursor;
      context.Ensure(baseOffset, CommonMeshBaseHeader.SerializedSize, "BaseHeader");
      var baseHeader = data.Slice(baseOffset, CommonMeshBaseHeader.SerializedSize);
      if (
        !baseHeader
          .Slice(0, 4)
          .SequenceEqual(new byte[] { (byte)'M', (byte)'E', (byte)'S', (byte)'H' })
      )
      {
        throw context.Structural("BaseHeader.Magic", baseOffset, "Expected MESH.");
      }

      var version = context.ReadUInt32(baseOffset + 4);
      if (version != 1)
      {
        throw context.Failure(
          MshDiagnosticCodes.UnsupportedVersion,
          1001,
          "BaseHeader.Version",
          baseOffset + 4,
          $"Unsupported MSH version {version}."
        );
      }

      var meshKind = context.ReadUInt32(baseOffset + 8);
      if (meshKind > 1)
      {
        throw context.Failure(
          MshDiagnosticCodes.UnsupportedMeshKind,
          1002,
          "BaseHeader.MeshKind",
          baseOffset + 8,
          $"Unsupported root mesh kind {meshKind}."
        );
      }

      var archiveSelectsDynamic = archiveType.GetValueOrDefault() != 0;
      var meshKindIsDynamic = meshKind == 1;
      if (archiveSelectsDynamic != meshKindIsDynamic)
      {
        context.AddDiagnostic(
          context.Compatibility(
            "ArchiveFraming.ArchiveType",
            sizeof(uint),
            "Archive type and root mesh kind select different payload shapes.",
            new Dictionary<string, string>
            {
              ["archiveType"] = archiveType
                .GetValueOrDefault()
                .ToString(CultureInfo.InvariantCulture),
              ["meshKind"] = meshKind.ToString(CultureInfo.InvariantCulture),
            }
          )
        );
      }

      var framing = new MeshArchiveFraming(declaration, archiveType, creationGuid);
      if (meshKindIsDynamic)
      {
        return DynamicMeshDecoder.Decode(context, framing, baseOffset, origin);
      }

      return StaticMeshDecoder.Decode(context, framing, baseOffset, origin);
    }
  }
}
