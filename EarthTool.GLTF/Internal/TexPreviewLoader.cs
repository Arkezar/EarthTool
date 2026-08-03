#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.TEX;
using SkiaSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace EarthTool.GLTF.Internal
{
  internal sealed class TexPreview
  {
    internal byte[] Png { get; }

    internal string ContentAddress { get; }

    internal TexPreview(byte[] png, string contentAddress)
    {
      Png = png;
      ContentAddress = contentAddress;
    }
  }

  internal sealed class TexPreviewLoadResult
  {
    internal IReadOnlyDictionary<StaticRenderObjectId, TexPreview> Previews { get; }

    internal IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    internal bool HasErrors => Diagnostics.Any(diagnostic =>
      diagnostic.Severity == DiagnosticSeverity.Error);

    internal TexPreviewLoadResult(
      IReadOnlyDictionary<StaticRenderObjectId, TexPreview> previews,
      IReadOnlyList<OperationDiagnostic> diagnostics)
    {
      Previews = previews;
      Diagnostics = diagnostics;
    }
  }

  internal sealed class DynamicTexPreviewLoadResult
  {
    internal IReadOnlyDictionary<int, TexPreview> Previews { get; }
    internal IReadOnlyList<OperationDiagnostic> Diagnostics { get; }
    internal bool HasErrors => Diagnostics.Any(diagnostic =>
      diagnostic.Severity == DiagnosticSeverity.Error);

    internal DynamicTexPreviewLoadResult(
      IReadOnlyDictionary<int, TexPreview> previews,
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
      if (options.TextureSearchRoots.Count > profile.MaxTextureSearchRoots)
      {
        throw new ResourceLimitException(
          options.TextureSearchRoots.Count,
          profile.MaxTextureSearchRoots);
      }
      var previews = new Dictionary<StaticRenderObjectId, TexPreview>();
      var diagnostics = new List<OperationDiagnostic>();
      var cache = new Dictionary<string, PreviewResolution>(StringComparer.OrdinalIgnoreCase);
      var previewOutputBytes = 0;
      var budget = new TexResolutionBudget(profile);
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
        if (!cache.TryGetValue(path, out var resolution))
        {
          resolution = LoadPreview(
            path,
            options.TextureSearchRoots,
            profile,
            budget);
          cache.Add(path, resolution);
        }

        var preview = resolution.Preview;
        if (preview is not null)
        {
          var isNewPreview = !previews.Values.Any(existing =>
            existing.ContentAddress == preview.ContentAddress);
          if (isNewPreview && preview.Png.Length > maxPreviewOutputBytes - previewOutputBytes)
          {
            AddDiagnostics(diagnostics, resolution, recordIndex, false);
            diagnostics.Add(Warning(
              GltfDiagnosticCodes.TexturePreviewUnavailable,
              1109,
              recordIndex,
              "The decoded TEX previews exceed the remaining output budget."));
            continue;
          }
          if (isNewPreview)
          {
            previewOutputBytes += preview.Png.Length;
          }
          AddDiagnostics(diagnostics, resolution, recordIndex, true);
          previews.Add(record.Id, preview);
        }
        else
        {
          AddDiagnostics(diagnostics, resolution, recordIndex, false);
        }
      }

      return new TexPreviewLoadResult(previews, diagnostics);
    }

    internal static DynamicTexPreviewLoadResult Load(
      DynamicMeshAsset asset,
      GltfExportOptions options,
      GltfOperationProfile profile,
      int maxPreviewOutputBytes,
      CancellationToken cancellationToken)
    {
      if (options.TextureSearchRoots.Count > profile.MaxTextureSearchRoots)
      {
        throw new ResourceLimitException(
          options.TextureSearchRoots.Count,
          profile.MaxTextureSearchRoots);
      }
      var previews = new Dictionary<int, TexPreview>();
      var diagnostics = new List<OperationDiagnostic>();
      var cache = new Dictionary<string, PreviewResolution>(StringComparer.OrdinalIgnoreCase);
      var previewOutputBytes = 0;
      var budget = new TexResolutionBudget(profile);
      var objects = new List<DynamicObject>();
      FlattenDynamic(asset.RootDynamicObject, 1, profile, objects);
      for (var index = 0; index < objects.Count; index++)
      {
        var extension = objects[index].Extension;
        var localId = options.DynamicObjectIds.Count == objects.Count
          ? options.DynamicObjectIds[index]
          : index + 1;
        cancellationToken.ThrowIfCancellationRequested();
        if (!DynamicGltfDocument.HasNativePreview(extension.KnownEffectType))
        {
          continue;
        }
        if (extension.TexturePathBytes.Count == 0)
        {
          diagnostics.Add(DynamicWarning(
            GltfDiagnosticCodes.TexturePreviewUnavailable,
            1109,
            localId,
            "The sprite effect has no TEX resource binding to preview."));
          continue;
        }
        var path = TryGetRelativePath(extension.TexturePathBytes);
        if (path is null)
        {
          diagnostics.Add(DynamicWarning(
            GltfDiagnosticCodes.TexturePreviewUnavailable,
            1109,
            localId,
            "The exact TEX binding cannot be used as a safe host resource path."));
          continue;
        }
        if (!cache.TryGetValue(path, out var resolution))
        {
          resolution = LoadPreview(path, options.TextureSearchRoots, profile, budget);
          cache.Add(path, resolution);
        }
        var preview = resolution.Preview;
        if (preview is null)
        {
          AddDynamicDiagnostics(diagnostics, resolution, localId, false);
          continue;
        }
        var isNewPreview = !previews.Values.Any(existing =>
          existing.ContentAddress == preview.ContentAddress);
        if (isNewPreview && preview.Png.Length > maxPreviewOutputBytes - previewOutputBytes)
        {
          AddDynamicDiagnostics(diagnostics, resolution, localId, false);
          diagnostics.Add(DynamicWarning(
            GltfDiagnosticCodes.TexturePreviewUnavailable,
            1109,
            localId,
            "The decoded TEX previews exceed the remaining output budget."));
          continue;
        }
        if (isNewPreview)
        {
          previewOutputBytes += preview.Png.Length;
        }
        AddDynamicDiagnostics(diagnostics, resolution, localId, true);
        previews.Add(localId, preview);
      }
      return new DynamicTexPreviewLoadResult(previews, diagnostics);
    }

    private static void FlattenDynamic(
      DynamicObject item,
      int depth,
      GltfOperationProfile profile,
      ICollection<DynamicObject> result)
    {
      if (depth > profile.MaxHierarchyDepth)
      {
        throw new ResourceLimitException(depth, profile.MaxHierarchyDepth);
      }
      if (result.Count == profile.MaxNodes)
      {
        throw new ResourceLimitException(result.Count + 1, profile.MaxNodes);
      }
      result.Add(item);
      foreach (var child in item.Children)
      {
        FlattenDynamic(child, depth + 1, profile, result);
      }
    }

    private static void AddDynamicDiagnostics(
      ICollection<OperationDiagnostic> diagnostics,
      PreviewResolution resolution,
      int localId,
      bool previewEmitted)
    {
      if (resolution.Ambiguous)
      {
        diagnostics.Add(new OperationDiagnostic(
          GltfDiagnosticCodes.AmbiguousTextureResource,
          1108,
          DiagnosticSeverity.Error,
          DynamicBindingPath(localId),
          "The TEX resource key has more than one case-insensitive match in the winning root."));
        return;
      }
      if (resolution.Missing)
      {
        diagnostics.Add(DynamicWarning(
          GltfDiagnosticCodes.TextureResourceMissing,
          1107,
          localId,
          "The explicit TEX resource binding was not found in the configured roots."));
      }
      if (resolution.Shadowed)
      {
        diagnostics.Add(DynamicWarning(
          GltfDiagnosticCodes.TextureResourceShadowed,
          1110,
          localId,
          "A later TEX search root contains a shadowed match for this preview resource."));
      }
      if (resolution.DefaultUsed && previewEmitted)
      {
        diagnostics.Add(DynamicWarning(
          GltfDiagnosticCodes.TextureDefaultPreviewUsed,
          1111,
          localId,
          "The unresolved TEX binding uses the runtime default resource as its preview."));
      }
      if (resolution.DiagnosticUsed && previewEmitted)
      {
        diagnostics.Add(DynamicWarning(
          GltfDiagnosticCodes.TextureDiagnosticPreviewUsed,
          1112,
          localId,
          "The unresolved TEX binding uses EarthTool's deterministic diagnostic preview."));
      }
      if (resolution.HasVariants && previewEmitted)
      {
        diagnostics.Add(DynamicWarning(
          GltfDiagnosticCodes.TextureVariantsNotRepresented,
          1113,
          localId,
          "The decoded preview represents only the first highest-resolution TEX image."));
      }
      if (resolution.UnavailableReason is not null)
      {
        diagnostics.Add(DynamicWarning(
          GltfDiagnosticCodes.TexturePreviewUnavailable,
          1109,
          localId,
          resolution.UnavailableReason));
      }
    }

    private static OperationDiagnostic DynamicWarning(
      string code,
      int eventId,
      int localId,
      string message)
    {
      return new OperationDiagnostic(
        code,
        eventId,
        DiagnosticSeverity.Warning,
        DynamicBindingPath(localId),
        message);
    }

    private static string DynamicBindingPath(int localId)
    {
      return $"DynamicObjectScopes[{localId}].Extension.TexturePathBytes";
    }

    private static PreviewResolution LoadPreview(
      string relativePath,
      IReadOnlyList<string> roots,
      GltfOperationProfile profile,
      TexResolutionBudget budget)
    {
      var match = Resolve(relativePath, roots, budget);
      if (match.Ambiguous)
      {
        return PreviewResolution.AmbiguousResource();
      }
      if (match.Path is not null)
      {
        try
        {
          var decoded = Decode(match.Path, match.Root!, profile, budget);
          return PreviewResolution.Resolved(decoded.Preview, match.Shadowed, decoded.HasVariants);
        }
        catch (Exception ex) when (IsPreviewFailure(ex))
        {
          return PreviewResolution.Unavailable(match.Shadowed, ex.Message);
        }
      }

      var defaultMatch = Resolve(Path.Combine("Textures", "Default.tex"), roots, budget);
      if (defaultMatch.Ambiguous)
      {
        return PreviewResolution.AmbiguousResource();
      }
      if (defaultMatch.Path is not null)
      {
        try
        {
          var decoded = Decode(defaultMatch.Path, defaultMatch.Root!, profile, budget);
          return PreviewResolution.Default(
            decoded.Preview,
            defaultMatch.Shadowed,
            decoded.HasVariants);
        }
        catch (Exception ex) when (IsPreviewFailure(ex))
        {
          return CreateDiagnosticResolution(budget, ex.Message);
        }
      }

      return CreateDiagnosticResolution(budget, null);
    }

    private static PreviewResolution CreateDiagnosticResolution(
      TexResolutionBudget budget,
      string? unavailableReason)
    {
      if (!budget.TryConsumePixels(4))
      {
        return PreviewResolution.DiagnosticUnavailable(
          unavailableReason ?? "The diagnostic TEX preview exceeds the configured pixel limit.");
      }
      return PreviewResolution.Diagnostic(CreateDiagnosticPreview(), unavailableReason);
    }

    private static bool IsPreviewFailure(Exception exception)
    {
      return exception is IOException
        or EndOfStreamException
        or InvalidDataException
        or NotSupportedException
        or OverflowException
        or ArgumentException;
    }

    private static void AddDiagnostics(
      ICollection<OperationDiagnostic> diagnostics,
      PreviewResolution resolution,
      int recordIndex,
      bool previewEmitted)
    {
      if (resolution.Ambiguous)
      {
        diagnostics.Add(new OperationDiagnostic(
          GltfDiagnosticCodes.AmbiguousTextureResource,
          1108,
          DiagnosticSeverity.Error,
          BindingPath(recordIndex),
          "The TEX resource key has more than one case-insensitive match in the winning root."));
        return;
      }
      if (resolution.Missing)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.TextureResourceMissing,
          1107,
          recordIndex,
          "The explicit TEX resource binding was not found in the configured roots."));
      }
      if (resolution.Shadowed)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.TextureResourceShadowed,
          1110,
          recordIndex,
          "A later TEX search root contains a shadowed match for this preview resource."));
      }
      if (resolution.DefaultUsed && previewEmitted)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.TextureDefaultPreviewUsed,
          1111,
          recordIndex,
          "The unresolved TEX binding uses the runtime default resource as its preview."));
      }
      if (resolution.DiagnosticUsed && previewEmitted)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.TextureDiagnosticPreviewUsed,
          1112,
          recordIndex,
          "The unresolved TEX binding uses EarthTool's deterministic diagnostic preview."));
      }
      if (resolution.HasVariants && previewEmitted)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.TextureVariantsNotRepresented,
          1113,
          recordIndex,
          "The decoded preview represents only the first highest-resolution TEX image."));
      }
      if (resolution.UnavailableReason is not null)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.TexturePreviewUnavailable,
          1109,
          recordIndex,
          resolution.UnavailableReason));
      }
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
      IReadOnlyList<string> roots,
      TexResolutionBudget budget)
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
                ? budget.EnumerateFileSystemEntries(current)
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

    private static (TexPreview Preview, bool HasVariants) Decode(
      string path,
      string root,
      GltfOperationProfile profile,
      TexResolutionBudget budget)
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
      budget.ConsumeTextureBytes(stream.Length);
      using var reader = new BinaryReader(stream, Encoding.UTF8, true);
      if (!reader.ReadBytes(_identifier.Length).SequenceEqual(_identifier))
      {
        throw new InvalidDataException("The TEX resource header is invalid.");
      }
      var header = ReadHeader(reader);
      var hasVariants = IsVariant(header.Flags);
      if (IsMultiImage(header.Flags))
      {
        if (!reader.ReadBytes(_identifier.Length).SequenceEqual(_identifier))
        {
          throw new InvalidDataException("The first TEX variant header is invalid.");
        }
        header = ReadHeader(reader);
      }
      if (!header.Flags.HasFlag(TexFlags.Rgba32)
        || !header.Flags.HasFlag(TexFlags.Mipmap))
      {
        throw new NotSupportedException("This TEX preview layout is not supported safely.");
      }
      var width = header.Width;
      var height = header.Height;
      var pixels = checked((long)width * height);
      if (width <= 0
        || height <= 0
        || width > 32768
        || height > 32768
        || pixels > profile.MaxPreviewPixels
        || !budget.TryConsumePixels(pixels))
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
      var rgba = new byte[checked((int)pixels * 4)];
      var pixelOffset = 0;
      for (var y = 0; y < height; y++)
      {
        for (var x = 0; x < width; x++)
        {
          var red = reader.ReadByte();
          var green = reader.ReadByte();
          var blue = reader.ReadByte();
          var alpha = reader.ReadByte();
          rgba[pixelOffset++] = red;
          rgba[pixelOffset++] = green;
          rgba[pixelOffset++] = blue;
          rgba[pixelOffset++] = alpha;
          bitmap.SetPixel(x, y, new SKColor(red, green, blue, alpha));
        }
      }
      using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100)
        ?? throw new InvalidDataException("The TEX preview could not be encoded as PNG.");
      return (new TexPreview(data.ToArray(), GetContentAddress(width, height, rgba)), hasVariants);
    }

    private static TexHeader ReadHeader(BinaryReader reader)
    {
      var flags = (TexFlags)reader.ReadUInt32();
      var count = 1;
      if (flags.HasFlag(TexFlags.DamageStates))
      {
        count = reader.ReadInt32();
      }
      if (flags.HasFlag(TexFlags.Container) || flags.HasFlag(TexFlags.SideColors))
      {
        count = checked(count * reader.ReadInt32());
      }
      if (count <= 0)
      {
        throw new InvalidDataException("The TEX image count is invalid.");
      }

      var width = 0;
      var height = 0;
      if (flags.HasFlag(TexFlags.Mipmap) && !flags.HasFlag(TexFlags.DamageStates))
      {
        if (reader.ReadInt32() != 0x8888)
        {
          throw new InvalidDataException("The TEX image declaration is invalid.");
        }
        width = reader.ReadInt32();
        height = reader.ReadInt32();
      }
      if (flags.HasFlag(TexFlags.Cursor))
      {
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
      }
      if (flags.HasFlag(TexFlags.Lod))
      {
        var lodCount = reader.ReadInt32();
        if (lodCount < 0)
        {
          throw new InvalidDataException("The TEX mip count is invalid.");
        }
      }
      return new TexHeader(flags, width, height);
    }

    private static bool IsMultiImage(TexFlags flags)
    {
      return flags == TexFlags.None
        || (flags & (TexFlags.Container | TexFlags.DamageStates | TexFlags.SideColors)) != 0;
    }

    private static bool IsVariant(TexFlags flags)
    {
      const TexFlags variants = TexFlags.Container
        | TexFlags.DamageStates
        | TexFlags.SideColors
        | TexFlags.Animated
        | TexFlags.Special;
      return flags == TexFlags.None || (flags & variants) != 0;
    }

    private static TexPreview CreateDiagnosticPreview()
    {
      var rgba = new byte[]
      {
        0xFF, 0, 0xFF, 0xFF,
        0, 0, 0, 0xFF,
        0, 0, 0, 0xFF,
        0xFF, 0, 0xFF, 0xFF
      };
      using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Unpremul);
      for (var index = 0; index < 4; index++)
      {
        var offset = index * 4;
        bitmap.SetPixel(
          index % 2,
          index / 2,
          new SKColor(rgba[offset], rgba[offset + 1], rgba[offset + 2], rgba[offset + 3]));
      }
      using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100)
        ?? throw new InvalidDataException("The diagnostic preview could not be encoded as PNG.");
      return new TexPreview(data.ToArray(), GetContentAddress(2, 2, rgba));
    }

    private static string GetContentAddress(int width, int height, byte[] rgba)
    {
      var preimage = new byte[checked(sizeof(int) * 2 + rgba.Length)];
      BinaryPrimitives.WriteInt32LittleEndian(preimage, width);
      BinaryPrimitives.WriteInt32LittleEndian(preimage.AsSpan(sizeof(int)), height);
      rgba.CopyTo(preimage, sizeof(int) * 2);
      using var sha256 = SHA256.Create();
      return BitConverter.ToString(sha256.ComputeHash(preimage)).Replace("-", string.Empty)
        .ToLowerInvariant();
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

    private sealed class TexResolutionBudget
    {
      private readonly int _maximumDirectoryEntries;
      private long _remainingTextureBytes;
      private long _remainingPixels;
      private int _examinedDirectoryEntries;

      internal TexResolutionBudget(GltfOperationProfile profile)
      {
        _remainingTextureBytes = profile.MaxTextureBytes;
        _remainingPixels = profile.MaxPreviewPixels;
        _maximumDirectoryEntries = profile.MaxTextureDirectoryEntries;
      }

      internal IEnumerable<string> EnumerateFileSystemEntries(string path)
      {
        foreach (var entry in Directory.EnumerateFileSystemEntries(path))
        {
          _examinedDirectoryEntries = checked(_examinedDirectoryEntries + 1);
          if (_examinedDirectoryEntries > _maximumDirectoryEntries)
          {
            throw new ResourceLimitException(
              _examinedDirectoryEntries,
              _maximumDirectoryEntries);
          }
          yield return entry;
        }
      }

      internal void ConsumeTextureBytes(long count)
      {
        if (count > _remainingTextureBytes)
        {
          throw new InvalidDataException("The TEX resources exceed the aggregate byte limit.");
        }
        _remainingTextureBytes -= count;
      }

      internal bool TryConsumePixels(long count)
      {
        if (count > _remainingPixels)
        {
          return false;
        }
        _remainingPixels -= count;
        return true;
      }
    }

    private readonly struct TexHeader
    {
      internal TexFlags Flags { get; }

      internal int Width { get; }

      internal int Height { get; }

      internal TexHeader(TexFlags flags, int width, int height)
      {
        Flags = flags;
        Width = width;
        Height = height;
      }
    }

    private enum PreviewResolutionKind
    {
      Resolved,
      MissingDefault,
      MissingDiagnostic,
      MissingUnavailable,
      Ambiguous,
      Unavailable
    }

    private sealed class PreviewResolution
    {
      private PreviewResolutionKind Kind { get; }

      internal TexPreview? Preview { get; }

      internal bool Missing => Kind is PreviewResolutionKind.MissingDefault
        or PreviewResolutionKind.MissingDiagnostic
        or PreviewResolutionKind.MissingUnavailable;

      internal bool Ambiguous => Kind == PreviewResolutionKind.Ambiguous;

      internal bool Shadowed { get; }

      internal bool DefaultUsed => Kind == PreviewResolutionKind.MissingDefault;

      internal bool DiagnosticUsed => Kind == PreviewResolutionKind.MissingDiagnostic;

      internal bool HasVariants { get; }

      internal string? UnavailableReason { get; }

      private PreviewResolution(
        PreviewResolutionKind kind,
        TexPreview? preview,
        bool shadowed,
        bool hasVariants,
        string? unavailableReason)
      {
        Kind = kind;
        Preview = preview;
        Shadowed = shadowed;
        HasVariants = hasVariants;
        UnavailableReason = unavailableReason;
      }

      internal static PreviewResolution Resolved(
        TexPreview preview,
        bool shadowed,
        bool hasVariants)
      {
        return new PreviewResolution(
          PreviewResolutionKind.Resolved,
          preview,
          shadowed,
          hasVariants,
          null);
      }

      internal static PreviewResolution Default(
        TexPreview preview,
        bool shadowed,
        bool hasVariants)
      {
        return new PreviewResolution(
          PreviewResolutionKind.MissingDefault,
          preview,
          shadowed,
          hasVariants,
          null);
      }

      internal static PreviewResolution Diagnostic(TexPreview preview, string? unavailableReason)
      {
        return new PreviewResolution(
          PreviewResolutionKind.MissingDiagnostic,
          preview,
          false,
          false,
          unavailableReason);
      }

      internal static PreviewResolution DiagnosticUnavailable(string reason)
      {
        return new PreviewResolution(
          PreviewResolutionKind.MissingUnavailable,
          null,
          false,
          false,
          reason);
      }

      internal static PreviewResolution AmbiguousResource()
      {
        return new PreviewResolution(
          PreviewResolutionKind.Ambiguous,
          null,
          false,
          false,
          null);
      }

      internal static PreviewResolution Unavailable(bool shadowed, string reason)
      {
        return new PreviewResolution(
          PreviewResolutionKind.Unavailable,
          null,
          shadowed,
          false,
          reason);
      }

    }
  }
}
