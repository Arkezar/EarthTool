#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Expert;
using EarthTool.MSH.Operations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;

namespace EarthTool.GLTF.Internal
{
  internal sealed class ReferencedMeshPreview
  {
    internal IReadOnlyList<Vector3> Positions { get; }
    internal IReadOnlyList<Vector3> Normals { get; }
    internal IReadOnlyList<Vector2> TextureCoordinates { get; }
    internal IReadOnlyList<uint> Indices { get; }

    internal ReferencedMeshPreview(
      IReadOnlyList<Vector3> positions,
      IReadOnlyList<Vector3> normals,
      IReadOnlyList<Vector2> textureCoordinates,
      IReadOnlyList<uint> indices)
    {
      Positions = positions;
      Normals = normals;
      TextureCoordinates = textureCoordinates;
      Indices = indices;
    }
  }

  internal sealed class MshPreviewLoadResult
  {
    internal IReadOnlyDictionary<int, ReferencedMeshPreview> Previews { get; }
    internal IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    internal MshPreviewLoadResult(
      IReadOnlyDictionary<int, ReferencedMeshPreview> previews,
      IReadOnlyList<OperationDiagnostic> diagnostics)
    {
      Previews = previews;
      Diagnostics = diagnostics;
    }
  }

  internal static class MshPreviewLoader
  {
    internal static MshPreviewLoadResult Load(
      DynamicMeshAsset asset,
      GltfExportOptions options,
      GltfOperationProfile profile,
      CancellationToken cancellationToken)
    {
      if (options.MeshResourceSearchRoots.Count > profile.MaxMeshResourceSearchRoots)
      {
        throw new ResourceLimitException(
          options.MeshResourceSearchRoots.Count,
          profile.MaxMeshResourceSearchRoots);
      }

      var objects = new List<DynamicObject>();
      Flatten(asset.RootDynamicObject, 1, profile, objects);
      var previews = new Dictionary<int, ReferencedMeshPreview>();
      var diagnostics = new List<OperationDiagnostic>();
      var cache = new Dictionary<string, Resolution>(StringComparer.OrdinalIgnoreCase);
      var budget = new ResolutionBudget(profile);
      for (var index = 0; index < objects.Count; index++)
      {
        var extension = objects[index].Extension;
        if (extension.KnownEffectType != DynamicEffectType.ScalableObject)
        {
          continue;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var localId = options.DynamicObjectIds.Count == objects.Count
          ? options.DynamicObjectIds[index]
          : index + 1;
        var key = TryGetResourceKey(extension.MeshNameBytes);
        if (key is null)
        {
          diagnostics.Add(Warning(
            GltfDiagnosticCodes.MeshPreviewUnavailable,
            1022,
            localId,
            "The exact MSH binding cannot be used as a safe game resource key."));
          diagnostics.Add(Warning(
            GltfDiagnosticCodes.MeshDiagnosticPreviewUsed,
            1024,
            localId,
            "The unresolved MSH binding uses EarthTool's deterministic diagnostic preview."));
          previews.Add(localId, CreateDiagnosticPreview());
          continue;
        }

        if (!cache.TryGetValue(key, out var resolution))
        {
          resolution = ResolveAndLoad(
            key,
            options.MeshResourceSearchRoots,
            profile,
            budget,
            new[] { key },
            1);
          cache.Add(key, resolution);
        }
        AddDiagnostics(diagnostics, resolution, localId);
        previews.Add(localId, resolution.Preview ?? CreateDiagnosticPreview());
      }
      return new MshPreviewLoadResult(previews, diagnostics);
    }

    private static Resolution ResolveAndLoad(
      string key,
      IReadOnlyList<string> roots,
      GltfOperationProfile profile,
      ResolutionBudget budget,
      IReadOnlyList<string> chain,
      int depth)
    {
      if (depth > profile.MaxMeshResourceDepth)
      {
        throw new ResourceLimitException(depth, profile.MaxMeshResourceDepth);
      }
      budget.ConsumeResource();
      var relativePath = Path.Combine(
        new[] { "Meshes" }.Concat(key.Split('\\')).ToArray()) + ".msh";
      SafeResourceMatch match;
      try
      {
        match = SafeResourceLookup.Resolve(
          relativePath,
          roots,
          budget.EnumerateFileSystemEntries);
      }
      catch (ResourceLimitException)
      {
        throw;
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        return Resolution.Unavailable(
          false,
          "The referenced MSH resource lookup could not be completed safely.");
      }
      if (match.Ambiguous)
      {
        return Resolution.AmbiguousResource();
      }
      if (match.Path is null)
      {
        return Resolution.MissingResource();
      }

      try
      {
        if (!SafeResourceLookup.IsSafeContainedPath(match.Root!, match.Path))
        {
          throw new InvalidDataException("The MSH resource escaped its configured search root.");
        }
        using var stream = new FileStream(match.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (!SafeResourceLookup.IsSafeContainedPath(match.Root!, match.Path))
        {
          throw new InvalidDataException("The MSH resource changed identity while it was opened.");
        }
        if (stream.Length > profile.MaxMeshResourceBytes)
        {
          throw new ResourceLimitException(stream.Length, profile.MaxMeshResourceBytes);
        }
        budget.ConsumeBytes(stream.Length);
        var bytes = new byte[checked((int)stream.Length)];
        var offset = 0;
        while (offset < bytes.Length)
        {
          var read = stream.Read(bytes, offset, bytes.Length - offset);
          if (read == 0)
          {
            throw new EndOfStreamException("The referenced MSH resource is truncated.");
          }
          offset += read;
        }
        if (!SafeResourceLookup.IsSafeContainedPath(match.Root!, match.Path))
        {
          throw new InvalidDataException("The MSH resource changed identity while it was read.");
        }

        var mshProfile = new MshOperationProfile(
          maxInputBytes: profile.MaxMeshResourceBytes,
          maxOutputBytes: profile.MaxMeshResourceBytes,
          maxStaticVerticesPerObject: 65536);
        var parsed = MshExpert.CreateStatic(
          bytes,
          new MeshAssetLineageId(Guid.NewGuid()),
          mshProfile);
        if (parsed.TryGetValue(out var staticAsset))
        {
          return Resolution.Resolved(
            CreatePreview(staticAsset!, profile, budget),
            match.Shadowed);
        }
        var dynamic = MshExpert.CreateDynamic(
          bytes,
          new MeshAssetLineageId(Guid.NewGuid()),
          mshProfile);
        if (!dynamic.TryGetValue(out var dynamicAsset))
        {
          return Resolution.Unavailable(
            match.Shadowed,
            "The referenced MSH resource is malformed.");
        }
        return ContainsCycle(dynamicAsset!, roots, profile, budget, chain, depth)
          ? Resolution.CyclicResource(match.Shadowed)
          : Resolution.UnsupportedResource(match.Shadowed);
      }
      catch (ResourceLimitException)
      {
        throw;
      }
      catch (Exception ex) when (ex is IOException
        or EndOfStreamException
        or InvalidDataException
        or NotSupportedException
        or OverflowException
        or ArgumentException
        or UnauthorizedAccessException)
      {
        return Resolution.Unavailable(
          match.Shadowed,
          "The referenced MSH resource could not be read or decoded safely.");
      }
    }

    private static bool ContainsCycle(
      DynamicMeshAsset asset,
      IReadOnlyList<string> roots,
      GltfOperationProfile profile,
      ResolutionBudget budget,
      IReadOnlyList<string> chain,
      int depth)
    {
      var objects = new List<DynamicObject>();
      Flatten(asset.RootDynamicObject, 1, profile, objects);
      foreach (var extension in objects.Select(item => item.Extension)
        .Where(item => item.KnownEffectType == DynamicEffectType.ScalableObject))
      {
        var nestedKey = TryGetResourceKey(extension.MeshNameBytes);
        if (nestedKey is null)
        {
          continue;
        }
        if (chain.Any(item => SafeResourceLookup.AsciiEquals(item, nestedKey)))
        {
          return true;
        }
        var nested = ResolveAndLoad(
          nestedKey,
          roots,
          profile,
          budget,
          chain.Concat(new[] { nestedKey }).ToArray(),
          depth + 1);
        if (nested.Cyclic)
        {
          return true;
        }
      }
      return false;
    }

    private static ReferencedMeshPreview CreatePreview(
      StaticMeshAsset asset,
      GltfOperationProfile profile,
      ResolutionBudget budget)
    {
      var positions = new List<Vector3>();
      var normals = new List<Vector3>();
      var textureCoordinates = new List<Vector2>();
      var indices = new List<uint>();
      foreach (var renderObject in asset.StaticRenderObjectSequence)
      {
        var baseVertex = checked((uint)positions.Count);
        foreach (var vertex in renderObject.RenderVertices)
        {
          var projected = GlbDocument.ProjectToGltf(vertex);
          if (!IsFinite(projected.Position) || !IsFinite(projected.Normal)
            || !float.IsFinite(projected.TextureCoordinate.X)
            || !float.IsFinite(projected.TextureCoordinate.Y))
          {
            throw new InvalidDataException("The referenced MSH contains non-finite preview geometry.");
          }
          positions.Add(projected.Position);
          normals.Add(projected.Normal);
          textureCoordinates.Add(projected.TextureCoordinate);
        }
        if (positions.Count > profile.MaxMeshPreviewVertices)
        {
          throw new ResourceLimitException(positions.Count, profile.MaxMeshPreviewVertices);
        }
        foreach (var triangle in renderObject.Triangles)
        {
          indices.Add(checked(baseVertex + triangle.Vertex0));
          indices.Add(checked(baseVertex + triangle.Vertex1));
          indices.Add(checked(baseVertex + triangle.Vertex2));
        }
      }
      if (positions.Count == 0 || indices.Count == 0)
      {
        throw new InvalidDataException("The referenced MSH has no renderable static geometry.");
      }
      budget.ConsumeVertices(positions.Count);
      return new ReferencedMeshPreview(
        positions.AsReadOnly(),
        normals.AsReadOnly(),
        textureCoordinates.AsReadOnly(),
        indices.AsReadOnly());
    }

    internal static ReferencedMeshPreview CreateDiagnosticPreview()
    {
      return new ReferencedMeshPreview(
        Array.AsReadOnly(new[]
        {
          new Vector3(-0.5f, 0, 0),
          new Vector3(0.5f, 0, 0),
          new Vector3(0, 1, 0)
        }),
        Array.AsReadOnly(new[] { Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ }),
        Array.AsReadOnly(new[] { Vector2.Zero, Vector2.UnitX, Vector2.UnitY }),
        Array.AsReadOnly(new uint[] { 0, 1, 2 }));
    }

    private static void AddDiagnostics(
      ICollection<OperationDiagnostic> diagnostics,
      Resolution resolution,
      int localId)
    {
      if (resolution.Ambiguous)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.AmbiguousMeshResource,
          1021,
          localId,
          "The MSH resource key has more than one ASCII case-insensitive match in the winning root."));
      }
      if (resolution.Missing)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.MeshResourceMissing,
          1020,
          localId,
          "The explicit MSH resource binding was not found in the configured roots."));
      }
      if (resolution.Shadowed)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.MeshResourceShadowed,
          1023,
          localId,
          "A later MSH search root contains a shadowed match for this preview resource."));
      }
      if (resolution.Unsupported)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.UnsupportedMeshResource,
          1025,
          localId,
          "The referenced MSH is dynamic and cannot be borrowed as static preview geometry."));
      }
      if (resolution.Cyclic)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.MeshResourceCycle,
          1026,
          localId,
          "The referenced dynamic MSH resource chain contains a cycle."));
      }
      if (resolution.UnavailableReason is not null)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.MeshPreviewUnavailable,
          1022,
          localId,
          resolution.UnavailableReason));
      }
      if (resolution.Preview is null)
      {
        diagnostics.Add(Warning(
          GltfDiagnosticCodes.MeshDiagnosticPreviewUsed,
          1024,
          localId,
          "The unresolved MSH binding uses EarthTool's deterministic diagnostic preview."));
      }
    }

    private static OperationDiagnostic Warning(
      string code,
      int eventId,
      int localId,
      string message)
    {
      return new OperationDiagnostic(
        code,
        eventId,
        DiagnosticSeverity.Warning,
        $"DynamicObjectScopes[{localId}].Extension.MeshNameBytes",
        message);
    }

    private static string? TryGetResourceKey(IReadOnlyList<byte> bytes)
    {
      if (bytes.Count == 0 || bytes.Any(value => value is 0 or > 0x7F))
      {
        return null;
      }
      var value = Encoding.ASCII.GetString(bytes.ToArray());
      if (Path.IsPathRooted(value) || value.IndexOf('/') >= 0 || value.IndexOf(':') >= 0)
      {
        return null;
      }
      var segments = value.Split('\\');
      return segments.All(segment => segment.Length > 0 && segment is not "." and not "..")
        ? value
        : null;
    }

    private static bool IsFinite(Vector3 value)
    {
      return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }

    private static void Flatten(
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
        Flatten(child, depth + 1, profile, result);
      }
    }

    private sealed class ResolutionBudget
    {
      private readonly int _maximumDirectoryEntries;
      private long _remainingBytes;
      private int _remainingResources;
      private int _remainingVertices;
      private int _examinedDirectoryEntries;

      internal ResolutionBudget(GltfOperationProfile profile)
      {
        _maximumDirectoryEntries = profile.MaxMeshResourceDirectoryEntries;
        _remainingBytes = profile.MaxMeshResourceBytes;
        _remainingResources = profile.MaxMeshResources;
        _remainingVertices = profile.MaxMeshPreviewVertices;
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

      internal void ConsumeBytes(long count)
      {
        if (count > _remainingBytes)
        {
          throw new ResourceLimitException(count, checked((int)_remainingBytes));
        }
        _remainingBytes -= count;
      }

      internal void ConsumeResource()
      {
        if (_remainingResources == 0)
        {
          throw new ResourceLimitException(1, 0);
        }
        _remainingResources--;
      }

      internal void ConsumeVertices(int count)
      {
        if (count > _remainingVertices)
        {
          throw new ResourceLimitException(count, _remainingVertices);
        }
        _remainingVertices -= count;
      }
    }

    private sealed class Resolution
    {
      internal ReferencedMeshPreview? Preview { get; }
      internal bool Ambiguous { get; }
      internal bool Missing { get; }
      internal bool Shadowed { get; }
      internal bool Unsupported { get; }
      internal bool Cyclic { get; }
      internal string? UnavailableReason { get; }

      private Resolution(
        ReferencedMeshPreview? preview,
        bool ambiguous,
        bool missing,
        bool shadowed,
        bool unsupported,
        bool cyclic,
        string? unavailableReason)
      {
        Preview = preview;
        Ambiguous = ambiguous;
        Missing = missing;
        Shadowed = shadowed;
        Unsupported = unsupported;
        Cyclic = cyclic;
        UnavailableReason = unavailableReason;
      }

      internal static Resolution Resolved(ReferencedMeshPreview preview, bool shadowed)
      {
        return new Resolution(preview, false, false, shadowed, false, false, null);
      }

      internal static Resolution MissingResource()
      {
        return new Resolution(null, false, true, false, false, false, null);
      }

      internal static Resolution AmbiguousResource()
      {
        return new Resolution(null, true, false, false, false, false, null);
      }

      internal static Resolution UnsupportedResource(bool shadowed)
      {
        return new Resolution(null, false, false, shadowed, true, false, null);
      }

      internal static Resolution CyclicResource(bool shadowed)
      {
        return new Resolution(null, false, false, shadowed, false, true, null);
      }

      internal static Resolution Unavailable(bool shadowed, string reason)
      {
        return new Resolution(null, false, false, shadowed, false, false, reason);
      }
    }
  }
}
