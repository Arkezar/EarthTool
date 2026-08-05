#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Operations;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace EarthTool.MSH.Internal
{
  internal sealed class MshDecodeContext
  {
    private readonly List<OperationDiagnostic> _diagnostics = new List<OperationDiagnostic>();

    internal byte[] Source { get; }
    internal ReadOnlySpan<byte> Data => Source;
    internal MshOperationProfile Profile { get; }
    internal CancellationToken CancellationToken { get; }

    internal MshDecodeContext(
      byte[] source,
      MshOperationProfile profile,
      CancellationToken cancellationToken)
    {
      Source = source;
      Profile = profile;
      CancellationToken = cancellationToken;
    }

    internal void ThrowIfCancellationRequested()
    {
      CancellationToken.ThrowIfCancellationRequested();
    }

    internal void AddDiagnostic(OperationDiagnostic diagnostic)
    {
      _diagnostics.Add(diagnostic);
    }

    internal void AddDiagnosticBounded(OperationDiagnostic diagnostic)
    {
      if (_diagnostics.Count < Profile.MaxDiagnostics)
      {
        _diagnostics.Add(diagnostic);
      }
      else if (
        _diagnostics.Count == Profile.MaxDiagnostics
        && _diagnostics[^1].Code != MshDiagnosticCodes.DiagnosticsTruncated)
      {
        _diagnostics[^1] = new OperationDiagnostic(
          MshDiagnosticCodes.DiagnosticsTruncated,
          1010,
          DiagnosticSeverity.Warning,
          "$",
          "Additional diagnostics were suppressed by the operation profile.");
      }
    }

    internal MshDecodeResult Complete(MeshAsset asset)
    {
      return new MshDecodeResult(asset, CapDiagnostics(_diagnostics, Profile.MaxDiagnostics));
    }

    internal void Ensure(int offset, int length, string path)
    {
      if (offset < 0 || length < 0 || offset > Data.Length - length)
      {
        throw Structural(
          path,
          Math.Min(offset, Data.Length),
          "The serialized representation is truncated.");
      }
    }

    internal void EnsureCounted(int offset, uint count, int elementSize, string path)
    {
      var length = (long)count * elementSize;
      if (length > int.MaxValue || offset < 0 || offset > Data.Length - length)
      {
        throw Structural(
          path,
          Math.Min(offset, Data.Length),
          "The declared elements do not fit in the serialized representation.");
      }
    }

    internal ushort ReadUInt16(int offset)
    {
      return BinaryPrimitives.ReadUInt16LittleEndian(Data.Slice(offset, sizeof(ushort)));
    }

    internal uint ReadUInt32(int offset)
    {
      return BinaryPrimitives.ReadUInt32LittleEndian(Data.Slice(offset, sizeof(uint)));
    }

    internal float ReadSingle(int offset)
    {
      return BitConverter.Int32BitsToSingle(
        BinaryPrimitives.ReadInt32LittleEndian(Data.Slice(offset, sizeof(float))));
    }

    internal MshContentException Structural(string path, long offset, string message)
    {
      return Failure(MshDiagnosticCodes.StructuralHazard, 1003, path, offset, message);
    }

    internal MshContentException ResourceLimit(
      string path,
      long offset,
      long actual,
      long maximum)
    {
      return new MshContentException(
        new OperationDiagnostic(
          MshDiagnosticCodes.ResourceLimitExceeded,
          1004,
          DiagnosticSeverity.Error,
          path,
          "The serialized representation exceeds the configured operation profile.",
          offset,
          new Dictionary<string, string>
          {
            ["actual"] = actual.ToString(CultureInfo.InvariantCulture),
            ["maximum"] = maximum.ToString(CultureInfo.InvariantCulture)
          }));
    }

    internal OperationDiagnostic Compatibility(
      string path,
      long offset,
      string message,
      IReadOnlyDictionary<string, string> data)
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.CompatibilityAnomaly,
        1009,
        DiagnosticSeverity.Warning,
        path,
        message,
        offset,
        data);
    }

    internal MshContentException Failure(
      string code,
      int eventId,
      string path,
      long offset,
      string message)
    {
      return new MshContentException(
        new OperationDiagnostic(
          code,
          eventId,
          DiagnosticSeverity.Error,
          path,
          message,
          offset));
    }

    private static IReadOnlyList<OperationDiagnostic> CapDiagnostics(
      IReadOnlyList<OperationDiagnostic> diagnostics,
      int maximum)
    {
      if (diagnostics.Count <= maximum)
      {
        return diagnostics;
      }

      var retainedDiagnosticCount = maximum - 1;
      var suppressedDiagnosticCount = diagnostics.Count - retainedDiagnosticCount;
      var retained = diagnostics.Take(retainedDiagnosticCount).ToList();
      retained.Add(
        new OperationDiagnostic(
          MshDiagnosticCodes.DiagnosticsTruncated,
          1010,
          DiagnosticSeverity.Warning,
          "$",
          "Additional diagnostics were suppressed by the operation profile.",
          data: new Dictionary<string, string>
          {
            ["suppressed"] = suppressedDiagnosticCount.ToString(CultureInfo.InvariantCulture)
          }));
      return retained;
    }
  }
}
