#nullable enable

using EarthTool.Common.Operations;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Authoring;
using EarthTool.MSH.Operations;
using SharpGLTF.Validation;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
        var animationDiagnostics = CreateAnimationDiagnostics(asset, baseline);
        var projectionDiagnostics = animationDiagnostics
          .Concat(CreateCannonRenderPositionDiagnostics(asset)).ToArray();
        var metadataLength = GlbDocument.GetMaximumMetadataByteCount(asset, baseline);
        if (metadataLength > profile.MaxMetadataBytes)
        {
          return Failed<GltfExportReceipt>(Limit("scenes[0].extras.earthtool", metadataLength, profile.MaxMetadataBytes));
        }

        var minimumOutputLength = GlbDocument.GetMinimumOutputByteCount(asset, baseline, true);
        if (minimumOutputLength > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", minimumOutputLength, profile.MaxOutputBytes));
        }
        var withoutPreviews = GlbDocument.Create(
          asset,
          baseline,
          new Dictionary<StaticRenderObjectId, TexPreview>(),
          out var fingerprint);
        if (withoutPreviews.Length > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", withoutPreviews.Length, profile.MaxOutputBytes));
        }
        var previewResult = TexPreviewLoader.Load(
          asset,
          options,
          profile,
          profile.MaxOutputBytes - withoutPreviews.Length,
          cancellationToken);
        if (previewResult.HasErrors)
        {
          return Failed<GltfExportReceipt>(previewResult.Diagnostics.First(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error));
        }

        var glb = previewResult.Previews.Count == 0
          ? withoutPreviews
          : GlbDocument.Create(asset, baseline, previewResult.Previews, out fingerprint);
        var exportDiagnostics = previewResult.Diagnostics.Concat(projectionDiagnostics).ToArray();
        if (glb.Length > profile.MaxOutputBytes)
        {
          glb = withoutPreviews;
          exportDiagnostics = WithoutEmittedPreviewDiagnostics(previewResult.Diagnostics)
            .Concat(projectionDiagnostics).ToArray();
        }

        GlbDocument.Validate(glb, profile);
        cancellationToken.ThrowIfCancellationRequested();
        await destination.WriteAsync(glb, 0, glb.Length, cancellationToken).ConfigureAwait(false);
        return new OperationResult<GltfExportReceipt>(
          OperationStatus.Succeeded,
          new GltfExportReceipt(baseline, fingerprint),
          exportDiagnostics);
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
      var sidecarTemporaryPaths = new Dictionary<string, string>(StringComparer.Ordinal);
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
        var animationDiagnostics = CreateAnimationDiagnostics(asset, baseline);
        var projectionDiagnostics = animationDiagnostics
          .Concat(CreateCannonRenderPositionDiagnostics(asset)).ToArray();
        var metadataLength = GlbDocument.GetMaximumMetadataByteCount(asset, baseline);
        if (metadataLength > profile.MaxMetadataBytes)
        {
          return Failed<GltfExportReceipt>(Limit("scenes[0].extras.earthtool", metadataLength, profile.MaxMetadataBytes));
        }

        var minimumOutputLength = GlbDocument.GetMinimumOutputByteCount(asset, baseline, false);
        if (minimumOutputLength > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", minimumOutputLength, profile.MaxOutputBytes));
        }
        var withoutPreviews = GlbDocument.CreateSeparate(
          asset,
          baseline,
          new Dictionary<StaticRenderObjectId, TexPreview>(),
          out var fingerprint);
        var withoutPreviewLength = checked(withoutPreviews.Json.Length + withoutPreviews.Binary.Length);
        if (withoutPreviewLength > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", withoutPreviewLength, profile.MaxOutputBytes));
        }
        var previewResult = TexPreviewLoader.Load(
          asset,
          options,
          profile,
          profile.MaxOutputBytes - withoutPreviewLength,
          cancellationToken);
        if (previewResult.HasErrors)
        {
          return Failed<GltfExportReceipt>(previewResult.Diagnostics.First(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error));
        }

        var package = previewResult.Previews.Count == 0
          ? withoutPreviews
          : GlbDocument.CreateSeparate(asset, baseline, previewResult.Previews, out fingerprint);
        var outputLength = checked(package.Json.Length
          + package.Binary.Length
          + package.ImageSidecars.Values.Sum(bytes => bytes.Length));
        var exportDiagnostics = previewResult.Diagnostics.Concat(projectionDiagnostics).ToArray();
        if (outputLength > profile.MaxOutputBytes)
        {
          package = withoutPreviews;
          exportDiagnostics = WithoutEmittedPreviewDiagnostics(previewResult.Diagnostics)
            .Concat(projectionDiagnostics).ToArray();
        }

        ValidateGeometryProfile(
          GlbDocument.ParseSeparate(package.Json, package.Binary, profile),
          profile);
        GlbDocument.ValidateSeparate(
          package.Json,
          package.Binary,
          package.BufferFileName,
          package.ImageSidecars);
        var directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath))
          ?? Directory.GetCurrentDirectory();
        var sidecars = new Dictionary<string, byte[]>(package.ImageSidecars, StringComparer.Ordinal)
        {
          [package.BufferFileName] = package.Binary
        };
        var manifestFullPath = Path.GetFullPath(destinationPath);
        if (Directory.Exists(manifestFullPath))
        {
          throw new IOException("The glTF manifest path collides with a directory.");
        }
        var sidecarPaths = sidecars.ToDictionary(
          sidecar => sidecar.Key,
          sidecar => Path.Combine(directory, sidecar.Key),
          StringComparer.Ordinal);

        foreach (var sidecar in sidecars.OrderBy(sidecar => sidecar.Key, StringComparer.Ordinal))
        {
          var sidecarPath = sidecarPaths[sidecar.Key];
          if (string.Equals(
            manifestFullPath,
            Path.GetFullPath(sidecarPath),
            StringComparison.OrdinalIgnoreCase))
          {
            throw new IOException("The glTF manifest path collides with a content-addressed sidecar.");
          }
          if (Directory.Exists(sidecarPath))
          {
            throw new IOException("A content-addressed glTF sidecar collides with a directory.");
          }
          if (File.Exists(sidecarPath) && !HasSameContent(sidecarPath, sidecar.Value))
          {
            throw new IOException("A content-addressed glTF sidecar has conflicting content.");
          }
        }

        foreach (var sidecar in sidecars.OrderBy(sidecar => sidecar.Key, StringComparer.Ordinal))
        {
          var sidecarPath = sidecarPaths[sidecar.Key];
          if (File.Exists(sidecarPath))
          {
            continue;
          }
          var temporaryPath = _fileSystem.GetTemporaryPath(sidecarPath);
          sidecarTemporaryPaths.Add(sidecar.Key, temporaryPath);
          using (var temporary = _fileSystem.CreateTemporary(temporaryPath))
          {
            await temporary.WriteAsync(
              sidecar.Value,
              0,
              sidecar.Value.Length,
              cancellationToken).ConfigureAwait(false);
            await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
          }
        }

        foreach (var sidecar in sidecars.OrderBy(sidecar => sidecar.Key, StringComparer.Ordinal))
        {
          var sidecarPath = sidecarPaths[sidecar.Key];
          if (File.Exists(sidecarPath))
          {
            if (!HasSameContent(sidecarPath, sidecar.Value))
            {
              throw new IOException("A content-addressed glTF sidecar changed during commit.");
            }
            if (sidecarTemporaryPaths.Remove(sidecar.Key, out var unusedTemporaryPath))
            {
              _fileSystem.TryDelete(unusedTemporaryPath);
            }
            continue;
          }
          var temporaryPath = sidecarTemporaryPaths[sidecar.Key];
          cancellationToken.ThrowIfCancellationRequested();
          _fileSystem.Commit(temporaryPath, sidecarPath);
          sidecarTemporaryPaths.Remove(sidecar.Key);
        }

        foreach (var sidecar in sidecars)
        {
          if (!File.Exists(sidecarPaths[sidecar.Key])
            || !HasSameContent(sidecarPaths[sidecar.Key], sidecar.Value))
          {
            throw new IOException("A committed glTF sidecar is incomplete or invalid.");
          }
        }

        cancellationToken.ThrowIfCancellationRequested();
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
          new GltfExportReceipt(baseline, fingerprint),
          exportDiagnostics);
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
        foreach (var temporaryPath in sidecarTemporaryPaths.Values)
        {
          _fileSystem.TryDelete(temporaryPath);
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
        var parsed = GlbDocument.Parse(bytes, profile);
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
        var parsed = GlbDocument.ParseSeparate(package.Json, package.Binary, profile);
        GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferUri, package.Images);
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

    /// <summary>Imports a metadata-free GLB as a canonical authored static mesh representation.</summary>
    public async Task<OperationResult<GltfNewModelImportResult>> ImportNewModelGlbAsync(
      Stream source,
      GltfNewModelImportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }

      profile ??= GltfOperationProfile.Default;
      options ??= new GltfNewModelImportOptions();
      try
      {
        var bytes = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken)
          .ConfigureAwait(false);
        var parsed = GlbDocument.ParseNewModel(bytes, profile);
        ValidateGeometryProfile(parsed, profile);
        return ImportNewModelParsed(parsed, profile, cancellationToken, options);
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfNewModelImportResult>();
      }
      catch (Exception ex)
      {
        return Failed<GltfNewModelImportResult>(ToDiagnostic(ex));
      }
    }

    /// <summary>Imports metadata-free separate glTF as a canonical authored static mesh representation.</summary>
    public async Task<OperationResult<GltfNewModelImportResult>> ImportNewModelGltfFileAsync(
      string sourcePath,
      GltfNewModelImportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (sourcePath is null)
      {
        throw new ArgumentNullException(nameof(sourcePath));
      }

      profile ??= GltfOperationProfile.Default;
      options ??= new GltfNewModelImportOptions();
      try
      {
        var package = await ReadSeparatePackageAsync(sourcePath, profile, cancellationToken)
          .ConfigureAwait(false);
        var parsed = GlbDocument.ParseSeparateNewModel(
          package.Json,
          package.Binary,
          profile);
        GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferUri, package.Images);
        ValidateGeometryProfile(parsed, profile);
        return ImportNewModelParsed(parsed, profile, cancellationToken, options);
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfNewModelImportResult>();
      }
      catch (Exception ex)
      {
        return Failed<GltfNewModelImportResult>(ToDiagnostic(ex, sourcePath));
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
        var parsed = GlbDocument.Parse(bytes, profile);
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
        var parsed = GlbDocument.ParseSeparate(package.Json, package.Binary, profile);
        ValidateGeometryProfile(parsed, profile);
        ValidateMetadataProfile(parsed, profile);
        GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferUri, package.Images);
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

    private static async Task<(
      byte[] Json,
      byte[] Binary,
      string BufferUri,
      IReadOnlyDictionary<string, byte[]> Images)> ReadSeparatePackageAsync(
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
      var bufferUri = GlbDocument.GetSeparateBufferUri(json, profile);
      if (Path.IsPathRooted(bufferUri)
        || !string.Equals(Path.GetFileName(bufferUri), bufferUri, StringComparison.Ordinal)
        || bufferUri.IndexOfAny(new[] { '/', '\\' }) >= 0)
      {
        throw new InvalidDataException("The external buffer URI must be a safe relative file name.");
      }

      var directory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))
        ?? Directory.GetCurrentDirectory();
      var bufferPath = Path.Combine(directory, bufferUri);
      EnsureRegularSidecar(bufferPath);
      await using var binaryStream = new FileStream(
        bufferPath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        81920,
        true);
      EnsureRegularSidecar(bufferPath);
      var remaining = profile.MaxInputBytes - json.Length;
      if (remaining <= 0)
      {
        throw new ResourceLimitException(json.Length, profile.MaxInputBytes);
      }

      var binary = await ReadBoundedAsync(binaryStream, remaining, cancellationToken)
        .ConfigureAwait(false);
      remaining -= binary.Length;
      var images = new Dictionary<string, byte[]>(StringComparer.Ordinal);
      foreach (var imageUri in GlbDocument.GetSeparateImageUris(json, profile))
      {
        if (Path.IsPathRooted(imageUri)
          || !string.Equals(Path.GetFileName(imageUri), imageUri, StringComparison.Ordinal)
          || imageUri.IndexOfAny(new[] { '/', '\\' }) >= 0
          || (!imageUri.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            && !imageUri.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            && !imageUri.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)))
        {
          throw new InvalidDataException("An external image URI must be a safe relative PNG or JPEG file name.");
        }
        if (remaining <= 0)
        {
          throw new ResourceLimitException(profile.MaxInputBytes, profile.MaxInputBytes);
        }
        var imagePath = Path.Combine(directory, imageUri);
        EnsureRegularSidecar(imagePath);
        await using var imageStream = new FileStream(
          imagePath,
          FileMode.Open,
          FileAccess.Read,
          FileShare.Read,
          81920,
          true);
        EnsureRegularSidecar(imagePath);
        var image = await ReadBoundedAsync(imageStream, remaining, cancellationToken).ConfigureAwait(false);
        remaining -= image.Length;
        images.Add(imageUri, image);
      }
      return (json, binary, bufferUri, images);
    }

    private static void EnsureRegularSidecar(string path)
    {
      var attributes = File.GetAttributes(path);
      if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
      {
        throw new InvalidDataException("A separate glTF sidecar must be a regular contained file.");
      }
    }

    private static OperationResult<GltfNewModelImportResult> ImportNewModelParsed(
      ParsedGlb parsed,
      GltfOperationProfile profile,
      CancellationToken cancellationToken,
      GltfNewModelImportOptions options)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (parsed.HasReservedMetadata)
      {
        return Failed<GltfNewModelImportResult>(Diagnostic(
          GltfDiagnosticCodes.MisplacedMetadata,
          2009,
          "$",
          "New-model import requires input without reserved EarthTool metadata."));
      }

      long serializedLength;
      try
      {
        serializedLength = EarthTool.MSH.Internal.MshCanonicalSerializer
          .GetCanonicalStaticSerializedLength(parsed.Nodes
            .Where(node => node.MeshIndex.HasValue)
            .SelectMany(node => parsed.Meshes[node.MeshIndex!.Value].Primitives)
            .Select(primitive => (
               primitive.Vertices.Count,
               primitive.Triangles.Count)));
      }
      catch (OverflowException)
      {
        throw new ResourceLimitException(long.MaxValue, profile.MaxOutputBytes);
      }
      if (serializedLength > profile.MaxOutputBytes)
      {
        throw new ResourceLimitException(serializedLength, profile.MaxOutputBytes);
      }

      var animations = CreateNewModelAnimations(parsed, serializedLength, profile.MaxOutputBytes);
      ValidateNewModelMaterialBindings(parsed, options);
      var draft = CreateNewModelSourceTree(parsed, options, animations.AnimatedSourceNodes);
      var lineage = new MeshAssetLineageId(Guid.NewGuid());
      var build = StaticMeshBuilder.Create(Guid.NewGuid(), lineage)
        .SetRootSourceObject(draft.Source)
        .Build(new MshOperationProfile(
          maxOutputBytes: profile.MaxOutputBytes,
          maxStaticVerticesPerObject: profile.MaxActiveRenderVertices,
          maxStaticHierarchyDepth: profile.MaxHierarchyDepth));
      if (!build.TryGetValue(out var asset))
      {
        return new OperationResult<GltfNewModelImportResult>(
          OperationStatus.Failed,
          diagnostics: build.Diagnostics.Select(ToGltfAuthoringDiagnostic));
      }

      var edit = asset.Edit();
      ApplyNewModelPivots(draft, asset.RootSourceObject, edit);
      ApplyNewModelAnimations(animations, draft, asset.RootSourceObject, edit);
      ApplyNewModelBaseHeaderArtistObjects(parsed, edit);
      var committed = edit.Commit(new MshOperationProfile(
        maxOutputBytes: profile.MaxOutputBytes,
        maxStaticVerticesPerObject: profile.MaxActiveRenderVertices,
        maxStaticHierarchyDepth: profile.MaxHierarchyDepth));
      if (!committed.TryGetValue(out var authored))
      {
        return new OperationResult<GltfNewModelImportResult>(
          OperationStatus.Failed,
          diagnostics: committed.Diagnostics.Select(ToGltfAuthoringDiagnostic));
      }

      var baseline = new InterchangeBaseline(lineage.Value, Guid.NewGuid());
      return new OperationResult<GltfNewModelImportResult>(
        OperationStatus.Succeeded,
        new GltfNewModelImportResult(authored, baseline, CreateNewModelPreservationReport()));
    }

    private static void ValidateNewModelMaterialBindings(
      ParsedGlb parsed,
      GltfNewModelImportOptions options)
    {
      var usedMaterialIndices = parsed.Meshes.SelectMany(mesh => mesh.Primitives)
        .Where(primitive => primitive.MaterialIndex.HasValue)
        .Select(primitive => primitive.MaterialIndex!.Value)
        .Distinct()
        .ToArray();
      if (usedMaterialIndices.Any(index => parsed.Materials[index].HasBaseColorTexture
        && !options.TextureResourceBindings.ContainsKey(index)))
      {
        throw new UnsupportedGltfDomainException("TexResourceBinding");
      }
      foreach (var binding in options.TextureResourceBindings)
      {
        if (binding.Key >= parsed.Materials.Count)
        {
          throw new UnsupportedGltfDomainException("TexResourceBinding");
        }
        if (binding.Value is null)
        {
          continue;
        }
        var bytes = System.Text.Encoding.ASCII.GetBytes(binding.Value);
        if (bytes.Length != binding.Value.Length || !IsCanonicalTextureResourceKey(bytes))
        {
          throw new UnsupportedGltfDomainException("TexResourceBinding");
        }
      }
    }

    private static NewModelSourceDraft CreateNewModelSourceTree(
      ParsedGlb parsed,
      GltfNewModelImportOptions options,
      ISet<int> animatedSourceNodes)
    {
      var roots = CreateNewModelSources(
        parsed.RootNodeIndex,
        System.Numerics.Matrix4x4.Identity,
        parsed,
        options,
        animatedSourceNodes);
      if (roots.Count != 1)
      {
        throw new UnsupportedGltfDomainException("SceneMembership");
      }

      return roots[0];
    }

    private static IReadOnlyList<NewModelSourceDraft> CreateNewModelSources(
      int nodeIndex,
      System.Numerics.Matrix4x4 inheritedLinearTransform,
      ParsedGlb parsed,
      GltfNewModelImportOptions options,
      ISet<int> animatedSourceNodes)
    {
      var node = parsed.Nodes[nodeIndex];
      var effectiveTransform = node.LocalTransform * inheritedLinearTransform;
      if (!node.MeshIndex.HasValue)
      {
        if (GlbDocument.TryParseAttachmentHelperName(node.Name, out _)
          || GlbDocument.TryParseCannonRenderPositionHelperName(node.Name, out _))
        {
          if (node.Children.Count != 0)
          {
            throw new UnsupportedGltfDomainException("TransformOrHierarchy");
          }
          return Array.Empty<NewModelSourceDraft>();
        }
        var collapsed = node.Children
          .SelectMany(child => CreateNewModelSources(
            child,
            effectiveTransform,
            parsed,
            options,
            animatedSourceNodes))
          .ToArray();
        if (collapsed.Length == 0)
        {
          throw new UnsupportedGltfDomainException("TransformOrHierarchy");
        }
        return Array.AsReadOnly(collapsed);
      }

      var translation = effectiveTransform.Translation;
      var linearTransform = effectiveTransform;
      linearTransform.Translation = System.Numerics.Vector3.Zero;
      var authoredLinearTransform = animatedSourceNodes.Contains(nodeIndex)
        ? System.Numerics.Matrix4x4.Identity
        : linearTransform;
      if (!System.Numerics.Matrix4x4.Invert(authoredLinearTransform, out var inverse))
      {
        throw new UnsupportedGltfDomainException("TransformOrHierarchy");
      }

      var normalTransform = System.Numerics.Matrix4x4.Transpose(inverse);
      var reverseWinding = authoredLinearTransform.GetDeterminant() < 0;
      var mesh = parsed.Meshes[node.MeshIndex.Value];
      var renderObjects = mesh.Primitives.Select(primitive => new CanonicalStaticRenderObject(
        primitive.Vertices.Select(vertex => TransformNewModelVertex(
          vertex,
          authoredLinearTransform,
          normalTransform)),
        primitive.Triangles.Select(triangle => reverseWinding
          ? new CanonicalTriangle(triangle.Vertex0, triangle.Vertex2, triangle.Vertex1)
          : new CanonicalTriangle(triangle.Vertex0, triangle.Vertex1, triangle.Vertex2)),
        primitive.MaterialIndex.HasValue
          && options.TextureResourceBindings.TryGetValue(
            primitive.MaterialIndex.Value,
            out var textureResourceKey)
          ? textureResourceKey
          : null))
        .ToArray();
      var children = node.Children
        .SelectMany(child => CreateNewModelSources(
          child,
          animatedSourceNodes.Contains(nodeIndex)
            ? System.Numerics.Matrix4x4.Identity
            : linearTransform,
          parsed,
          options,
          animatedSourceNodes))
        .ToArray();
      var pivot = new System.Numerics.Vector3(translation.X, -translation.Z, translation.Y);
      return new[]
      {
        new NewModelSourceDraft(
          nodeIndex,
          new CanonicalStaticSourceObject(renderObjects, children.Select(child => child.Source)),
          pivot,
          children)
      };
    }

    private static void ApplyNewModelBaseHeaderArtistObjects(
      ParsedGlb parsed,
      StaticMeshEditSession edit)
    {
      var nodes = parsed.Nodes.Select(node => (node, (MetadataEnvelope?)null)).ToArray();
      var transforms = CreateArtistObjectTransforms(parsed.RootNodeIndex, nodes);
      var attachments = new Dictionary<int, int>();
      var cannons = new Dictionary<int, int>();
      for (var index = 0; index < parsed.Nodes.Count; index++)
      {
        var node = parsed.Nodes[index];
        if (node.MeshIndex.HasValue)
        {
          continue;
        }
        if (GlbDocument.TryParseAttachmentHelperName(node.Name, out var physicalNumber))
        {
          if (!attachments.TryAdd(physicalNumber, index))
          {
            throw ArtistObjectConflict("A physical attachment target is occupied more than once.");
          }
        }
        else if (GlbDocument.TryParseCannonRenderPositionHelperName(node.Name, out physicalNumber))
        {
          if (!cannons.TryAdd(physicalNumber, index))
          {
            throw ArtistObjectConflict("A cannon render-position target is occupied more than once.");
          }
        }
      }
      foreach (var attachment in attachments)
      {
        edit.ReplaceAttachmentRecord(
          attachment.Key,
          CreateAttachmentRecord(transforms[attachment.Value], 0x80));
      }
      foreach (var cannon in cannons)
      {
        edit.ReplaceCannonRenderPosition(
          cannon.Key,
          CreateCannonRenderPositionRecord(transforms[cannon.Value].Translation));
      }
    }

    private static CanonicalStaticVertex TransformNewModelVertex(
      RenderVertex vertex,
      System.Numerics.Matrix4x4 linearTransform,
      System.Numerics.Matrix4x4 normalTransform)
    {
      var position = System.Numerics.Vector3.Transform(vertex.Position, linearTransform);
      var normal = System.Numerics.Vector3.TransformNormal(vertex.Normal, normalTransform);
      var normalLengthSquared = normal.LengthSquared();
      if (!IsFinite(position)
        || !IsFinite(normal)
        || !float.IsFinite(normalLengthSquared)
        || normalLengthSquared == 0)
      {
        throw new UnsupportedGltfDomainException("TransformOrHierarchy");
      }

      normal = System.Numerics.Vector3.Normalize(normal);
      if (!IsFinite(normal) || normal.LengthSquared() == 0)
      {
        throw new UnsupportedGltfDomainException("TransformOrHierarchy");
      }
      return new CanonicalStaticVertex(
        new System.Numerics.Vector3(position.X, -position.Z, position.Y),
        new System.Numerics.Vector3(normal.X, -normal.Z, normal.Y),
        vertex.TextureCoordinate);
    }

    private static void ApplyNewModelPivots(
      NewModelSourceDraft draft,
      StaticSourceObject source,
      StaticMeshEditSession edit)
    {
      if (draft.Pivot != System.Numerics.Vector3.Zero)
      {
        edit.ReplacePivot(source.StaticRenderObjectIds[0], draft.Pivot);
      }
      for (var index = 0; index < draft.Children.Count; index++)
      {
        ApplyNewModelPivots(draft.Children[index], source.Children[index], edit);
      }
    }

    private static NewModelAnimationSet CreateNewModelAnimations(
      ParsedGlb parsed,
      long serializedLength,
      int maximumOutputLength)
    {
      if (parsed.Animations.Count == 0)
      {
        return new NewModelAnimationSet(default, Array.Empty<NewModelAnimationTrack>());
      }
      if (parsed.Animations.GroupBy(animation => animation.Name, StringComparer.Ordinal)
        .Any(group => group.Key is null || group.Count() != 1))
      {
        throw new UnsupportedGltfDomainException("animations");
      }

      var lengths = new byte[4];
      var tracks = new List<NewModelAnimationTrack>();
      var animatedSourceNodes = new HashSet<int>();
      foreach (var animation in parsed.Animations)
      {
        var classIndex = animation.Name switch
        {
          "EarthTool A" => 0,
          "EarthTool B" => 1,
          "EarthTool C" => 2,
          "EarthTool D" => 3,
          _ => throw new UnsupportedGltfDomainException("animations")
        };
        var endFrameValue = animation.EndTime * 24d;
        var endFrame = (int)Math.Round(endFrameValue);
        if (Math.Abs(endFrameValue - endFrame) > 1e-5 || endFrame is < 0 or >= byte.MaxValue)
        {
          throw new UnsupportedGltfDomainException("animations");
        }
        var frameCount = endFrame + 1;
        lengths[classIndex] = checked((byte)frameCount);
        var animatedObjects = animation.Objects.ToDictionary(item => item.NodeIndex);
        var consumedTargets = new HashSet<int>();
        var paths = new List<(int NodeIndex, IReadOnlyList<int> Path)>();
        CollectNewModelAnimationPaths(
          parsed.RootNodeIndex,
          Array.Empty<int>(),
          parsed,
          animatedObjects.Keys.ToHashSet(),
          consumedTargets,
          paths);
        if (!consumedTargets.OrderBy(value => value)
          .SequenceEqual(animatedObjects.Keys.OrderBy(value => value)))
        {
          throw new UnsupportedGltfDomainException("animations");
        }
        serializedLength = checked(serializedLength
          + ((long)paths.Count * frameCount * (12 + 12 + 64)));
        if (serializedLength > maximumOutputLength)
        {
          throw new ResourceLimitException(serializedLength, maximumOutputLength);
        }
        var sampledByNode = animatedObjects.ToDictionary(
          item => item.Key,
          item => item.Value.SampleFrames(frameCount));
        foreach (var path in paths)
        {
          tracks.Add(new NewModelAnimationTrack(
            path.NodeIndex,
            classIndex,
            ComposeNewModelAnimationFrames(path.Path, parsed, sampledByNode, frameCount)));
        }
      }

      foreach (var track in tracks)
      {
        if (!animatedSourceNodes.Add(track.NodeIndex))
        {
          throw new UnsupportedGltfDomainException("animations");
        }
      }
      return new NewModelAnimationSet(
        new AnimationClassBytes(lengths[0], lengths[1], lengths[2], lengths[3]),
        tracks.AsReadOnly());
    }

    private static void CollectNewModelAnimationPaths(
      int nodeIndex,
      IReadOnlyList<int> collapsedPath,
      ParsedGlb parsed,
      ISet<int> animatedNodes,
      ISet<int> consumedTargets,
      ICollection<(int NodeIndex, IReadOnlyList<int> Path)> paths)
    {
      var path = collapsedPath.Concat(new[] { nodeIndex }).ToArray();
      var node = parsed.Nodes[nodeIndex];
      if (node.MeshIndex.HasValue)
      {
        var targets = path.Where(animatedNodes.Contains).ToArray();
        if (targets.Length > 0)
        {
          foreach (var target in targets)
          {
            consumedTargets.Add(target);
          }
          paths.Add((nodeIndex, Array.AsReadOnly(path)));
        }
        foreach (var child in node.Children)
        {
          CollectNewModelAnimationPaths(
            child,
            Array.Empty<int>(),
            parsed,
            animatedNodes,
            consumedTargets,
            paths);
        }
        return;
      }

      foreach (var child in node.Children)
      {
        CollectNewModelAnimationPaths(
          child,
          path,
          parsed,
          animatedNodes,
          consumedTargets,
          paths);
      }
    }

    private static IReadOnlyList<ProjectedAnimationFrame> ComposeNewModelAnimationFrames(
      IReadOnlyList<int> path,
      ParsedGlb parsed,
      IReadOnlyDictionary<int, IReadOnlyList<ProjectedAnimationFrame>> sampledByNode,
      int frameCount)
    {
      var frames = new ProjectedAnimationFrame[frameCount];
      for (var frame = 0; frame < frameCount; frame++)
      {
        var effective = System.Numerics.Matrix4x4.Identity;
        foreach (var nodeIndex in path)
        {
          var local = sampledByNode.TryGetValue(nodeIndex, out var samples)
            ? ToMatrix(samples[frame])
            : parsed.Nodes[nodeIndex].LocalTransform;
          effective = local * effective;
        }
        try
        {
          frames[frame] = StaticAnimationProjection.Canonicalize(effective);
        }
        catch (InvalidDataException)
        {
          throw new UnsupportedGltfDomainException("animations");
        }
      }
      return Array.AsReadOnly(frames);
    }

    private static System.Numerics.Matrix4x4 ToMatrix(ProjectedAnimationFrame frame)
    {
      return System.Numerics.Matrix4x4.CreateScale(frame.Scale)
        * System.Numerics.Matrix4x4.CreateFromQuaternion(frame.Rotation)
        * System.Numerics.Matrix4x4.CreateTranslation(frame.Translation);
    }

    private static void ApplyNewModelAnimations(
      NewModelAnimationSet animations,
      NewModelSourceDraft draft,
      StaticSourceObject rootSourceObject,
      StaticMeshEditSession edit)
    {
      if (animations.Tracks.Count == 0)
      {
        return;
      }

      var sources = new Dictionary<int, StaticSourceObject>();
      AddNewModelSources(draft, rootSourceObject, sources);
      foreach (var animation in animations.Tracks)
      {
        var tracks = StaticAnimationProjection.CreateCanonicalTracks(animation.Frames);
        edit.ReplaceAnimation(
          sources[animation.NodeIndex].StaticRenderObjectIds[0],
          tracks.ScaleFrames,
          tracks.TranslationFrames,
          tracks.Matrices,
          checked((uint)animation.ClassIndex));
      }
      edit.ReplaceAnimationLengths(animations.Lengths);
    }

    private static void AddNewModelSources(
      NewModelSourceDraft draft,
      StaticSourceObject source,
      IDictionary<int, StaticSourceObject> result)
    {
      result.Add(draft.NodeIndex, source);
      for (var index = 0; index < draft.Children.Count; index++)
      {
        AddNewModelSources(draft.Children[index], source.Children[index], result);
      }
    }

    private static PreservationReport CreateNewModelPreservationReport()
    {
      return new PreservationReport(new[]
      {
        Canonicalized("ArchiveFraming"),
        Canonicalized("CommonBaseHeader"),
        Canonicalized("CommonBaseHeader.AnimationLengths"),
        Canonicalized("CommonBaseHeader.AnimationFrameIndices"),
        Canonicalized("CommonBaseHeader.Footprint"),
        Canonicalized("CommonBaseHeader.AttachmentTable"),
        Canonicalized("CommonBaseHeader.CannonRenderPositions"),
        Canonicalized("CommonBaseHeader.StaticLights"),
        Canonicalized("CommonBaseHeader.HorizontalExtents"),
        Canonicalized("StaticRenderObjectSequence"),
        Canonicalized("StaticRenderObjectSequence[*].AnimationClassValue"),
        Canonicalized("StaticRenderObjectSequence[*].AnimationTracks.ScaleFrames"),
        Canonicalized("StaticRenderObjectSequence[*].AnimationTracks.TranslationFrames"),
        Canonicalized("StaticRenderObjectSequence[*].AnimationTracks.Matrices"),
        Canonicalized("StaticRenderObjectSequence[*].TexturePathBytes"),
        Canonicalized("RootSourceObject"),
        Canonicalized("RootTrailingBytes")
      });
    }

    private static PreservationChange Canonicalized(string path)
    {
      return new PreservationChange(path, PreservationDisposition.Canonicalized, "NewModelImport");
    }

    private static OperationDiagnostic ToGltfAuthoringDiagnostic(OperationDiagnostic diagnostic)
    {
      return new OperationDiagnostic(
        diagnostic.Code == MshDiagnosticCodes.ResourceLimitExceeded
          ? GltfDiagnosticCodes.ResourceLimitExceeded
          : GltfDiagnosticCodes.InvalidGeometry,
        diagnostic.Code == MshDiagnosticCodes.ResourceLimitExceeded ? 1101 : 1106,
        diagnostic.Severity,
        diagnostic.Path,
        diagnostic.Message,
        diagnostic.ByteOffset,
        diagnostic.Data);
    }

    private sealed class NewModelAnimationSet
    {
      internal AnimationClassBytes Lengths { get; }

      internal IReadOnlyList<NewModelAnimationTrack> Tracks { get; }

      internal ISet<int> AnimatedSourceNodes { get; }

      internal NewModelAnimationSet(
        AnimationClassBytes lengths,
        IReadOnlyList<NewModelAnimationTrack> tracks)
      {
        Lengths = lengths;
        Tracks = tracks;
        AnimatedSourceNodes = new HashSet<int>(tracks.Select(track => track.NodeIndex));
      }
    }

    private sealed class NewModelAnimationTrack
    {
      internal int NodeIndex { get; }

      internal int ClassIndex { get; }

      internal IReadOnlyList<ProjectedAnimationFrame> Frames { get; }

      internal NewModelAnimationTrack(
        int nodeIndex,
        int classIndex,
        IReadOnlyList<ProjectedAnimationFrame> frames)
      {
        NodeIndex = nodeIndex;
        ClassIndex = classIndex;
        Frames = frames;
      }
    }

    private sealed class NewModelSourceDraft
    {
      internal int NodeIndex { get; }

      internal CanonicalStaticSourceObject Source { get; }

      internal System.Numerics.Vector3 Pivot { get; }

      internal IReadOnlyList<NewModelSourceDraft> Children { get; }

      internal NewModelSourceDraft(
        int nodeIndex,
        CanonicalStaticSourceObject source,
        System.Numerics.Vector3 pivot,
        IReadOnlyList<NewModelSourceDraft> children)
      {
        NodeIndex = nodeIndex;
        Source = source;
        Pivot = pivot;
        Children = children;
      }
    }

    private static async Task<OperationResult<GltfEditImportResult>> ImportParsedAsync(
      ParsedGlb parsed,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile profile,
      CancellationToken cancellationToken)
    {
      var manifest = GlbDocument.ParseMetadata(
        parsed.ManifestMetadata ?? throw new MissingMetadataException("scene"),
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
            mesh.Metadata ?? throw new MissingMetadataException("mesh"),
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
      ReconcileBaseHeaderArtistObjects(
        parsed,
        nodes.Select(node => (node.Parsed, node.Metadata)).ToArray(),
        asset,
        expectedBaseline,
        edit);
      var hierarchy = ReconcileHierarchy(
        parsed,
        nodes.Select(node => (node.Parsed, node.Metadata)).ToArray(),
        meshes.Select(mesh => (mesh.Parsed, mesh.Metadata)).ToArray(),
        asset,
        expectedBaseline,
        edit);
      try
      {
        var animationReplacements = ValidateAnimationProjection(
          parsed,
          manifest,
          nodes.Where(node => node.Metadata?.ScopeKind == "object"
              && node.Metadata.AttachmentRecord is null
              && node.Metadata.CannonRenderPosition is null)
            .Select(node => node.Metadata).ToArray(),
          asset,
          expectedBaseline,
          profile.MaxOutputBytes);
        var sourcesByLocalId = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
          .ToDictionary(source => source.Id.Value);
        foreach (var replacement in animationReplacements)
        {
          var tracks = StaticAnimationProjection.CreateCanonicalTracks(replacement.Frames);
          edit.ReplaceAnimation(
            sourcesByLocalId[replacement.SourceObjectLocalId].StaticRenderObjectIds[0],
            tracks.ScaleFrames,
            tracks.TranslationFrames,
            tracks.Matrices,
            replacement.AnimationClassValue);
        }
      }
      catch (StaleNativeProjectionException ex)
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.StaleNativeProjection,
          2008,
          "animations",
          ex.Message));
      }
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
      ApplyMaterialBindings(
        parsed,
        asset,
        expectedBaseline,
        partitionMatches.Select(match => match.Partition)
          .Concat(hierarchy.AddedPartitions)
          .ToArray(),
        edit,
        profile);

      if (hierarchy.Changed)
      {
        edit.ApplyHierarchy(hierarchy.Root, hierarchy.Sequence);
      }

      var partitions = partitionMatches.Select(match => match.Partition)
        .Concat(hierarchy.AddedPartitions).ToArray();
      var fingerprint = StaticGeometryFingerprint.Create(expectedBaseline, partitions);
      var committed = edit.Commit(new MshOperationProfile(
        maxOutputBytes: profile.MaxOutputBytes,
        maxStaticVerticesPerObject: profile.MaxActiveRenderVertices,
        maxStaticHierarchyDepth: profile.MaxHierarchyDepth));
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
      var restoredRecordIndices = partitionMatches
        .Where(match => match.Retained)
        .Select(match => asset.StaticRenderObjectSequence
          .Select((record, index) => (record, index))
          .Single(item => item.record.LocalId == match.Partition.LocalId).index)
        .Where(index => !changedRecordPaths.Any(path =>
          path == $"StaticRenderObjectSequence[{index}]"
          || path.StartsWith($"StaticRenderObjectSequence[{index}].", StringComparison.Ordinal)))
        .Distinct()
        .OrderBy(index => index)
        .ToArray();
      var restoredPaths = new[]
      {
        "ArchiveFraming",
        "CommonBaseHeader.AnimationLengths",
        "CommonBaseHeader.AnimationFrameIndices"
      }.Concat(restoredRecordIndices.Select(index => $"StaticRenderObjectSequence[{index}]"))
        .Concat(restoredRecordIndices.SelectMany(index => new[]
        {
          $"StaticRenderObjectSequence[{index}].AnimationClassValue",
          $"StaticRenderObjectSequence[{index}].AnimationTracks.ScaleFrames",
          $"StaticRenderObjectSequence[{index}].AnimationTracks.TranslationFrames",
          $"StaticRenderObjectSequence[{index}].AnimationTracks.Matrices"
        }))
        .Concat(Enumerable.Range(1, 49)
          .Select(number => $"CommonBaseHeader.AttachmentTable[{number}]")
          .Where(path => !changedRecordPaths.Contains(path, StringComparer.Ordinal)))
        .Concat(Enumerable.Range(1, 4)
          .Select(number => $"CommonBaseHeader.CannonRenderPositions[{number}]")
          .Where(path => !changedRecordPaths.Contains(path, StringComparer.Ordinal)));
      return new OperationResult<GltfEditImportResult>(
        OperationStatus.Succeeded,
        new GltfEditImportResult(
          reconciled,
          nextBaseline,
          fingerprint,
          committed.Preservation,
          restoredPaths));
    }

    private static void ApplyMaterialBindings(
      ParsedGlb parsed,
      StaticMeshAsset asset,
      InterchangeBaseline expectedBaseline,
      IReadOnlyList<GeometryPartition> partitions,
      StaticMeshEditSession edit,
      GltfOperationProfile profile)
    {
      var materials = parsed.Materials.Select(material => GlbDocument.ParseMetadata(
        material.Metadata ?? throw new MissingMetadataException("material"),
        profile.MaxMetadataBytes,
        profile.MaxJsonDepth)).ToArray();
      foreach (var material in materials)
      {
        if (material.ScopeKind != "material"
          || material.LocalId <= 0
          || material.AssetLineageId != expectedBaseline.AssetLineageId
          || material.DocumentId != expectedBaseline.DocumentId
          || material.SourceMsh is not null
          || material.Fingerprint is not null
          || material.Partitions.Count != 0
          || material.TextureBinding is null)
        {
          throw new MalformedMetadataException("The material metadata envelope is malformed.");
        }
        var origin = asset.StaticRenderObjectSequence.SingleOrDefault(record =>
          record.LocalId == material.LocalId);
        if ((origin is null || !origin.TexturePathBytes.SequenceEqual(material.TextureBinding))
          && material.TextureBinding.Count != 0)
        {
          if (!IsCanonicalTextureResourceKey(material.TextureBinding))
          {
            throw new UnsupportedGltfDomainException("TexResourceBinding");
          }
        }
      }

      foreach (var partition in partitions)
      {
        var materialIndex = partition.MaterialIndex
          ?? throw new UnsupportedGltfDomainException("TexResourceBinding");
        if (materialIndex < 0 || materialIndex >= materials.Length)
        {
          throw new MalformedMetadataException("A material assignment is outside the material scope set.");
        }
        var binding = materials[materialIndex].TextureBinding!;
        var record = asset.StaticRenderObjectSequence.SingleOrDefault(item =>
          item.LocalId == partition.LocalId);
        if (record is not null && record.TexturePathBytes.SequenceEqual(binding))
        {
          continue;
        }
        var id = record?.Id ?? new StaticRenderObjectId(asset.LineageId, partition.LocalId);
        edit.ReplaceTexturePathBytes(id, binding);
      }
    }

    private static bool IsCanonicalTextureResourceKey(IReadOnlyList<byte> bytes)
    {
      if (bytes.Any(value => value is 0 or > 0x7F))
      {
        return false;
      }
      var value = System.Text.Encoding.ASCII.GetString(bytes.ToArray());
      return EarthTool.MSH.Authoring.AuthoringValidation.IsCanonicalTextureResourceKey(value);
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
        parsed.ManifestMetadata ?? throw new MissingMetadataException("scene"),
        profile.MaxMetadataBytes,
        profile.MaxJsonDepth);
      foreach (var metadata in parsed.Nodes.Select(node => node.Metadata)
        .Concat(parsed.Meshes.Select(mesh => mesh.Metadata))
        .Concat(parsed.Materials.Select(material => material.Metadata)))
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

    private static IReadOnlyList<AnimationReplacement> ValidateAnimationProjection(
      ParsedGlb parsed,
      MetadataEnvelope manifest,
      IReadOnlyList<MetadataEnvelope?> nodes,
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      int maximumOutputLength)
    {
      long estimatedOutputLength = asset.SerializedLength;
      if (estimatedOutputLength > maximumOutputLength)
      {
        throw new ResourceLimitException(estimatedOutputLength, maximumOutputLength);
      }
      var expected = StaticAnimationProjection.Create(asset, baseline);
      if (!manifest.AnimationLengths.HasValue
        || !manifest.AnimationLengths.Value.Equals(asset.CommonBaseHeader.AnimationLengths)
        || !manifest.AnimationFrameIndices.HasValue
        || !manifest.AnimationFrameIndices.Value.Equals(asset.CommonBaseHeader.AnimationFrameIndices)
        || manifest.AnimationClasses.Select(item => item.ClassIndex).Distinct().Count()
          != manifest.AnimationClasses.Count)
      {
        throw new MalformedMetadataException("The manifest static animation metadata is malformed.");
      }

      var expectedGroups = expected.Objects.Where(item => item.HasSourceTracks)
        .GroupBy(item => item.ClassIndex)
        .OrderBy(group => group.Key).ToArray();
      if (manifest.AnimationClasses.Count != expectedGroups.Length)
      {
        throw new MalformedMetadataException("The manifest static animation class set is incomplete.");
      }
      foreach (var group in expectedGroups)
      {
        var metadata = manifest.AnimationClasses.SingleOrDefault(item => item.ClassIndex == group.Key)
          ?? throw new MalformedMetadataException("The manifest static animation class set is incomplete.");
        var expectedObjects = group.OrderBy(item => item.SourceObjectLocalId).ToArray();
        var expectedNativeObjects = expectedObjects.Where(item => item.IsNative).ToArray();
        var expectedClip = expected.Clips.SingleOrDefault(item => item.ClassIndex == group.Key);
        if (!metadata.Objects.SequenceEqual(expectedObjects.Select(item => item.SourceObjectLocalId))
          || !metadata.NativeObjects.SequenceEqual(expectedNativeObjects.Select(item => item.SourceObjectLocalId))
          || !string.Equals(metadata.Fingerprint, expectedClip?.Fingerprint, StringComparison.Ordinal))
        {
          throw new MalformedMetadataException("The manifest static animation class binding is malformed.");
        }
      }

      var expectedBySource = expected.Objects.ToDictionary(item => item.SourceObjectLocalId);
      var retainedNodeBySource = new Dictionary<int, int>();
      for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
      {
        var metadata = nodes[nodeIndex];
        if (metadata is null)
        {
          continue;
        }
        if (!expectedBySource.TryGetValue(metadata.LocalId, out var expectedObject))
        {
          if (metadata.AnimationProjection is not null)
          {
            throw new MalformedMetadataException("Animation metadata appears on an object without source tracks.");
          }
          continue;
        }
        var animation = metadata.AnimationProjection;
        if (animation is null
          || animation.AnimationClassValue != expectedObject.AnimationClassValue
          || animation.ClassIndex != expectedObject.ClassIndex
          || animation.DeclaredLength != expectedObject.DeclaredLength
          || animation.IsNative != expectedObject.IsNative
          || animation.HasSourceTracks != expectedObject.HasSourceTracks
          || !string.Equals(animation.Fingerprint, expectedObject.Fingerprint, StringComparison.Ordinal)
          || !animation.ScaleFrames.SequenceEqual(
            StaticAnimationProjection.SerializeScaleFrames(expectedObject.SourceTracks))
          || !animation.TranslationFrames.SequenceEqual(
            StaticAnimationProjection.SerializeTranslationFrames(expectedObject.SourceTracks))
          || !animation.Matrices.SequenceEqual(
            StaticAnimationProjection.SerializeMatrices(expectedObject.SourceTracks)))
        {
          throw new MalformedMetadataException("The object static animation metadata is malformed.");
        }
        retainedNodeBySource.Add(metadata.LocalId, nodeIndex);
      }

      var matchedClasses = new HashSet<int>();
      var matchedObjects = new HashSet<int>();
      var replacements = new List<AnimationReplacement>();
      foreach (var clip in parsed.Animations)
      {
        if (clip.Objects.Count == 0)
        {
          throw new StaleNativeProjectionException("A native animation clip has no participating objects.");
        }
        int? classIndex = null;
        foreach (var item in clip.Objects)
        {
          var sourcePair = retainedNodeBySource.SingleOrDefault(pair => pair.Value == item.NodeIndex);
          if (sourcePair.Key == 0
            || !expectedBySource.TryGetValue(sourcePair.Key, out var expectedObject)
            || !expectedObject.IsNative)
          {
            throw new StaleNativeProjectionException(
              "A native animation targets an unexpected or metadata-only object.");
          }
          if (classIndex.HasValue && classIndex.Value != expectedObject.ClassIndex)
          {
            throw new StaleNativeProjectionException("One native clip combines different animation classes.");
          }
          classIndex = expectedObject.ClassIndex;
          if (!matchedObjects.Add(sourcePair.Key))
          {
            throw new StaleNativeProjectionException(
              "One object/class animation maps to multiple native clips.");
          }
          var frameCount = expectedObject.DeclaredLength == 0 ? 1 : expectedObject.DeclaredLength;
          if (item.EndTime > ((frameCount - 1) / 24f) + 1e-6f)
          {
            throw new StaleNativeProjectionException(
              "A native animation extends beyond its guarded class declaration.");
          }
          var frames = item.SampleFrames(frameCount);
          var fingerprint = AnimationProjectionFingerprint.CreateObject(
            baseline,
            sourcePair.Key,
            expectedObject.ClassIndex,
            expectedObject.DeclaredLength,
            frames);
          if (!string.Equals(fingerprint, expectedObject.Fingerprint, StringComparison.Ordinal))
          {
            estimatedOutputLength = GetAnimationReplacementLength(
              estimatedOutputLength,
              expectedObject.SourceTracks,
              frames.Count);
            if (estimatedOutputLength > maximumOutputLength)
            {
              throw new ResourceLimitException(estimatedOutputLength, maximumOutputLength);
            }
            replacements.Add(new AnimationReplacement(
              sourcePair.Key,
              expectedObject.AnimationClassValue,
              frames));
          }
        }

        var resolvedClass = classIndex!.Value;
        if (!matchedClasses.Add(resolvedClass))
        {
          throw new StaleNativeProjectionException("One animation class maps to multiple native clips.");
        }
      }

      foreach (var deleted in expected.Objects.Where(item =>
        item.IsNative
        && retainedNodeBySource.ContainsKey(item.SourceObjectLocalId)
        && !matchedObjects.Contains(item.SourceObjectLocalId)))
      {
        estimatedOutputLength = GetAnimationReplacementLength(
          estimatedOutputLength,
          deleted.SourceTracks,
          0);
        replacements.Add(new AnimationReplacement(
          deleted.SourceObjectLocalId,
          deleted.AnimationClassValue,
          Array.Empty<ProjectedAnimationFrame>()));
      }
      return replacements.AsReadOnly();
    }

    private static long GetAnimationReplacementLength(
      long currentLength,
      StaticAnimationTracks source,
      int frameCount)
    {
      var sourceLength = checked(
        (source.ScaleFrames.Count * 12L)
        + (source.TranslationFrames.Count * 12L)
        + (source.Matrices.Count * 64L));
      return checked(currentLength - sourceLength + (frameCount * 88L));
    }

    private sealed class AnimationReplacement
    {
      internal int SourceObjectLocalId { get; }

      internal uint AnimationClassValue { get; }

      internal IReadOnlyList<ProjectedAnimationFrame> Frames { get; }

      internal AnimationReplacement(
        int sourceObjectLocalId,
        uint animationClassValue,
        IReadOnlyList<ProjectedAnimationFrame> frames)
      {
        SourceObjectLocalId = sourceObjectLocalId;
        AnimationClassValue = animationClassValue;
        Frames = frames;
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
        var sourceGeometry = sourceRecords.Select((record, index) => new GeometryPartition(
          record.LocalId,
          record.RenderVertices.Select(GlbDocument.ProjectToGltf).ToArray(),
          record.Triangles,
          index < mesh.Parsed.Primitives.Count
            ? mesh.Parsed.Primitives[index].MaterialIndex
            : null)).ToArray();
        var currentGeometry = mesh.Parsed.Primitives.Select(primitive => new GeometryPartition(
          0,
          primitive.Vertices,
          primitive.Triangles,
          primitive.MaterialIndex)).ToArray();
        if (string.Equals(
          StaticGeometryFingerprint.CreateSurfaceKey(sourceGeometry),
          StaticGeometryFingerprint.CreateSurfaceKey(currentGeometry),
          StringComparison.Ordinal))
        {
          if (sourceGeometry.Length != currentGeometry.Length)
          {
            var materialIndices = currentGeometry.Select(partition => partition.MaterialIndex)
              .Distinct()
              .ToArray();
            if (materialIndices.Length != 1)
            {
              throw new AmbiguousPartitionCorrespondenceException(
                "A representation-only partition split has conflicting material assignments.");
            }
            result.AddRange(sourceGeometry.Select(partition => new PartitionMatch(
              new GeometryPartition(
                partition.LocalId,
                partition.Vertices,
                partition.Triangles,
                materialIndices[0]),
              source.Id,
              true,
              false)));
            continue;
          }
          var sourceBySurface = sourceGeometry.GroupBy(partition =>
              StaticGeometryFingerprint.CreateSurfaceKey(
                partition.Vertices,
                partition.Triangles))
            .ToDictionary(
              group => group.Key,
              group => new Queue<GeometryPartition>(group));
          foreach (var current in currentGeometry)
          {
            var surface = StaticGeometryFingerprint.CreateSurfaceKey(
              current.Vertices,
              current.Triangles);
            if (!sourceBySurface.TryGetValue(surface, out var candidates)
              || candidates.Count == 0)
            {
              throw new AmbiguousPartitionCorrespondenceException(
                "The unchanged native surface set did not identify each preserved partition.");
            }
            var sourcePartition = candidates.Dequeue();
            result.Add(new PartitionMatch(
              new GeometryPartition(
                sourcePartition.LocalId,
                current.Vertices,
                current.Triangles,
                current.MaterialIndex),
              source.Id,
              true,
              false));
          }
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
                  primitive.Triangles,
                  primitive.MaterialIndex),
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
                  primitive.Triangles,
                  primitive.MaterialIndex),
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
              new GeometryPartition(
                -1,
                item.Primitive.Vertices,
                item.Primitive.Triangles,
                item.Primitive.MaterialIndex),
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
                primitive.Triangles,
                primitive.MaterialIndex),
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

    private static void ReconcileBaseHeaderArtistObjects(
      ParsedGlb parsed,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      StaticMeshAsset asset,
      InterchangeBaseline expected,
      StaticMeshEditSession edit)
    {
      var transforms = CreateArtistObjectTransforms(parsed.RootNodeIndex, nodes);
      var attachmentCandidates = new Dictionary<int, List<int>>();
      var cannonCandidates = new Dictionary<int, List<int>>();
      for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
      {
        var node = nodes[nodeIndex];
        int physicalNumber;
        if (node.Metadata?.AttachmentRecord is not null)
        {
          physicalNumber = ValidateAttachmentMetadata(node.Metadata, asset, expected);
          AddArtistCandidate(attachmentCandidates, physicalNumber, nodeIndex);
        }
        else if (node.Metadata?.CannonRenderPosition is not null)
        {
          physicalNumber = ValidateCannonRenderPositionMetadata(node.Metadata, asset, expected);
          AddArtistCandidate(cannonCandidates, physicalNumber, nodeIndex);
        }
        else if (node.Metadata is null
          && !node.Parsed.MeshIndex.HasValue
          && GlbDocument.TryParseAttachmentHelperName(node.Parsed.Name, out physicalNumber))
        {
          AddArtistCandidate(attachmentCandidates, physicalNumber, nodeIndex);
        }
        else if (node.Metadata is null
          && !node.Parsed.MeshIndex.HasValue
          && GlbDocument.TryParseCannonRenderPositionHelperName(node.Parsed.Name, out physicalNumber))
        {
          AddArtistCandidate(cannonCandidates, physicalNumber, nodeIndex);
        }
      }

      if (attachmentCandidates.Values.Any(candidates => candidates.Count != 1)
        || cannonCandidates.Values.Any(candidates => candidates.Count != 1))
      {
        throw ArtistObjectConflict("A physical helper target is occupied by more than one artist object.");
      }
      foreach (var candidate in attachmentCandidates.Concat(cannonCandidates))
      {
        var node = nodes[candidate.Value[0]].Parsed;
        if (node.MeshIndex.HasValue || node.Children.Count != 0 || !transforms.ContainsKey(candidate.Value[0]))
        {
          throw new UnsupportedGltfDomainException("AttachmentOrCannonArtistObject");
        }
      }

      var attachmentTable = asset.CommonBaseHeader.AttachmentTable.ToArray();
      for (var physicalNumber = 1; physicalNumber <= 49; physicalNumber++)
      {
        if (physicalNumber is >= 13 and <= 20)
        {
          continue;
        }
        var sourceRecord = attachmentTable.AsSpan((physicalNumber - 1) * 8, 8).ToArray();
        var sourceActive = BinaryPrimitives.ReadInt16LittleEndian(sourceRecord) != short.MinValue;
        if (!attachmentCandidates.TryGetValue(physicalNumber, out var candidates))
        {
          if (sourceActive)
          {
            edit.ReplaceAttachmentRecord(physicalNumber, CreateAbsentAttachmentRecord());
          }
          continue;
        }

        var nodeIndex = candidates[0];
        var metadata = nodes[nodeIndex].Metadata;
        if (metadata is not null
          && metadata.LocalId != physicalNumber
          && sourceActive)
        {
          throw ArtistObjectConflict("An attachment cannot be rebound to an occupied physical target.");
        }
        var extra = metadata is null ? (byte)0x80 : metadata.AttachmentRecord![7];
        var replacement = CreateAttachmentRecord(transforms[nodeIndex], extra);
        if (metadata is null || !replacement.SequenceEqual(sourceRecord))
        {
          edit.ReplaceAttachmentRecord(physicalNumber, replacement);
        }
      }

      var cannonRecords = asset.CommonBaseHeader.CannonRenderPositions.ToArray();
      for (var physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        if (!cannonCandidates.TryGetValue(physicalNumber, out var candidates))
        {
          throw ArtistObjectConflict("Every cannon render-position artist object must remain present.");
        }
        var sourceRecord = cannonRecords.AsSpan((physicalNumber - 1) * 12, 12).ToArray();
        var translation = transforms[candidates[0]].Translation;
        var sourcePreview = new Vector3(
          GlbDocument.ReadFinitePreview(sourceRecord, 0),
          GlbDocument.ReadFinitePreview(sourceRecord, 8),
          GlbDocument.ReadFinitePreview(sourceRecord, 4));
        if (translation != sourcePreview)
        {
          edit.ReplaceCannonRenderPosition(
            physicalNumber,
            CreateCannonRenderPositionRecord(translation));
        }
      }
    }

    private static int ValidateAttachmentMetadata(
      MetadataEnvelope metadata,
      StaticMeshAsset asset,
      InterchangeBaseline expected)
    {
      var physicalNumber = metadata.AttachmentPhysicalNumber;
      if (metadata.AssetLineageId != expected.AssetLineageId
        || metadata.DocumentId != expected.DocumentId
        || metadata.ScopeKind != "object"
        || metadata.LocalId is < 1 or > 49
        || metadata.LocalId is >= 13 and <= 20
        || physicalNumber is null or < 1 or > 49
        || physicalNumber is >= 13 and <= 20
        || metadata.AttachmentRecord?.Count != 8
        || metadata.FingerprintName != "attachment.pose"
        || metadata.FingerprintVersion != 1
        || !HasNoUnrelatedArtistObjectMetadata(metadata))
      {
        throw new MalformedMetadataException("The attachment metadata envelope is malformed.");
      }
      var sourceRecord = asset.CommonBaseHeader.AttachmentTable
        .Skip((metadata.LocalId - 1) * 8).Take(8);
      if (!sourceRecord.SequenceEqual(metadata.AttachmentRecord)
        || metadata.Fingerprint != GlbDocument.CreateAttachmentPoseFingerprint(
          expected,
          metadata.LocalId,
          sourceRecord.ToArray())
        || BinaryPrimitives.ReadInt16LittleEndian(metadata.AttachmentRecord.ToArray()) == short.MinValue)
      {
        throw new MalformedMetadataException("The attachment metadata does not match its source record.");
      }
      return physicalNumber.Value;
    }

    private static int ValidateCannonRenderPositionMetadata(
      MetadataEnvelope metadata,
      StaticMeshAsset asset,
      InterchangeBaseline expected)
    {
      var physicalNumber = metadata.CannonRenderPositionNumber;
      if (metadata.AssetLineageId != expected.AssetLineageId
        || metadata.DocumentId != expected.DocumentId
        || metadata.ScopeKind != "object"
        || metadata.LocalId is < 1 or > 4
        || metadata.LocalId != physicalNumber
        || physicalNumber is null or < 1 or > 4
        || metadata.CannonRenderPosition?.Count != 12
        || metadata.FingerprintName != "cannonRenderPosition.position"
        || metadata.FingerprintVersion != 1
        || !HasNoUnrelatedArtistObjectMetadata(metadata))
      {
        throw new MalformedMetadataException("The cannon render-position metadata envelope is malformed.");
      }
      var sourceRecord = asset.CommonBaseHeader.CannonRenderPositions
        .Skip((metadata.LocalId - 1) * 12).Take(12);
      if (!sourceRecord.SequenceEqual(metadata.CannonRenderPosition)
        || metadata.Fingerprint != GlbDocument.CreateCannonRenderPositionFingerprint(
          expected,
          metadata.LocalId,
          sourceRecord.ToArray()))
      {
        throw new MalformedMetadataException(
          "The cannon render-position metadata does not match its source record.");
      }
      return physicalNumber.Value;
    }

    private static bool HasNoUnrelatedArtistObjectMetadata(MetadataEnvelope metadata)
    {
      return metadata.SourceMsh is null
        && metadata.Partitions.Count == 0
        && metadata.StaticRenderObjectLocalIds.Count == 0
        && metadata.SourceObjectLocalIds.Count == 0
        && metadata.StaticRenderObjectInventory.Count == 0
        && metadata.SourceObjectInventory.Count == 0
        && metadata.NextStaticRenderObjectLocalId is null
        && metadata.NextSourceObjectLocalId is null
        && metadata.TextureBinding is null
        && metadata.AnimationLengths is null
        && metadata.AnimationFrameIndices is null
        && metadata.AnimationClasses.Count == 0
        && metadata.AnimationProjection is null;
    }

    private static void AddArtistCandidate(
      IDictionary<int, List<int>> candidates,
      int physicalNumber,
      int nodeIndex)
    {
      if (!candidates.TryGetValue(physicalNumber, out var nodes))
      {
        nodes = new List<int>();
        candidates.Add(physicalNumber, nodes);
      }
      nodes.Add(nodeIndex);
    }

    private static MetadataIdentityException ArtistObjectConflict(string message)
    {
      return new MetadataIdentityException(
        GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
        2012,
        message);
    }

    private static IReadOnlyDictionary<int, Matrix4x4> CreateArtistObjectTransforms(
      int rootNodeIndex,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes)
    {
      var result = new Dictionary<int, Matrix4x4>();
      AddArtistObjectTransforms(rootNodeIndex, Matrix4x4.Identity, nodes, result);
      return result;
    }

    private static void AddArtistObjectTransforms(
      int nodeIndex,
      Matrix4x4 inheritedTransform,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      IDictionary<int, Matrix4x4> result)
    {
      var node = nodes[nodeIndex];
      var effective = node.Parsed.LocalTransform * inheritedTransform;
      var isArtistObject = node.Metadata?.AttachmentRecord is not null
        || node.Metadata?.CannonRenderPosition is not null
        || node.Metadata is null
          && (GlbDocument.TryParseAttachmentHelperName(node.Parsed.Name, out _)
            || GlbDocument.TryParseCannonRenderPositionHelperName(node.Parsed.Name, out _));
      if (isArtistObject)
      {
        result.Add(nodeIndex, effective);
        return;
      }
      var childInherited = node.Parsed.MeshIndex.HasValue ? Matrix4x4.Identity : effective;
      foreach (var child in node.Parsed.Children)
      {
        AddArtistObjectTransforms(child, childInherited, nodes, result);
      }
    }

    private static byte[] CreateAttachmentRecord(Matrix4x4 transform, byte extra)
    {
      if (!Matrix4x4.Decompose(transform, out var scale, out var rotation, out var translation)
        || !IsFinite(scale)
        || !IsFinite(translation)
        || !IsFinite(rotation))
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }
      var reconstructed = Matrix4x4.CreateScale(scale)
        * Matrix4x4.CreateFromQuaternion(rotation)
        * Matrix4x4.CreateTranslation(translation);
      if (!NearlyEqual(transform, reconstructed, 1e-4f))
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }

      var direction = Vector3.TransformNormal(-Vector3.UnitZ, Matrix4x4.CreateFromQuaternion(rotation));
      if (!IsFinite(direction) || direction.LengthSquared() == 0)
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }
      direction = Vector3.Normalize(direction);
      const float halfHeadingStep = MathF.PI / 256;
      var up = Vector3.TransformNormal(Vector3.UnitY, Matrix4x4.CreateFromQuaternion(rotation));
      if (MathF.Abs(direction.Y) > MathF.Sin(halfHeadingStep) + 1e-5f
        || !IsFinite(up)
        || Vector3.Dot(Vector3.Normalize(up), Vector3.UnitY) < MathF.Cos(halfHeadingStep) - 1e-5f)
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }
      var horizontal = new Vector2(direction.X, direction.Z);
      if (horizontal.LengthSquared() == 0)
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }
      horizontal = Vector2.Normalize(horizontal);
      var angle = MathF.Atan2(-horizontal.Y, horizontal.X);
      if (angle < 0)
      {
        angle += MathF.PI * 2;
      }
      var heading = unchecked((byte)((int)MathF.Floor((angle * 256 / (MathF.PI * 2)) + 0.5f) & 0xFF));
      var reconstructedDirection = new Vector2(
        MathF.Cos(heading * MathF.PI * 2 / 256),
        -MathF.Sin(heading * MathF.PI * 2 / 256));
      var error = MathF.Acos(Math.Clamp(Vector2.Dot(horizontal, reconstructedDirection), -1, 1));
      var reconstructedRotation = Quaternion.CreateFromAxisAngle(
        Vector3.UnitY,
        (heading * MathF.PI * 2 / 256) - (MathF.PI / 2));
      var rotationError = 2 * MathF.Acos(Math.Clamp(
        MathF.Abs(Quaternion.Dot(Quaternion.Normalize(rotation), reconstructedRotation)),
        -1,
        1));
      if (error > halfHeadingStep + 1e-5f
        || rotationError > halfHeadingStep + 1e-5f)
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }

      var record = new byte[8];
      BinaryPrimitives.WriteInt16LittleEndian(record, QuantizeAttachmentCoordinate(translation.X, true));
      BinaryPrimitives.WriteInt16LittleEndian(
        record.AsSpan(2),
        QuantizeAttachmentCoordinate(translation.Z, false));
      BinaryPrimitives.WriteInt16LittleEndian(
        record.AsSpan(4),
        QuantizeAttachmentCoordinate(translation.Y, false));
      record[6] = heading;
      record[7] = extra;
      return record;
    }

    private static short QuantizeAttachmentCoordinate(float value, bool rejectsSentinel)
    {
      var scaled = Math.Truncate(value * 256d);
      if (!double.IsFinite(scaled) || scaled < short.MinValue || scaled > short.MaxValue)
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }
      var result = (short)scaled;
      if (rejectsSentinel && result == short.MinValue)
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }
      return result;
    }

    private static byte[] CreateAbsentAttachmentRecord()
    {
      var record = new byte[8];
      BinaryPrimitives.WriteInt16LittleEndian(record, short.MinValue);
      BinaryPrimitives.WriteInt16LittleEndian(record.AsSpan(2), short.MinValue);
      BinaryPrimitives.WriteInt16LittleEndian(record.AsSpan(4), short.MinValue);
      return record;
    }

    private static byte[] CreateCannonRenderPositionRecord(Vector3 translation)
    {
      if (!IsFinite(translation))
      {
        throw new UnsupportedGltfDomainException("CannonRenderPosition");
      }
      var result = new byte[12];
      WriteSingle(result, 0, translation.X);
      WriteSingle(result, 4, translation.Z);
      WriteSingle(result, 8, translation.Y);
      return result;
    }

    private static void WriteSingle(byte[] destination, int offset, float value)
    {
      BinaryPrimitives.WriteInt32LittleEndian(
        destination.AsSpan(offset),
        BitConverter.SingleToInt32Bits(value == 0 ? 0 : value));
    }

    private static bool IsFinite(Quaternion value)
    {
      return float.IsFinite(value.X)
        && float.IsFinite(value.Y)
        && float.IsFinite(value.Z)
        && float.IsFinite(value.W);
    }

    private static bool NearlyEqual(Matrix4x4 left, Matrix4x4 right, float tolerance)
    {
      return MathF.Abs(left.M11 - right.M11) <= tolerance
        && MathF.Abs(left.M12 - right.M12) <= tolerance
        && MathF.Abs(left.M13 - right.M13) <= tolerance
        && MathF.Abs(left.M14 - right.M14) <= tolerance
        && MathF.Abs(left.M21 - right.M21) <= tolerance
        && MathF.Abs(left.M22 - right.M22) <= tolerance
        && MathF.Abs(left.M23 - right.M23) <= tolerance
        && MathF.Abs(left.M24 - right.M24) <= tolerance
        && MathF.Abs(left.M31 - right.M31) <= tolerance
        && MathF.Abs(left.M32 - right.M32) <= tolerance
        && MathF.Abs(left.M33 - right.M33) <= tolerance
        && MathF.Abs(left.M34 - right.M34) <= tolerance
        && MathF.Abs(left.M41 - right.M41) <= tolerance
        && MathF.Abs(left.M42 - right.M42) <= tolerance
        && MathF.Abs(left.M43 - right.M43) <= tolerance
        && MathF.Abs(left.M44 - right.M44) <= tolerance;
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
        && (node.Metadata is not null
            && node.Metadata.AttachmentRecord is null
            && node.Metadata.CannonRenderPosition is null
          || node.Metadata is null
            && node.Parsed.Children.Count == 0
            && !GlbDocument.TryParseAttachmentHelperName(node.Parsed.Name, out _)
            && !GlbDocument.TryParseCannonRenderPositionHelperName(node.Parsed.Name, out _))))
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
          var partition = new GeometryPartition(
            -1,
            primitive.Vertices,
            primitive.Triangles,
            primitive.MaterialIndex);
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
        new GeometryPartition(
          match.Partition.LocalId,
          vertices,
          triangles,
          match.Partition.MaterialIndex),
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

    private static OperationDiagnostic PreviewOutputLimitWarning()
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.TexturePreviewUnavailable,
        1109,
        DiagnosticSeverity.Warning,
        "$",
        "Decoded TEX previews were omitted to keep the package within the output limit.");
    }

    private static IReadOnlyList<OperationDiagnostic> CreateAnimationDiagnostics(
      StaticMeshAsset asset,
      InterchangeBaseline baseline)
    {
      var sources = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
        .ToDictionary(source => source.Id.Value);
      var indices = asset.StaticRenderObjectSequence
        .Select((record, index) => new { record.Id, Index = index })
        .ToDictionary(item => item.Id, item => item.Index);
      var diagnostics = new List<OperationDiagnostic>();
      foreach (var item in StaticAnimationProjection.Create(asset, baseline).Objects
        .OrderBy(item => item.ClassIndex)
        .ThenBy(item => item.SourceObjectLocalId))
      {
        var recordIndex = indices[sources[item.SourceObjectLocalId].StaticRenderObjectIds[0]];
        var commonData = new Dictionary<string, string>
        {
          ["sourceObject"] = item.SourceObjectLocalId.ToString(
            System.Globalization.CultureInfo.InvariantCulture),
          ["animationClassValue"] = item.AnimationClassValue.ToString(
            System.Globalization.CultureInfo.InvariantCulture),
          ["class"] = ((char)('A' + item.ClassIndex)).ToString()
        };
        if (item.AnimationClassValue > 3)
        {
          diagnostics.Add(new OperationDiagnostic(
            GltfDiagnosticCodes.AnimationClassUnrecognized,
            1115,
            DiagnosticSeverity.Warning,
            $"StaticRenderObjectSequence[{recordIndex}].AnimationClassValue",
            "An unrecognized animation-class value uses its modulo-four class for native projection.",
            data: commonData));
        }
        if (item.FailureFrame.HasValue)
        {
          var metadataOnlyData = new Dictionary<string, string>(commonData)
          {
            ["frame"] = item.FailureFrame!.Value.ToString(
              System.Globalization.CultureInfo.InvariantCulture)
          };
          diagnostics.Add(new OperationDiagnostic(
            GltfDiagnosticCodes.AnimationMetadataOnly,
            1114,
            DiagnosticSeverity.Warning,
            $"StaticRenderObjectSequence[{recordIndex}].AnimationTracks",
            "The source animation cannot be represented exactly as native glTF TRS and remains metadata-only.",
            data: metadataOnlyData));
        }
      }
      return diagnostics.AsReadOnly();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateCannonRenderPositionDiagnostics(
      StaticMeshAsset asset)
    {
      var records = asset.CommonBaseHeader.CannonRenderPositions.ToArray();
      var diagnostics = new List<OperationDiagnostic>();
      for (var physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        var record = records.AsSpan((physicalNumber - 1) * 12, 12);
        var substituted = new List<int>();
        for (var component = 0; component < 3; component++)
        {
          var value = BitConverter.Int32BitsToSingle(
            BinaryPrimitives.ReadInt32LittleEndian(record.Slice(component * 4, 4)));
          if (!float.IsFinite(value))
          {
            substituted.Add(component);
          }
        }
        if (substituted.Count == 0)
        {
          continue;
        }
        diagnostics.Add(new OperationDiagnostic(
          GltfDiagnosticCodes.CannonRenderPositionPreviewSubstituted,
          1116,
          DiagnosticSeverity.Warning,
          $"CommonBaseHeader.CannonRenderPositions[{physicalNumber}]",
          "Non-finite cannon render-position components use zero in the native preview.",
          data: new Dictionary<string, string>
          {
            ["physicalNumber"] = physicalNumber.ToString(
              System.Globalization.CultureInfo.InvariantCulture),
            ["components"] = string.Join(",", substituted)
          }));
      }
      return diagnostics.AsReadOnly();
    }

    private static IReadOnlyList<OperationDiagnostic> WithoutEmittedPreviewDiagnostics(
      IEnumerable<OperationDiagnostic> diagnostics)
    {
      return diagnostics.Where(diagnostic =>
          diagnostic.Code != GltfDiagnosticCodes.TextureDefaultPreviewUsed
          && diagnostic.Code != GltfDiagnosticCodes.TextureDiagnosticPreviewUsed
          && diagnostic.Code != GltfDiagnosticCodes.TextureVariantsNotRepresented)
        .Concat(new[] { PreviewOutputLimitWarning() })
        .ToArray();
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
