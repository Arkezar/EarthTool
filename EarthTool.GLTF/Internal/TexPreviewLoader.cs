#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.TEX;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace EarthTool.GLTF.Internal
{
  internal sealed class TexPreviewLoadResult
  {
    internal IReadOnlyDictionary<StaticRenderObjectId, byte[]> Previews { get; }

    internal IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    internal bool HasErrors => Diagnostics.Any(diagnostic =>
      diagnostic.Severity == DiagnosticSeverity.Error);

    internal TexPreviewLoadResult(
      IReadOnlyDictionary<StaticRenderObjectId, byte[]> previews,
      IReadOnlyList<OperationDiagnostic> diagnostics)
    {
      Previews = previews;
      Diagnostics = diagnostics;
    }
  }

  internal static class TexPreviewLoader
  {
    private static readonly byte[] _identifier = { 0x54, 0x45, 0x58, 0, 1, 0, 0, 0 };

    internal static TexPreviewLoadResult Load(
      StaticMeshAsset asset,
      GltfExportOptions options,
      GltfOperationProfile profile,
      int maxPreviewOutputBytes,
      CancellationToken cancellationToken)
    {
      var previews = new Dictionary<StaticRenderObjectId, byte[]>();
      var diagnostics = new List<OperationDiagnostic>();
      var cache = new Dictionary<string, DecodedPreview?>(StringComparer.OrdinalIgnoreCase);
      long decodedPixels = 0;
      var previewOutputBytes = 0;
      for (var recordIndex = 0; recordIndex < asset.StaticRenderObjectSequence.Count; recordIndex++)
      {
        var record = asset.StaticRenderObjectSequence[recordIndex];
        cancellationToken.ThrowIfCancellationRequested();
        if (record.TexturePathBytes.Count == 0)
        {
          continue;
        }
        var path = TryGetRelativePath(record.TexturePathBytes);
        if (path is null)
        {
          diagnostics.Add(Warning(
            GltfDiagnosticCodes.TexturePreviewUnavailable,
            1109,
            recordIndex,
            "The exact TEX binding cannot be used as a safe host resource path."));
          continue;
        }
        if (!cache.TryGetValue(path, out var png))
        {
          var resolution = Resolve(path, options.TextureSearchRoots);
          if (resolution.Ambiguous)
          {
            diagnostics.Add(new OperationDiagnostic(
              GltfDiagnosticCodes.AmbiguousTextureResource,
              1108,
              DiagnosticSeverity.Error,
              BindingPath(recordIndex),
              "The TEX resource key has more than one case-insensitive match in the winning root."));
            continue;
          }
          if (resolution.Path is null)
          {
            diagnostics.Add(Warning(
              GltfDiagnosticCodes.TextureResourceMissing,
              1107,
              recordIndex,
              "The explicit TEX resource binding was not found in the configured roots."));
            cache.Add(path, null);
            continue;
          }
          if (resolution.Shadowed)
          {
            diagnostics.Add(Warning(
              GltfDiagnosticCodes.TextureResourceShadowed,
              1110,
              recordIndex,
              "A later TEX search root contains a shadowed match for this binding."));
          }
          try
          {
            png = Decode(
              resolution.Path,
              resolution.Root!,
              profile,
              profile.MaxPreviewPixels - decodedPixels);
            decodedPixels = checked(decodedPixels + png.PixelCount);
          }
          catch (Exception ex) when (ex is IOException
            || ex is EndOfStreamException
            || ex is InvalidDataException
            || ex is NotSupportedException
            || ex is OverflowException
            || ex is ArgumentException)
          {
            diagnostics.Add(Warning(
              GltfDiagnosticCodes.TexturePreviewUnavailable,
              1109,
              recordIndex,
              ex.Message));
            png = null;
          }
          if (png is not null && png.Png.Length > maxPreviewOutputBytes - previewOutputBytes)
          {
            diagnostics.Add(Warning(
              GltfDiagnosticCodes.TexturePreviewUnavailable,
              1109,
              recordIndex,
              "The decoded TEX previews exceed the remaining output budget."));
            png = null;
          }
          cache.Add(path, png);
        }
        if (png is not null)
        {
          if (png.Png.Length > maxPreviewOutputBytes - previewOutputBytes)
          {
            diagnostics.Add(Warning(
              GltfDiagnosticCodes.TexturePreviewUnavailable,
              1109,
              recordIndex,
              "The decoded TEX previews exceed the remaining output budget."));
            continue;
          }
          previewOutputBytes += png.Png.Length;
          previews.Add(record.Id, png.Png);
        }
      }

      return new TexPreviewLoadResult(previews, diagnostics);
    }

    private static string? TryGetRelativePath(IReadOnlyList<byte> bytes)
    {
      if (bytes.Any(value => value is 0 or > 0x7F))
      {
        return null;
      }
      var value = Encoding.ASCII.GetString(bytes.ToArray());
      if (Path.IsPathRooted(value)
        || value.IndexOf('/') >= 0
        || value.IndexOf(':') >= 0)
      {
        return null;
      }
      var segments = value.Split('\\');
      return segments.Length > 0
        && segments.All(segment => segment.Length > 0 && segment is not "." and not "..")
          ? Path.Combine(segments)
          : null;
    }

    private static (string? Path, string? Root, bool Ambiguous, bool Shadowed) Resolve(
      string relativePath,
      IReadOnlyList<string> roots)
    {
      var segments = relativePath.Split(Path.DirectorySeparatorChar);
      string? selected = null;
      string? selectedRoot = null;
      var shadowed = false;
      foreach (var root in roots)
      {
        if (!Directory.Exists(root) || HasLinkInAncestry(root))
        {
          continue;
        }
        var candidates = new[] { root };
        foreach (var segment in segments)
        {
          candidates = candidates.SelectMany(current =>
              Directory.Exists(current)
                ? Directory.EnumerateFileSystemEntries(current)
                .Where(path => string.Equals(
                  Path.GetFileName(path),
                  segment,
                  StringComparison.OrdinalIgnoreCase))
                : Array.Empty<string>())
            .Where(path => !IsLink(path))
            .ToArray();
          if (candidates.Length == 0)
          {
            break;
          }
        }
        var completeMatches = candidates.Where(File.Exists).ToArray();
        if (completeMatches.Length == 0)
        {
          continue;
        }
        if (completeMatches.Length > 1)
        {
          if (selected is null)
          {
            return (null, null, true, false);
          }
          shadowed = true;
          continue;
        }
        if (selected is null)
        {
          selected = completeMatches[0];
          selectedRoot = root;
        }
        else
        {
          shadowed = true;
        }
      }
      return (selected, selectedRoot, false, shadowed);
    }

    private static bool IsLink(string path)
    {
      return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    }

    private static bool HasLinkInAncestry(string path)
    {
      for (DirectoryInfo? current = new DirectoryInfo(Path.GetFullPath(path));
        current is not null;
        current = current.Parent)
      {
        if (IsLink(current.FullName))
        {
          return true;
        }
      }
      return false;
    }

    private static DecodedPreview Decode(
      string path,
      string root,
      GltfOperationProfile profile,
      long remainingPixels)
    {
      using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      if (IsLink(path)
        || HasLinkInAncestry(path)
        || !IsContainedBy(root, path))
      {
        throw new InvalidDataException("The TEX resource escaped its configured search root.");
      }
      if (stream.Length > profile.MaxTextureBytes)
      {
        throw new InvalidDataException("The TEX resource exceeds the configured byte limit.");
      }
      using var reader = new BinaryReader(stream, Encoding.UTF8, true);
      if (!reader.ReadBytes(_identifier.Length).SequenceEqual(_identifier))
      {
        throw new InvalidDataException("The TEX resource header is invalid.");
      }
      var flags = (TexFlags)reader.ReadUInt32();
      var unsupportedLayout = TexFlags.Container
        | TexFlags.DamageStates
        | TexFlags.SideColors
        | TexFlags.Cursor
        | TexFlags.Lod;
      if (!flags.HasFlag(TexFlags.Rgba32)
        || !flags.HasFlag(TexFlags.Mipmap)
        || (flags & unsupportedLayout) != 0)
      {
        throw new NotSupportedException("This TEX preview layout is not supported safely.");
      }
      if (reader.ReadInt32() != 0x8888)
      {
        throw new InvalidDataException("The TEX image declaration is invalid.");
      }
      var width = reader.ReadInt32();
      var height = reader.ReadInt32();
      var pixels = checked((long)width * height);
      if (width <= 0
        || height <= 0
        || width > 32768
        || height > 32768
        || pixels > profile.MaxPreviewPixels
        || pixels > remainingPixels)
      {
        throw new InvalidDataException("The TEX preview exceeds the configured pixel limit.");
      }
      if (checked(stream.Position + pixels * 4) > stream.Length)
      {
        throw new EndOfStreamException("The TEX preview pixels are truncated.");
      }

      using var bitmap = new SKBitmap(
        width,
        height,
        SKColorType.Rgba8888,
        SKAlphaType.Unpremul);
      for (var y = 0; y < height; y++)
      {
        for (var x = 0; x < width; x++)
        {
          bitmap.SetPixel(x, y, new SKColor(
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte()));
        }
      }
      using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100)
        ?? throw new InvalidDataException("The TEX preview could not be encoded as PNG.");
      return new DecodedPreview(data.ToArray(), pixels);
    }

    private static OperationDiagnostic Warning(
      string code,
      int eventId,
      int recordIndex,
      string message)
    {
      return new OperationDiagnostic(
        code,
        eventId,
        DiagnosticSeverity.Warning,
        BindingPath(recordIndex),
        message);
    }

    private static string BindingPath(int recordIndex)
    {
      return $"StaticRenderObjectSequence[{recordIndex}].TexturePathBytes";
    }

    private static bool IsContainedBy(string root, string path)
    {
      var rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)
        + Path.DirectorySeparatorChar;
      return Path.GetFullPath(path).StartsWith(rootPath, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class DecodedPreview
    {
      internal byte[] Png { get; }

      internal long PixelCount { get; }

      internal DecodedPreview(byte[] png, long pixelCount)
      {
        Png = png;
        PixelCount = pixelCount;
      }
    }
  }
}
