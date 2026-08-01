#nullable enable

using EarthTool.Common.Operations;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Operations;
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

      cancellationToken.ThrowIfCancellationRequested();
      if (manifest.StaticRenderObjectLocalIds.Count == 0
        || manifest.SourceObjectLocalIds.Count == 0
        || manifest.StaticRenderObjectLocalIds.Any(id => id <= 0)
        || manifest.SourceObjectLocalIds.Any(id => id <= 0)
        || manifest.StaticRenderObjectLocalIds.Distinct().Count()
          != manifest.StaticRenderObjectLocalIds.Count
        || manifest.SourceObjectLocalIds.Distinct().Count() != manifest.SourceObjectLocalIds.Count
        || !IsStrictlyIncreasing(manifest.StaticRenderObjectInventory)
        || !IsStrictlyIncreasing(manifest.SourceObjectInventory)
        || !manifest.StaticRenderObjectInventory.SequenceEqual(
          manifest.StaticRenderObjectLocalIds.OrderBy(id => id))
        || !manifest.SourceObjectInventory.SequenceEqual(
          manifest.SourceObjectLocalIds.OrderBy(id => id))
        || !manifest.NextStaticRenderObjectLocalId.HasValue
        || manifest.NextStaticRenderObjectLocalId.Value
          <= manifest.StaticRenderObjectLocalIds.Max()
        || !manifest.NextSourceObjectLocalId.HasValue
        || manifest.NextSourceObjectLocalId.Value <= manifest.SourceObjectLocalIds.Max())
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.MalformedMetadata,
          2001,
          "scenes[0]",
          "Preserved MSH state did not pass the safe MSH reader."));
      }

      StaticMeshAsset asset;
      try
      {
        var decoded = EarthTool.MSH.Internal.MshV1Decoder.Decode(
          sourceMsh,
          MshOperationProfile.Default,
          cancellationToken,
          new MeshAssetLineageId(expectedBaseline.AssetLineageId),
          MeshAssetOrigin.Loaded,
          rootSourceObjectLocalId: manifest.SourceObjectLocalIds[0],
          staticRenderObjectLocalIds: manifest.StaticRenderObjectLocalIds,
          sourceObjectLocalIds: manifest.SourceObjectLocalIds,
          nextStaticRenderObjectLocalId: manifest.NextStaticRenderObjectLocalId,
          nextSourceObjectLocalId: manifest.NextSourceObjectLocalId);
        asset = decoded.Asset as StaticMeshAsset
          ?? throw new InvalidDataException("Preserved MSH state is not static.");
      }
      catch (Exception ex) when (ex is EarthTool.MSH.Internal.MshContentException
        || ex is ArgumentException
        || ex is InvalidDataException)
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.MalformedMetadata,
          2001,
          "scenes[0]",
          ex.Message));
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
          Metadata = node.Metadata is null
            ? null
            : GlbDocument.ParseMetadata(
              node.Metadata,
              profile.MaxMetadataBytes,
              profile.MaxJsonDepth)
        })
        .ToArray();
      var edit = asset.Edit();
      var hierarchy = ReconcileHierarchy(
        parsed,
        nodes.Select(node => (node.Parsed, node.Metadata)).ToArray(),
        meshes.Select(mesh => (mesh.Parsed, mesh.Metadata)).ToArray(),
        asset,
        expectedBaseline,
        edit);
      IReadOnlyList<PartitionMatch> partitionMatches;
      try
      {
        partitionMatches = MatchPartitions(meshes.Select((mesh, index) =>
          (mesh.Parsed, mesh.Metadata, Index: index))
          .Where(mesh => hierarchy.RetainedMeshIndices.Contains(mesh.Index))
          .Select(mesh => (mesh.Parsed, mesh.Metadata)).ToArray(), asset, expectedBaseline);
        partitionMatches = partitionMatches.Select(match =>
          hierarchy.Transforms.TryGetValue(match.SourceObjectId, out var transform)
            ? TransformPartition(match, transform)
            : match).ToArray();
      }
      catch (StaleNativeProjectionException ex)
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.StaleNativeProjection,
          2008,
          "meshes",
          ex.Message));
      }
      catch (AmbiguousPartitionCorrespondenceException ex)
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          "meshes",
          ex.Message));
      }

      foreach (var replacement in hierarchy.Pivots)
      {
        edit.ReplacePivot(replacement.Key, replacement.Value);
      }
      var matchedLocalIds = partitionMatches.Where(match => !match.Added)
        .Select(match => match.Partition.LocalId).ToHashSet();
      foreach (var removed in asset.StaticRenderObjectSequence.Where(record =>
        !matchedLocalIds.Contains(record.LocalId)))
      {
        edit.RemoveRenderObject(removed.Id);
      }

      foreach (var match in partitionMatches.Where(match => !match.Retained))
      {
        var vertices = match.Partition.Vertices.Select(ToCanonicalVertex).ToArray();
        var triangles = match.Partition.Triangles.Select(triangle => new CanonicalTriangle(
          triangle.Vertex0,
          triangle.Vertex1,
          triangle.Vertex2)).ToArray();
        if (match.Added)
        {
          var added = edit.AddRenderObject(match.SourceObjectId, vertices, triangles);
          match.Partition.AssignLocalId(added.Value);
        }
        else
        {
          var renderObject = asset.StaticRenderObjectSequence.Single(record =>
            record.LocalId == match.Partition.LocalId);
          edit.ReplaceGeometry(renderObject.Id, vertices, triangles);
        }
      }

      if (hierarchy.Changed)
      {
        edit.ApplyHierarchy(hierarchy.Root, hierarchy.Sequence);
      }

      var partitions = partitionMatches.Select(match => match.Partition)
        .Concat(hierarchy.AddedPartitions).ToArray();
      var fingerprint = StaticGeometryFingerprint.Create(expectedBaseline, partitions);
      var committed = edit.Commit();
      if (!committed.TryGetValue(out var reconciled))
      {
        var message = string.Join("; ", committed.Diagnostics.Select(diagnostic => diagnostic.Message));
        return Failed<GltfEditImportResult>(InvalidGeometry("meshes", message));
      }

      var nextBaseline = new InterchangeBaseline(expectedBaseline.AssetLineageId, Guid.NewGuid());
      var changedRecordPaths = committed.Preservation.Changes
        .Where(change => change.Disposition != PreservationDisposition.Retained)
        .Select(change => change.FieldPath)
        .ToArray();
      return new OperationResult<GltfEditImportResult>(
        OperationStatus.Succeeded,
        new GltfEditImportResult(
          reconciled,
          nextBaseline,
          fingerprint,
          committed.Preservation,
          new[] { "ArchiveFraming", "BaseHeader" }
            .Concat(partitionMatches
              .Where(match => match.Retained)
              .Select(match => asset.StaticRenderObjectSequence
                .Select((record, index) => (record, index))
                .Single(item => item.record.LocalId == match.Partition.LocalId).index)
              .Where(index => !changedRecordPaths.Any(path =>
                path == $"StaticRenderObjectSequence[{index}]"
                || path.StartsWith($"StaticRenderObjectSequence[{index}].", StringComparison.Ordinal)))
              .OrderBy(index => index)
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

        if (renderObject.RenderVertices.Any(vertex =>
          !IsFinite(vertex.TextureCoordinate.X)
          || !IsFinite(vertex.TextureCoordinate.Y)))
        {
          return InvalidGeometry(
            $"StaticRenderObjectSequence[{renderObject.LocalId}].RenderVertices",
            "Static geometry texture coordinates must be finite.");
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
        if (metadata is not null)
        {
          GlbDocument.ParseMetadata(metadata, profile.MaxMetadataBytes, profile.MaxJsonDepth);
        }
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

    private static bool IsStrictlyIncreasing(IReadOnlyList<int> values)
    {
      return values.Count > 0
        && values[0] > 0
        && values.Zip(values.Skip(1), (left, right) => left < right).All(increasing => increasing);
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

    private static IReadOnlyList<PartitionMatch> MatchPartitions(
      IReadOnlyList<(ParsedGltfMesh Parsed, MetadataEnvelope Metadata)> meshes,
      StaticMeshAsset asset,
      InterchangeBaseline expected)
    {
      var sources = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
        .ToDictionary(source => source.Id.Value);
      if (meshes.Select(mesh => mesh.Metadata.LocalId).Distinct().Count() != meshes.Count)
      {
        throw new MalformedMetadataException("The mesh scope set does not match the source hierarchy.");
      }

      var result = new List<PartitionMatch>();
      foreach (var mesh in meshes)
      {
        var metadata = mesh.Metadata;
        if (metadata.ScopeKind != "mesh"
          || metadata.LocalId <= 0
          || metadata.Fingerprint is null
          || metadata.FingerprintName != "static-geometry"
          || metadata.FingerprintVersion != 1
          || metadata.AssetLineageId != expected.AssetLineageId
          || metadata.DocumentId != expected.DocumentId
          || !sources.TryGetValue(metadata.LocalId, out var source)
          || metadata.Partitions.Count != source.StaticRenderObjectIds.Count
          || metadata.Partitions.Select(partition => partition.LocalId).Distinct().Count()
            != metadata.Partitions.Count
          || !metadata.Partitions.Select(partition => partition.LocalId)
            .OrderBy(value => value)
            .SequenceEqual(source.StaticRenderObjectIds.Select(id => id.Value).OrderBy(value => value)))
        {
          throw new MalformedMetadataException("The mesh metadata envelope is malformed.");
        }

        var sourceRecords = source.StaticRenderObjectIds
          .Select(id => asset.StaticRenderObjectSequence.Single(record => record.Id.Equals(id)))
          .ToArray();
        var sourceGeometry = sourceRecords.Select(record => new GeometryPartition(
          record.LocalId,
          record.RenderVertices.Select(GlbDocument.ProjectToGltf).ToArray(),
          record.Triangles)).ToArray();
        var currentGeometry = mesh.Parsed.Primitives.Select(primitive => new GeometryPartition(
          0,
          primitive.Vertices,
          primitive.Triangles)).ToArray();
        if (string.Equals(
          StaticGeometryFingerprint.CreateSurfaceKey(sourceGeometry),
          StaticGeometryFingerprint.CreateSurfaceKey(currentGeometry),
          StringComparison.Ordinal))
        {
          result.AddRange(sourceGeometry.Select(partition => new PartitionMatch(
            partition,
            source.Id,
            true,
            false)));
          continue;
        }

        var sourceSurfaceCounts = sourceRecords.GroupBy(record =>
          StaticGeometryFingerprint.CreateSurfaceKey(
            record.RenderVertices.Select(GlbDocument.ProjectToGltf).ToArray(),
            record.Triangles)).ToDictionary(group => group.Key, group => group.Count());
        var currentSurfaceCounts = mesh.Parsed.Primitives.GroupBy(primitive =>
          StaticGeometryFingerprint.CreateSurfaceKey(primitive.Vertices, primitive.Triangles))
          .ToDictionary(group => group.Key, group => group.Count());
        if (sourceSurfaceCounts.Any(item => item.Value > 1
          && (!currentSurfaceCounts.TryGetValue(item.Key, out var currentCount)
            || currentCount < item.Value)))
        {
          throw new AmbiguousPartitionCorrespondenceException(
            "Duplicate native geometry identifies more than one possible preserved partition deletion.");
        }

        var unmatched = new List<MetadataPartition>(metadata.Partitions);
        var pending = new List<(ParsedGltfPrimitive Primitive, int Index)>();
        for (var primitiveIndex = 0; primitiveIndex < mesh.Parsed.Primitives.Count; primitiveIndex++)
        {
          var primitive = mesh.Parsed.Primitives[primitiveIndex];
          if (primitiveIndex < metadata.Partitions.Count)
          {
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
              result.Add(new PartitionMatch(
                new GeometryPartition(
                  positional.LocalId,
                  primitive.Vertices,
                  primitive.Triangles),
                source.Id,
                true,
                false));
              continue;
            }
          }

          pending.Add((primitive, primitiveIndex));
        }

        while (pending.Count > 0)
        {
          var resolved = false;
          for (var pendingIndex = pending.Count - 1; pendingIndex >= 0; pendingIndex--)
          {
            var primitive = pending[pendingIndex].Primitive;
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
              continue;
            }

            var match = matches[0];
            unmatched.Remove(match);
            pending.RemoveAt(pendingIndex);
            result.Add(new PartitionMatch(
              new GeometryPartition(
                match.LocalId,
                primitive.Vertices,
                primitive.Triangles),
              source.Id,
              true,
              false));
            resolved = true;
          }

          if (resolved)
          {
            continue;
          }

          if (unmatched.Count == 0)
          {
            result.AddRange(pending.Select(item => new PartitionMatch(
              new GeometryPartition(-1, item.Primitive.Vertices, item.Primitive.Triangles),
              source.Id,
              false,
              true)));
            pending.Clear();
            continue;
          }

          if (pending.Count == 1 && unmatched.Count == 1)
          {
            var stale = unmatched[0];
            var primitive = pending[0].Primitive;
            unmatched.Clear();
            pending.Clear();
            result.Add(new PartitionMatch(
              new GeometryPartition(
                stale.LocalId,
                primitive.Vertices,
                primitive.Triangles),
              source.Id,
              false,
              false));
            continue;
          }

          var actual = string.Join(",", pending.Select(item =>
            string.Join(",", unmatched.Select(partition =>
              StaticGeometryFingerprint.CreatePartition(
                expected,
                partition.LocalId,
                item.Primitive.Vertices,
                item.Primitive.Triangles)))));
          throw new AmbiguousPartitionCorrespondenceException(
            $"The native geometry did not identify one partition correspondence. Expected: {string.Join(",", unmatched.Select(partition => partition.Fingerprint))}. Actual: {actual}.");
        }
      }

      return result.AsReadOnly();
    }

    private static CanonicalStaticVertex ToCanonicalVertex(RenderVertex vertex)
    {
      return new CanonicalStaticVertex(
        new System.Numerics.Vector3(vertex.Position.X, -vertex.Position.Z, vertex.Position.Y),
        new System.Numerics.Vector3(vertex.Normal.X, -vertex.Normal.Z, vertex.Normal.Y),
        vertex.TextureCoordinate);
    }

    private sealed class PartitionMatch
    {
      internal GeometryPartition Partition { get; }

      internal bool Retained { get; }

      internal SourceObjectId SourceObjectId { get; }

      internal bool Added { get; }

      internal PartitionMatch(
        GeometryPartition partition,
        SourceObjectId sourceObjectId,
        bool retained,
        bool added)
      {
        Partition = partition;
        SourceObjectId = sourceObjectId;
        Retained = retained;
        Added = added;
      }
    }

    private static StaticHierarchyPlan ReconcileHierarchy(
      ParsedGlb parsed,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      IReadOnlyList<(ParsedGltfMesh Parsed, MetadataEnvelope Metadata)> meshes,
      StaticMeshAsset asset,
      InterchangeBaseline expected,
      StaticMeshEditSession edit)
    {
      var sources = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
        .ToDictionary(source => source.Id.Value);
      if (nodes.Any(node => !node.Parsed.MeshIndex.HasValue
        && (node.Metadata is not null || node.Parsed.Children.Count == 0)))
      {
        throw new MalformedMetadataException("The object scope set does not match the source hierarchy.");
      }

      var identifiedSourceIds = nodes.Where(node => node.Parsed.MeshIndex.HasValue && node.Metadata is not null)
        .Select(node => node.Metadata!.LocalId).ToArray();
      if (identifiedSourceIds.Distinct().Count() != identifiedSourceIds.Length)
      {
        throw new MetadataIdentityException(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          "Duplicate object scope identities require an explicit fork resolution.");
      }
      if (nodes.Any(node => node.Parsed.MeshIndex.HasValue && node.Metadata is null)
        && sources.Keys.Any(id => !identifiedSourceIds.Contains(id)))
      {
        throw new MetadataIdentityException(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          "An untagged object cannot be distinguished from a missing expected object scope.");
      }
      if (nodes.Count(node => node.Parsed.MeshIndex.HasValue && node.Metadata is null) > 1)
      {
        throw new MetadataIdentityException(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          "Multiple untagged objects require explicit scope identities.");
      }
      var meshLocalIds = meshes.Select(mesh => mesh.Metadata.LocalId).ToArray();
      if (meshLocalIds.Distinct().Count() != meshLocalIds.Length)
      {
        throw new MetadataIdentityException(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          "Duplicate mesh scope identities require an explicit fork resolution.");
      }

      var effectiveTransforms = CreateEffectiveTransforms(parsed.RootNodeIndex, nodes);
      var sourceByNode = new Dictionary<int, StaticSourceObject>();
      var pivots = new Dictionary<StaticRenderObjectId, System.Numerics.Vector3>();
      var transforms = new Dictionary<SourceObjectId, System.Numerics.Matrix4x4>();
      var retainedMeshIndices = new HashSet<int>();
      var addedPartitions = new List<GeometryPartition>();
      for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
      {
        var node = nodes[nodeIndex];
        if (!node.Parsed.MeshIndex.HasValue)
        {
          continue;
        }
        var metadata = node.Metadata;
        StaticSourceObject? originalSource = null;
        if (metadata is not null
          && (metadata.ScopeKind != "object"
            || metadata.LocalId <= 0
            || metadata.SourceMsh is not null
            || metadata.Fingerprint is not null
            || metadata.Partitions.Count != 0
            || metadata.AssetLineageId != expected.AssetLineageId
            || metadata.DocumentId != expected.DocumentId
            || !sources.TryGetValue(metadata.LocalId, out originalSource)))
        {
          throw new MalformedMetadataException("The object metadata envelope is malformed.");
        }

        if (!node.Parsed.MeshIndex.HasValue
          || node.Parsed.MeshIndex.Value >= meshes.Count
          || meshes[node.Parsed.MeshIndex.Value].Metadata.ScopeKind != "mesh")
        {
          throw new UnsupportedGltfDomainException("HierarchyEdits");
        }

        var effectiveTransform = effectiveTransforms[nodeIndex];
        var translation = effectiveTransform.Translation;
        var linearTransform = effectiveTransform;
        linearTransform.Translation = System.Numerics.Vector3.Zero;
        if (!System.Numerics.Matrix4x4.Invert(linearTransform, out _))
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }
        var hasLinearTransform = linearTransform != System.Numerics.Matrix4x4.Identity;
        var pivot = new System.Numerics.Vector3(translation.X, -translation.Z, translation.Y);

        if (metadata is not null)
        {
          if (meshes[node.Parsed.MeshIndex.Value].Metadata.LocalId != metadata.LocalId)
          {
            throw new UnsupportedGltfDomainException("HierarchyEdits");
          }
          retainedMeshIndices.Add(node.Parsed.MeshIndex.Value);
          sourceByNode.Add(nodeIndex, originalSource!);
          if (hasLinearTransform)
          {
            transforms.Add(originalSource!.Id, linearTransform);
          }
          var pivotRecordId = originalSource!.StaticRenderObjectIds[0];
          var sourcePivot = asset.StaticRenderObjectSequence.Single(record =>
            record.Id.Equals(pivotRecordId)).Pivot;
          if (pivot != sourcePivot)
          {
            pivots.Add(pivotRecordId, pivot);
          }
          continue;
        }

        var sourceId = edit.AllocateSourceObjectId();
        var renderObjectIds = new List<StaticRenderObjectId>();
        foreach (var primitive in meshes[node.Parsed.MeshIndex.Value].Parsed.Primitives)
        {
          var partition = new GeometryPartition(-1, primitive.Vertices, primitive.Triangles);
          if (hasLinearTransform)
          {
            partition = TransformPartition(
              new PartitionMatch(partition, sourceId, false, true),
              linearTransform).Partition;
          }
          var vertices = partition.Vertices.Select(ToCanonicalVertex).ToArray();
          var triangles = partition.Triangles.Select(triangle => new CanonicalTriangle(
            triangle.Vertex0,
            triangle.Vertex1,
            triangle.Vertex2)).ToArray();
          var renderObjectId = edit.AddRenderObject(sourceId, vertices, triangles);
          partition.AssignLocalId(renderObjectId.Value);
          renderObjectIds.Add(renderObjectId);
          addedPartitions.Add(partition);
        }
        if (renderObjectIds.Count == 0)
        {
          throw new UnsupportedGltfDomainException("Geometry");
        }
        pivots.Add(renderObjectIds[0], pivot);
        sourceByNode.Add(nodeIndex, new StaticSourceObject(
          sourceId,
          renderObjectIds,
          Array.Empty<StaticSourceObject>()));
      }

      var editedRoots = BuildHierarchy(parsed.RootNodeIndex, nodes, sourceByNode);
      if (editedRoots.Count != 1 || !editedRoots[0].Id.Equals(asset.RootSourceObjectId))
      {
        throw new UnsupportedGltfDomainException("HierarchyEdits");
      }

      var editedRoot = editedRoots[0];
      var changed = !SameHierarchy(asset.RootSourceObject, editedRoot);
      if (changed)
      {
        editedRoot = SortHierarchy(editedRoot);
      }
      var sequence = changed
        ? FlattenCanonical(editedRoot).ToArray()
        : asset.StaticRenderObjectSequence.Select(record => record.Id).ToArray();
      return new StaticHierarchyPlan(
        editedRoot,
        sequence,
        pivots,
        transforms,
        retainedMeshIndices,
        addedPartitions,
        changed);
    }

    private static IReadOnlyList<StaticSourceObject> BuildHierarchy(
      int nodeIndex,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      IReadOnlyDictionary<int, StaticSourceObject> sources)
    {
      var children = nodes[nodeIndex].Parsed.Children
        .SelectMany(child => BuildHierarchy(child, nodes, sources))
        .ToArray();
      if (!sources.TryGetValue(nodeIndex, out var source))
      {
        return children;
      }
      return new[] { new StaticSourceObject(source.Id, source.StaticRenderObjectIds, children) };
    }

    private static StaticSourceObject SortHierarchy(StaticSourceObject source)
    {
      return new StaticSourceObject(
        source.Id,
        source.StaticRenderObjectIds,
        source.Children.OrderBy(child => child.Id.Value).Select(SortHierarchy));
    }

    private static IReadOnlyDictionary<int, System.Numerics.Matrix4x4> CreateEffectiveTransforms(
      int rootNodeIndex,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes)
    {
      var result = new Dictionary<int, System.Numerics.Matrix4x4>();
      AddEffectiveTransforms(
        rootNodeIndex,
        System.Numerics.Matrix4x4.Identity,
        nodes,
        result);
      return result;
    }

    private static void AddEffectiveTransforms(
      int nodeIndex,
      System.Numerics.Matrix4x4 scaffoldingTransform,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      IDictionary<int, System.Numerics.Matrix4x4> result)
    {
      var node = nodes[nodeIndex].Parsed;
      // System.Numerics uses row-vector composition, so a collapsed parent follows the child.
      var effective = node.LocalTransform * scaffoldingTransform;
      if (node.MeshIndex.HasValue)
      {
        result.Add(nodeIndex, effective);
        foreach (var child in node.Children)
        {
          AddEffectiveTransforms(child, System.Numerics.Matrix4x4.Identity, nodes, result);
        }
        return;
      }

      foreach (var child in node.Children)
      {
        AddEffectiveTransforms(child, effective, nodes, result);
      }
    }

    private static bool SameHierarchy(StaticSourceObject left, StaticSourceObject right)
    {
      return left.Id.Equals(right.Id)
        && left.Children.Select(child => child.Id).SequenceEqual(right.Children.Select(child => child.Id))
        && left.Children.Zip(right.Children, SameHierarchy).All(value => value);
    }

    private static IEnumerable<StaticRenderObjectId> FlattenCanonical(StaticSourceObject source)
    {
      yield return source.StaticRenderObjectIds[0];
      foreach (var child in source.Children.OrderBy(item => item.Id.Value))
      {
        foreach (var id in FlattenCanonical(child))
        {
          yield return id;
        }
      }
      foreach (var id in source.StaticRenderObjectIds.Skip(1).OrderBy(item => item.Value))
      {
        yield return id;
      }
    }

    private static PartitionMatch TransformPartition(
      PartitionMatch match,
      System.Numerics.Matrix4x4 transform)
    {
      if (!System.Numerics.Matrix4x4.Invert(transform, out var inverse))
      {
        throw new UnsupportedGltfDomainException("TransformOrHierarchy");
      }
      // Normals follow the inverse transpose; a reflected basis reverses winding exactly once.
      var normalTransform = System.Numerics.Matrix4x4.Transpose(inverse);
      var vertices = match.Partition.Vertices.Select(vertex =>
      {
        var normal = System.Numerics.Vector3.TransformNormal(vertex.Normal, normalTransform);
        if (normal.LengthSquared() == 0
          || !float.IsFinite(normal.X)
          || !float.IsFinite(normal.Y)
          || !float.IsFinite(normal.Z))
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }
        return new RenderVertex(
          System.Numerics.Vector3.Transform(vertex.Position, transform),
          System.Numerics.Vector3.Normalize(normal),
          vertex.TextureCoordinate);
      }).ToArray();
      var reversesWinding = transform.GetDeterminant() < 0;
      var triangles = match.Partition.Triangles.Select(triangle => reversesWinding
        ? new StaticTriangle(
          triangle.Vertex0,
          triangle.Vertex2,
          triangle.Vertex1,
          triangle.TriangleRenderPassFlags)
        : triangle).ToArray();
      return new PartitionMatch(
        new GeometryPartition(match.Partition.LocalId, vertices, triangles),
        match.SourceObjectId,
        false,
        match.Added);
    }

    private sealed class StaticHierarchyPlan
    {
      internal StaticSourceObject Root { get; }
      internal IReadOnlyList<StaticRenderObjectId> Sequence { get; }
      internal IReadOnlyDictionary<StaticRenderObjectId, System.Numerics.Vector3> Pivots { get; }
      internal IReadOnlyDictionary<SourceObjectId, System.Numerics.Matrix4x4> Transforms { get; }
      internal IReadOnlyCollection<int> RetainedMeshIndices { get; }
      internal IReadOnlyList<GeometryPartition> AddedPartitions { get; }
      internal bool Changed { get; }

      internal StaticHierarchyPlan(
        StaticSourceObject root,
        IReadOnlyList<StaticRenderObjectId> sequence,
        IReadOnlyDictionary<StaticRenderObjectId, System.Numerics.Vector3> pivots,
        IReadOnlyDictionary<SourceObjectId, System.Numerics.Matrix4x4> transforms,
        IReadOnlyCollection<int> retainedMeshIndices,
        IReadOnlyList<GeometryPartition> addedPartitions,
        bool changed)
      {
        Root = root;
        Sequence = sequence;
        Pivots = pivots;
        Transforms = transforms;
        RetainedMeshIndices = retainedMeshIndices;
        AddedPartitions = addedPartitions;
        Changed = changed;
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

  internal sealed class AmbiguousPartitionCorrespondenceException : Exception
  {
    internal AmbiguousPartitionCorrespondenceException(string message)
      : base(message)
    {
    }
  }
}
