#nullable enable

using EarthTool.Common.Operations;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Services;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.GLTF
{
  /// <summary>Provides the sealed static MSH and GLB interchange facade.</summary>
  public sealed class GltfInterchange
  {
    private readonly ITransactionalFileSystem _fileSystem;

    /// <summary>Initializes the facade using the platform filesystem.</summary>
    public GltfInterchange()
      : this(new TransactionalFileSystem())
    {
    }

    internal GltfInterchange(ITransactionalFileSystem fileSystem)
    {
      _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <summary>Exports one supported static asset as a strictly validated GLB.</summary>
    public async Task<OperationResult<GltfExportReceipt>> ExportGlbAsync(
      StaticMeshAsset asset,
      Stream destination,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (asset is null)
      {
        throw new ArgumentNullException(nameof(asset));
      }

      if (destination is null)
      {
        throw new ArgumentNullException(nameof(destination));
      }

      profile ??= GltfOperationProfile.Default;
      options ??= new GltfExportOptions();
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        var unsupported = ValidateAssetProfile(asset, profile);
        if (unsupported is not null)
        {
          return Failed<GltfExportReceipt>(unsupported);
        }

        var baseline = new InterchangeBaseline(
          options.AssetLineageId ?? Guid.NewGuid(),
          options.DocumentId ?? Guid.NewGuid());
        var metadataLength = GlbDocument.GetManifestMetadataByteCount(asset, baseline);
        if (metadataLength > profile.MaxMetadataBytes)
        {
          return Failed<GltfExportReceipt>(Limit("scenes[0].extras.earthtool", metadataLength, profile.MaxMetadataBytes));
        }

        var minimumOutputLength = GlbDocument.GetMinimumOutputByteCount(asset, baseline, true);
        if (minimumOutputLength > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", minimumOutputLength, profile.MaxOutputBytes));
        }

        var glb = GlbDocument.Create(asset, baseline, out var fingerprint);
        if (glb.Length > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", glb.Length, profile.MaxOutputBytes));
        }

        GlbDocument.Validate(glb, profile.MaxJsonDepth);
        cancellationToken.ThrowIfCancellationRequested();
        await destination.WriteAsync(glb, 0, glb.Length, cancellationToken).ConfigureAwait(false);
        return new OperationResult<GltfExportReceipt>(
          OperationStatus.Succeeded,
          new GltfExportReceipt(baseline, fingerprint));
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfExportReceipt>();
      }
      catch (Exception ex)
      {
        return Failed<GltfExportReceipt>(ToDiagnostic(ex));
      }
    }

    /// <summary>Transactionally exports one supported static asset to a GLB file.</summary>
    public async Task<OperationResult<GltfExportReceipt>> ExportGlbFileAsync(
      StaticMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (destinationPath is null)
      {
        throw new ArgumentNullException(nameof(destinationPath));
      }

      var temporaryPath = _fileSystem.GetTemporaryPath(destinationPath);
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        OperationResult<GltfExportReceipt> result;
        using (var temporary = _fileSystem.CreateTemporary(temporaryPath))
        {
          result = await ExportGlbAsync(asset, temporary, options, profile, cancellationToken).ConfigureAwait(false);
          if (!result.Succeeded)
          {
            return result;
          }

          await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        _fileSystem.Commit(temporaryPath, destinationPath);
        return result;
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfExportReceipt>();
      }
      catch (Exception ex)
      {
        return Failed<GltfExportReceipt>(ToDiagnostic(ex, destinationPath));
      }
      finally
      {
        _fileSystem.TryDelete(temporaryPath);
      }
    }

    /// <summary>Transactionally exports one supported static asset as separate glTF and buffer files.</summary>
    public async Task<OperationResult<GltfExportReceipt>> ExportGltfFileAsync(
      StaticMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (asset is null)
      {
        throw new ArgumentNullException(nameof(asset));
      }

      if (destinationPath is null)
      {
        throw new ArgumentNullException(nameof(destinationPath));
      }

      profile ??= GltfOperationProfile.Default;
      options ??= new GltfExportOptions();
      var manifestTemporaryPath = _fileSystem.GetTemporaryPath(destinationPath);
      string? bufferTemporaryPath = null;
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        var unsupported = ValidateAssetProfile(asset, profile);
        if (unsupported is not null)
        {
          return Failed<GltfExportReceipt>(unsupported);
        }

        var baseline = new InterchangeBaseline(
          options.AssetLineageId ?? Guid.NewGuid(),
          options.DocumentId ?? Guid.NewGuid());
        var metadataLength = GlbDocument.GetManifestMetadataByteCount(asset, baseline);
        if (metadataLength > profile.MaxMetadataBytes)
        {
          return Failed<GltfExportReceipt>(Limit("scenes[0].extras.earthtool", metadataLength, profile.MaxMetadataBytes));
        }

        var minimumOutputLength = GlbDocument.GetMinimumOutputByteCount(asset, baseline, false);
        if (minimumOutputLength > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", minimumOutputLength, profile.MaxOutputBytes));
        }

        var package = GlbDocument.CreateSeparate(asset, baseline, out var fingerprint);
        var outputLength = checked(package.Json.Length + package.Binary.Length);
        if (outputLength > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", outputLength, profile.MaxOutputBytes));
        }

        ValidateGeometryProfile(
          GlbDocument.ParseSeparate(package.Json, package.Binary, profile.MaxJsonDepth),
          profile);
        GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferFileName);
        var directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath))
          ?? Directory.GetCurrentDirectory();
        var bufferPath = Path.Combine(directory, package.BufferFileName);
        if (string.Equals(
          Path.GetFullPath(destinationPath),
          Path.GetFullPath(bufferPath),
          StringComparison.OrdinalIgnoreCase))
        {
          throw new IOException("The glTF manifest path collides with its content-addressed buffer.");
        }

        if (File.Exists(bufferPath))
        {
          if (!HasSameContent(bufferPath, package.Binary))
          {
            throw new IOException("A content-addressed glTF buffer has conflicting content.");
          }
        }
        else
        {
          bufferTemporaryPath = _fileSystem.GetTemporaryPath(bufferPath);
          using (var temporary = _fileSystem.CreateTemporary(bufferTemporaryPath))
          {
            await temporary.WriteAsync(
              package.Binary,
              0,
              package.Binary.Length,
              cancellationToken).ConfigureAwait(false);
            await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
          }

          cancellationToken.ThrowIfCancellationRequested();
          _fileSystem.Commit(bufferTemporaryPath, bufferPath);
          bufferTemporaryPath = null;
        }

        using (var temporary = _fileSystem.CreateTemporary(manifestTemporaryPath))
        {
          await temporary.WriteAsync(
            package.Json,
            0,
            package.Json.Length,
            cancellationToken).ConfigureAwait(false);
          await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        _fileSystem.Commit(manifestTemporaryPath, destinationPath);
        return new OperationResult<GltfExportReceipt>(
          OperationStatus.Succeeded,
          new GltfExportReceipt(baseline, fingerprint));
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfExportReceipt>();
      }
      catch (Exception ex)
      {
        return Failed<GltfExportReceipt>(ToDiagnostic(ex, destinationPath));
      }
      finally
      {
        _fileSystem.TryDelete(manifestTemporaryPath);
        if (bufferTemporaryPath is not null)
        {
          _fileSystem.TryDelete(bufferTemporaryPath);
        }
      }
    }

    private static bool HasSameContent(string path, byte[] expected)
    {
      using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      if (stream.Length != expected.Length)
      {
        return false;
      }

      var buffer = new byte[Math.Min(81920, expected.Length)];
      var offset = 0;
      while (offset < expected.Length)
      {
        var read = stream.Read(buffer, 0, Math.Min(buffer.Length, expected.Length - offset));
        if (read == 0)
        {
          return false;
        }

        if (!expected.AsSpan(offset, read).SequenceEqual(buffer.AsSpan(0, read)))
        {
          return false;
        }

        offset += read;
      }

      return stream.ReadByte() == -1;
    }

    /// <summary>Imports an unchanged GLB into an expected lineage and document baseline.</summary>
    public async Task<OperationResult<GltfEditImportResult>> ImportEditGlbAsync(
      Stream source,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }

      if (expectedBaseline is null)
      {
        throw new ArgumentNullException(nameof(expectedBaseline));
      }

      profile ??= GltfOperationProfile.Default;
      try
      {
        var bytes = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken).ConfigureAwait(false);
        var parsed = GlbDocument.Parse(bytes, profile.MaxJsonDepth);
        ValidateGeometryProfile(parsed, profile);
        ValidateMetadataProfile(parsed, profile);
        return await ImportParsedAsync(
          parsed,
          expectedBaseline,
          profile,
          cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfEditImportResult>();
      }
      catch (Exception ex)
      {
        return Failed<GltfEditImportResult>(ToDiagnostic(ex));
      }
    }

    /// <summary>Imports an unchanged separate glTF package into an expected baseline.</summary>
    public async Task<OperationResult<GltfEditImportResult>> ImportEditGltfFileAsync(
      string sourcePath,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (sourcePath is null)
      {
        throw new ArgumentNullException(nameof(sourcePath));
      }

      if (expectedBaseline is null)
      {
        throw new ArgumentNullException(nameof(expectedBaseline));
      }

      profile ??= GltfOperationProfile.Default;
      try
      {
        var package = await ReadSeparatePackageAsync(sourcePath, profile, cancellationToken)
          .ConfigureAwait(false);
        GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferUri);
        var parsed = GlbDocument.ParseSeparate(package.Json, package.Binary, profile.MaxJsonDepth);
        ValidateGeometryProfile(parsed, profile);
        return await ImportParsedAsync(parsed, expectedBaseline, profile, cancellationToken)
          .ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfEditImportResult>();
      }
      catch (Exception ex)
      {
        return Failed<GltfEditImportResult>(ToDiagnostic(ex, sourcePath));
      }
    }

    /// <summary>Strictly validates one supported GLB without materializing MSH output.</summary>
    public async Task<OperationResult> ValidateGlbAsync(
      Stream source,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }

      profile ??= GltfOperationProfile.Default;
      try
      {
        var bytes = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken).ConfigureAwait(false);
        var parsed = GlbDocument.Parse(bytes, profile.MaxJsonDepth);
        ValidateGeometryProfile(parsed, profile);
        ValidateMetadataProfile(parsed, profile);
        return new OperationResult(OperationStatus.Succeeded);
      }
      catch (OperationCanceledException)
      {
        return Cancelled();
      }
      catch (Exception ex)
      {
        return new OperationResult(OperationStatus.Failed, new[] { ToDiagnostic(ex) });
      }
    }

    /// <summary>Strictly validates one separate glTF package and its external buffer.</summary>
    public async Task<OperationResult> ValidateGltfFileAsync(
      string sourcePath,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (sourcePath is null)
      {
        throw new ArgumentNullException(nameof(sourcePath));
      }

      profile ??= GltfOperationProfile.Default;
      try
      {
        var package = await ReadSeparatePackageAsync(sourcePath, profile, cancellationToken)
          .ConfigureAwait(false);
        var parsed = GlbDocument.ParseSeparate(package.Json, package.Binary, profile.MaxJsonDepth);
        ValidateGeometryProfile(parsed, profile);
        ValidateMetadataProfile(parsed, profile);
        GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferUri);
        return new OperationResult(OperationStatus.Succeeded);
      }
      catch (OperationCanceledException)
      {
        return Cancelled();
      }
      catch (Exception ex)
      {
        return new OperationResult(OperationStatus.Failed, new[] { ToDiagnostic(ex, sourcePath) });
      }
    }

    private static async Task<(byte[] Json, byte[] Binary, string BufferUri)> ReadSeparatePackageAsync(
      string sourcePath,
      GltfOperationProfile profile,
      CancellationToken cancellationToken)
    {
      await using var jsonStream = new FileStream(
        sourcePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        81920,
        true);
      var json = await ReadBoundedAsync(jsonStream, profile.MaxInputBytes, cancellationToken)
        .ConfigureAwait(false);
      var bufferUri = GlbDocument.GetSeparateBufferUri(json, profile.MaxJsonDepth);
      if (Path.IsPathRooted(bufferUri)
        || !string.Equals(Path.GetFileName(bufferUri), bufferUri, StringComparison.Ordinal)
        || bufferUri.IndexOfAny(new[] { '/', '\\' }) >= 0)
      {
        throw new InvalidDataException("The external buffer URI must be a safe relative file name.");
      }

      var directory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))
        ?? Directory.GetCurrentDirectory();
      var bufferPath = Path.Combine(directory, bufferUri);
      await using var binaryStream = new FileStream(
        bufferPath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        81920,
        true);
      var remaining = profile.MaxInputBytes - json.Length;
      if (remaining <= 0)
      {
        throw new ResourceLimitException(json.Length, profile.MaxInputBytes);
      }

      var binary = await ReadBoundedAsync(binaryStream, remaining, cancellationToken)
        .ConfigureAwait(false);
      return (json, binary, bufferUri);
    }

    private static async Task<OperationResult<GltfEditImportResult>> ImportParsedAsync(
      ParsedGlb parsed,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile profile,
      CancellationToken cancellationToken)
    {
      var manifest = GlbDocument.ParseMetadata(
        parsed.ManifestMetadata,
        profile.MaxMetadataBytes,
        profile.MaxJsonDepth);
      ValidateManifestMetadata(manifest, expectedBaseline);

      byte[] sourceMsh;
      try
      {
        sourceMsh = Convert.FromBase64String(manifest.SourceMsh!);
      }
      catch (FormatException ex)
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.MalformedMetadata,
          2001,
          "scenes[0]",
          ex.Message));
      }

      await using var mshStream = new MemoryStream(sourceMsh, false);
      var read = await new MshReader().ReadAsync(mshStream, cancellationToken: cancellationToken).ConfigureAwait(false);
      if (read.Value is not StaticMeshAsset asset)
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.MalformedMetadata,
          2001,
          "scenes[0]",
          "Preserved MSH state did not pass the safe MSH reader."));
      }

      var meshes = parsed.Meshes
        .Select(mesh => new
        {
          Parsed = mesh,
          Metadata = GlbDocument.ParseMetadata(
            mesh.Metadata,
            profile.MaxMetadataBytes,
            profile.MaxJsonDepth)
        })
        .ToArray();
      var nodes = parsed.Nodes
        .Select(node => new
        {
          Parsed = node,
          Metadata = GlbDocument.ParseMetadata(
            node.Metadata,
            profile.MaxMetadataBytes,
            profile.MaxJsonDepth)
        })
        .ToArray();
      ValidateHierarchy(
        parsed,
        nodes.Select(node => (node.Parsed, node.Metadata)).ToArray(),
        meshes.Select(mesh => mesh.Metadata).ToArray(),
        asset,
        expectedBaseline);
      IReadOnlyList<GeometryPartition> partitions;
      try
      {
        partitions = MatchPartitions(meshes.Select(mesh =>
          (mesh.Parsed, mesh.Metadata)).ToArray(), asset, expectedBaseline);
      }
      catch (StaleNativeProjectionException ex)
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.StaleNativeProjection,
          2008,
          "meshes",
          ex.Message));
      }

      var fingerprint = StaticGeometryFingerprint.Create(expectedBaseline, partitions);
      if (meshes.Any(mesh =>
      {
        var localIds = mesh.Metadata.Partitions.Select(partition => partition.LocalId).ToHashSet();
        var meshFingerprint = StaticGeometryFingerprint.CreateMesh(
          expectedBaseline,
          mesh.Metadata.LocalId,
          partitions.Where(partition => localIds.Contains(partition.LocalId)).ToArray());
        return !string.Equals(
          mesh.Metadata.Fingerprint,
          meshFingerprint.Sha256,
          StringComparison.Ordinal);
      }))
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.StaleNativeProjection,
          2008,
          "meshes",
          "The native geometry no longer matches its preservation fingerprint."));
      }

      var nextBaseline = new InterchangeBaseline(expectedBaseline.AssetLineageId, Guid.NewGuid());
      return new OperationResult<GltfEditImportResult>(
        OperationStatus.Succeeded,
        new GltfEditImportResult(
          asset,
          nextBaseline,
          fingerprint,
          new[] { "ArchiveFraming", "BaseHeader" }
            .Concat(Enumerable.Range(0, asset.StaticRenderObjectSequence.Count)
              .Select(index => $"StaticRenderObjectSequence[{index}]"))));
    }

    private static OperationDiagnostic? ValidateAssetProfile(
      StaticMeshAsset asset,
      GltfOperationProfile profile)
    {
      if (asset.StaticRenderObjectSequence.Count == 0)
      {
        return Unsupported("StaticRenderObjectSequence");
      }

      foreach (var renderObject in asset.StaticRenderObjectSequence)
      {
        if (renderObject.RenderVertices.Count == 0
          || renderObject.RenderVertices.Count > profile.MaxActiveRenderVertices
          || renderObject.Triangles.Count == 0)
        {
          return renderObject.RenderVertices.Count > profile.MaxActiveRenderVertices
            ? Limit(
              $"StaticRenderObjectSequence[{renderObject.LocalId}].RenderVertices",
              renderObject.RenderVertices.Count,
              profile.MaxActiveRenderVertices)
            : InvalidGeometry(
              $"StaticRenderObjectSequence[{renderObject.LocalId}]",
              "Static geometry must contain vertices and triangles.");
        }

        if (renderObject.RenderVertices.Any(vertex =>
            !IsFinite(vertex.Position)
            || !IsFinite(vertex.Normal)))
        {
          return InvalidGeometry(
            $"StaticRenderObjectSequence[{renderObject.LocalId}].RenderVertices",
            "Static geometry positions and normals must be finite.");
        }

        if (renderObject.Triangles.Any(triangle =>
            triangle.Vertex0 >= renderObject.RenderVertices.Count
            || triangle.Vertex1 >= renderObject.RenderVertices.Count
            || triangle.Vertex2 >= renderObject.RenderVertices.Count))
        {
          return InvalidGeometry(
            $"StaticRenderObjectSequence[{renderObject.LocalId}].Triangles",
            "Triangle index is outside the active render-vertex range.");
        }
      }

      return null;
    }

    private static void ValidateGeometryProfile(ParsedGlb parsed, GltfOperationProfile profile)
    {
      foreach (var primitive in parsed.Meshes.SelectMany(mesh => mesh.Primitives))
      {
        if (primitive.Vertices.Count > profile.MaxActiveRenderVertices)
        {
          throw new ResourceLimitException(
            primitive.Vertices.Count,
            profile.MaxActiveRenderVertices);
        }
      }
    }

    private static void ValidateMetadataProfile(ParsedGlb parsed, GltfOperationProfile profile)
    {
      GlbDocument.ParseMetadata(
        parsed.ManifestMetadata,
        profile.MaxMetadataBytes,
        profile.MaxJsonDepth);
      foreach (var metadata in parsed.Nodes.Select(node => node.Metadata)
        .Concat(parsed.Meshes.Select(mesh => mesh.Metadata)))
      {
        GlbDocument.ParseMetadata(metadata, profile.MaxMetadataBytes, profile.MaxJsonDepth);
      }
    }

    private static bool IsFinite(System.Numerics.Vector3 value)
    {
      return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
    }

    private static bool IsFinite(float value)
    {
      return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static void ValidateManifestMetadata(
      MetadataEnvelope manifest,
      InterchangeBaseline expected)
    {
      if (manifest.ScopeKind != "manifest" || manifest.LocalId != 0 || manifest.SourceMsh is null)
      {
        throw new InvalidDataException("The scene metadata manifest is malformed.");
      }

      if (manifest.AssetLineageId != expected.AssetLineageId)
      {
        throw new MetadataIdentityException(GltfDiagnosticCodes.AssetLineageMismatch, 2003,
          "The GLB belongs to a different asset lineage.");
      }
      if (manifest.DocumentId != expected.DocumentId)
      {
        throw new MetadataIdentityException(GltfDiagnosticCodes.DocumentMismatch, 2004,
          "The GLB belongs to a different interchange document.");
      }

    }

    private static IReadOnlyList<GeometryPartition> MatchPartitions(
      IReadOnlyList<(ParsedGltfMesh Parsed, MetadataEnvelope Metadata)> meshes,
      StaticMeshAsset asset,
      InterchangeBaseline expected)
    {
      var sources = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
        .ToDictionary(source => source.Id.Value);
      if (meshes.Count != sources.Count
        || meshes.Select(mesh => mesh.Metadata.LocalId).Distinct().Count() != meshes.Count)
      {
        throw new MalformedMetadataException("The mesh scope set does not match the source hierarchy.");
      }

      var result = new List<GeometryPartition>();
      foreach (var mesh in meshes)
      {
        var metadata = mesh.Metadata;
        if (metadata.ScopeKind != "mesh"
          || metadata.Fingerprint is null
          || metadata.FingerprintName != "static-geometry"
          || metadata.FingerprintVersion != 1
          || metadata.AssetLineageId != expected.AssetLineageId
          || metadata.DocumentId != expected.DocumentId
          || !sources.TryGetValue(metadata.LocalId, out var source)
          || metadata.Partitions.Count != source.StaticRenderObjectIds.Count
          || mesh.Parsed.Primitives.Count != metadata.Partitions.Count
          || metadata.Partitions.Select(partition => partition.LocalId).Distinct().Count()
            != metadata.Partitions.Count
          || !metadata.Partitions.Select(partition => partition.LocalId)
            .OrderBy(value => value)
            .SequenceEqual(source.StaticRenderObjectIds.Select(id => id.Value).OrderBy(value => value)))
        {
          throw new MalformedMetadataException("The mesh metadata envelope is malformed.");
        }

        var unmatched = new List<MetadataPartition>(metadata.Partitions);
        for (var primitiveIndex = 0; primitiveIndex < mesh.Parsed.Primitives.Count; primitiveIndex++)
        {
          var primitive = mesh.Parsed.Primitives[primitiveIndex];
          var positional = metadata.Partitions[primitiveIndex];
          var positionalFingerprint = StaticGeometryFingerprint.CreatePartition(
            expected,
            positional.LocalId,
            primitive.Vertices,
            primitive.Triangles);
          if (unmatched.Contains(positional)
            && string.Equals(positional.Fingerprint, positionalFingerprint, StringComparison.Ordinal))
          {
            unmatched.Remove(positional);
            result.Add(new GeometryPartition(
              positional.LocalId,
              primitive.Vertices,
              primitive.Triangles));
            continue;
          }

          var matches = unmatched.Where(partition => string.Equals(
            partition.Fingerprint,
            StaticGeometryFingerprint.CreatePartition(
              expected,
              partition.LocalId,
              primitive.Vertices,
              primitive.Triangles),
            StringComparison.Ordinal)).ToArray();
          if (matches.Length != 1)
          {
            var actual = string.Join(",", unmatched.Select(partition =>
              StaticGeometryFingerprint.CreatePartition(
                expected,
                partition.LocalId,
                primitive.Vertices,
                primitive.Triangles)));
            throw new StaleNativeProjectionException(
              $"The native geometry did not match one partition fingerprint. Expected: {string.Join(",", unmatched.Select(partition => partition.Fingerprint))}. Actual: {actual}.");
          }

          var match = matches[0];
          unmatched.Remove(match);
          result.Add(new GeometryPartition(
            match.LocalId,
            primitive.Vertices,
            primitive.Triangles));
        }
      }

      return result.AsReadOnly();
    }

    private static void ValidateHierarchy(
      ParsedGlb parsed,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope Metadata)> nodes,
      IReadOnlyList<MetadataEnvelope> meshes,
      StaticMeshAsset asset,
      InterchangeBaseline expected)
    {
      var sources = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
        .ToDictionary(source => source.Id.Value);
      if (nodes.Count != sources.Count
        || nodes.Select(node => node.Metadata.LocalId).Distinct().Count() != nodes.Count)
      {
        throw new MalformedMetadataException("The object scope set does not match the source hierarchy.");
      }

      for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
      {
        var node = nodes[nodeIndex];
        var metadata = node.Metadata;
        if (metadata.ScopeKind != "object"
          || metadata.SourceMsh is not null
          || metadata.Fingerprint is not null
          || metadata.Partitions.Count != 0
          || metadata.AssetLineageId != expected.AssetLineageId
          || metadata.DocumentId != expected.DocumentId
          || !sources.TryGetValue(metadata.LocalId, out var source))
        {
          throw new MalformedMetadataException("The object metadata envelope is malformed.");
        }

        if (node.Parsed.MeshIndex < 0
          || node.Parsed.MeshIndex >= meshes.Count
          || meshes[node.Parsed.MeshIndex].LocalId != metadata.LocalId)
        {
          throw new UnsupportedGltfDomainException("HierarchyEdits");
        }

        var childLocalIds = node.Parsed.Children
          .Select(childIndex => nodes[childIndex].Metadata.LocalId);
        if (!childLocalIds.SequenceEqual(source.Children.Select(child => child.Id.Value)))
        {
          throw new UnsupportedGltfDomainException("HierarchyEdits");
        }
      }

      if (nodes[parsed.RootNodeIndex].Metadata.LocalId != asset.RootSourceObjectId.Value)
      {
        throw new UnsupportedGltfDomainException("HierarchyEdits");
      }
    }

    private static async Task<byte[]> ReadBoundedAsync(
      Stream source,
      int maximum,
      CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (source.CanSeek && source.Length - source.Position > maximum)
      {
        throw new ResourceLimitException(source.Length - source.Position, maximum);
      }

      using var owned = new MemoryStream();
      var buffer = new byte[81920];
      while (true)
      {
        var read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
        if (read == 0)
        {
          return owned.ToArray();
        }

        if (owned.Length + read > maximum)
        {
          throw new ResourceLimitException(owned.Length + read, maximum);
        }

        owned.Write(buffer, 0, read);
      }
    }

    private static OperationDiagnostic ToDiagnostic(Exception exception, string path = "$")
    {
      if (exception is MetadataIdentityException identity)
      {
        return Diagnostic(identity.Code, identity.EventId, path, identity.Message);
      }

      if (exception is MissingMetadataException)
      {
        return Diagnostic(GltfDiagnosticCodes.MissingManifest, 2000, path, exception.Message);
      }

      if (exception is UnsupportedMetadataVersionException)
      {
        return Diagnostic(GltfDiagnosticCodes.UnsupportedMetadataVersion, 2002, path, exception.Message);
      }

      if (exception is MalformedMetadataException)
      {
        return Diagnostic(GltfDiagnosticCodes.MalformedMetadata, 2001, path, exception.Message);
      }

      if (exception is UnsupportedGltfDomainException unsupported)
      {
        return Unsupported(unsupported.Domain);
      }

      if (exception is ResourceLimitException limit)
      {
        return Limit(path, limit.Actual, limit.Maximum);
      }

      if (exception is ModelException)
      {
        return Diagnostic(GltfDiagnosticCodes.StrictValidationFailed, 1103, path, exception.Message);
      }

      if (exception is IOException || exception is UnauthorizedAccessException)
      {
        return Diagnostic(GltfDiagnosticCodes.IoFailure, 1104, path, exception.Message);
      }

      return Diagnostic(GltfDiagnosticCodes.InvalidGlb, 1100, path, exception.Message);
    }

    private static OperationDiagnostic Limit(string path, long actual, int maximum)
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.ResourceLimitExceeded,
        1101,
        DiagnosticSeverity.Error,
        path,
        "The glTF input exceeds the configured operation profile.",
        data: new Dictionary<string, string>
        {
          ["actual"] = actual.ToString(System.Globalization.CultureInfo.InvariantCulture),
          ["maximum"] = maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
    }

    private static OperationDiagnostic Unsupported(string domain)
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.UnsupportedDomain,
        1102,
        DiagnosticSeverity.Error,
        "$",
        $"The {domain} domain is outside the one-triangle walking-skeleton profile.",
        data: new Dictionary<string, string> { ["domain"] = domain });
    }

    private static OperationDiagnostic InvalidGeometry(string path, string message)
    {
      return Diagnostic(GltfDiagnosticCodes.InvalidGeometry, 1106, path, message);
    }

    private static OperationDiagnostic Diagnostic(string code, int eventId, string path, string message)
    {
      return new OperationDiagnostic(code, eventId, DiagnosticSeverity.Error, path, message);
    }

    private static OperationResult<T> Failed<T>(OperationDiagnostic diagnostic)
      where T : class
    {
      return new OperationResult<T>(OperationStatus.Failed, diagnostics: new[] { diagnostic });
    }

    private static OperationResult<T> Cancelled<T>()
      where T : class
    {
      return new OperationResult<T>(OperationStatus.Cancelled, diagnostics: new[] { CancelledDiagnostic() });
    }

    private static OperationResult Cancelled()
    {
      return new OperationResult(OperationStatus.Cancelled, new[] { CancelledDiagnostic() });
    }

    private static OperationDiagnostic CancelledDiagnostic()
    {
      return Diagnostic(GltfDiagnosticCodes.Cancelled, 1105, "$", "The glTF operation was cancelled.");
    }
  }

  internal sealed class MetadataIdentityException : Exception
  {
    internal string Code { get; }

    internal int EventId { get; }

    internal MetadataIdentityException(string code, int eventId, string message)
      : base(message)
    {
      Code = code;
      EventId = eventId;
    }
  }

  internal sealed class ResourceLimitException : Exception
  {
    internal long Actual { get; }

    internal int Maximum { get; }

    internal ResourceLimitException(long actual, int maximum)
      : base("The operation profile limit was exceeded.")
    {
      Actual = actual;
      Maximum = maximum;
    }
  }

  internal sealed class StaleNativeProjectionException : Exception
  {
    internal StaleNativeProjectionException(string message)
      : base(message)
    {
    }
  }
}
