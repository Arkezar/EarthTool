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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.GLTF
{
  /// <summary>Provides the sealed MSH and glTF interchange facade.</summary>
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
          .Concat(CreateCannonRenderPositionDiagnostics(asset))
          .Concat(CreateStaticLightDiagnostics(asset))
          .Concat(CreateEmitterHierarchyDiagnostics(asset)).ToArray();
        var metadataLength = GlbDocument.GetMaximumMetadataByteCount(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds);
        if (metadataLength > profile.MaxMetadataBytes)
        {
          return Failed<GltfExportReceipt>(Limit("scenes[0].extras.earthtool", metadataLength, profile.MaxMetadataBytes));
        }

        var minimumOutputLength = GlbDocument.GetMinimumOutputByteCount(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds,
          true);
        if (minimumOutputLength > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", minimumOutputLength, profile.MaxOutputBytes));
        }
        var withoutPreviews = GlbDocument.Create(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds,
          new Dictionary<StaticRenderObjectId, TexPreview>(),
          options.SourceBaseName,
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
          : GlbDocument.Create(
            asset,
            baseline,
            options.PreservedUnknownMetadata,
            options.MetadataNextIds,
            options.ArtistObjectLocalIds,
            previewResult.Previews,
            options.SourceBaseName,
            out fingerprint);
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
          .Concat(CreateCannonRenderPositionDiagnostics(asset))
          .Concat(CreateStaticLightDiagnostics(asset))
          .Concat(CreateEmitterHierarchyDiagnostics(asset)).ToArray();
        var metadataLength = GlbDocument.GetMaximumMetadataByteCount(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds);
        if (metadataLength > profile.MaxMetadataBytes)
        {
          return Failed<GltfExportReceipt>(Limit("scenes[0].extras.earthtool", metadataLength, profile.MaxMetadataBytes));
        }

        var minimumOutputLength = GlbDocument.GetMinimumOutputByteCount(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds,
          false);
        if (minimumOutputLength > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", minimumOutputLength, profile.MaxOutputBytes));
        }
        var withoutPreviews = GlbDocument.CreateSeparate(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds,
          new Dictionary<StaticRenderObjectId, TexPreview>(),
          options.SourceBaseName,
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
          : GlbDocument.CreateSeparate(
            asset,
            baseline,
            options.PreservedUnknownMetadata,
            options.MetadataNextIds,
            options.ArtistObjectLocalIds,
            previewResult.Previews,
            options.SourceBaseName,
            out fingerprint);
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

    /// <summary>Exports one bounded supported dynamic asset as a strictly validated GLB.</summary>
    public async Task<OperationResult<GltfExportReceipt>> ExportGlbAsync(
      DynamicMeshAsset asset,
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
        var baseline = new InterchangeBaseline(
          options.AssetLineageId ?? Guid.NewGuid(),
          options.DocumentId ?? Guid.NewGuid());
        var previewResult = TexPreviewLoader.Load(
          asset,
          options,
          profile,
          profile.MaxOutputBytes,
          cancellationToken);
        if (previewResult.HasErrors)
        {
          return Failed<GltfExportReceipt>(previewResult.Diagnostics.First(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error));
        }
        var meshPreviewResult = MshPreviewLoader.Load(
          asset,
          options,
          profile,
          cancellationToken);
        var glb = DynamicGltfDocument.Create(
          asset,
          baseline,
          profile,
          previewResult.Previews,
          meshPreviewResult.Previews,
          options.DynamicObjectIds,
          options.SourceBaseName,
          out var fingerprint);
        DynamicGltfDocument.ValidateGlb(glb, profile);
        cancellationToken.ThrowIfCancellationRequested();
        await destination.WriteAsync(glb, 0, glb.Length, cancellationToken).ConfigureAwait(false);
        return new OperationResult<GltfExportReceipt>(
          OperationStatus.Succeeded,
          new GltfExportReceipt(baseline, fingerprint),
          previewResult.Diagnostics.Concat(meshPreviewResult.Diagnostics));
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

    /// <summary>Transactionally exports one bounded supported dynamic asset to a GLB file.</summary>
    public async Task<OperationResult<GltfExportReceipt>> ExportGlbFileAsync(
      DynamicMeshAsset asset,
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

      var temporaryPath = _fileSystem.GetTemporaryPath(destinationPath);
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        OperationResult<GltfExportReceipt> result;
        using (var temporary = _fileSystem.CreateTemporary(temporaryPath))
        {
          result = await ExportGlbAsync(asset, temporary, options, profile, cancellationToken)
            .ConfigureAwait(false);
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

    /// <summary>Transactionally exports one bounded supported dynamic asset as separate glTF.</summary>
    public async Task<OperationResult<GltfExportReceipt>> ExportGltfFileAsync(
      DynamicMeshAsset asset,
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
      string? sidecarTemporaryPath = null;
      string? committedSidecarPath = null;
      var manifestCommitted = false;
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        var baseline = new InterchangeBaseline(
          options.AssetLineageId ?? Guid.NewGuid(),
          options.DocumentId ?? Guid.NewGuid());
        var previewResult = TexPreviewLoader.Load(
          asset,
          options,
          profile,
          profile.MaxOutputBytes,
          cancellationToken);
        if (previewResult.HasErrors)
        {
          return Failed<GltfExportReceipt>(previewResult.Diagnostics.First(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error));
        }
        var meshPreviewResult = MshPreviewLoader.Load(
          asset,
          options,
          profile,
          cancellationToken);
        var package = DynamicGltfDocument.CreateSeparate(
          asset,
          baseline,
          profile,
          previewResult.Previews,
          meshPreviewResult.Previews,
          options.DynamicObjectIds,
          options.SourceBaseName,
          out var fingerprint);
        GlbDocument.ValidateSeparate(
          package.Json,
          package.Binary,
          package.BufferFileName,
          package.ImageSidecars);
        var directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath))
          ?? Directory.GetCurrentDirectory();
        var sidecarPath = Path.Combine(directory, package.BufferFileName);
        if (Directory.Exists(sidecarPath)
          || File.Exists(sidecarPath) && !HasSameContent(sidecarPath, package.Binary))
        {
          throw new IOException("A content-addressed dynamic glTF sidecar has conflicting content.");
        }
        if (!File.Exists(sidecarPath))
        {
          sidecarTemporaryPath = _fileSystem.GetTemporaryPath(sidecarPath);
          using (var temporary = _fileSystem.CreateTemporary(sidecarTemporaryPath))
          {
            await temporary.WriteAsync(
              package.Binary,
              0,
              package.Binary.Length,
              cancellationToken).ConfigureAwait(false);
            await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
          }
          cancellationToken.ThrowIfCancellationRequested();
          _fileSystem.Commit(sidecarTemporaryPath, sidecarPath);
          committedSidecarPath = sidecarPath;
          sidecarTemporaryPath = null;
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
        manifestCommitted = true;
        return new OperationResult<GltfExportReceipt>(
          OperationStatus.Succeeded,
          new GltfExportReceipt(baseline, fingerprint),
          previewResult.Diagnostics.Concat(meshPreviewResult.Diagnostics));
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
        if (sidecarTemporaryPath is not null)
        {
          _fileSystem.TryDelete(sidecarTemporaryPath);
        }
        if (!manifestCommitted && committedSidecarPath is not null)
        {
          _fileSystem.TryDelete(committedSidecarPath);
        }
      }
    }

    /// <summary>Imports a dynamic GLB into an expected lineage and document baseline.</summary>
    public async Task<OperationResult<GltfDynamicEditImportResult>> ImportEditDynamicGlbAsync(
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
        var glb = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken)
          .ConfigureAwait(false);
        var imported = DynamicGltfDocument.ImportGlb(
          glb,
          expectedBaseline,
          profile,
          cancellationToken);
        var nextBaseline = new InterchangeBaseline(expectedBaseline.AssetLineageId, Guid.NewGuid());
        return new OperationResult<GltfDynamicEditImportResult>(
          OperationStatus.Succeeded,
          new GltfDynamicEditImportResult(
            imported.Asset,
            nextBaseline,
            imported.Fingerprint,
            imported.Preservation,
            new[] { "RootDynamicObject" },
            imported.ObjectIds),
          CreateDynamicPlacementDiagnostics(imported));
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfDynamicEditImportResult>();
      }
      catch (Exception ex)
      {
        return Failed<GltfDynamicEditImportResult>(ToDiagnostic(ex));
      }
    }

    /// <summary>Imports a dynamic separate-glTF package into an expected baseline.</summary>
    public async Task<OperationResult<GltfDynamicEditImportResult>> ImportEditDynamicGltfFileAsync(
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
        var imported = DynamicGltfDocument.ImportSeparate(
          package.Json,
          package.Binary,
          package.BufferUri,
          package.Images,
          expectedBaseline,
          profile,
          cancellationToken);
        var nextBaseline = new InterchangeBaseline(expectedBaseline.AssetLineageId, Guid.NewGuid());
        return new OperationResult<GltfDynamicEditImportResult>(
          OperationStatus.Succeeded,
          new GltfDynamicEditImportResult(
            imported.Asset,
            nextBaseline,
            imported.Fingerprint,
            imported.Preservation,
            new[] { "RootDynamicObject" },
            imported.ObjectIds),
          CreateDynamicPlacementDiagnostics(imported));
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfDynamicEditImportResult>();
      }
      catch (Exception ex)
      {
        return Failed<GltfDynamicEditImportResult>(ToDiagnostic(ex, sourcePath));
      }
    }

    /// <summary>Imports a static or dynamic GLB while preserving existing kind-specific contracts.</summary>
    public async Task<OperationResult<GltfMeshEditImportResult>> ImportEditMeshGlbAsync(
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
        var glb = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken)
          .ConfigureAwait(false);
        var jsonLength = glb.Length >= 20
          ? checked((int)BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12, sizeof(uint))))
          : 0;
        if (jsonLength > 0 && 20 + jsonLength <= glb.Length
          && DynamicGltfDocument.HasDynamicManifest(
            glb.AsMemory(20, jsonLength),
            profile.MaxJsonDepth))
        {
          await using var dynamicSource = new MemoryStream(glb, false);
          return ToMeshResult(await ImportEditDynamicGlbAsync(
            dynamicSource,
            expectedBaseline,
            profile,
            cancellationToken).ConfigureAwait(false));
        }
        await using var staticSource = new MemoryStream(glb, false);
        return ToMeshResult(await ImportEditGlbAsync(
          staticSource,
          expectedBaseline,
          profile,
          cancellationToken).ConfigureAwait(false));
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfMeshEditImportResult>();
      }
      catch (Exception ex)
      {
        return Failed<GltfMeshEditImportResult>(ToDiagnostic(ex));
      }
    }

    /// <summary>Imports a static or dynamic separate-glTF package.</summary>
    public async Task<OperationResult<GltfMeshEditImportResult>> ImportEditMeshGltfFileAsync(
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
        await using var source = new FileStream(
          sourcePath,
          FileMode.Open,
          FileAccess.Read,
          FileShare.Read,
          81920,
          true);
        var json = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken)
          .ConfigureAwait(false);
        return DynamicGltfDocument.HasDynamicManifest(json, profile.MaxJsonDepth)
          ? ToMeshResult(await ImportEditDynamicGltfFileAsync(
            sourcePath,
            expectedBaseline,
            profile,
            cancellationToken).ConfigureAwait(false))
          : ToMeshResult(await ImportEditGltfFileAsync(
            sourcePath,
            expectedBaseline,
            profile,
            cancellationToken).ConfigureAwait(false));
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfMeshEditImportResult>();
      }
      catch (Exception ex)
      {
        return Failed<GltfMeshEditImportResult>(ToDiagnostic(ex, sourcePath));
      }
    }

    /// <summary>Imports a planned static GLB through the kind-neutral result contract.</summary>
    public async Task<OperationResult<GltfMeshEditImportResult>> ImportEditMeshGlbWithPlanAsync(
      Stream source,
      InterchangeBaseline expectedBaseline,
      GltfImportPlan plan,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      return ToMeshResult(await ImportEditGlbWithPlanAsync(
        source,
        expectedBaseline,
        plan,
        profile,
        cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Imports a planned static separate-glTF package through the kind-neutral result contract.</summary>
    public async Task<OperationResult<GltfMeshEditImportResult>> ImportEditMeshGltfFileWithPlanAsync(
      string sourcePath,
      InterchangeBaseline expectedBaseline,
      GltfImportPlan plan,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      return ToMeshResult(await ImportEditGltfFileWithPlanAsync(
        sourcePath,
        expectedBaseline,
        plan,
        profile,
        cancellationToken).ConfigureAwait(false));
    }

    private static OperationResult<GltfMeshEditImportResult> ToMeshResult(
      OperationResult<GltfEditImportResult> result)
    {
      return result.Succeeded
        ? new OperationResult<GltfMeshEditImportResult>(
          OperationStatus.Succeeded,
          new GltfMeshEditImportResult(
            result.Value!.Asset,
            result.Value.NextBaseline,
            result.Value.AppliedFingerprint,
            result.Value.Preservation,
            result.Value.RestoredSerializedRepresentationPaths,
            result.Value.LineageDisposition,
            result.Value.AppliedConflictResolutions),
          result.Diagnostics)
        : new OperationResult<GltfMeshEditImportResult>(result.Status, diagnostics: result.Diagnostics);
    }

    private static OperationResult<GltfMeshEditImportResult> ToMeshResult(
      OperationResult<GltfDynamicEditImportResult> result)
    {
      return result.Succeeded
        ? new OperationResult<GltfMeshEditImportResult>(
          OperationStatus.Succeeded,
          new GltfMeshEditImportResult(
            result.Value!.Asset,
            result.Value.NextBaseline,
            result.Value.AppliedFingerprint,
            result.Value.Preservation,
            result.Value.RestoredSerializedRepresentationPaths,
            GltfMetadataLineageDisposition.Retained),
          result.Diagnostics)
        : new OperationResult<GltfMeshEditImportResult>(result.Status, diagnostics: result.Diagnostics);
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

    /// <summary>Imports a GLB into an expected lineage and document baseline.</summary>
    public Task<OperationResult<GltfEditImportResult>> ImportEditGlbAsync(
      Stream source,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      return ImportEditGlbWithResolutionsAsync(
        source,
        expectedBaseline,
        new GltfEditImportOptions(),
        profile,
        cancellationToken);
    }

    /// <summary>Imports a GLB using one validated, source-bound edit plan.</summary>
    public async Task<OperationResult<GltfEditImportResult>> ImportEditGlbWithPlanAsync(
      Stream source,
      InterchangeBaseline expectedBaseline,
      GltfImportPlan plan,
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
      if (plan is null)
      {
        throw new ArgumentNullException(nameof(plan));
      }
      profile ??= GltfOperationProfile.Default;
      var mismatch = ValidatePlan(
        plan,
        GltfImportPlanKind.Edit,
        GltfPackageKind.Glb,
        expectedBaseline,
        profile);
      if (mismatch is not null)
      {
        return Failed<GltfEditImportResult>(mismatch);
      }
      try
      {
        var bytes = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken).ConfigureAwait(false);
        if (!MatchesPlanSource(bytes, plan))
        {
          return Failed<GltfEditImportResult>(PlanMismatch("sourceSha256"));
        }
        using var captured = new MemoryStream(bytes, false);
        var result = await ImportEditGlbWithResolutionsAsync(
          captured,
          expectedBaseline,
          plan.EditOptions!,
          profile,
          cancellationToken).ConfigureAwait(false);
        return TranslateStalePlan(result, plan);
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

    /// <summary>Imports a GLB and transactionally applies exact metadata conflict resolutions.</summary>
    public async Task<OperationResult<GltfEditImportResult>> ImportEditGlbWithResolutionsAsync(
      Stream source,
      InterchangeBaseline expectedBaseline,
      GltfEditImportOptions options,
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

      if (options is null)
      {
        throw new ArgumentNullException(nameof(options));
      }
      profile ??= GltfOperationProfile.Default;
      byte[]? bytes = null;
      try
      {
        bytes = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken).ConfigureAwait(false);
        var parsed = GlbDocument.Parse(bytes, profile);
        ValidateGeometryProfile(parsed, profile);
        return await ImportParsedAsync(
          parsed,
          expectedBaseline,
          options,
          profile,
          cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfEditImportResult>();
      }
      catch (Exception ex)
      {
        if (bytes is not null
          && TryGetParseScopeResolution(
            ex,
            expectedBaseline,
            options,
            out var scopeResolution,
            out var scopeFailure))
        {
          if (scopeFailure is not null)
          {
            return Failed<GltfEditImportResult>(scopeFailure);
          }
          try
          {
            var rewritten = RewriteGlbMetadata(bytes, scopeResolution!.Diagnostic, scopeResolution.Resolution);
            var reparsed = GlbDocument.Parse(rewritten, profile);
            ValidateGeometryProfile(reparsed, profile);
            var retried = await ImportParsedAsync(
              reparsed,
              expectedBaseline,
              new GltfEditImportOptions(options.ConflictResolutions.Where(resolution =>
                !ReferenceEquals(resolution, scopeResolution.Resolution))),
              profile,
              cancellationToken).ConfigureAwait(false);
            return WithAppliedResolution(retried, scopeResolution.Resolution);
          }
          catch (Exception rewriteException)
          {
            return Failed<GltfEditImportResult>(BindConflictToBaseline(
              ToDiagnostic(rewriteException),
              expectedBaseline));
          }
        }
        if (bytes is not null
          && TryGetWholeLineageResolution(
            ex,
            expectedBaseline,
            options,
            out var resolution,
            out var resolutionFailure))
        {
          if (resolutionFailure is not null)
          {
            return Failed<GltfEditImportResult>(resolutionFailure);
          }
          try
          {
            var stripped = RemoveGlbMetadata(bytes);
            var parsed = GlbDocument.ParseNewModel(stripped, profile);
            ValidateGeometryProfile(parsed, profile);
            return ImportWithoutMetadata(parsed, profile, cancellationToken, resolution!);
          }
          catch (Exception discardException)
          {
            return Failed<GltfEditImportResult>(ToDiagnostic(discardException));
          }
        }
        return Failed<GltfEditImportResult>(BindConflictToBaseline(ToDiagnostic(ex), expectedBaseline));
      }
    }

    /// <summary>Imports separate glTF into an expected lineage and document baseline.</summary>
    public Task<OperationResult<GltfEditImportResult>> ImportEditGltfFileAsync(
      string sourcePath,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      return ImportEditGltfFileWithResolutionsAsync(
        sourcePath,
        expectedBaseline,
        new GltfEditImportOptions(),
        profile,
        cancellationToken);
    }

    /// <summary>Imports separate glTF using one validated, source-bound edit plan.</summary>
    public async Task<OperationResult<GltfEditImportResult>> ImportEditGltfFileWithPlanAsync(
      string sourcePath,
      InterchangeBaseline expectedBaseline,
      GltfImportPlan plan,
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
      if (plan is null)
      {
        throw new ArgumentNullException(nameof(plan));
      }
      profile ??= GltfOperationProfile.Default;
      var mismatch = ValidatePlan(
        plan,
        GltfImportPlanKind.Edit,
        GltfPackageKind.Gltf,
        expectedBaseline,
        profile);
      if (mismatch is not null)
      {
        return Failed<GltfEditImportResult>(mismatch);
      }
      try
      {
        var package = await ReadSeparatePackageAsync(sourcePath, profile, cancellationToken)
          .ConfigureAwait(false);
        if (!GltfImportPlanSerializer.MatchesSeparateSource(package, plan.SourceSha256))
        {
          return Failed<GltfEditImportResult>(PlanMismatch("sourceSha256"));
        }
        var result = await ImportEditSeparatePackageAsync(
          package,
          expectedBaseline,
          plan.EditOptions!,
          profile,
          sourcePath,
          cancellationToken).ConfigureAwait(false);
        return TranslateStalePlan(result, plan);
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

    /// <summary>Imports separate glTF and transactionally applies exact metadata conflict resolutions.</summary>
    public async Task<OperationResult<GltfEditImportResult>> ImportEditGltfFileWithResolutionsAsync(
      string sourcePath,
      InterchangeBaseline expectedBaseline,
      GltfEditImportOptions options,
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

      if (options is null)
      {
        throw new ArgumentNullException(nameof(options));
      }
      profile ??= GltfOperationProfile.Default;
      try
      {
        var package = await ReadSeparatePackageAsync(sourcePath, profile, cancellationToken)
          .ConfigureAwait(false);
        return await ImportEditSeparatePackageAsync(
          package,
          expectedBaseline,
          options,
          profile,
          sourcePath,
          cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        return Cancelled<GltfEditImportResult>();
      }
      catch (Exception ex)
      {
        return Failed<GltfEditImportResult>(BindConflictToBaseline(
          ToDiagnostic(ex, sourcePath),
          expectedBaseline));
      }
    }

    private static async Task<OperationResult<GltfEditImportResult>> ImportEditSeparatePackageAsync(
      SeparateGltfPackage package,
      InterchangeBaseline expectedBaseline,
      GltfEditImportOptions options,
      GltfOperationProfile profile,
      string sourcePath,
      CancellationToken cancellationToken)
    {
      try
      {
        var parsed = GlbDocument.ParseSeparate(package.Json, package.Binary, profile);
        GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferUri, package.Images);
        ValidateGeometryProfile(parsed, profile);
        return await ImportParsedAsync(parsed, expectedBaseline, options, profile, cancellationToken)
          .ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        if (TryGetParseScopeResolution(
            ex,
            expectedBaseline,
            options,
            out var scopeResolution,
            out var scopeFailure))
        {
          if (scopeFailure is not null)
          {
            return Failed<GltfEditImportResult>(scopeFailure);
          }
          try
          {
            var rewritten = RewriteJsonScopeMetadata(
              package.Json,
              scopeResolution!.Diagnostic,
              scopeResolution.Resolution);
            var reparsed = GlbDocument.ParseSeparate(rewritten, package.Binary, profile);
            GlbDocument.ValidateSeparate(
              rewritten,
              package.Binary,
              package.BufferUri,
              package.Images);
            ValidateGeometryProfile(reparsed, profile);
            var retried = await ImportParsedAsync(
              reparsed,
              expectedBaseline,
              new GltfEditImportOptions(options.ConflictResolutions.Where(resolution =>
                !ReferenceEquals(resolution, scopeResolution.Resolution))),
              profile,
              cancellationToken).ConfigureAwait(false);
            return WithAppliedResolution(retried, scopeResolution.Resolution);
          }
          catch (Exception rewriteException)
          {
            return Failed<GltfEditImportResult>(BindConflictToBaseline(
              ToDiagnostic(rewriteException, sourcePath),
              expectedBaseline));
          }
        }
        if (TryGetWholeLineageResolution(
            ex,
            expectedBaseline,
            options,
            out var resolution,
            out var resolutionFailure))
        {
          if (resolutionFailure is not null)
          {
            return Failed<GltfEditImportResult>(resolutionFailure);
          }
          try
          {
            var stripped = RemoveJsonMetadata(package.Json);
            var parsed = GlbDocument.ParseSeparateNewModel(stripped, package.Binary, profile);
            GlbDocument.ValidateSeparate(
              stripped,
              package.Binary,
              package.BufferUri,
              package.Images);
            ValidateGeometryProfile(parsed, profile);
            return ImportWithoutMetadata(parsed, profile, cancellationToken, resolution!);
          }
          catch (Exception discardException)
          {
            return Failed<GltfEditImportResult>(ToDiagnostic(discardException, sourcePath));
          }
        }
        return Failed<GltfEditImportResult>(BindConflictToBaseline(
          ToDiagnostic(ex, sourcePath),
          expectedBaseline));
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

    /// <summary>Imports a metadata-free GLB using one validated, source-bound semantic plan.</summary>
    public async Task<OperationResult<GltfNewModelImportResult>> ImportNewModelGlbWithPlanAsync(
      Stream source,
      GltfImportPlan plan,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }
      if (plan is null)
      {
        throw new ArgumentNullException(nameof(plan));
      }
      profile ??= GltfOperationProfile.Default;
      var mismatch = ValidatePlan(
        plan,
        GltfImportPlanKind.NewModel,
        GltfPackageKind.Glb,
        null,
        profile);
      if (mismatch is not null)
      {
        return Failed<GltfNewModelImportResult>(mismatch);
      }
      try
      {
        var bytes = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken).ConfigureAwait(false);
        if (!MatchesPlanSource(bytes, plan))
        {
          return Failed<GltfNewModelImportResult>(PlanMismatch("sourceSha256"));
        }
        using var captured = new MemoryStream(bytes, false);
        return await ImportNewModelGlbAsync(
          captured,
          plan.NewModelOptions,
          profile,
          cancellationToken).ConfigureAwait(false);
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
        return ImportNewModelSeparatePackage(package, profile, cancellationToken, options);
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

    /// <summary>Imports metadata-free separate glTF using one validated, source-bound semantic plan.</summary>
    public async Task<OperationResult<GltfNewModelImportResult>> ImportNewModelGltfFileWithPlanAsync(
      string sourcePath,
      GltfImportPlan plan,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (sourcePath is null)
      {
        throw new ArgumentNullException(nameof(sourcePath));
      }
      if (plan is null)
      {
        throw new ArgumentNullException(nameof(plan));
      }
      profile ??= GltfOperationProfile.Default;
      var mismatch = ValidatePlan(
        plan,
        GltfImportPlanKind.NewModel,
        GltfPackageKind.Gltf,
        null,
        profile);
      if (mismatch is not null)
      {
        return Failed<GltfNewModelImportResult>(mismatch);
      }
      try
      {
        var package = await ReadSeparatePackageAsync(sourcePath, profile, cancellationToken)
          .ConfigureAwait(false);
        if (!GltfImportPlanSerializer.MatchesSeparateSource(package, plan.SourceSha256))
        {
          return Failed<GltfNewModelImportResult>(PlanMismatch("sourceSha256"));
        }
        return ImportNewModelSeparatePackage(
          package,
          profile,
          cancellationToken,
          plan.NewModelOptions!);
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

    private static OperationResult<GltfNewModelImportResult> ImportNewModelSeparatePackage(
      SeparateGltfPackage package,
      GltfOperationProfile profile,
      CancellationToken cancellationToken,
      GltfNewModelImportOptions options)
    {
      var parsed = GlbDocument.ParseSeparateNewModel(
        package.Json,
        package.Binary,
        profile);
      GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferUri, package.Images);
      ValidateGeometryProfile(parsed, profile);
      return ImportNewModelParsed(parsed, profile, cancellationToken, options);
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
        var jsonLength = bytes.Length >= 20
          ? checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12, sizeof(uint))))
          : 0;
        if (jsonLength > 0 && 20 + jsonLength <= bytes.Length
          && DynamicGltfDocument.HasDynamicManifest(
            bytes.AsMemory(20, jsonLength),
            profile.MaxJsonDepth))
        {
          DynamicGltfDocument.ValidateGlb(bytes, profile);
          return new OperationResult(OperationStatus.Succeeded);
        }
        var parsed = GlbDocument.Parse(bytes, profile);
        ValidateGeometryProfile(parsed, profile);
        var metadataConflicts = parsed.MetadataConflicts.Build();
        if (metadataConflicts.Count != 0)
        {
          return new OperationResult(
            OperationStatus.Failed,
            metadataConflicts.Select(conflict => ToDiagnostic(conflict)));
        }
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
        if (DynamicGltfDocument.HasDynamicManifest(package.Json, profile.MaxJsonDepth))
        {
          DynamicGltfDocument.ValidateSeparatePackage(
            package.Json,
            package.Binary,
            package.BufferUri,
            package.Images,
            profile);
          return new OperationResult(OperationStatus.Succeeded);
        }
        var parsed = GlbDocument.ParseSeparate(package.Json, package.Binary, profile);
        ValidateGeometryProfile(parsed, profile);
        GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferUri, package.Images);
        var metadataConflicts = parsed.MetadataConflicts.Build();
        if (metadataConflicts.Count != 0)
        {
          return new OperationResult(
            OperationStatus.Failed,
            metadataConflicts.Select(conflict => ToDiagnostic(conflict, sourcePath)));
        }
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

    internal static async Task<SeparateGltfPackage> ReadSeparatePackageAsync(
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
      var directory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))
        ?? Directory.GetCurrentDirectory();
      var bufferPath = ResolveContainedSidecar(directory, bufferUri);
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
        if (!imageUri.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
          && !imageUri.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
          && !imageUri.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
          throw new InvalidDataException("An external image URI must identify a PNG or JPEG file.");
        }
        if (remaining <= 0)
        {
          throw new ResourceLimitException(profile.MaxInputBytes, profile.MaxInputBytes);
        }
        var imagePath = ResolveContainedSidecar(directory, imageUri);
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
      return new SeparateGltfPackage(json, binary, bufferUri, images);
    }

    internal sealed class SeparateGltfPackage
    {
      internal byte[] Json { get; }

      internal byte[] Binary { get; }

      internal string BufferUri { get; }

      internal IReadOnlyDictionary<string, byte[]> Images { get; }

      internal SeparateGltfPackage(
        byte[] json,
        byte[] binary,
        string bufferUri,
        IReadOnlyDictionary<string, byte[]> images)
      {
        Json = json;
        Binary = binary;
        BufferUri = bufferUri;
        Images = images;
      }
    }

    private static string ResolveContainedSidecar(string directory, string relativeUri)
    {
      if (string.IsNullOrWhiteSpace(relativeUri)
        || relativeUri.IndexOfAny(new[] { '\\', '?', '#' }) >= 0
        || Uri.TryCreate(relativeUri, UriKind.Absolute, out _))
      {
        throw new InvalidDataException("A glTF sidecar URI must be safe, relative, and contained.");
      }
      var decoded = Uri.UnescapeDataString(relativeUri);
      var segments = decoded.Split('/');
      if (segments.Any(segment => string.IsNullOrEmpty(segment) || segment is "." or ".."))
      {
        throw new InvalidDataException("A glTF sidecar URI must be safe, relative, and contained.");
      }
      var root = Path.GetFullPath(directory);
      var path = Path.GetFullPath(Path.Combine(new[] { root }.Concat(segments).ToArray()));
      if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
      {
        throw new InvalidDataException("A glTF sidecar URI escapes the package directory.");
      }
      var current = root;
      foreach (var segment in segments.Take(segments.Length - 1))
      {
        current = Path.Combine(current, segment);
        if (Directory.Exists(current)
          && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
        {
          throw new InvalidDataException("A glTF sidecar URI traverses a symbolic link.");
        }
      }
      return path;
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
      var sceneLightDiagnostics = CreateIgnoredSceneLightDiagnostics(parsed);
      var lightIntensityDiagnostics = CreateIgnoredNewModelLightIntensityDiagnostics(parsed, options);
      var animationDiagnostics = CreateIgnoredNewModelAnimationDiagnostics(parsed);
      var inertDiagnostics = CreateIgnoredInertDataDiagnostics(parsed)
        .Concat(CreateIgnoredSceneNodeDiagnostics(parsed)).ToArray();
      var texBindingDiagnostics = CreateNewModelTexBindingDiagnostics(parsed, options);
      if (parsed.HasReservedMetadata)
      {
        return Failed<GltfNewModelImportResult>(Diagnostic(
          GltfDiagnosticCodes.OrphanEnvelope,
          2011,
          "$",
          "New-model import requires input without reserved EarthTool metadata."));
      }

      ValidateNewModelMaterialBindings(parsed, options);
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
        serializedLength = checked(serializedLength + parsed.Nodes
          .Where(node => node.MeshIndex.HasValue)
          .SelectMany(node => parsed.Meshes[node.MeshIndex!.Value].Primitives)
          .Sum(primitive => primitive.MaterialIndex.HasValue
            && options.TextureResourceBindings.TryGetValue(
              GetMaterialHandle(parsed, primitive.MaterialIndex.Value),
              out var binding)
            ? binding?.Length ?? 0
            : 0));
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
      ValidateNewModelObjectRoles(parsed, options);
      var emitterOwnership = ResolveNewModelEmitterOwnership(parsed);
      var draft = CreateNewModelSourceTree(
        parsed,
        options,
        animations.AnimatedSourceNodes,
        emitterOwnership);
      var effectivePositions = CreateEffectivePositions(
        draft,
        source => source.Source.RenderObjects.SelectMany(renderObject =>
          renderObject.RenderVertices.Select(vertex => vertex.Position)),
        source => source.Pivot,
        source => source.Children);
      CanonicalStaticFootprint footprint;
      if (options.Footprint is not null)
      {
        footprint = options.Footprint.ToCanonical();
      }
      else
      {
        var maximumZ = effectivePositions.Max(position => position.Z);
        if (!float.IsFinite(maximumZ) || maximumZ < 0 || maximumZ * 256d > ushort.MaxValue)
        {
          return Failed<GltfNewModelImportResult>(InvalidGeometry(
            "CommonBaseHeader.Footprint",
            $"The derived occupied top elevation {maximumZ:R} is outside the representable range 0..{ushort.MaxValue / 256f:R}."));
        }
        var elevations = new float[16];
        elevations[15] = maximumZ;
        footprint = new CanonicalStaticFootprint(0x8000, elevations, new byte[16]);
      }
      var horizontalExtents = options.HorizontalExtents?.ToCanonical();
      if (horizontalExtents is null
        && !TryCreateHorizontalExtents(
          effectivePositions,
          out horizontalExtents,
          out var rangeFailure))
      {
        return Failed<GltfNewModelImportResult>(InvalidGeometry(
          "CommonBaseHeader.HorizontalExtents",
          rangeFailure!));
      }
      var lineage = new MeshAssetLineageId(Guid.NewGuid());
      var builder = StaticMeshBuilder.Create(Guid.NewGuid(), lineage)
        .SetRootSourceObject(draft.Source)
        .SetFootprint(footprint)
        .SetHorizontalExtents(horizontalExtents!);
      var build = builder.Build(new MshOperationProfile(
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
      ApplyNewModelBaseHeaderArtistObjects(parsed, edit, options, emitterOwnership);
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
        new GltfNewModelImportResult(authored, baseline, CreateNewModelPreservationReport(authored)),
        sceneLightDiagnostics.Concat(lightIntensityDiagnostics).Concat(animationDiagnostics)
          .Concat(inertDiagnostics).Concat(texBindingDiagnostics).Concat(committed.Diagnostics));
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
      foreach (var materialIndex in usedMaterialIndices.Where(index =>
        parsed.Materials[index].HasBaseColorTexture))
      {
        var materialHandle = GetMaterialHandle(parsed, materialIndex);
        if (!options.TextureResourceBindings.TryGetValue(materialHandle, out var binding)
          || binding is null)
        {
          throw new RequiredTextureResourceBindingException(materialIndex, materialHandle);
        }
      }
      if (parsed.Meshes.SelectMany(mesh => mesh.Primitives).Any(primitive =>
        primitive.MaterialIndex.HasValue
          && parsed.Materials[primitive.MaterialIndex.Value].HasBaseColorTexture
          && !primitive.HasTextureCoordinate))
      {
        throw new UnsupportedGltfDomainException("TexResourceBinding");
      }
      foreach (var binding in options.TextureResourceBindings)
      {
        var materialIndex = GetMaterialIndex(parsed, binding.Key);
        if (!materialIndex.HasValue || !usedMaterialIndices.Contains(materialIndex.Value))
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

    private static void ValidateNewModelObjectRoles(
      ParsedGlb parsed,
      GltfNewModelImportOptions options)
    {
      for (var nodeIndex = 0; nodeIndex < parsed.Nodes.Count; nodeIndex++)
      {
        if (GlbDocument.TryParseStaticLightHelperName(
            parsed.Nodes[nodeIndex].Name,
            out _,
            out _)
          && (!parsed.Nodes[nodeIndex].LightIndex.HasValue
            || parsed.Nodes[nodeIndex].MeshIndex.HasValue
            || parsed.Nodes[nodeIndex].CameraIndex.HasValue
            || parsed.Nodes[nodeIndex].Children.Count != 0))
        {
          throw new UnsupportedGltfDomainException("StaticLights", $"nodes[{nodeIndex}]");
        }
      }
      foreach (var role in options.ObjectRoles)
      {
        var nodeIndex = GetNodeIndex(parsed, role.Key);
        if (!nodeIndex.HasValue || !parsed.Nodes[nodeIndex.Value].MeshIndex.HasValue)
        {
          throw new UnsupportedGltfDomainException("ObjectRoles");
        }
      }
      foreach (var light in options.StaticLightOptions)
      {
        var lightIndex = GetLightIndex(parsed, light.Key);
        if (!lightIndex.HasValue
          || !parsed.Nodes.Select((node, index) => (node, index)).Any(item =>
            item.node.LightIndex == lightIndex.Value
              && GlbDocument.TryParseStaticLightHelperName(item.node.Name, out _, out _)))
        {
          throw new UnsupportedGltfDomainException("StaticLights");
        }
        if (parsed.Lights[lightIndex.Value].Type == "point" && light.Value.TargetDistance.HasValue)
        {
          throw new UnsupportedGltfDomainException("StaticLights");
        }
      }
    }

    private static int[] GetNodeOrder(ParsedGlb parsed)
    {
      if (parsed.NewModelNodeOrder is not null)
      {
        return parsed.NewModelNodeOrder;
      }
      var result = new List<int>();
      AddNodeOrder(parsed.RootNodeIndex, parsed, result);
      parsed.NewModelNodeOrder = result.ToArray();
      return parsed.NewModelNodeOrder;
    }

    private static void AddNodeOrder(int nodeIndex, ParsedGlb parsed, ICollection<int> result)
    {
      result.Add(nodeIndex);
      foreach (var child in parsed.Nodes[nodeIndex].Children)
      {
        AddNodeOrder(child, parsed, result);
      }
    }

    private static int? GetNodeIndex(ParsedGlb parsed, GltfNodeHandle handle)
    {
      var order = GetNodeOrder(parsed);
      return handle.Value <= order.Length ? order[handle.Value - 1] : null;
    }

    private static GltfNodeHandle GetNodeHandle(ParsedGlb parsed, int nodeIndex)
    {
      var order = GetNodeOrder(parsed);
      return new GltfNodeHandle(Array.IndexOf(order, nodeIndex) + 1);
    }

    private static int[] GetMaterialOrder(ParsedGlb parsed)
    {
      if (parsed.NewModelMaterialOrder is not null)
      {
        return parsed.NewModelMaterialOrder;
      }
      parsed.NewModelMaterialOrder = GetNodeOrder(parsed)
        .Where(nodeIndex => parsed.Nodes[nodeIndex].MeshIndex.HasValue)
        .SelectMany(nodeIndex => parsed.Meshes[parsed.Nodes[nodeIndex].MeshIndex!.Value].Primitives)
        .Where(primitive => primitive.MaterialIndex.HasValue)
        .Select(primitive => primitive.MaterialIndex!.Value)
        .Distinct()
        .ToArray();
      return parsed.NewModelMaterialOrder;
    }

    private static int? GetMaterialIndex(ParsedGlb parsed, GltfMaterialHandle handle)
    {
      var order = GetMaterialOrder(parsed);
      return handle.Value <= order.Length ? order[handle.Value - 1] : null;
    }

    private static GltfMaterialHandle GetMaterialHandle(ParsedGlb parsed, int materialIndex)
    {
      var order = GetMaterialOrder(parsed);
      return new GltfMaterialHandle(Array.IndexOf(order, materialIndex) + 1);
    }

    private static int[] GetLightOrder(ParsedGlb parsed)
    {
      if (parsed.NewModelLightOrder is not null)
      {
        return parsed.NewModelLightOrder;
      }
      parsed.NewModelLightOrder = GetNodeOrder(parsed)
        .Where(nodeIndex => parsed.Nodes[nodeIndex].LightIndex.HasValue)
        .Select(nodeIndex => parsed.Nodes[nodeIndex].LightIndex!.Value)
        .Distinct()
        .ToArray();
      return parsed.NewModelLightOrder;
    }

    private static int? GetLightIndex(ParsedGlb parsed, GltfLightHandle handle)
    {
      var order = GetLightOrder(parsed);
      return handle.Value <= order.Length ? order[handle.Value - 1] : null;
    }

    private static GltfLightHandle GetLightHandle(ParsedGlb parsed, int lightIndex)
    {
      var order = GetLightOrder(parsed);
      return new GltfLightHandle(Array.IndexOf(order, lightIndex) + 1);
    }

    private static EmitterOwnershipPlan ResolveNewModelEmitterOwnership(ParsedGlb parsed)
    {
      var candidates = new Dictionary<int, List<int>>();
      for (var nodeIndex = 0; nodeIndex < parsed.Nodes.Count; nodeIndex++)
      {
        if (GlbDocument.TryParseAttachmentHelperName(
            parsed.Nodes[nodeIndex].Name,
            out var physicalNumber)
          && physicalNumber is >= 5 and <= 8)
        {
          AddArtistCandidate(candidates, physicalNumber, nodeIndex);
        }
      }
      var duplicate = candidates.FirstOrDefault(item => item.Value.Count != 1);
      if (duplicate.Value is not null)
      {
        var paths = string.Join(", ", duplicate.Value.Select(index => $"nodes[{index}]"));
        throw ArtistObjectConflict(
          $"Emitter {duplicate.Key - 4} is declared by multiple artist objects: {paths}.",
          $"nodes[{duplicate.Value[0]}]");
      }

      var nodes = parsed.Nodes.Select(node => (node, (MetadataEnvelope?)null)).ToArray();
      var parentIndices = CreateParentIndices(nodes);
      var markersBySource = new Dictionary<int, StaticRenderObjectFlags>();
      var emitterNodes = new HashSet<int>();
      var scaffoldingNodes = new HashSet<int>();
      foreach (var candidate in candidates)
      {
        var emitterNode = candidate.Value[0];
        if (parsed.Nodes[emitterNode].MeshIndex.HasValue)
        {
          throw new UnsupportedGltfDomainException(
            "EmitterMarkerHierarchy",
            $"nodes[{emitterNode}]");
        }
        emitterNodes.Add(emitterNode);
        var current = parentIndices[emitterNode];
        while (current >= 0 && !parsed.Nodes[current].MeshIndex.HasValue)
        {
          scaffoldingNodes.Add(current);
          current = parentIndices[current];
        }
        if (current < 0)
        {
          throw new UnsupportedGltfDomainException(
            "EmitterMarkerHierarchy",
            $"nodes[{emitterNode}]");
        }

        var flag = GlbDocument.GetMarkerAttachmentFlag(candidate.Key - 4);
        markersBySource[current] = markersBySource.TryGetValue(current, out var existing)
          ? existing | flag
          : flag;
      }
      return new EmitterOwnershipPlan(markersBySource, emitterNodes, scaffoldingNodes);
    }

    private static NewModelSourceDraft CreateNewModelSourceTree(
      ParsedGlb parsed,
      GltfNewModelImportOptions options,
      ISet<int> animatedSourceNodes,
      EmitterOwnershipPlan emitterOwnership)
    {
      var roots = CreateNewModelSources(
        parsed.RootNodeIndex,
        System.Numerics.Matrix4x4.Identity,
        parsed,
        options,
        animatedSourceNodes,
        emitterOwnership);
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
      ISet<int> animatedSourceNodes,
      EmitterOwnershipPlan emitterOwnership)
    {
      var node = parsed.Nodes[nodeIndex];
      var effectiveTransform = node.LocalTransform * inheritedLinearTransform;
      if (!node.MeshIndex.HasValue)
      {
        var claimedArtistObject = GlbDocument.TryParseAttachmentHelperName(node.Name, out _)
          || GlbDocument.TryParseCannonHelperName(node.Name, out _)
          || GlbDocument.TryParseStaticLightHelperName(node.Name, out _, out _);
        if (claimedArtistObject)
        {
          if (node.Children.Count != 0)
          {
            throw new UnsupportedGltfDomainException("TransformOrHierarchy", $"nodes[{nodeIndex}]");
          }
          return Array.Empty<NewModelSourceDraft>();
        }
        if ((node.LightIndex.HasValue || node.CameraIndex.HasValue) && node.Children.Count == 0)
        {
          return Array.Empty<NewModelSourceDraft>();
        }
        var collapsed = node.Children
          .SelectMany(child => CreateNewModelSources(
            child,
            effectiveTransform,
            parsed,
            options,
            animatedSourceNodes,
            emitterOwnership))
          .ToArray();
        if (collapsed.Length == 0
          && !emitterOwnership.ScaffoldingNodeIndices.Contains(nodeIndex))
        {
          return Array.Empty<NewModelSourceDraft>();
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
            GetMaterialHandle(parsed, primitive.MaterialIndex.Value),
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
          animatedSourceNodes,
          emitterOwnership))
        .ToArray();
      var pivot = new System.Numerics.Vector3(translation.X, -translation.Z, translation.Y);
      var typedRole = options.ObjectRoles.TryGetValue(
        GetNodeHandle(parsed, nodeIndex),
        out var role)
          ? role.ToCanonical()
          : null;
      var inferredMarkerFlags = emitterOwnership.MarkerFlagsBySourceNode.TryGetValue(
        nodeIndex,
        out var markers)
          ? markers
          : StaticRenderObjectFlags.None;
      var canonicalRole = typedRole is null && inferredMarkerFlags == StaticRenderObjectFlags.None
        ? null
        : new CanonicalStaticObjectRole(
          (typedRole?.Flags ?? StaticRenderObjectFlags.None) | inferredMarkerFlags,
          typedRole?.BarrelMaximumAngle ?? 0);
      return new[]
      {
        new NewModelSourceDraft(
          nodeIndex,
          new CanonicalStaticSourceObject(
            renderObjects,
            children.Select(child => child.Source),
            canonicalRole),
          pivot,
          children)
      };
    }

    private static void ApplyNewModelBaseHeaderArtistObjects(
      ParsedGlb parsed,
      StaticMeshEditSession edit,
      GltfNewModelImportOptions options,
      EmitterOwnershipPlan emitterOwnership)
    {
      var nodes = parsed.Nodes.Select(node => (node, (MetadataEnvelope?)null)).ToArray();
      var transforms = CreateArtistObjectTransforms(parsed.RootNodeIndex, nodes)
        .ToDictionary(item => item.Key, item => item.Value);
      var parentIndices = CreateParentIndices(nodes);
      foreach (var nodeIndex in emitterOwnership.EmitterNodeIndices)
      {
        transforms[nodeIndex] = CreateEffectiveNodeTransform(
          nodeIndex,
          parsed.RootNodeIndex,
          parentIndices,
          nodes);
      }
      var attachments = new Dictionary<int, int>();
      var cannons = new Dictionary<int, int>();
      var lights = new Dictionary<(string Type, int Number), int>();
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
        else if (GlbDocument.TryParseCannonHelperName(node.Name, out physicalNumber))
        {
          if (!cannons.TryAdd(physicalNumber, index))
          {
            throw ArtistObjectConflict("A cannon render-position target is occupied more than once.");
          }
        }
        else if (node.LightIndex.HasValue
          && GlbDocument.TryParseStaticLightHelperName(node.Name, out var type, out physicalNumber))
        {
          if (!lights.TryAdd((type, physicalNumber), index))
          {
            throw ArtistObjectConflict(
              "A static-light target is occupied more than once.",
              $"nodes[{index}]");
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
        edit.ReplaceAttachmentRecord(
          cannon.Key,
          CreateAttachmentRecord(transforms[cannon.Value], 0x80));
        edit.ReplaceCannonRenderPosition(
          cannon.Key,
          CreateCannonRenderPositionRecord(transforms[cannon.Value].Translation));
      }
      var definitionReferenceCounts = new int[parsed.Lights.Count];
      foreach (var lightIndex in parsed.Nodes.Select(node => node.LightIndex).OfType<int>())
      {
        if (lightIndex >= 0 && lightIndex < definitionReferenceCounts.Length)
        {
          definitionReferenceCounts[lightIndex]++;
        }
      }
      var usedLightDefinitions = new HashSet<int>();
      foreach (var item in lights)
      {
        var node = parsed.Nodes[item.Value];
        if (node.LightIndex is null
          || node.LightIndex.Value < 0
          || node.LightIndex.Value >= parsed.Lights.Count
          || parsed.Lights[node.LightIndex.Value].Type != item.Key.Type)
        {
          throw new UnsupportedGltfDomainException(
            "StaticLights",
            $"nodes[{item.Value}].extensions.KHR_lights_punctual");
        }
        if (definitionReferenceCounts[node.LightIndex.Value] != 1)
        {
          throw ArtistObjectConflict(
            "A static-light artist object must own an unshared punctual-light definition.",
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}]");
        }
        var lightOptions = options.StaticLightOptions.TryGetValue(
          GetLightHandle(parsed, node.LightIndex.Value),
          out var explicitLightOptions)
          ? explicitLightOptions
          : null;
        var range = parsed.Lights[node.LightIndex.Value].Range;
        if (item.Key.Type == "spot"
          && range.HasValue
          && lightOptions?.TargetDistance.HasValue == true)
        {
          throw new UnsupportedGltfDomainException(
            "StaticLights",
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}].range");
        }
        var targetDistance = range ?? lightOptions?.TargetDistance;
        if (item.Key.Type == "spot"
          && (targetDistance is not > 0 || !float.IsFinite(targetDistance.Value)))
        {
          throw new UnsupportedGltfDomainException(
            "StaticLights",
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}].range");
        }
        if (!usedLightDefinitions.Add(node.LightIndex.Value))
        {
          throw ArtistObjectConflict(
            "A new-model static-light definition cannot be shared.",
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}]");
        }
        var hasCanonicalNodeName = GlbDocument.TryParseStaticLightHelperName(
          node.Name,
          out _,
          out _);
        var hasCanonicalDefinitionName = GlbDocument.TryParseStaticLightHelperName(
          parsed.Lights[node.LightIndex.Value].Name,
          out var definitionType,
          out var definitionNumber);
        if (hasCanonicalNodeName && !hasCanonicalDefinitionName
          || hasCanonicalDefinitionName
            && (definitionType != item.Key.Type || definitionNumber != item.Key.Number))
        {
          throw new StaticLightMetadataException(
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}].name",
            "The canonical static-light instance and definition names contradict each other.");
        }
        var attachmentNumber = item.Key.Type == "spot" ? item.Key.Number + 12 : item.Key.Number + 16;
        edit.ReplaceAttachmentRecord(
          attachmentNumber,
          CreateStaticLightAttachmentRecord(
            transforms[item.Value].Translation,
            $"nodes[{item.Value}].translation"));
        edit.ReplaceStaticLightRecord(
          ToStaticLightRecordKind(item.Key.Type),
          item.Key.Number,
          CreateConvertedStaticLightRecord(
            parsed.Lights[node.LightIndex.Value],
            transforms[item.Value],
            $"nodes[{item.Value}]",
            lightOptions,
            StaticLightAuthoringIntent.NewModel),
          new[] { "NewStaticLight" });
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
      var authoredAnimations = parsed.Animations.Select((animation, index) => (animation, index))
        .Where(item => TryGetCanonicalAnimationClass(item.animation.Name, out _)).ToArray();
      if (authoredAnimations.Length == 0)
      {
        return new NewModelAnimationSet(default, Array.Empty<NewModelAnimationTrack>());
      }

      var lengths = new byte[4];
      var tracks = new List<NewModelAnimationTrack>();
      var animatedSourceNodes = new HashSet<int>();
      foreach (var authored in authoredAnimations)
      {
        var animation = authored.animation;
        var classIndex = TryGetCanonicalAnimationClass(animation.Name, out var canonicalClass)
          ? canonicalClass
          : throw new UnsupportedGltfDomainException("animations");
        if (lengths[classIndex] != 0)
        {
          throw new UnsupportedGltfDomainException("animations");
        }
        if (animation.Objects.Count == 0
          || !TryGetCanonicalAnimationFrameCount(animation, out var frameCount))
        {
          throw new UnsupportedGltfDomainException("animations");
        }
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

    private static PreservationReport CreateNewModelPreservationReport(StaticMeshAsset asset)
    {
      var changes = new List<PreservationChange>
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
        Canonicalized("RootSourceObject"),
        Canonicalized("RootTrailingBytes")
      };
      for (var index = 0; index < asset.StaticRenderObjectSequence.Count; index++)
      {
        var path = $"StaticRenderObjectSequence[{index}]";
        changes.Add(Canonicalized(path + ".ObjectFlags"));
        changes.Add(Canonicalized(path + ".BarrelMaximumAngle"));
        changes.Add(Canonicalized(path + ".Pivot"));
        changes.Add(Canonicalized(path + ".RenderVertices"));
        changes.Add(Canonicalized(path + ".VertexBlockCount"));
        changes.Add(Canonicalized(path + ".VertexBlockPadding"));
        changes.Add(Canonicalized(path + ".Triangles"));
        changes.Add(Canonicalized(path + ".AnimationClassValue"));
        changes.Add(Canonicalized(path + ".AnimationTracks.ScaleFrames"));
        changes.Add(Canonicalized(path + ".AnimationTracks.TranslationFrames"));
        changes.Add(Canonicalized(path + ".AnimationTracks.Matrices"));
        changes.Add(Canonicalized(path + ".TexturePathBytes"));
        changes.Add(Canonicalized(path + ".NextRecordMarker"));
      }
      changes.Add(Canonicalized("StoredTrailingHierarchyUnwindCount"));
      return new PreservationReport(changes);
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

    private sealed class EmitterOwnershipPlan
    {
      internal IReadOnlyDictionary<int, StaticRenderObjectFlags> MarkerFlagsBySourceNode { get; }
      internal ISet<int> EmitterNodeIndices { get; }
      internal ISet<int> ScaffoldingNodeIndices { get; }

      internal EmitterOwnershipPlan(
        IReadOnlyDictionary<int, StaticRenderObjectFlags> markerFlagsBySourceNode,
        ISet<int> emitterNodeIndices,
        ISet<int> scaffoldingNodeIndices)
      {
        MarkerFlagsBySourceNode = markerFlagsBySourceNode;
        EmitterNodeIndices = emitterNodeIndices;
        ScaffoldingNodeIndices = scaffoldingNodeIndices;
      }
    }

    private static async Task<OperationResult<GltfEditImportResult>> ImportParsedAsync(
      ParsedGlb parsed,
      InterchangeBaseline expectedBaseline,
      GltfEditImportOptions options,
      GltfOperationProfile profile,
      CancellationToken cancellationToken)
    {
      var sceneLightDiagnostics = CreateIgnoredSceneLightDiagnostics(parsed);
      var manifest = GlbDocument.ParseMetadata(
        parsed.ManifestMetadata ?? throw new MissingMetadataException("scene"),
        profile);
      ValidateManifestMetadata(manifest, expectedBaseline, parsed.MetadataConflicts);
      var metadataBaseline = new InterchangeBaseline(manifest.AssetLineageId, manifest.DocumentId);

      byte[] sourceMsh;
      try
      {
        sourceMsh = GlbDocument.DecodeBase64Url(
          manifest.SourceMsh!,
          profile.MaxMetadataBytes).ToArray();
      }
      catch (FormatException ex)
      {
        return Failed<GltfEditImportResult>(MetadataDiagnostic(
          GltfDiagnosticCodes.MalformedMetadata,
          2003,
          "scenes[0]",
          ex.Message,
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.RetryWithMetadata,
          GltfMetadataConflictActions.DiscardLineage));
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
        return Failed<GltfEditImportResult>(MetadataDiagnostic(
          GltfDiagnosticCodes.MalformedMetadata,
          2003,
          "scenes[0]",
          "Preserved MSH state did not pass the safe MSH reader.",
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.RetryWithMetadata,
          GltfMetadataConflictActions.DiscardLineage));
      }

      StaticMeshAsset asset;
      try
      {
        var decoded = EarthTool.MSH.Internal.MshV1Decoder.Decode(
          sourceMsh,
          MshOperationProfile.Default,
          cancellationToken,
          new MeshAssetLineageId(manifest.AssetLineageId),
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
        return Failed<GltfEditImportResult>(MetadataDiagnostic(
          GltfDiagnosticCodes.MalformedMetadata,
          2003,
          "scenes[0]",
          ex.Message,
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.DiscardLineage));
      }

      MetadataGraphDetector.Detect(
        parsed,
        manifest,
        asset,
        metadataBaseline,
        profile,
        parsed.MetadataConflicts);
      var metadataConflicts = parsed.MetadataConflicts.Build();
      var lineageResolution = options.ConflictResolutions.SingleOrDefault(resolution =>
        resolution.Action == GltfMetadataConflictActions.AdoptAsNew
        || resolution.Action == GltfMetadataConflictActions.DiscardLineage);
      if (lineageResolution is not null)
      {
        var conflictDiagnostics = metadataConflicts.Select(conflict =>
          BindConflictToBaseline(ToDiagnostic(conflict), expectedBaseline)).ToArray();
        var matching = conflictDiagnostics.SingleOrDefault(diagnostic =>
          diagnostic.Data["conflictKey"] == lineageResolution.ConflictKey);
        if (options.ConflictResolutions.Count != 1
          || matching is null
          || !matching.Data["actions"].Split(',').Contains(
            lineageResolution.Action,
            StringComparer.Ordinal))
        {
          return Failed<GltfEditImportResult>(InvalidConflictResolution(
            "The whole-lineage action is stale or is not allowed for the matching conflict."));
        }

        return ImportWithoutMetadata(
          parsed,
          profile,
          cancellationToken,
          lineageResolution);
      }
      var deletionResolution = options.ConflictResolutions.FirstOrDefault(resolution =>
        resolution.Action == GltfMetadataConflictActions.AcceptDeletion);
      if (deletionResolution is not null)
      {
        var conflictDiagnostics = metadataConflicts.Select(conflict =>
          BindConflictToBaseline(ToDiagnostic(conflict), expectedBaseline)).ToArray();
        var matching = conflictDiagnostics.SingleOrDefault(diagnostic =>
          diagnostic.Data["conflictKey"] == deletionResolution.ConflictKey);
        if (matching is null || matching.Code != GltfDiagnosticCodes.MissingExpectedScope)
        {
          return Failed<GltfEditImportResult>(InvalidConflictResolution(
            "Deletion acceptance is stale or unsupported for the matching conflict."));
        }
        var accepted = matching.Data["carrierType"] == "material"
          ? RestoreDeletedMaterialScope(parsed, matching, metadataBaseline, profile)
          : matching.Data["carrierType"] == "mesh"
            ? RestoreDeletedMeshScope(parsed, matching, metadataBaseline, asset, profile)
            : AcceptDeletedNativeScope(parsed, matching, profile);
        var retried = await ImportParsedAsync(
          accepted,
          expectedBaseline,
          new GltfEditImportOptions(options.ConflictResolutions.Where(resolution =>
            !ReferenceEquals(resolution, deletionResolution))),
          profile,
          cancellationToken).ConfigureAwait(false);
        return WithAppliedResolution(retried, deletionResolution);
      }
      var guardResolution = options.ConflictResolutions.FirstOrDefault(resolution =>
        resolution.Action == GltfMetadataConflictActions.RegenerateDerivedState
        || resolution.Action == GltfMetadataConflictActions.DiscardAffectedState);
      if (guardResolution is not null)
      {
        var conflictDiagnostics = metadataConflicts.Select(conflict =>
          BindConflictToBaseline(ToDiagnostic(conflict), expectedBaseline)).ToArray();
        var matching = conflictDiagnostics.SingleOrDefault(diagnostic =>
          diagnostic.Data["conflictKey"] == guardResolution.ConflictKey);
        if (matching is not null
          && matching.Path.Contains(".guards.", StringComparison.Ordinal)
          && matching.Data["actions"].Split(',').Contains(
            guardResolution.Action,
            StringComparer.Ordinal))
        {
          var discard = guardResolution.Action == GltfMetadataConflictActions.DiscardAffectedState;
          var regenerated = matching.Data["carrierType"] switch
          {
            "mesh" => RewriteMeshGuard(parsed, matching, metadataBaseline, discard, profile),
            "node" => RewriteNodeGuard(parsed, matching, metadataBaseline, asset, discard, profile),
            "light" => RewriteLightGuard(parsed, matching, metadataBaseline, discard, profile),
            _ => null
          };
          if (regenerated is null)
          {
            return Failed<GltfEditImportResult>(InvalidConflictResolution(
              "The guard action is not supported for the matching carrier."));
          }
          var retried = await ImportParsedAsync(
            regenerated,
            expectedBaseline,
            new GltfEditImportOptions(options.ConflictResolutions.Where(resolution =>
              !ReferenceEquals(resolution, guardResolution))),
            profile,
            cancellationToken).ConfigureAwait(false);
          return WithAppliedResolution(retried, guardResolution);
        }
      }
      var mapResolution = options.ConflictResolutions.FirstOrDefault(resolution =>
        resolution.Action == GltfMetadataConflictActions.MapScope);
      if (mapResolution is not null)
      {
        var conflictDiagnostics = metadataConflicts.Select(conflict =>
          BindConflictToBaseline(ToDiagnostic(conflict), expectedBaseline)).ToArray();
        var matching = conflictDiagnostics.SingleOrDefault(diagnostic =>
          diagnostic.Data["conflictKey"] == mapResolution.ConflictKey);
        if (matching is null
          || !matching.Data["actions"].Split(',').Contains(
            GltfMetadataConflictActions.MapScope,
            StringComparer.Ordinal))
        {
          return Failed<GltfEditImportResult>(InvalidConflictResolution(
            "The scope mapping is stale, incomplete, or is not allowed for the matching conflict."));
        }

        ParsedGlb mapped;
        try
        {
          mapped = MapScope(parsed, matching, mapResolution, profile);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidDataException)
        {
          return Failed<GltfEditImportResult>(InvalidConflictResolution(ex.Message));
        }
        var retried = await ImportParsedAsync(
          mapped,
          expectedBaseline,
          new GltfEditImportOptions(options.ConflictResolutions.Where(resolution =>
            !ReferenceEquals(resolution, mapResolution))),
          profile,
          cancellationToken).ConfigureAwait(false);
        return WithAppliedResolution(retried, mapResolution);
      }
      var forkResolution = options.ConflictResolutions.FirstOrDefault(resolution =>
        resolution.Action == GltfMetadataConflictActions.ForkScope);
      if (forkResolution is not null)
      {
        var conflictDiagnostics = metadataConflicts.Select(conflict =>
          BindConflictToBaseline(ToDiagnostic(conflict), expectedBaseline)).ToArray();
        var matching = conflictDiagnostics.SingleOrDefault(diagnostic =>
          diagnostic.Data["conflictKey"] == forkResolution.ConflictKey);
        if (matching is null
          || !matching.Data["actions"].Split(',').Contains(
            GltfMetadataConflictActions.ForkScope,
            StringComparer.Ordinal))
        {
          return Failed<GltfEditImportResult>(InvalidConflictResolution(
            "The scope fork is stale, incomplete, or is not allowed for the matching conflict."));
        }

        var forked = RemoveScopeMetadata(parsed, matching.Data["nativePath"], profile);
        var retried = await ImportParsedAsync(
          forked,
          expectedBaseline,
          new GltfEditImportOptions(options.ConflictResolutions.Where(resolution =>
            !ReferenceEquals(resolution, forkResolution))),
          profile,
          cancellationToken).ConfigureAwait(false);
        return WithAppliedResolution(retried, forkResolution);
      }
      var conflictResolution = ResolveMetadataConflicts(metadataConflicts, expectedBaseline, options);
      if (conflictResolution.Diagnostics.Count != 0)
      {
        return Failed<GltfEditImportResult>(conflictResolution.Diagnostics);
      }
      var branchAccepted = conflictResolution.Applied.Any(resolution =>
        resolution.Action == GltfMetadataConflictActions.AcceptBranch);
      var reconciliationBaseline = branchAccepted ? metadataBaseline : expectedBaseline;

      var meshes = parsed.Meshes
        .Select(mesh => new
        {
          Parsed = mesh,
          Metadata = mesh.Metadata is null
            ? null
            : GlbDocument.ParseMetadata(mesh.Metadata, profile)
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
              profile)
        })
        .ToArray();
      var lights = parsed.Lights
        .Select(light => new
        {
          Parsed = light,
          Metadata = light.Metadata is null
            ? null
            : GlbDocument.ParseMetadata(
              light.Metadata,
              profile)
        })
        .ToArray();
      var edit = asset.Edit();
      var hierarchy = ReconcileHierarchy(
        parsed,
        nodes.Select(node => (node.Parsed, node.Metadata)).ToArray(),
        meshes.Select(mesh => (mesh.Parsed, mesh.Metadata)).ToArray(),
        asset,
        reconciliationBaseline,
        edit);
      var artistObjects = ReconcileBaseHeaderArtistObjects(
        parsed,
        nodes.Select(node => (node.Parsed, node.Metadata)).ToArray(),
        lights.Select(light => (light.Parsed, light.Metadata)).ToArray(),
        asset,
        hierarchy,
        reconciliationBaseline,
        manifest.ScopeNextIds,
        edit);
      try
      {
        var animationPlan = CreateAnimationEditPlan(
          parsed,
          manifest,
          nodes.Select(node => node.Metadata?.ScopeKind == "object"
              && node.Metadata.AttachmentRecord is null
              && node.Metadata.CannonRenderPositionRecord is null
              && node.Metadata.StaticLightAttachmentRecord is null
                ? node.Metadata
                : null).ToArray(),
          asset,
          reconciliationBaseline,
          profile.MaxOutputBytes);
        var sourcesByLocalId = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
          .ToDictionary(source => source.Id.Value);
        foreach (var replacement in animationPlan.Replacements)
        {
          var tracks = StaticAnimationProjection.CreateCanonicalTracks(replacement.Frames);
          edit.ReplaceAnimation(
            sourcesByLocalId[replacement.SourceObjectLocalId].StaticRenderObjectIds[0],
            tracks.ScaleFrames,
            tracks.TranslationFrames,
            tracks.Matrices,
            replacement.AnimationClassValue);
        }
        if (!animationPlan.Lengths.Equals(asset.CommonBaseHeader.AnimationLengths))
        {
          edit.ReplaceAnimationLengths(animationPlan.Lengths);
        }
        if (!animationPlan.FrameIndices.Equals(asset.CommonBaseHeader.AnimationFrameIndices))
        {
          edit.ReplaceAnimationFrameIndices(animationPlan.FrameIndices);
        }
      }
      catch (StaleNativeProjectionException ex)
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.StaleNativeProjection,
          2016,
          "animations",
          ex.Message));
      }
      IReadOnlyList<PartitionMatch> partitionMatches;
      try
      {
        partitionMatches = MatchPartitions(meshes.Select((mesh, index) =>
          (mesh.Parsed,
            mesh.Metadata,
            Discarded: parsed.DiscardedMetadataScopes.Contains($"meshes[{index}]"),
            Index: index))
          .Where(mesh => hierarchy.RetainedMeshIndices.Contains(mesh.Index))
          .Select(mesh => (mesh.Parsed, Metadata: mesh.Metadata!, mesh.Discarded)).ToArray(),
          asset,
          reconciliationBaseline);
        partitionMatches = partitionMatches.Select(match =>
          hierarchy.Transforms.TryGetValue(match.SourceObjectId, out var transform)
            ? TransformPartition(match, transform)
            : match).ToArray();
      }
      catch (StaleNativeProjectionException ex)
      {
        return Failed<GltfEditImportResult>(Diagnostic(
          GltfDiagnosticCodes.StaleNativeProjection,
          2016,
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
      ApplyEmitterMarkerOwnershipChanges(
        asset,
        hierarchy.Root,
        partitionMatches,
        artistObjects,
        edit);
      ApplyMaterialBindings(
        parsed,
        asset,
        reconciliationBaseline,
        partitionMatches.Select(match => match.Partition)
          .Concat(hierarchy.AddedPartitions)
          .ToArray(),
        edit,
        profile);

      var sourceEffectivePositions = CreateEffectivePositions(
        asset.RootSourceObject,
        source => source.StaticRenderObjectIds.SelectMany(renderObjectId =>
          asset.StaticRenderObjectSequence.Single(record =>
            record.Id.Equals(renderObjectId)).RenderVertices.Select(vertex => vertex.Position)),
        source => asset.StaticRenderObjectSequence.Single(record =>
          record.Id.Equals(source.StaticRenderObjectIds[0])).Pivot,
        source => source.Children);
      var partitionsByLocalId = partitionMatches.Select(match => match.Partition)
        .Concat(hierarchy.AddedPartitions)
        .ToDictionary(partition => partition.LocalId);
      var sourceRecordsById = asset.StaticRenderObjectSequence.ToDictionary(record => record.Id);
      var sourceRecordsByLocalId = asset.StaticRenderObjectSequence.ToDictionary(record => record.LocalId);
      var currentEffectivePositions = CreateEffectivePositions(
        hierarchy.Root,
        source => source.StaticRenderObjectIds.SelectMany(renderObjectId =>
          partitionsByLocalId.TryGetValue(renderObjectId.Value, out var partition)
            ? partition.Vertices.Select(vertex => ToCanonicalPosition(vertex.Position))
            : Enumerable.Empty<System.Numerics.Vector3>()),
        source => hierarchy.Pivots.TryGetValue(source.StaticRenderObjectIds[0], out var pivot)
          ? pivot
          : sourceRecordsById.TryGetValue(source.StaticRenderObjectIds[0], out var record)
            ? record.Pivot
            : System.Numerics.Vector3.Zero,
        source => source.Children);
      var effectivePositionsChanged = hierarchy.Transforms.Count != 0
        || hierarchy.Pivots.Keys.Any(renderObjectId =>
          !renderObjectId.Equals(hierarchy.Root.StaticRenderObjectIds[0]))
        || !HaveSameEffectivePositions(sourceEffectivePositions, currentEffectivePositions)
        || partitionMatches.Any(match => !match.Added
          && !match.Retained
          && !sourceRecordsByLocalId[match.Partition.LocalId].RenderVertices
            .Select(vertex => vertex.Position)
            .SequenceEqual(match.Partition.Vertices.Select(vertex =>
              ToCanonicalPosition(vertex.Position))));
      if (effectivePositionsChanged)
      {
        if (!TryCreateHorizontalExtents(
          currentEffectivePositions,
          out var horizontalExtents,
          out var rangeFailure))
        {
          return Failed<GltfEditImportResult>(InvalidGeometry(
            "CommonBaseHeader.HorizontalExtents",
            rangeFailure!));
        }
        edit.ReplaceHorizontalExtents(horizontalExtents!);
      }

      if (hierarchy.Changed)
      {
        edit.ApplyHierarchy(hierarchy.Root, hierarchy.Sequence);
      }

      var partitions = partitionMatches.Select(match => match.Partition)
        .Concat(hierarchy.AddedPartitions).ToArray();
      var fingerprint = StaticGeometryFingerprint.Create(reconciliationBaseline, partitions);
      var committed = edit.Commit(new MshOperationProfile(
        maxOutputBytes: profile.MaxOutputBytes,
        maxStaticVerticesPerObject: profile.MaxActiveRenderVertices,
        maxStaticHierarchyDepth: profile.MaxHierarchyDepth));
      if (!committed.TryGetValue(out var reconciled))
      {
        var message = string.Join("; ", committed.Diagnostics.Select(diagnostic => diagnostic.Message));
        return Failed<GltfEditImportResult>(InvalidGeometry("meshes", message));
      }

      var nextBaseline = new InterchangeBaseline(reconciliationBaseline.AssetLineageId, Guid.NewGuid());
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
          .Where(path => !changedRecordPaths.Contains(path, StringComparer.Ordinal)))
        .Concat(Enumerable.Range(1, 4)
          .Select(number => $"CommonBaseHeader.StaticSpotLights[{number}]")
          .Where(path => !changedRecordPaths.Any(changed =>
            changed == path || changed.StartsWith(path + ".", StringComparison.Ordinal))))
        .Concat(Enumerable.Range(1, 4)
          .Select(number => $"CommonBaseHeader.StaticOmniLights[{number}]")
          .Where(path => !changedRecordPaths.Any(changed =>
            changed == path || changed.StartsWith(path + ".", StringComparison.Ordinal))));
      var nextMetadataIds = manifest.ScopeNextIds.ToDictionary(
        pair => pair.Key,
        pair => pair.Value,
        StringComparer.Ordinal);
      nextMetadataIds["object"] = Math.Max(
        nextMetadataIds.TryGetValue("object", out var nextObjectLocalId) ? nextObjectLocalId : 1,
        artistObjects.NextObjectLocalId);
      return new OperationResult<GltfEditImportResult>(
        OperationStatus.Succeeded,
        new GltfEditImportResult(
          reconciled,
          nextBaseline,
          fingerprint,
          committed.Preservation,
          restoredPaths,
          CollectUnknownMetadata(
            new[] { manifest }
              .Concat(meshes.Where(item => item.Metadata is not null).Select(item => item.Metadata!))
              .Concat(nodes.Where(item => item.Metadata is not null).Select(item => item.Metadata!))
              .Concat(lights.Where(item => item.Metadata is not null).Select(item => item.Metadata!))),
          nextMetadataIds,
          artistObjects.ArtistObjectLocalIds,
          branchAccepted
            ? GltfMetadataLineageDisposition.BranchAccepted
            : GltfMetadataLineageDisposition.Retained,
          conflictResolution.Applied),
        sceneLightDiagnostics
          .Concat(CreateIgnoredInertDataDiagnostics(parsed))
          .Concat(CreateIgnoredSceneNodeDiagnostics(parsed))
          .Concat(CreateEmitterHierarchyDiagnostics(reconciled))
          .Concat(committed.Diagnostics));
    }

    private static IReadOnlyDictionary<string, string> CollectUnknownMetadata(
      IEnumerable<MetadataEnvelope> envelopes)
    {
      return new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
        envelopes.SelectMany(envelope => envelope.UnknownMembers.Select(member => new
        {
          Key = $"{envelope.ScopeKind}:{envelope.LocalId}:{member.Key}",
          member.Value
        }))
          .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));
    }

    private static OperationResult<GltfEditImportResult> ImportWithoutMetadata(
      ParsedGlb parsed,
      GltfOperationProfile profile,
      CancellationToken cancellationToken,
      GltfMetadataConflictResolution resolution)
    {
      var unclaimed = new ParsedGlb(
        manifestMetadata: null,
        hasReservedMetadata: false,
        parsed.Meshes.Select(mesh => new ParsedGltfMesh(null, mesh.Primitives)).ToArray(),
        parsed.Nodes.Select(node => new ParsedGltfNode(
          node.Name,
          node.IsPlacementRoot,
          metadata: null,
          node.MeshIndex,
          node.LightIndex,
          node.CameraIndex,
          node.Children,
          node.LocalTransform)).ToArray(),
        parsed.Materials.Select(material => new ParsedGltfMaterial(
          metadata: null,
          material.HasBaseColorTexture)).ToArray(),
        parsed.Animations,
        parsed.Lights.Select(light => new ParsedGltfLight(
          light.Name,
          metadata: null,
          light.Type,
          light.Color,
          light.Intensity,
          light.Range,
          light.InnerConeAngle,
          light.OuterConeAngle)).ToArray(),
        GetNewModelRootNodeIndex(parsed),
        new MetadataConflictCollector(profile.MaxMetadataConflicts),
        parsed.IgnoredInertPaths);
      var imported = ImportNewModelParsed(
        unclaimed,
        profile,
        cancellationToken,
        new GltfNewModelImportOptions());
      if (imported.Value is not GltfNewModelImportResult value)
      {
        return Failed<GltfEditImportResult>(imported.Diagnostics);
      }

      var disposition = resolution.Action == GltfMetadataConflictActions.AdoptAsNew
        ? GltfMetadataLineageDisposition.AdoptedAsNew
        : GltfMetadataLineageDisposition.Discarded;
      return new OperationResult<GltfEditImportResult>(
        OperationStatus.Succeeded,
        new GltfEditImportResult(
          value.Asset,
          value.Baseline,
          appliedFingerprint: null,
          value.Preservation,
          Array.Empty<string>(),
          lineageDisposition: disposition,
          appliedConflictResolutions: new[] { resolution }),
        imported.Diagnostics);
    }

    private static int GetNewModelRootNodeIndex(ParsedGlb parsed)
    {
      for (var index = 0; index < parsed.Nodes.Count; index++)
      {
        if (parsed.Nodes[index].IsPlacementRoot)
        {
          return index;
        }
      }
      return parsed.RootNodeIndex;
    }

    private static ParsedGlb RemoveScopeMetadata(
      ParsedGlb parsed,
      string nativePath,
      GltfOperationProfile profile)
    {
      return RewriteScopeMetadata(parsed, nativePath, metadata: null, profile);
    }

    private static ParsedGlb MapScope(
      ParsedGlb parsed,
      OperationDiagnostic conflict,
      GltfMetadataConflictResolution resolution,
      GltfOperationProfile profile)
    {
      var targetMetadata = GetScopeMetadata(parsed, resolution.TargetNativePath!);
      if (targetMetadata is null)
      {
        throw new InvalidDataException("The scope mapping target has no valid metadata envelope.");
      }
      var target = JsonNode.Parse(targetMetadata)?.AsObject()
        ?? throw new InvalidDataException("The scope mapping target metadata is malformed.");
      var targetId = target["id"]?.GetValue<int>()
        ?? throw new InvalidDataException("The scope mapping target has no local ID.");
      if (conflict.Data.TryGetValue("referencedScopeKind", out var referencedKind)
        && target["kind"]?.GetValue<string>() != referencedKind)
      {
        throw new InvalidDataException("The scope mapping target kind does not match the reference.");
      }

      var nativePath = conflict.Data["nativePath"];
      var sourceMetadata = GetScopeMetadata(parsed, nativePath);
      if (sourceMetadata is null)
      {
        throw new InvalidDataException("The conflicted scope has no metadata envelope to map.");
      }
      var source = JsonNode.Parse(sourceMetadata)?.AsObject()
        ?? throw new InvalidDataException("The conflicted scope metadata is malformed.");
      if (conflict.Path.Contains(".payload.partitions[", StringComparison.Ordinal))
      {
        var marker = conflict.Path.IndexOf(".payload.partitions[", StringComparison.Ordinal)
          + ".payload.partitions[".Length;
        var end = conflict.Path.IndexOf(']', marker);
        var partitionIndex = int.Parse(
          conflict.Path.Substring(marker, end - marker),
          System.Globalization.CultureInfo.InvariantCulture);
        source["payload"]!["partitions"]![partitionIndex]!["localId"] = targetId;
      }
      else if (conflict.Path.EndsWith(
        ".payload.staticLightInstance.definitionLocalId",
        StringComparison.Ordinal))
      {
        source["payload"]!["staticLightInstance"]!["definitionLocalId"] = targetId;
      }
      else
      {
        throw new InvalidDataException("The conflict does not expose a supported scope reference mapping.");
      }

      return RewriteScopeMetadata(parsed, nativePath, source.ToJsonString(), profile);
    }

    private static ParsedGlb RewriteMeshGuard(
      ParsedGlb parsed,
      OperationDiagnostic conflict,
      InterchangeBaseline baseline,
      bool discardAffectedState,
      GltfOperationProfile profile)
    {
      var nativePath = conflict.Data["nativePath"];
      if (!TryReadIndex(nativePath, "meshes", out var meshIndex))
      {
        throw new InvalidDataException("The guard conflict is not owned by a mesh scope.");
      }
      var metadata = JsonNode.Parse(parsed.Meshes[meshIndex].Metadata!)?.AsObject()
        ?? throw new InvalidDataException("The mesh metadata envelope is malformed.");
      var localId = metadata["id"]!.GetValue<int>();
      var partitions = parsed.Meshes[meshIndex].Primitives.Select((primitive, index) =>
        new GeometryPartition(
          metadata["payload"]!["partitions"]![index]!["localId"]!.GetValue<int>(),
          primitive.Vertices,
          primitive.Triangles)).ToArray();
      var fingerprint = StaticGeometryFingerprint.CreateMesh(baseline, localId, partitions);
      metadata["guards"]!["nativeProjection"] = new JsonObject
      {
        ["projection"] = "static-geometry",
        ["version"] = 1,
        ["algorithm"] = "sha256",
        ["digest"] = EncodeSha256(fingerprint.Sha256)
      };
      if (discardAffectedState)
      {
        const string discarded = "0000000000000000000000000000000000000000000000000000000000000000";
        foreach (var partition in metadata["payload"]!["partitions"]!.AsArray())
        {
          partition!["sha256"] = discarded;
        }
      }
      var rewritten = RewriteScopeMetadata(parsed, nativePath, metadata.ToJsonString(), profile);
      if (discardAffectedState)
      {
        rewritten.DiscardedMetadataScopes.Add(nativePath);
      }
      return rewritten;
    }

    private static ParsedGlb RewriteNodeGuard(
      ParsedGlb parsed,
      OperationDiagnostic conflict,
      InterchangeBaseline baseline,
      StaticMeshAsset asset,
      bool discardAffectedState,
      GltfOperationProfile profile)
    {
      var nativePath = conflict.Data["nativePath"];
      if (discardAffectedState)
      {
        return RemoveScopeMetadata(parsed, nativePath, profile);
      }
      var value = GetScopeMetadata(parsed, nativePath)
        ?? throw new InvalidDataException("The node guard carrier has no metadata.");
      var envelope = GlbDocument.ParseMetadata(value, profile);
      var guardName = conflict.Path.Substring(
        conflict.Path.IndexOf(".guards.", StringComparison.Ordinal) + ".guards.".Length);
      string digest;
      string projection;
      if (envelope.AttachmentRecord is not null)
      {
        var number = envelope.AttachmentPhysicalNumber
          ?? throw new InvalidDataException("The attachment guard has no physical target.");
        digest = GlbDocument.CreateAttachmentPoseFingerprint(
          baseline,
          envelope.LocalId,
          number,
          envelope.AttachmentRecord);
        projection = "attachment.pose";
      }
      else if (envelope.CannonPhysicalNumber is int cannonNumber
        && envelope.CannonAttachmentRecord is not null
        && envelope.CannonRenderPositionRecord is not null)
      {
        var guards = GlbDocument.CreateCannonGuards(
          baseline,
          envelope.LocalId,
          cannonNumber,
          envelope.CannonAttachmentRecord,
          envelope.CannonRenderPositionRecord);
        if (!guards.TryGetValue(guardName, out var cannonDigest))
        {
          throw new InvalidDataException("The cannon guard is not regenerable.");
        }
        digest = cannonDigest;
        projection = guardName;
      }
      else
      {
        throw new InvalidDataException("The node scope has no regenerable derived guard.");
      }
      var metadata = JsonNode.Parse(value)!.AsObject();
      metadata["guards"]![guardName] = CreateGuard(projection, digest);
      return RewriteScopeMetadata(parsed, nativePath, metadata.ToJsonString(), profile);
    }

    private static ParsedGlb RewriteLightGuard(
      ParsedGlb parsed,
      OperationDiagnostic conflict,
      InterchangeBaseline baseline,
      bool discardAffectedState,
      GltfOperationProfile profile)
    {
      var nativePath = conflict.Data["nativePath"];
      if (!TryReadIndex(nativePath, "extensions.KHR_lights_punctual.lights", out var lightIndex))
      {
        throw new InvalidDataException("The light guard conflict has no light carrier.");
      }
      if (discardAffectedState)
      {
        var discarded = RemoveScopeMetadata(parsed, nativePath, profile);
        for (var index = 0; index < discarded.Nodes.Count; index++)
        {
          if (discarded.Nodes[index].LightIndex == lightIndex
            && discarded.Nodes[index].Metadata is not null)
          {
            discarded = RewriteScopeMetadata(discarded, $"nodes[{index}]", null, profile);
          }
        }
        discarded.DiscardedMetadataScopes.Add(nativePath);
        return discarded;
      }
      var value = parsed.Lights[lightIndex].Metadata
        ?? throw new InvalidDataException("The light guard carrier has no metadata.");
      var envelope = GlbDocument.ParseMetadata(value, profile);
      var instance = parsed.Nodes.Where(node => node.LightIndex == lightIndex && node.Metadata is not null)
        .Select(node => GlbDocument.ParseMetadata(node.Metadata!, profile))
        .Single(node => node.StaticLightDefinitionLocalId == envelope.LocalId);
      var guards = GlbDocument.CreateStaticLightGuards(
        baseline,
        envelope.StaticLightType!,
        envelope.StaticLightPhysicalNumber!.Value,
        envelope.LocalId,
        envelope.StaticLightRecord!.ToArray(),
        instance.StaticLightAttachmentRecord!.ToArray());
      var guardName = conflict.Path.Substring(
        conflict.Path.IndexOf(".guards.", StringComparison.Ordinal) + ".guards.".Length);
      var metadata = JsonNode.Parse(value)!.AsObject();
      metadata["guards"]![guardName] = CreateGuard(guardName, guards[guardName]);
      return RewriteScopeMetadata(parsed, nativePath, metadata.ToJsonString(), profile);
    }

    private static JsonObject CreateGuard(string projection, string hexadecimal)
    {
      return new JsonObject
      {
        ["projection"] = projection,
        ["version"] = 1,
        ["algorithm"] = "sha256",
        ["digest"] = EncodeSha256(hexadecimal)
      };
    }

    private static ParsedGlb RestoreDeletedMaterialScope(
      ParsedGlb parsed,
      OperationDiagnostic conflict,
      InterchangeBaseline baseline,
      GltfOperationProfile profile)
    {
      var localId = int.Parse(
        conflict.Data["localId"],
        System.Globalization.CultureInfo.InvariantCulture);
      var metadata = new JsonObject
      {
        ["format"] = "earthtool.msh.gltf",
        ["version"] = 1,
        ["kind"] = "material",
        ["lineage"] = baseline.AssetLineageId.ToString("D"),
        ["document"] = baseline.DocumentId.ToString("D"),
        ["id"] = localId,
        ["guards"] = new JsonObject(),
        ["payload"] = new JsonObject
        {
          ["textureBinding"] = string.Empty
        }
      };
      return RewriteScopeMetadata(
        parsed,
        conflict.Data["nativePath"],
        metadata.ToJsonString(),
        profile);
    }

    private static ParsedGlb AcceptDeletedNativeScope(
      ParsedGlb parsed,
      OperationDiagnostic conflict,
      GltfOperationProfile profile)
    {
      var nativePath = conflict.Data["nativePath"];
      var current = parsed;
      if (TryReadIndex(nativePath, "meshes", out var meshIndex))
      {
        for (var index = 0; index < current.Nodes.Count; index++)
        {
          if (current.Nodes[index].MeshIndex == meshIndex && current.Nodes[index].Metadata is not null)
          {
            current = RewriteScopeMetadata(current, $"nodes[{index}]", metadata: null, profile);
          }
        }
      }
      else
      {
        const string lights = "extensions.KHR_lights_punctual.lights";
        if (!TryReadIndex(nativePath, lights, out var lightIndex))
        {
          throw new InvalidDataException("Deletion acceptance requires a mesh, material, or light scope.");
        }
        for (var index = 0; index < current.Nodes.Count; index++)
        {
          if (current.Nodes[index].LightIndex == lightIndex && current.Nodes[index].Metadata is not null)
          {
            current = RewriteScopeMetadata(current, $"nodes[{index}]", metadata: null, profile);
          }
        }
      }
      current.AcceptedDeletionScopes.Add(nativePath);
      return current;
    }

    private static ParsedGlb RestoreDeletedMeshScope(
      ParsedGlb parsed,
      OperationDiagnostic conflict,
      InterchangeBaseline baseline,
      StaticMeshAsset asset,
      GltfOperationProfile profile)
    {
      var nativePath = conflict.Data["nativePath"];
      if (!TryReadIndex(nativePath, "meshes", out var meshIndex))
      {
        throw new InvalidDataException("Deletion acceptance does not identify a mesh scope.");
      }
      var localId = int.Parse(
        conflict.Data["localId"],
        System.Globalization.CultureInfo.InvariantCulture);
      var source = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
        .Single(item => item.Id.Value == localId);
      var primitives = parsed.Meshes[meshIndex].Primitives;
      if (primitives.Count != source.StaticRenderObjectIds.Count)
      {
        throw new InvalidDataException(
          "Deletion acceptance requires one native partition per source render object.");
      }
      var partitions = primitives.Select((primitive, index) => new GeometryPartition(
        source.StaticRenderObjectIds[index].Value,
        primitive.Vertices,
        primitive.Triangles)).ToArray();
      var fingerprint = StaticGeometryFingerprint.CreateMesh(baseline, localId, partitions);
      var metadataPartitions = new JsonArray(partitions.Select(partition => new JsonObject
      {
        ["localId"] = partition.LocalId,
        ["sha256"] = StaticGeometryFingerprint.CreatePartition(
          baseline,
          partition.LocalId,
          partition.Vertices,
          partition.Triangles)
      }).ToArray());
      var metadata = new JsonObject
      {
        ["format"] = "earthtool.msh.gltf",
        ["version"] = 1,
        ["kind"] = "mesh",
        ["lineage"] = baseline.AssetLineageId.ToString("D"),
        ["document"] = baseline.DocumentId.ToString("D"),
        ["id"] = localId,
        ["guards"] = new JsonObject
        {
          ["nativeProjection"] = new JsonObject
          {
            ["projection"] = "static-geometry",
            ["version"] = 1,
            ["algorithm"] = "sha256",
            ["digest"] = EncodeSha256(fingerprint.Sha256)
          }
        },
        ["payload"] = new JsonObject
        {
          ["partitions"] = metadataPartitions
        }
      };
      var restored = RewriteScopeMetadata(parsed, nativePath, metadata.ToJsonString(), profile);
      restored.DiscardedMetadataScopes.Add(nativePath);
      return restored;
    }

    private static string EncodeSha256(string hexadecimal)
    {
      var bytes = Enumerable.Range(0, 32).Select(index => byte.Parse(
        hexadecimal.Substring(index * 2, 2),
        System.Globalization.NumberStyles.HexNumber,
        System.Globalization.CultureInfo.InvariantCulture)).ToArray();
      return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string? GetScopeMetadata(ParsedGlb parsed, string nativePath)
    {
      if (TryReadIndex(nativePath, "nodes", out var nodeIndex))
      {
        return parsed.Nodes[nodeIndex].Metadata;
      }
      if (TryReadIndex(nativePath, "meshes", out var meshIndex))
      {
        return parsed.Meshes[meshIndex].Metadata;
      }
      if (TryReadIndex(nativePath, "materials", out var materialIndex))
      {
        return parsed.Materials[materialIndex].Metadata;
      }
      const string lights = "extensions.KHR_lights_punctual.lights";
      return TryReadIndex(nativePath, lights, out var lightIndex)
        ? parsed.Lights[lightIndex].Metadata
        : null;
    }

    private static bool TryReadIndex(string path, string collection, out int index)
    {
      var prefix = collection + "[";
      if (path.StartsWith(prefix, StringComparison.Ordinal)
        && path.EndsWith("]", StringComparison.Ordinal)
        && int.TryParse(
          path.Substring(prefix.Length, path.Length - prefix.Length - 1),
          System.Globalization.NumberStyles.None,
          System.Globalization.CultureInfo.InvariantCulture,
          out index))
      {
        return true;
      }
      index = -1;
      return false;
    }

    private static ParsedGlb RewriteScopeMetadata(
      ParsedGlb parsed,
      string nativePath,
      string? metadata,
      GltfOperationProfile profile)
    {
      var nodes = parsed.Nodes.Select((node, index) => new ParsedGltfNode(
        node.Name,
        node.IsPlacementRoot,
        nativePath == $"nodes[{index}]" ? metadata : node.Metadata,
        node.MeshIndex,
        node.LightIndex,
        node.CameraIndex,
        node.Children,
        node.LocalTransform)).ToArray();
      var meshes = parsed.Meshes.Select((mesh, index) => new ParsedGltfMesh(
        nativePath == $"meshes[{index}]" ? metadata : mesh.Metadata,
        mesh.Primitives)).ToArray();
      var materials = parsed.Materials.Select((material, index) => new ParsedGltfMaterial(
        nativePath == $"materials[{index}]" ? metadata : material.Metadata,
        material.HasBaseColorTexture)).ToArray();
      var lights = parsed.Lights.Select((light, index) => new ParsedGltfLight(
        light.Name,
        nativePath == $"extensions.KHR_lights_punctual.lights[{index}]"
          ? metadata
          : light.Metadata,
        light.Type,
        light.Color,
        light.Intensity,
        light.Range,
        light.InnerConeAngle,
        light.OuterConeAngle)).ToArray();
      return CopyDiscardedScopes(parsed, new ParsedGlb(
        parsed.ManifestMetadata,
        parsed.HasReservedMetadata,
        meshes,
        nodes,
        materials,
        parsed.Animations,
        lights,
        parsed.RootNodeIndex,
        new MetadataConflictCollector(profile.MaxMetadataConflicts),
        parsed.IgnoredInertPaths), profile);
    }

    private static ParsedGlb CopyDiscardedScopes(
      ParsedGlb source,
      ParsedGlb destination,
      GltfOperationProfile profile)
    {
      foreach (var path in source.DiscardedMetadataScopes)
      {
        destination.DiscardedMetadataScopes.Add(path);
      }
      foreach (var path in source.AcceptedDeletionScopes)
      {
        destination.AcceptedDeletionScopes.Add(path);
      }
      GlbDocument.RevalidateParsedMetadataGraph(destination, profile);
      return destination;
    }

    private static OperationResult<GltfEditImportResult> WithAppliedResolution(
      OperationResult<GltfEditImportResult> result,
      GltfMetadataConflictResolution resolution)
    {
      if (result.Value is not GltfEditImportResult value)
      {
        return result;
      }
      return new OperationResult<GltfEditImportResult>(
        result.Status,
        new GltfEditImportResult(
          value.Asset,
          value.NextBaseline,
          value.AppliedFingerprint,
          value.Preservation,
          value.RestoredSerializedRepresentationPaths,
          value.PreservedUnknownMetadata,
          value.NextExportOptions.MetadataNextIds,
          value.NextExportOptions.ArtistObjectLocalIds,
          value.LineageDisposition,
          new[] { resolution }.Concat(value.AppliedConflictResolutions)),
        result.Diagnostics);
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredSceneLightDiagnostics(ParsedGlb parsed)
    {
      return parsed.Nodes.Select((node, index) => (node, index))
          .Where(item => item.node.LightIndex.HasValue
          && item.node.Metadata is null
          && !GlbDocument.TryParseStaticLightHelperName(item.node.Name, out _, out _))
        .Select(item => new OperationDiagnostic(
          GltfDiagnosticCodes.SceneLightIgnored,
          1118,
          DiagnosticSeverity.Warning,
          $"nodes[{item.index}]",
          "An untagged noncanonical punctual light remains scene-only artist lighting."))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredNewModelLightIntensityDiagnostics(
      ParsedGlb parsed,
      GltfNewModelImportOptions options)
    {
      return parsed.Nodes.Select(node => node.LightIndex)
        .OfType<int>()
        .Where(index => index >= 0 && index < parsed.Lights.Count)
        .Where(index => parsed.Nodes.Select((node, nodeIndex) => (node, nodeIndex)).Any(item =>
          item.node.LightIndex == index
          && GlbDocument.TryParseStaticLightHelperName(item.node.Name, out _, out _)))
        .Distinct()
        .Where(index => parsed.Lights[index].Intensity != 1)
        .Select(index => new OperationDiagnostic(
          GltfDiagnosticCodes.NewModelPhotometricIntensityIgnored,
          1120,
          DiagnosticSeverity.Warning,
          $"extensions.KHR_lights_punctual.lights[{index}].intensity",
          "New-model photometric intensity was not used as terrain-light amplitude."))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredNewModelAnimationDiagnostics(ParsedGlb parsed)
    {
      return parsed.Animations.Select((animation, index) => (animation, index))
        .Where(item => !TryGetCanonicalAnimationClass(item.animation.Name, out _))
        .Select(item => new OperationDiagnostic(
          GltfDiagnosticCodes.InertDataIgnored,
          1119,
          DiagnosticSeverity.Warning,
          $"animations[{item.index}]",
          "A noncanonical metadata-free animation remains scene-only and was ignored."))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredInertDataDiagnostics(
      ParsedGlb parsed)
    {
      return parsed.IgnoredInertPaths.Select(path => new OperationDiagnostic(
        GltfDiagnosticCodes.InertDataIgnored,
        1119,
        DiagnosticSeverity.Warning,
        path,
        "Inert native glTF data was excluded from canonical MSH state."))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredSceneNodeDiagnostics(ParsedGlb parsed)
    {
      return GetNodeOrder(parsed).Where(nodeIndex =>
        {
          var node = parsed.Nodes[nodeIndex];
          return node.Children.Count == 0
            && !node.IsPlacementRoot
            && node.Metadata is null
            && !node.MeshIndex.HasValue
            && !node.LightIndex.HasValue
            && !node.CameraIndex.HasValue
            && !GlbDocument.TryParseAttachmentHelperName(node.Name, out _)
            && !GlbDocument.TryParseCannonHelperName(node.Name, out _)
            && !GlbDocument.TryParseStaticLightHelperName(node.Name, out _, out _);
        })
        .Select(nodeIndex => new OperationDiagnostic(
          GltfDiagnosticCodes.InertDataIgnored,
          1119,
          DiagnosticSeverity.Warning,
          $"nodes[{nodeIndex}]",
          "An unknown metadata-free empty leaf node remains scene-only and was ignored."))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateDynamicPlacementDiagnostics(
      DynamicGltfImport imported)
    {
      return imported.PlacementDataIgnored
        ? new[]
        {
          new OperationDiagnostic(
            GltfDiagnosticCodes.InertDataIgnored,
            1119,
            DiagnosticSeverity.Warning,
            $"nodes[{imported.PlacementRootIndex}]",
            "Placement-root transforms and animation remain scene-only and were excluded from canonical MSH state.")
        }
        : Array.Empty<OperationDiagnostic>();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateNewModelTexBindingDiagnostics(
      ParsedGlb parsed,
      GltfNewModelImportOptions options)
    {
      return options.TextureResourceBindings
        .Where(binding => binding.Value is not null)
        .Select(binding => (binding, MaterialIndex: GetMaterialIndex(parsed, binding.Key)))
        .Where(item => item.MaterialIndex.HasValue
          && !parsed.Materials[item.MaterialIndex.Value].HasBaseColorTexture)
        .Select(item => new OperationDiagnostic(
          GltfDiagnosticCodes.TextureResourceMissing,
          1107,
          DiagnosticSeverity.Warning,
          $"materials[{item.MaterialIndex!.Value}]",
          "The explicit TEX resource binding has no decoded native preview and remains reference-only."))
        .ToArray();
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
        profile)).ToArray();
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
      InterchangeBaseline expected,
      MetadataConflictCollector conflicts)
    {
      if (manifest.ScopeKind != "manifest" || manifest.LocalId != 0 || manifest.SourceMsh is null)
      {
        throw new MalformedMetadataException("The scene metadata manifest is malformed.");
      }

      if (manifest.AssetLineageId != expected.AssetLineageId)
      {
        conflicts.Add(new MetadataConflictException(
          GltfDiagnosticCodes.AssetLineageMismatch,
          2006,
          "scenes[0]",
          "The GLB belongs to a different asset lineage.",
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.AdoptAsNew,
          GltfMetadataConflictActions.DiscardLineage));
      }
      if (manifest.DocumentId != expected.DocumentId)
      {
        conflicts.Add(new MetadataConflictException(
          GltfDiagnosticCodes.DocumentMismatch,
          2007,
          "scenes[0]",
          "The GLB belongs to a different interchange document.",
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.RetryWithMetadata,
          GltfMetadataConflictActions.AcceptBranch));
      }

    }

    private static AnimationEditPlan CreateAnimationEditPlan(
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

      var sourceByNode = retainedNodeBySource.ToDictionary(item => item.Value, item => item.Key);
      var matchedClasses = new Dictionary<int, int>();
      var matchedObjects = new HashSet<int>();
      var replacements = new List<AnimationReplacement>();
      var currentFrameCounts = new Dictionary<int, byte>();
      for (var clipIndex = 0; clipIndex < parsed.Animations.Count; clipIndex++)
      {
        var clip = parsed.Animations[clipIndex];
        if (!TryGetCanonicalAnimationClass(clip.Name, out var classIndex))
        {
          if (clip.Objects.Any(item => sourceByNode.TryGetValue(item.NodeIndex, out var sourceId)
            && expectedBySource[sourceId].IsNative))
          {
            throw new StaleNativeProjectionException(
              "A metadata-backed expected animation clip no longer has its canonical class name.");
          }
          continue;
        }
        if (clip.Objects.Count == 0)
        {
          throw new StaleNativeProjectionException("A native animation clip has no participating objects.");
        }
        if (!matchedClasses.TryAdd(classIndex, clipIndex))
        {
          throw new StaleNativeProjectionException("One animation class maps to multiple native clips.");
        }
        var frameCount = GetCanonicalAnimationFrameCount(clip);
        currentFrameCounts.Add(classIndex, checked((byte)frameCount));
        foreach (var item in clip.Objects)
        {
          if (!sourceByNode.TryGetValue(item.NodeIndex, out var sourceId)
            || !expectedBySource.TryGetValue(sourceId, out var expectedObject))
          {
            throw new StaleNativeProjectionException(
              "A canonical animation targets an unsupported object.");
          }
          if (!matchedObjects.Add(sourceId))
          {
            throw new StaleNativeProjectionException(
              "One source object participates in multiple canonical animation classes.");
          }
          var frames = item.SampleFrames(frameCount);
          var fingerprint = AnimationProjectionFingerprint.CreateObject(
            baseline,
            sourceId,
            classIndex,
            checked((byte)frameCount),
            frames);
          var classChanged = expectedObject.ClassIndex != classIndex;
          if (classChanged
            || expectedObject.DeclaredLength != frameCount
            || !string.Equals(fingerprint, expectedObject.Fingerprint, StringComparison.Ordinal))
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
              sourceId,
              classChanged ? checked((uint)classIndex) : expectedObject.AnimationClassValue,
              frames));
          }
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
          0,
          Array.Empty<ProjectedAnimationFrame>()));
      }

      if (parsed.Animations.Any(clip => !TryGetCanonicalAnimationClass(clip.Name, out _))
        && expected.Clips.Any(clip => !matchedClasses.ContainsKey(clip.ClassIndex)))
      {
        throw new StaleNativeProjectionException(
          "A metadata-backed expected animation clip no longer has its canonical class name.");
      }

      var lengths = ToAnimationBytes(asset.CommonBaseHeader.AnimationLengths);
      var frameIndices = ToAnimationBytes(asset.CommonBaseHeader.AnimationFrameIndices);
      var baselineClasses = expected.Clips.Select(clip => clip.ClassIndex).ToHashSet();
      for (var classIndex = 0; classIndex < 4; classIndex++)
      {
        if (currentFrameCounts.TryGetValue(classIndex, out var frameCount))
        {
          if (lengths[classIndex] != frameCount || !baselineClasses.Contains(classIndex))
          {
            lengths[classIndex] = frameCount;
            frameIndices[classIndex] = 0;
          }
        }
        else if (baselineClasses.Contains(classIndex))
        {
          lengths[classIndex] = 0;
          frameIndices[classIndex] = 0;
        }
      }
      return new AnimationEditPlan(
        replacements.AsReadOnly(),
        ToAnimationClassBytes(lengths),
        ToAnimationClassBytes(frameIndices));
    }

    private static int GetCanonicalAnimationFrameCount(ParsedGltfAnimation animation)
    {
      if (TryGetCanonicalAnimationFrameCount(animation, out var frameCount))
      {
        return frameCount;
      }
      throw new StaleNativeProjectionException(
        "A canonical animation must end on an integer 24 FPS frame before frame 255.");
    }

    private static bool TryGetCanonicalAnimationFrameCount(
      ParsedGltfAnimation animation,
      out int frameCount)
    {
      var endFrameValue = animation.EndTime * 24d;
      var endFrame = (int)Math.Round(endFrameValue);
      if (Math.Abs(endFrameValue - endFrame) > 1e-5 || endFrame is < 0 or >= byte.MaxValue)
      {
        frameCount = 0;
        return false;
      }
      frameCount = endFrame + 1;
      return true;
    }

    private static bool TryGetCanonicalAnimationClass(string? name, out int classIndex)
    {
      classIndex = name switch
      {
        "EarthTool A" => 0,
        "EarthTool B" => 1,
        "EarthTool C" => 2,
        "EarthTool D" => 3,
        _ => -1
      };
      return classIndex >= 0;
    }

    private static byte[] ToAnimationBytes(AnimationClassBytes value)
    {
      return new[] { value.A, value.B, value.C, value.D };
    }

    private static AnimationClassBytes ToAnimationClassBytes(IReadOnlyList<byte> values)
    {
      return new AnimationClassBytes(values[0], values[1], values[2], values[3]);
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

    private sealed class AnimationEditPlan
    {
      internal IReadOnlyList<AnimationReplacement> Replacements { get; }

      internal AnimationClassBytes Lengths { get; }

      internal AnimationClassBytes FrameIndices { get; }

      internal AnimationEditPlan(
        IReadOnlyList<AnimationReplacement> replacements,
        AnimationClassBytes lengths,
        AnimationClassBytes frameIndices)
      {
        Replacements = replacements;
        Lengths = lengths;
        FrameIndices = frameIndices;
      }
    }

    private static IReadOnlyList<PartitionMatch> MatchPartitions(
      IReadOnlyList<(ParsedGltfMesh Parsed, MetadataEnvelope Metadata, bool Discarded)> meshes,
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
        if (mesh.Discarded)
        {
          if (currentGeometry.Length != metadata.Partitions.Count)
          {
            throw new AmbiguousPartitionCorrespondenceException(
              "Discarding affected geometry requires one native partition per preserved identity.");
          }
          result.AddRange(currentGeometry.Select((partition, index) => new PartitionMatch(
            new GeometryPartition(
              metadata.Partitions[index].LocalId,
              partition.Vertices,
              partition.Triangles,
              partition.MaterialIndex),
            source.Id,
            retained: false,
            added: false)));
          continue;
        }
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
        ToCanonicalPosition(vertex.Position),
        new System.Numerics.Vector3(vertex.Normal.X, -vertex.Normal.Z, vertex.Normal.Y),
        vertex.TextureCoordinate);
    }

    private static System.Numerics.Vector3 ToCanonicalPosition(System.Numerics.Vector3 position)
    {
      return new System.Numerics.Vector3(position.X, -position.Z, position.Y);
    }

    private static IReadOnlyList<System.Numerics.Vector3> CreateEffectivePositions<TSource>(
      TSource root,
      Func<TSource, IEnumerable<System.Numerics.Vector3>> getPositions,
      Func<TSource, System.Numerics.Vector3> getPivot,
      Func<TSource, IEnumerable<TSource>> getChildren)
    {
      var positions = new List<System.Numerics.Vector3>();
      AddEffectivePositions(
        root,
        System.Numerics.Vector3.Zero,
        true,
        getPositions,
        getPivot,
        getChildren,
        positions);
      return positions;
    }

    private static void AddEffectivePositions<TSource>(
      TSource source,
      System.Numerics.Vector3 parentOffset,
      bool root,
      Func<TSource, IEnumerable<System.Numerics.Vector3>> getPositions,
      Func<TSource, System.Numerics.Vector3> getPivot,
      Func<TSource, IEnumerable<TSource>> getChildren,
      ICollection<System.Numerics.Vector3> positions)
    {
      var offset = root ? System.Numerics.Vector3.Zero : parentOffset + getPivot(source);
      foreach (var position in getPositions(source))
      {
        positions.Add(position + offset);
      }
      foreach (var child in getChildren(source))
      {
        AddEffectivePositions(child, offset, false, getPositions, getPivot, getChildren, positions);
      }
    }

    private static bool TryCreateHorizontalExtents(
      IReadOnlyCollection<System.Numerics.Vector3> positions,
      out CanonicalHorizontalExtents? horizontalExtents,
      out string? rangeFailure)
    {
      var positiveY = Math.Max(0, positions.Max(position => position.Y));
      var negativeY = -Math.Min(0, positions.Min(position => position.Y));
      var positiveX = Math.Max(0, positions.Max(position => position.X));
      var negativeX = -Math.Min(0, positions.Min(position => position.X));
      var values = new[]
      {
        (Axis: "+Y", Value: positiveY),
        (Axis: "-Y", Value: negativeY),
        (Axis: "+X", Value: positiveX),
        (Axis: "-X", Value: negativeX)
      };
      foreach (var value in values)
      {
        if (!float.IsFinite(value.Value) || value.Value * 256d > ushort.MaxValue)
        {
          horizontalExtents = null;
          rangeFailure = $"The derived {value.Axis} horizontal extent {value.Value:R} exceeds the representable maximum {ushort.MaxValue / 256f:R}.";
          return false;
        }
      }
      horizontalExtents = new CanonicalHorizontalExtents(
        positiveY,
        negativeY,
        positiveX,
        negativeX);
      rangeFailure = null;
      return true;
    }

    private static bool HaveSameEffectivePositions(
      IEnumerable<System.Numerics.Vector3> source,
      IEnumerable<System.Numerics.Vector3> current)
    {
      var sourceCounts = source.GroupBy(position => position)
        .ToDictionary(group => group.Key, group => group.Count());
      var currentCounts = current.GroupBy(position => position)
        .ToDictionary(group => group.Key, group => group.Count());
      return sourceCounts.Count == currentCounts.Count
        && sourceCounts.All(item => currentCounts.TryGetValue(item.Key, out var count)
          && count == item.Value);
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

    private static EditArtistObjectPlan ReconcileBaseHeaderArtistObjects(
      ParsedGlb parsed,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      IReadOnlyList<(ParsedGltfLight Parsed, MetadataEnvelope? Metadata)> lights,
      StaticMeshAsset asset,
      StaticHierarchyPlan hierarchy,
      InterchangeBaseline expected,
      IReadOnlyDictionary<string, int> metadataNextIds,
      StaticMeshEditSession edit)
    {
      var attachmentCandidates = new Dictionary<int, List<int>>();
      var cannonCandidates = new Dictionary<int, List<int>>();
      var sourcePhysicalNumbers = new Dictionary<int, int>();
      for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
      {
        var node = nodes[nodeIndex];
        int physicalNumber;
        if (node.Metadata?.AttachmentRecord is not null)
        {
          var sourcePhysicalNumber = ValidateAttachmentMetadata(node.Metadata, asset, expected);
          physicalNumber = ReadCanonicalAttachmentTarget(node.Parsed.Name, sourcePhysicalNumber, nodeIndex);
          sourcePhysicalNumbers.Add(nodeIndex, sourcePhysicalNumber);
          AddArtistCandidate(attachmentCandidates, physicalNumber, nodeIndex);
        }
        else if (node.Metadata?.CannonRenderPositionRecord is not null)
        {
          var sourcePhysicalNumber = ValidateCannonMetadata(node.Metadata, asset, expected);
          physicalNumber = ReadCanonicalCannonTarget(node.Parsed.Name, sourcePhysicalNumber, nodeIndex);
          sourcePhysicalNumbers.Add(nodeIndex, sourcePhysicalNumber);
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
          && GlbDocument.TryParseCannonHelperName(node.Parsed.Name, out physicalNumber))
        {
          AddArtistCandidate(cannonCandidates, physicalNumber, nodeIndex);
        }
      }

      var duplicate = attachmentCandidates.Concat(cannonCandidates)
        .FirstOrDefault(item => item.Value.Count != 1);
      if (duplicate.Value is not null)
      {
        var paths = string.Join(", ", duplicate.Value.Select(index => $"nodes[{index}]"));
        throw ArtistObjectConflict(
          $"Physical helper target {duplicate.Key} is occupied by multiple artist objects: {paths}.",
          $"nodes[{duplicate.Value[0]}]");
      }
      var nextObjectLocalId = metadataNextIds.TryGetValue("object", out var nextObject)
        ? nextObject
        : checked(nodes.Select(node => node.Metadata?.LocalId)
          .OfType<int>()
          .Concat(StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
            .Select(source => source.Id.Value))
          .DefaultIfEmpty(0)
          .Max() + 1);
      var attachmentArtistObjectLocalIds = new Dictionary<int, int>();
      var cannonArtistObjectLocalIds = new Dictionary<int, int>();
      foreach (var candidate in attachmentCandidates)
      {
        var node = nodes[candidate.Value[0]];
        var localId = node.Metadata?.LocalId ?? nextObjectLocalId;
        if (node.Metadata is null)
        {
          nextObjectLocalId = checked(nextObjectLocalId + 1);
        }
        attachmentArtistObjectLocalIds.Add(candidate.Key, localId);
      }
      foreach (var candidate in cannonCandidates)
      {
        var node = nodes[candidate.Value[0]];
        var localId = node.Metadata?.LocalId ?? nextObjectLocalId;
        if (node.Metadata is null)
        {
          nextObjectLocalId = checked(nextObjectLocalId + 1);
        }
        cannonArtistObjectLocalIds.Add(candidate.Key, localId);
      }
      var parentIndices = CreateParentIndices(nodes);
      var sourceParentedEmitters = new HashSet<int>();
      var markerOwnershipChanges = new Dictionary<int, SourceObjectId?>();
      var unchangedMarkerRecords = new Dictionary<int, UnchangedEmitterOwnership>();
      foreach (var candidate in attachmentCandidates.Where(item => item.Key is >= 5 and <= 8))
      {
        var nodeIndex = candidate.Value[0];
        var sourcePhysicalNumber = nodes[nodeIndex].Metadata is null
          ? candidate.Key
          : sourcePhysicalNumbers[nodeIndex];
        var markerSources = GlbDocument.GetMarkerAttachmentSourceObjects(
          asset,
          sourcePhysicalNumber - 4);
        var expectedParent = markerSources.Count == 1 ? markerSources[0] : asset.RootSourceObject;
        var ownerNodeIndex = parentIndices[nodeIndex];
        while (ownerNodeIndex >= 0 && !hierarchy.SourceByNode.ContainsKey(ownerNodeIndex))
        {
          ownerNodeIndex = parentIndices[ownerNodeIndex];
        }
        if (ownerNodeIndex < 0)
        {
          throw new UnsupportedGltfDomainException(
            "EmitterMarkerHierarchy",
            $"nodes[{nodeIndex}]");
        }
        var owner = hierarchy.SourceByNode[ownerNodeIndex];
        var ownershipChanged = nodes[nodeIndex].Metadata is null
          || sourcePhysicalNumber != candidate.Key
          || !owner.Id.Equals(expectedParent.Id);
        if (ownershipChanged)
        {
          markerOwnershipChanges.Add(candidate.Key - 4, owner.Id);
        }
        else
        {
          var flag = GlbDocument.GetMarkerAttachmentFlag(candidate.Key - 4);
          unchangedMarkerRecords.Add(
            candidate.Key - 4,
            new UnchangedEmitterOwnership(
              owner.Id,
              asset.StaticRenderObjectSequence.Where(record =>
                (record.KnownFlags & flag) != 0).Select(record => record.Id).ToArray()));
        }
        if (markerSources.Count == 1 || ownershipChanged)
        {
          sourceParentedEmitters.Add(nodeIndex);
        }
      }
      var transforms = CreateArtistObjectTransforms(parsed.RootNodeIndex, nodes)
        .ToDictionary(item => item.Key, item => item.Value);
      foreach (var nodeIndex in sourceParentedEmitters)
      {
        transforms[nodeIndex] = CreateEffectiveNodeTransform(
          nodeIndex,
          parsed.RootNodeIndex,
          parentIndices,
          nodes);
      }
      foreach (var candidate in attachmentCandidates.Concat(cannonCandidates))
      {
        var node = nodes[candidate.Value[0]].Parsed;
        if (node.MeshIndex.HasValue
          || node.Children.Count != 0
          || !transforms.ContainsKey(candidate.Value[0]))
        {
          throw new UnsupportedGltfDomainException(
            "AttachmentOrCannonArtistObject",
            $"nodes[{candidate.Value[0]}]");
        }
      }

      var attachmentTable = asset.CommonBaseHeader.AttachmentTable.ToArray();
      for (var number = 1; number <= 4; number++)
      {
        var physicalNumber = number + 4;
        var sourceActive = BinaryPrimitives.ReadInt16LittleEndian(
          attachmentTable.AsSpan((physicalNumber - 1) * 8, 8)) != short.MinValue;
        if (sourceActive && !attachmentCandidates.ContainsKey(physicalNumber))
        {
          markerOwnershipChanges[number] = null;
        }
      }
      for (var physicalNumber = 5; physicalNumber <= 49; physicalNumber++)
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
        var originalPhysicalNumber = metadata is null ? physicalNumber : sourcePhysicalNumbers[nodeIndex];
        if (metadata is not null
          && originalPhysicalNumber != physicalNumber
          && sourceActive)
        {
          throw ArtistObjectConflict(
            $"The attachment at nodes[{nodeIndex}] cannot target occupied CommonBaseHeader.AttachmentTable[{physicalNumber}].",
            $"nodes[{nodeIndex}].name");
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
        var sourceAttachment = attachmentTable.AsSpan((physicalNumber - 1) * 8, 8).ToArray();
        var sourceActive = BinaryPrimitives.ReadInt16LittleEndian(sourceAttachment) != short.MinValue;
        if (!cannonCandidates.TryGetValue(physicalNumber, out var candidates))
        {
          if (sourceActive)
          {
            edit.ReplaceAttachmentRecord(physicalNumber, CreateAbsentAttachmentRecord());
            edit.ReplaceCannonRenderPosition(physicalNumber, new byte[12]);
          }
          continue;
        }

        var nodeIndex = candidates[0];
        var transform = transforms[nodeIndex];
        var metadata = nodes[nodeIndex].Metadata;
        var originalPhysicalNumber = metadata is null ? physicalNumber : sourcePhysicalNumbers[nodeIndex];
        if (metadata is not null && originalPhysicalNumber != physicalNumber && sourceActive)
        {
          throw ArtistObjectConflict(
            $"The cannon at nodes[{nodeIndex}] cannot target occupied CommonBaseHeader.AttachmentTable[{physicalNumber}].",
            $"nodes[{nodeIndex}].name");
        }
        var originalAttachment = attachmentTable.AsSpan(
          (originalPhysicalNumber - 1) * 8,
          8).ToArray();
        if (originalPhysicalNumber != physicalNumber)
        {
          var originalRenderPosition = cannonRecords.AsSpan(
            (originalPhysicalNumber - 1) * 12,
            12).ToArray();
          var (retargetTranslation, retargetHeading) = ReadAttachmentTransform(transform);
          var retargetSourcePreview = new Vector3(
            GlbDocument.ReadFinitePreview(originalRenderPosition, 0),
            GlbDocument.ReadFinitePreview(originalRenderPosition, 8),
            GlbDocument.ReadFinitePreview(originalRenderPosition, 4));
          var retargetTranslationChanged = retargetTranslation != retargetSourcePreview;
          var replacementAttachment = originalAttachment.ToArray();
          if (retargetTranslationChanged)
          {
            CreateAttachmentRecord(transform, originalAttachment[7]).AsSpan(0, 6)
              .CopyTo(replacementAttachment);
          }
          replacementAttachment[6] = retargetHeading;
          edit.ReplaceAttachmentRecord(
            physicalNumber,
            replacementAttachment);
          edit.ReplaceCannonRenderPosition(
            physicalNumber,
            retargetTranslationChanged
              ? CreateCannonRenderPositionRecord(retargetTranslation)
              : originalRenderPosition);
          continue;
        }
        var sourceRecord = cannonRecords.AsSpan((physicalNumber - 1) * 12, 12).ToArray();
        var (translation, heading) = ReadAttachmentTransform(transform);
        var sourcePreview = new Vector3(
          GlbDocument.ReadFinitePreview(sourceRecord, 0),
          GlbDocument.ReadFinitePreview(sourceRecord, 8),
          GlbDocument.ReadFinitePreview(sourceRecord, 4));
        var translationChanged = !sourceActive || translation != sourcePreview;
        var rotationChanged = !sourceActive || heading != sourceAttachment[6];
        var generatedAttachment = translationChanged
          ? CreateAttachmentRecord(transform, sourceActive ? sourceAttachment[7] : (byte)0x80)
          : null;
        if (translationChanged)
        {
          edit.ReplaceCannonRenderPosition(
            physicalNumber,
            CreateCannonRenderPositionRecord(translation));
        }
        if (translationChanged || rotationChanged)
        {
          var replacement = sourceActive ? sourceAttachment.ToArray() : generatedAttachment!;
          if (translationChanged && sourceActive)
          {
            generatedAttachment!.AsSpan(0, 6).CopyTo(replacement);
          }
          if (rotationChanged && sourceActive)
          {
            replacement[6] = heading;
          }
          edit.ReplaceAttachmentRecord(physicalNumber, replacement);
        }
      }

      var replacementLightIndices = parsed.AcceptedDeletionScopes
        .Concat(parsed.DiscardedMetadataScopes)
        .Select(path => TryReadIndex(
          path,
          "extensions.KHR_lights_punctual.lights",
          out var index) ? index : -1)
        .Where(index => index >= 0)
        .ToHashSet();
      var staticLightArtistObjectLocalIds = ReconcileStaticLights(
        nodes,
        lights,
        transforms,
        asset,
        expected,
        replacementLightIndices,
        ref nextObjectLocalId,
        edit);
      return new EditArtistObjectPlan(
        markerOwnershipChanges,
        unchangedMarkerRecords,
        new GltfArtistObjectLocalIds(
          attachmentArtistObjectLocalIds,
          cannonArtistObjectLocalIds,
          staticLightArtistObjectLocalIds),
        nextObjectLocalId);
    }

    private static int ReadCanonicalAttachmentTarget(string? name, int sourcePhysicalNumber, int nodeIndex)
    {
      if (!GlbDocument.TryParseAttachmentHelperName(name, out var targetPhysicalNumber))
      {
        throw ArtistObjectConflict(
          $"The metadata-backed attachment at CommonBaseHeader.AttachmentTable[{sourcePhysicalNumber}] must retain a canonical attachment artist identifier at nodes[{nodeIndex}].name.",
          $"nodes[{nodeIndex}].name");
      }
      if (GlbDocument.GetAttachmentHelperFamilyStart(sourcePhysicalNumber)
        != GlbDocument.GetAttachmentHelperFamilyStart(targetPhysicalNumber))
      {
        throw ArtistObjectConflict(
          $"The attachment at CommonBaseHeader.AttachmentTable[{sourcePhysicalNumber}] cannot change canonical helper family at nodes[{nodeIndex}].name.",
          $"nodes[{nodeIndex}].name");
      }
      return targetPhysicalNumber;
    }

    private static int ReadCanonicalCannonTarget(string? name, int sourcePhysicalNumber, int nodeIndex)
    {
      if (!GlbDocument.TryParseCannonHelperName(name, out var targetPhysicalNumber))
      {
        throw ArtistObjectConflict(
          $"The metadata-backed cannon at CommonBaseHeader.AttachmentTable[{sourcePhysicalNumber}] must retain an ET_Turret_n canonical artist identifier at nodes[{nodeIndex}].name.",
          $"nodes[{nodeIndex}].name");
      }
      return targetPhysicalNumber;
    }

    private static void ApplyEmitterMarkerOwnershipChanges(
      StaticMeshAsset asset,
      StaticSourceObject hierarchyRoot,
      IReadOnlyList<PartitionMatch> partitions,
      EditArtistObjectPlan ownership,
      StaticMeshEditSession edit)
    {
      const StaticRenderObjectFlags markerMask = StaticRenderObjectFlagMasks.MarkerAttachments;
      var sourceRecords = asset.StaticRenderObjectSequence.ToDictionary(record => record.Id);
      var sources = StaticSourceObjectTraversal.Flatten(hierarchyRoot).ToArray();
      var partitionIds = partitions.Select(partition => new StaticRenderObjectId(
        asset.LineageId,
        partition.Partition.LocalId)).ToArray();
      var finalRecordIds = sources.SelectMany(source => source.StaticRenderObjectIds)
        .Where(id => !sourceRecords.ContainsKey(id) || partitionIds.Contains(id))
        .Concat(partitionIds)
        .Distinct()
        .ToArray();
      var changes = ownership.Changes.ToDictionary(item => item.Key, item => item.Value);
      foreach (var unchanged in ownership.UnchangedMarkerRecords)
      {
        if (unchanged.Value.MarkerRecordIds.Count != 0
          && !unchanged.Value.MarkerRecordIds.Any(finalRecordIds.Contains))
        {
          changes[unchanged.Key] = unchanged.Value.Owner;
        }
      }
      if (changes.Count == 0)
      {
        return;
      }
      var finalFlags = finalRecordIds
        .ToDictionary(
          id => id,
          id => sourceRecords.TryGetValue(id, out var record)
            ? record.KnownFlags & markerMask
            : StaticRenderObjectFlags.None);
      foreach (var change in changes)
      {
        var flag = GlbDocument.GetMarkerAttachmentFlag(change.Key);
        foreach (var id in finalFlags.Keys.ToArray())
        {
          finalFlags[id] &= ~flag;
        }
        if (change.Value is not null)
        {
          var source = sources.Single(item => item.Id.Equals(change.Value.Value));
          var first = source.StaticRenderObjectIds.FirstOrDefault(finalRecordIds.Contains);
          if (first.Equals(default(StaticRenderObjectId)))
          {
            var localId = partitions.First(partition => partition.SourceObjectId.Equals(source.Id))
              .Partition.LocalId;
            first = new StaticRenderObjectId(asset.LineageId, localId);
          }
          finalFlags[first] |= flag;
        }
      }
      foreach (var replacement in finalFlags)
      {
        var sourceFlags = sourceRecords.TryGetValue(replacement.Key, out var source)
          ? source.KnownFlags & markerMask
          : StaticRenderObjectFlags.None;
        if (replacement.Value != sourceFlags)
        {
          edit.ReplaceMarkerAttachmentFlags(replacement.Key, replacement.Value);
        }
      }
    }

    private sealed class EditArtistObjectPlan
    {
      internal IReadOnlyDictionary<int, SourceObjectId?> Changes { get; }
      internal IReadOnlyDictionary<int, UnchangedEmitterOwnership> UnchangedMarkerRecords { get; }
      internal GltfArtistObjectLocalIds ArtistObjectLocalIds { get; }
      internal int NextObjectLocalId { get; }

      internal EditArtistObjectPlan(
        IReadOnlyDictionary<int, SourceObjectId?> changes,
        IReadOnlyDictionary<int, UnchangedEmitterOwnership> unchangedMarkerRecords,
        GltfArtistObjectLocalIds artistObjectLocalIds,
        int nextObjectLocalId)
      {
        Changes = changes;
        UnchangedMarkerRecords = unchangedMarkerRecords;
        ArtistObjectLocalIds = artistObjectLocalIds;
        NextObjectLocalId = nextObjectLocalId;
      }
    }

    private sealed class UnchangedEmitterOwnership
    {
      internal SourceObjectId Owner { get; }
      internal IReadOnlyList<StaticRenderObjectId> MarkerRecordIds { get; }

      internal UnchangedEmitterOwnership(
        SourceObjectId owner,
        IReadOnlyList<StaticRenderObjectId> markerRecordIds)
      {
        Owner = owner;
        MarkerRecordIds = markerRecordIds;
      }
    }

    private static IReadOnlyDictionary<int, int> ReconcileStaticLights(
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      IReadOnlyList<(ParsedGltfLight Parsed, MetadataEnvelope? Metadata)> lights,
      IReadOnlyDictionary<int, Matrix4x4> transforms,
      StaticMeshAsset asset,
      InterchangeBaseline expected,
      ISet<int> replacementLightIndices,
      ref int nextObjectLocalId,
      StaticMeshEditSession edit)
    {
      var artistObjectLocalIds = new Dictionary<int, int>();
      var definitionReferenceCounts = new int[lights.Count];
      var taggedDefinitionReferenceCounts = new int[lights.Count];
      for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
      {
        var lightIndex = nodes[nodeIndex].Parsed.LightIndex;
        if (!lightIndex.HasValue)
        {
          continue;
        }
        if (lightIndex.Value < 0 || lightIndex.Value >= lights.Count)
        {
          throw new StaticLightMetadataException(
            $"nodes[{nodeIndex}].extensions.KHR_lights_punctual.light",
            "A static-light instance references a missing definition.");
        }
        definitionReferenceCounts[lightIndex.Value]++;
        if (nodes[nodeIndex].Metadata?.StaticLightAttachmentRecord is not null)
        {
          taggedDefinitionReferenceCounts[lightIndex.Value]++;
        }
      }
      for (var lightIndex = 0; lightIndex < lights.Count; lightIndex++)
      {
        if (lights[lightIndex].Metadata is not null
          && (definitionReferenceCounts[lightIndex] != 1
            || taggedDefinitionReferenceCounts[lightIndex] != 1))
        {
          throw ArtistObjectConflict(
            "A tagged static-light definition must have exactly one tagged instance.",
            $"extensions.KHR_lights_punctual.lights[{lightIndex}]");
        }
      }

      var candidates = new Dictionary<(string Type, int Number), int>();
      var reservedTargets = new HashSet<(string Type, int Number)>();
      for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
      {
        var node = nodes[nodeIndex];
        if (node.Metadata?.StaticLightAttachmentRecord is null)
        {
          if (node.Metadata is null
            && node.Parsed.LightIndex.HasValue
            && GlbDocument.TryParseStaticLightHelperName(
              node.Parsed.Name,
              out var namedType,
              out var namedNumber))
          {
            if (!candidates.TryAdd((namedType, namedNumber), nodeIndex))
            {
              throw ArtistObjectConflict(
                "A static-light physical target is occupied more than once.",
                $"nodes[{nodeIndex}]");
            }
            artistObjectLocalIds.Add(
              namedType == "spot" ? namedNumber : namedNumber + 4,
              nextObjectLocalId);
            nextObjectLocalId = checked(nextObjectLocalId + 1);
          }
          continue;
        }
        var type = node.Metadata.StaticLightType;
        var number = node.Metadata.StaticLightPhysicalNumber;
        var lightIndex = node.Parsed.LightIndex;
        if (type is not ("spot" or "point")
          || number is null or < 1 or > 4
          || lightIndex is null or < 0
          || lightIndex.Value >= lights.Count
          || node.Metadata.ScopeKind != "object"
          || node.Metadata.StaticLightDefinitionLocalId is null
          || node.Metadata.AssetLineageId != expected.AssetLineageId
          || node.Metadata.DocumentId != expected.DocumentId
          || node.Parsed.MeshIndex.HasValue
          || node.Parsed.Children.Count != 0
          || !transforms.ContainsKey(nodeIndex)
          || node.Metadata.AttachmentRecord is not null
          || node.Metadata.CannonRenderPositionRecord is not null
          || node.Metadata.StaticLightRecord is not null
          || node.Metadata.Fingerprint is not null
          || node.Metadata.Guards.Count != 0
          || !HasNoUnrelatedArtistObjectMetadata(node.Metadata))
        {
          throw new StaticLightMetadataException(
            $"nodes[{nodeIndex}].extras.earthtool",
            "The static-light instance metadata envelope is malformed.");
        }
        if (!GlbDocument.TryParseStaticLightHelperName(
              node.Parsed.Name,
              out var targetType,
              out var targetNumber))
        {
          throw ArtistObjectConflict(
            $"The metadata-backed static light at CommonBaseHeader.{(type == "spot" ? "StaticSpotLights" : "StaticOmniLights")}[{number.Value}] must retain a canonical static-light artist identifier.",
            $"nodes[{nodeIndex}].name");
        }
        if (targetType != type)
        {
          throw new UnsupportedGltfDomainException(
            "StaticLightTypeConversion",
            $"nodes[{nodeIndex}].name");
        }
        var key = (targetType, targetNumber);
        if (!candidates.TryAdd(key, nodeIndex))
        {
          throw ArtistObjectConflict(
            "A static-light physical target is occupied more than once.",
            $"nodes[{nodeIndex}]");
        }
        artistObjectLocalIds.Add(
          targetType == "spot" ? targetNumber : targetNumber + 4,
          node.Metadata.LocalId);

        var definition = lights[lightIndex.Value];
        var metadata = definition.Metadata
          ?? throw new StaticLightMetadataException(
            $"extensions.KHR_lights_punctual.lights[{lightIndex.Value}]",
            "A tagged static-light definition lost its metadata.");
        if (!GlbDocument.TryParseStaticLightHelperName(
              definition.Parsed.Name,
              out var definitionType,
              out var definitionNumber)
          || definitionType != targetType
          || definitionNumber != targetNumber)
        {
          throw new StaticLightMetadataException(
            $"extensions.KHR_lights_punctual.lights[{lightIndex.Value}].name",
            "The canonical static-light instance and definition names contradict each other.");
        }
        var localId = type == "spot" ? number.Value : number.Value + 4;
        var record = type == "spot"
          ? asset.CommonBaseHeader.StaticSpotLights.Skip((number.Value - 1) * 0x30).Take(0x30).ToArray()
          : asset.CommonBaseHeader.StaticOmniLights.Skip((number.Value - 1) * 0x1C).Take(0x1C).ToArray();
        var attachmentNumber = type == "spot" ? number.Value + 12 : number.Value + 16;
        var attachment = asset.CommonBaseHeader.AttachmentTable
          .Skip((attachmentNumber - 1) * 8).Take(8).ToArray();
        var expectedGuards = GlbDocument.CreateStaticLightGuards(
          expected,
          type,
          number.Value,
          localId,
          record,
          attachment);
        if (metadata.ScopeKind != "light"
          || metadata.LocalId != localId
          || metadata.AssetLineageId != expected.AssetLineageId
          || metadata.DocumentId != expected.DocumentId
          || metadata.StaticLightType != type
          || metadata.StaticLightPhysicalNumber != number
          || metadata.StaticLightRecord?.Count != record.Length
          || !metadata.StaticLightRecord.SequenceEqual(record)
          || !node.Metadata.StaticLightAttachmentRecord.SequenceEqual(attachment)
          || metadata.Guards.Count != expectedGuards.Count
          || expectedGuards.Any(guard => !metadata.Guards.TryGetValue(guard.Key, out var value)
            || !string.Equals(value, guard.Value, StringComparison.Ordinal))
          || metadata.AttachmentRecord is not null
          || metadata.CannonRenderPositionRecord is not null
          || metadata.StaticLightAttachmentRecord is not null
          || metadata.Fingerprint is not null
          || metadata.FingerprintName is not null
          || metadata.FingerprintVersion is not null
          || !HasNoUnrelatedArtistObjectMetadata(metadata))
        {
          throw new StaticLightMetadataException(
            $"extensions.KHR_lights_punctual.lights[{lightIndex.Value}].extras.earthtool",
            "The static-light metadata does not match its source records.");
        }


        var actualGuards = CreateCurrentStaticLightGuards(
          expected,
          localId,
          definition.Parsed,
          transforms[nodeIndex],
          $"nodes[{nodeIndex}]");
        var changed = expectedGuards.Keys.Where(key =>
          !string.Equals(metadata.Guards[key], actualGuards[key], StringComparison.Ordinal)).ToHashSet();
        var retargeted = targetNumber != number.Value;
        if (changed.Count == 0 && !retargeted)
        {
          continue;
        }

        if (changed.Contains("staticLight.type"))
        {
          throw new UnsupportedGltfDomainException(
            "StaticLightTypeConversion",
            $"extensions.KHR_lights_punctual.lights[{lightIndex.Value}].type");
        }
        var replacement = record.ToArray();
        var attachmentReplacement = attachment.ToArray();
        var changedFields = new List<string>();
        if (changed.Contains("staticLight.pose"))
        {
          var translation = transforms[nodeIndex].Translation;
          if (!IsFinite(translation))
          {
            throw new UnsupportedGltfDomainException(
              "StaticLightPose",
              $"nodes[{nodeIndex}].translation");
          }
          WriteSingle(replacement, 0, translation.X);
          WriteSingle(replacement, 4, translation.Z);
          WriteSingle(replacement, 8, translation.Y);
          BinaryPrimitives.WriteInt16LittleEndian(
            attachmentReplacement,
            QuantizeAttachmentCoordinate(
              translation.X,
              true,
              "StaticLightPose",
              $"nodes[{nodeIndex}].translation"));
          BinaryPrimitives.WriteInt16LittleEndian(
            attachmentReplacement.AsSpan(2),
            QuantizeAttachmentCoordinate(
              translation.Z,
              false,
              "StaticLightPose",
              $"nodes[{nodeIndex}].translation"));
          BinaryPrimitives.WriteInt16LittleEndian(
            attachmentReplacement.AsSpan(4),
            QuantizeAttachmentCoordinate(
              translation.Y,
              false,
              "StaticLightPose",
              $"nodes[{nodeIndex}].translation"));
          changedFields.Add("Position");
        }
        if (changed.Contains("staticLight.color"))
        {
          if (!IsFinite(definition.Parsed.Color)
            || definition.Parsed.Color.X < 0
            || definition.Parsed.Color.Y < 0
            || definition.Parsed.Color.Z < 0)
          {
            throw new UnsupportedGltfDomainException(
              "StaticLightColor",
              $"extensions.KHR_lights_punctual.lights[{lightIndex.Value}].color");
          }
          WriteSingle(replacement, 0x0C, definition.Parsed.Color.X);
          WriteSingle(replacement, 0x10, definition.Parsed.Color.Y);
          WriteSingle(replacement, 0x14, definition.Parsed.Color.Z);
          changedFields.Add("Color");
        }
        if (changed.Contains("staticLight.intensity"))
        {
          if (!float.IsFinite(definition.Parsed.Intensity) || definition.Parsed.Intensity < 0)
          {
            throw new UnsupportedGltfDomainException(
              "StaticLightIntensity",
              $"extensions.KHR_lights_punctual.lights[{lightIndex.Value}].intensity");
          }
          WriteSingle(replacement, type == "spot" ? 0x2C : 0x18, definition.Parsed.Intensity);
          changedFields.Add("TerrainLightAmplitude");
        }
        if (changed.Contains("staticLight.direction"))
        {
          if (type != "spot")
          {
            throw new UnsupportedGltfDomainException(
              "StaticLightDirection",
              $"nodes[{nodeIndex}].rotation");
          }
          WriteStaticLightDirection(
            replacement,
            transforms[nodeIndex],
            $"nodes[{nodeIndex}].rotation");
          changedFields.Add("Direction");
        }
        if (changed.Contains("staticLight.cones"))
        {
          if (type != "spot"
            || !float.IsFinite(definition.Parsed.InnerConeAngle)
            || !float.IsFinite(definition.Parsed.OuterConeAngle)
            || definition.Parsed.InnerConeAngle < 0
            || definition.Parsed.OuterConeAngle < definition.Parsed.InnerConeAngle
            || definition.Parsed.OuterConeAngle > MathF.PI / 2)
          {
            throw new UnsupportedGltfDomainException(
              "StaticLightCones",
              $"extensions.KHR_lights_punctual.lights[{lightIndex.Value}].spot");
          }
          var targetDistance = ReadSingle(replacement, 0x18);
          if (!float.IsFinite(targetDistance))
          {
            throw new UnsupportedGltfDomainException(
              "StaticLightCones",
              $"extensions.KHR_lights_punctual.lights[{lightIndex.Value}].spot");
          }
          WriteSingle(replacement, 0x20, MathF.Tan(definition.Parsed.InnerConeAngle));
          WriteSingle(replacement, 0x24, definition.Parsed.OuterConeAngle * targetDistance);
          changedFields.Add("Cones");
        }
        if (retargeted)
        {
          var targetAttachmentNumber = type == "spot" ? targetNumber + 12 : targetNumber + 16;
          var targetAttachment = asset.CommonBaseHeader.AttachmentTable
            .Skip((targetAttachmentNumber - 1) * 8).Take(8).ToArray();
          if (BinaryPrimitives.ReadInt16LittleEndian(targetAttachment) != short.MinValue)
          {
            throw ArtistObjectConflict(
              $"The static light at nodes[{nodeIndex}] cannot target occupied CommonBaseHeader.AttachmentTable[{targetAttachmentNumber}].",
              $"nodes[{nodeIndex}].name");
          }
          if (!reservedTargets.Add((type, targetNumber)))
          {
            throw ArtistObjectConflict(
              "More than one static-light edit targets the same physical record.",
              $"nodes[{nodeIndex}].name");
          }
          edit.ReplaceAttachmentRecord(attachmentNumber, CreateAbsentAttachmentRecord());
          edit.ReplaceStaticLightRecord(
            ToStaticLightRecordKind(type),
            number.Value,
            new byte[type == "spot" ? 0x30 : 0x1C],
            new[] { "RetargetSourceCleared" });
          edit.ReplaceAttachmentRecord(targetAttachmentNumber, attachmentReplacement);
          changedFields.Add("Retarget");
          edit.ReplaceStaticLightRecord(
            ToStaticLightRecordKind(type),
            targetNumber,
            replacement,
            changedFields);
        }
        else
        {
          if (changed.Contains("staticLight.pose"))
          {
            edit.ReplaceAttachmentRecord(attachmentNumber, attachmentReplacement);
          }
          edit.ReplaceStaticLightRecord(
            ToStaticLightRecordKind(type),
            number.Value,
            replacement,
            changedFields);
        }
      }


      foreach (var candidate in candidates.Where(item => nodes[item.Value].Metadata is null).ToArray())
      {
        var node = nodes[candidate.Value].Parsed;
        var lightIndex = node.LightIndex!.Value;
        if (lightIndex >= 0
          && lightIndex < definitionReferenceCounts.Length
          && definitionReferenceCounts[lightIndex] != 1)
        {
          throw ArtistObjectConflict(
            "A static-light artist object must own an unshared punctual-light definition.",
            $"extensions.KHR_lights_punctual.lights[{lightIndex}]");
        }
        if (lightIndex < 0 || lightIndex >= lights.Count
          || lights[lightIndex].Metadata is not null
          || lights[lightIndex].Parsed.Type != candidate.Key.Type
          || node.Children.Count != 0
          || !transforms.TryGetValue(candidate.Value, out var transform))
        {
          throw new StaticLightMetadataException(
            $"nodes[{candidate.Value}]",
            "The canonically named static-light addition is malformed.");
        }
        if (!GlbDocument.TryParseStaticLightHelperName(
              lights[lightIndex].Parsed.Name,
              out var definitionType,
              out var definitionNumber)
          || definitionType != candidate.Key.Type
          || definitionNumber != candidate.Key.Number)
        {
          throw new StaticLightMetadataException(
            $"extensions.KHR_lights_punctual.lights[{lightIndex}].name",
            "The canonical static-light instance and definition names contradict each other.");
        }
        var attachmentNumber = candidate.Key.Type == "spot"
          ? candidate.Key.Number + 12
          : candidate.Key.Number + 16;
        var sourceAttachment = asset.CommonBaseHeader.AttachmentTable
          .Skip((attachmentNumber - 1) * 8).Take(8).ToArray();
        var replacing = replacementLightIndices.Contains(lightIndex);
        if (BinaryPrimitives.ReadInt16LittleEndian(sourceAttachment) != short.MinValue && !replacing)
        {
          throw ArtistObjectConflict(
            "A canonically named static light targets an occupied physical record.",
            $"CommonBaseHeader.AttachmentTable[{attachmentNumber}]");
        }
        if (!reservedTargets.Add(candidate.Key))
        {
          throw ArtistObjectConflict(
            "More than one static-light edit targets the same physical record.",
            $"CommonBaseHeader.{(candidate.Key.Type == "spot" ? "StaticSpotLights" : "StaticOmniLights")}[{candidate.Key.Number}]");
        }
        edit.ReplaceAttachmentRecord(
          attachmentNumber,
          CreateStaticLightAttachmentRecord(
            transform.Translation,
            $"nodes[{candidate.Value}].translation"));
        var sourceRecord = candidate.Key.Type == "spot"
          ? asset.CommonBaseHeader.StaticSpotLights
            .Skip((candidate.Key.Number - 1) * 0x30).Take(0x30).ToArray()
          : asset.CommonBaseHeader.StaticOmniLights
            .Skip((candidate.Key.Number - 1) * 0x1C).Take(0x1C).ToArray();
        var localId = candidate.Key.Type == "spot"
          ? candidate.Key.Number
          : candidate.Key.Number + 4;
        var inactiveGuards = GlbDocument.CreateStaticLightGuards(
          expected,
          candidate.Key.Type,
          candidate.Key.Number,
          localId,
          sourceRecord,
          sourceAttachment);
        var currentGuards = CreateCurrentStaticLightGuards(
          expected,
          localId,
          lights[lightIndex].Parsed,
          transform,
          $"nodes[{candidate.Value}]");
        if (replacing || inactiveGuards.Any(guard => !string.Equals(
          guard.Value,
          currentGuards[guard.Key],
          StringComparison.Ordinal)))
        {
          edit.ReplaceStaticLightRecord(
            ToStaticLightRecordKind(candidate.Key.Type),
            candidate.Key.Number,
            CreateConvertedStaticLightRecord(
              lights[lightIndex].Parsed,
              transform,
              $"extensions.KHR_lights_punctual.lights[{lightIndex}]"),
            new[] { replacing ? "Regeneration" : "Addition" });
        }
      }

      for (var number = 1; number <= 4; number++)
      {
        foreach (var type in new[] { "spot", "point" })
        {
          var attachmentNumber = type == "spot" ? number + 12 : number + 16;
          var attachment = asset.CommonBaseHeader.AttachmentTable
            .Skip((attachmentNumber - 1) * 8).Take(8).ToArray();
          var active = BinaryPrimitives.ReadInt16LittleEndian(attachment) != short.MinValue;
          if (active && !candidates.ContainsKey((type, number)))
          {
            edit.ReplaceAttachmentRecord(attachmentNumber, CreateAbsentAttachmentRecord());
            edit.ReplaceStaticLightRecord(
              ToStaticLightRecordKind(type),
              number,
              new byte[type == "spot" ? 0x30 : 0x1C],
              new[] { "Deletion" });
          }
        }
      }
      return artistObjectLocalIds;
    }

    private static IReadOnlyDictionary<string, string> CreateCurrentStaticLightGuards(
      InterchangeBaseline baseline,
      int localId,
      ParsedGltfLight light,
      Matrix4x4 transform,
      string path)
    {
      if (!Matrix4x4.Decompose(transform, out _, out var rotation, out var translation)
        || !IsFinite(rotation)
        || !IsFinite(translation))
      {
        throw new UnsupportedGltfDomainException("StaticLightPose", path);
      }
      var direction = Vector3.Transform(-Vector3.UnitZ, rotation);
      if (light.Type == "point")
      {
        direction = -Vector3.UnitZ;
      }
      return new Dictionary<string, string>(StringComparer.Ordinal)
      {
        ["staticLight.pose"] = StaticLightFingerprint(
          baseline,
          localId,
          "staticLight.pose",
          writer => WriteFingerprintVector(writer, translation)),
        ["staticLight.type"] = StaticLightFingerprint(
          baseline,
          localId,
          "staticLight.type",
          writer => WriteFingerprintString(writer, light.Type)),
        ["staticLight.color"] = StaticLightFingerprint(
          baseline,
          localId,
          "staticLight.color",
          writer => WriteFingerprintVector(writer, light.Color)),
        ["staticLight.intensity"] = StaticLightFingerprint(
          baseline,
          localId,
          "staticLight.intensity",
          writer => WriteFingerprintFloat(writer, light.Intensity)),
        ["staticLight.direction"] = StaticLightFingerprint(
          baseline,
          localId,
          "staticLight.direction",
          writer => WriteFingerprintDirection(writer, direction)),
        ["staticLight.cones"] = StaticLightFingerprint(
          baseline,
          localId,
          "staticLight.cones",
          writer =>
          {
            WriteFingerprintFloat(writer, light.InnerConeAngle);
            WriteFingerprintFloat(writer, light.OuterConeAngle);
          })
      };
    }

    private static StaticLightRecordKind ToStaticLightRecordKind(string type)
    {
      return type == "spot" ? StaticLightRecordKind.Spot : StaticLightRecordKind.Omni;
    }

    private static string StaticLightFingerprint(
      InterchangeBaseline baseline,
      int localId,
      string projection,
      Action<BinaryWriter> writeProjection)
    {
      return GlbDocument.CreateStaticLightFingerprint(
        baseline,
        localId,
        projection,
        writeProjection);
    }

    private static void WriteFingerprintVector(BinaryWriter writer, Vector3 value)
    {
      WriteFingerprintFloat(writer, value.X);
      WriteFingerprintFloat(writer, value.Y);
      WriteFingerprintFloat(writer, value.Z);
    }

    private static void WriteFingerprintDirection(BinaryWriter writer, Vector3 value)
    {
      WriteFingerprintFloat(writer, value.X);
      WriteFingerprintFloat(writer, value.Y);
      WriteFingerprintFloat(writer, value.Z);
    }

    private static void WriteFingerprintFloat(BinaryWriter writer, float value)
    {
      var canonical = MathF.Round(value, 5);
      writer.Write(canonical == 0 ? 0 : canonical);
    }

    private static void WriteFingerprintString(BinaryWriter writer, string value)
    {
      var bytes = System.Text.Encoding.UTF8.GetBytes(value);
      writer.Write(bytes.Length);
      writer.Write(bytes);
    }

    private static float ReadSingle(byte[] source, int offset)
    {
      return BitConverter.Int32BitsToSingle(
        BinaryPrimitives.ReadInt32LittleEndian(source.AsSpan(offset)));
    }

    private static void WriteStaticLightDirection(
      byte[] record,
      Matrix4x4 transform,
      string path)
    {
      if (!Matrix4x4.Decompose(transform, out _, out var rotation, out _)
        || !IsFinite(rotation))
      {
        throw new UnsupportedGltfDomainException("StaticLightDirection", path);
      }
      var direction = Vector3.Transform(-Vector3.UnitZ, rotation);
      if (!IsFinite(direction) || direction.LengthSquared() == 0)
      {
        throw new UnsupportedGltfDomainException("StaticLightDirection", path);
      }
      direction = Vector3.Normalize(direction);
      var horizontalLength = MathF.Sqrt((direction.X * direction.X) + (direction.Z * direction.Z));
      if (!float.IsFinite(horizontalLength) || horizontalLength < 1e-5f)
      {
        throw new UnsupportedGltfDomainException("StaticLightDirection", path);
      }
      var heading = MathF.Atan2(-direction.Z, direction.X);
      if (heading < 0)
      {
        heading += MathF.PI * 2;
      }
      record[0x1C] = unchecked((byte)((int)MathF.Floor(
        (heading * 256 / (MathF.PI * 2)) + 0.5f) & 0xFF));
      WriteSingle(record, 0x28, direction.Y / horizontalLength);
    }

    private static byte[] CreateStaticLightAttachmentRecord(Vector3 translation, string path)
    {
      if (!IsFinite(translation))
      {
        throw new UnsupportedGltfDomainException("StaticLightPose", path);
      }
      var result = new byte[8];
      BinaryPrimitives.WriteInt16LittleEndian(
        result,
        QuantizeAttachmentCoordinate(translation.X, true, "StaticLightPose", path));
      BinaryPrimitives.WriteInt16LittleEndian(
        result.AsSpan(2),
        QuantizeAttachmentCoordinate(translation.Z, false, "StaticLightPose", path));
      BinaryPrimitives.WriteInt16LittleEndian(
        result.AsSpan(4),
        QuantizeAttachmentCoordinate(translation.Y, false, "StaticLightPose", path));
      return result;
    }

    private static byte[] CreateConvertedStaticLightRecord(
      ParsedGltfLight light,
      Matrix4x4 transform,
      string path,
      GltfNewModelStaticLightOptions? options = null,
      StaticLightAuthoringIntent intent = StaticLightAuthoringIntent.EditProjection)
    {
      if (!IsFinite(light.Color)
        || light.Color.X < 0
        || light.Color.Y < 0
        || light.Color.Z < 0
        || !float.IsFinite(light.Intensity)
        || light.Intensity < 0
        || !IsFinite(transform.Translation))
      {
        throw new UnsupportedGltfDomainException("StaticLightTypeConversion", path);
      }
      var result = new byte[light.Type == "spot" ? 0x30 : 0x1C];
      WriteSingle(result, 0, transform.Translation.X);
      WriteSingle(result, 4, transform.Translation.Z);
      WriteSingle(result, 8, transform.Translation.Y);
      WriteSingle(result, 0x0C, light.Color.X);
      WriteSingle(result, 0x10, light.Color.Y);
      WriteSingle(result, 0x14, light.Color.Z);
      if (light.Type == "point")
      {
        WriteSingle(result, 0x18, intent == StaticLightAuthoringIntent.NewModel
          ? options?.TerrainLightAmplitude ?? 1
          : light.Intensity);
        return result;
      }
      if (light.InnerConeAngle < 0
        || light.OuterConeAngle < light.InnerConeAngle
        || light.OuterConeAngle > MathF.PI / 2)
      {
        throw new UnsupportedGltfDomainException("StaticLightTypeConversion", path);
      }
      var distance = light.Range is > 0 && float.IsFinite(light.Range.Value)
        ? light.Range.Value
        : options?.TargetDistance ?? 1;
      WriteSingle(result, 0x18, distance);
      WriteStaticLightDirection(result, transform, path + ".rotation");
      WriteSingle(result, 0x20, MathF.Tan(light.InnerConeAngle));
      WriteSingle(result, 0x24, light.OuterConeAngle * distance);
      WriteSingle(result, 0x2C, intent == StaticLightAuthoringIntent.NewModel
        ? options?.TerrainLightAmplitude ?? 1
        : light.Intensity);
      return result;
    }

    private enum StaticLightAuthoringIntent
    {
      EditProjection,
      NewModel
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
        || physicalNumber is null or < 5 or > 49
        || physicalNumber is >= 13 and <= 20
        || metadata.AttachmentRecord?.Count != 8
        || metadata.FingerprintName != "attachment.pose"
        || metadata.FingerprintVersion != 1
        || metadata.StaticLightType is not null
        || metadata.StaticLightPhysicalNumber is not null
        || metadata.StaticLightDefinitionLocalId is not null
        || metadata.StaticLightRecord is not null
        || metadata.StaticLightAttachmentRecord is not null
        || metadata.CannonPhysicalNumber is not null
        || metadata.CannonAttachmentRecord is not null
        || metadata.CannonRenderPositionRecord is not null
        || metadata.Guards.Count != 0
        || !HasNoUnrelatedArtistObjectMetadata(metadata))
      {
        throw new MalformedMetadataException("The attachment metadata envelope is malformed.");
      }
      var sourceRecord = asset.CommonBaseHeader.AttachmentTable
        .Skip((physicalNumber.Value - 1) * 8).Take(8);
      if (!sourceRecord.SequenceEqual(metadata.AttachmentRecord)
        || metadata.Fingerprint != GlbDocument.CreateAttachmentPoseFingerprint(
          expected,
          metadata.LocalId,
          physicalNumber.Value,
          sourceRecord.ToArray())
        || BinaryPrimitives.ReadInt16LittleEndian(metadata.AttachmentRecord.ToArray()) == short.MinValue)
      {
        throw new MalformedMetadataException("The attachment metadata does not match its source record.");
      }
      return physicalNumber.Value;
    }

    private static int ValidateCannonMetadata(
      MetadataEnvelope metadata,
      StaticMeshAsset asset,
      InterchangeBaseline expected)
    {
      var physicalNumber = metadata.CannonPhysicalNumber;
      if (metadata.AssetLineageId != expected.AssetLineageId
        || metadata.DocumentId != expected.DocumentId
        || metadata.ScopeKind != "object"
        || physicalNumber is null or < 1 or > 4
        || metadata.CannonAttachmentRecord?.Count != 8
        || metadata.CannonRenderPositionRecord?.Count != 12
        || metadata.AttachmentPhysicalNumber is not null
        || metadata.AttachmentRecord is not null
        || metadata.Fingerprint is not null
        || metadata.FingerprintName is not null
        || metadata.FingerprintVersion is not null
        || metadata.StaticLightType is not null
        || metadata.StaticLightPhysicalNumber is not null
        || metadata.StaticLightDefinitionLocalId is not null
        || metadata.StaticLightRecord is not null
        || metadata.StaticLightAttachmentRecord is not null
        || metadata.Guards.Count != 2
        || !HasNoUnrelatedArtistObjectMetadata(metadata))
      {
        throw new MalformedMetadataException("The cannon metadata envelope is malformed.");
      }
      var sourceAttachment = asset.CommonBaseHeader.AttachmentTable
        .Skip((physicalNumber.Value - 1) * 8).Take(8).ToArray();
      var sourceRenderPosition = asset.CommonBaseHeader.CannonRenderPositions
        .Skip((physicalNumber.Value - 1) * 12).Take(12).ToArray();
      var expectedGuards = GlbDocument.CreateCannonGuards(
        expected,
        metadata.LocalId,
        physicalNumber.Value,
        sourceAttachment,
        sourceRenderPosition);
      if (!sourceAttachment.SequenceEqual(metadata.CannonAttachmentRecord)
        || !sourceRenderPosition.SequenceEqual(metadata.CannonRenderPositionRecord)
        || BinaryPrimitives.ReadInt16LittleEndian(sourceAttachment) == short.MinValue
        || expectedGuards.Any(guard => !metadata.Guards.TryGetValue(guard.Key, out var digest)
          || !string.Equals(digest, guard.Value, StringComparison.Ordinal)))
      {
        throw new MalformedMetadataException("The cannon metadata does not match its source records.");
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

    private static MetadataIdentityException ArtistObjectConflict(string message, string? path = null)
    {
      return new MetadataIdentityException(
        GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
        2012,
        message,
        path);
    }

    private static IReadOnlyDictionary<int, Matrix4x4> CreateArtistObjectTransforms(
      int rootNodeIndex,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      ISet<int>? explicitArtistObjects = null)
    {
      var result = new Dictionary<int, Matrix4x4>();
      AddArtistObjectTransforms(
        rootNodeIndex,
        Matrix4x4.Identity,
        nodes,
        result,
        explicitArtistObjects ?? new HashSet<int>());
      return result;
    }

    private static void AddArtistObjectTransforms(
      int nodeIndex,
      Matrix4x4 inheritedTransform,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      IDictionary<int, Matrix4x4> result,
      ISet<int> explicitArtistObjects)
    {
      var node = nodes[nodeIndex];
      var effective = node.Parsed.LocalTransform * inheritedTransform;
      var isArtistObject = IsArtistObject(nodeIndex, node, explicitArtistObjects);
      if (isArtistObject)
      {
        result.Add(nodeIndex, effective);
        return;
      }
      var childInherited = node.Parsed.MeshIndex.HasValue ? Matrix4x4.Identity : effective;
      foreach (var child in node.Parsed.Children)
      {
        AddArtistObjectTransforms(child, childInherited, nodes, result, explicitArtistObjects);
      }
    }

    private static int[] CreateParentIndices(
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes)
    {
      var result = Enumerable.Repeat(-1, nodes.Count).ToArray();
      for (var parentIndex = 0; parentIndex < nodes.Count; parentIndex++)
      {
        foreach (var childIndex in nodes[parentIndex].Parsed.Children)
        {
          result[childIndex] = parentIndex;
        }
      }
      return result;
    }

    private static Matrix4x4 CreateEffectiveNodeTransform(
      int nodeIndex,
      int rootNodeIndex,
      IReadOnlyList<int> parentIndices,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes)
    {
      var result = Matrix4x4.Identity;
      var current = nodeIndex;
      while (current >= 0)
      {
        result *= nodes[current].Parsed.LocalTransform;
        if (current == rootNodeIndex)
        {
          return result;
        }
        current = parentIndices[current];
      }
      throw new UnsupportedGltfDomainException("EmitterMarkerHierarchy");
    }

    private static bool IsArtistObject(
      int nodeIndex,
      (ParsedGltfNode Parsed, MetadataEnvelope? Metadata) node,
      ISet<int> explicitArtistObjects)
    {
      return explicitArtistObjects.Contains(nodeIndex)
        || node.Metadata?.AttachmentRecord is not null
        || node.Metadata?.CannonRenderPositionRecord is not null
        || node.Metadata?.StaticLightAttachmentRecord is not null
        || node.Metadata is null
          && (GlbDocument.TryParseAttachmentHelperName(node.Parsed.Name, out _)
            || GlbDocument.TryParseCannonHelperName(node.Parsed.Name, out _)
            || node.Parsed.LightIndex.HasValue
              && GlbDocument.TryParseStaticLightHelperName(node.Parsed.Name, out _, out _));
    }

    private static byte[] CreateAttachmentRecord(Matrix4x4 transform, byte extra)
    {
      var (translation, heading) = ReadAttachmentTransform(transform);
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

    private static (Vector3 Translation, byte Heading) ReadAttachmentTransform(Matrix4x4 transform)
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

      if (!AttachmentHeadingProjection.TryReadHeading(rotation, out var heading))
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }

      return (translation, heading);
    }

    private static short QuantizeAttachmentCoordinate(
      float value,
      bool rejectsSentinel,
      string domain = "AttachmentPose",
      string? path = null)
    {
      var scaled = Math.Truncate(value * 256d);
      if (!double.IsFinite(scaled) || scaled < short.MinValue || scaled > short.MaxValue)
      {
        throw new UnsupportedGltfDomainException(domain, path);
      }
      var result = (short)scaled;
      if (rejectsSentinel && result == short.MinValue)
      {
        throw new UnsupportedGltfDomainException(domain, path);
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
      IReadOnlyList<(ParsedGltfMesh Parsed, MetadataEnvelope? Metadata)> meshes,
      StaticMeshAsset asset,
      InterchangeBaseline expected,
      StaticMeshEditSession edit)
    {
      var sources = StaticSourceObjectTraversal.Flatten(asset.RootSourceObject)
        .ToDictionary(source => source.Id.Value);
      if (nodes.Any(node => !node.Parsed.MeshIndex.HasValue
        && node.Metadata is not null
        && node.Metadata.AttachmentRecord is null
        && node.Metadata.CannonRenderPositionRecord is null
        && node.Metadata.StaticLightAttachmentRecord is null))
      {
        throw new MalformedMetadataException("The object scope set does not match the source hierarchy.");
      }

      var identifiedSourceIds = nodes.Where(node => node.Parsed.MeshIndex.HasValue && node.Metadata is not null)
        .Select(node => node.Metadata!.LocalId).ToArray();
      var acceptedDeletionMeshIndices = parsed.AcceptedDeletionScopes
        .Select(path => TryReadIndex(path, "meshes", out var index) ? index : -1)
        .Where(index => index >= 0)
        .ToHashSet();
      if (identifiedSourceIds.Distinct().Count() != identifiedSourceIds.Length)
      {
        throw new MetadataIdentityException(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          "Duplicate object scope identities require an explicit fork resolution.");
      }
      if (nodes.Any(node => node.Parsed.MeshIndex.HasValue
          && node.Metadata is null
          && !acceptedDeletionMeshIndices.Contains(node.Parsed.MeshIndex.Value))
        && sources.Keys.Any(id => !identifiedSourceIds.Contains(id)))
      {
        throw new MetadataIdentityException(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          "An untagged object cannot be distinguished from a missing expected object scope.");
      }
      if (nodes.Count(node => node.Parsed.MeshIndex.HasValue
        && node.Metadata is null
        && !acceptedDeletionMeshIndices.Contains(node.Parsed.MeshIndex.Value)) > 1)
      {
        throw new MetadataIdentityException(
          GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
          2012,
          "Multiple untagged objects require explicit scope identities.");
      }
      var meshLocalIds = meshes.Where(mesh => mesh.Metadata is not null)
        .Select(mesh => mesh.Metadata!.LocalId).ToArray();
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
          || metadata is not null
            && meshes[node.Parsed.MeshIndex.Value].Metadata?.ScopeKind != "mesh")
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
          if (meshes[node.Parsed.MeshIndex.Value].Metadata!.LocalId != metadata.LocalId)
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
        sourceByNode,
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
      internal IReadOnlyDictionary<int, StaticSourceObject> SourceByNode { get; }
      internal IReadOnlyList<StaticRenderObjectId> Sequence { get; }
      internal IReadOnlyDictionary<StaticRenderObjectId, System.Numerics.Vector3> Pivots { get; }
      internal IReadOnlyDictionary<SourceObjectId, System.Numerics.Matrix4x4> Transforms { get; }
      internal IReadOnlyCollection<int> RetainedMeshIndices { get; }
      internal IReadOnlyList<GeometryPartition> AddedPartitions { get; }
      internal bool Changed { get; }

      internal StaticHierarchyPlan(
        StaticSourceObject root,
        IReadOnlyDictionary<int, StaticSourceObject> sourceByNode,
        IReadOnlyList<StaticRenderObjectId> sequence,
        IReadOnlyDictionary<StaticRenderObjectId, System.Numerics.Vector3> pivots,
        IReadOnlyDictionary<SourceObjectId, System.Numerics.Matrix4x4> transforms,
        IReadOnlyCollection<int> retainedMeshIndices,
        IReadOnlyList<GeometryPartition> addedPartitions,
        bool changed)
      {
        Root = root;
        SourceByNode = sourceByNode;
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
      if (exception is MetadataConflictException conflict)
      {
        var actions = GltfMetadataConflictCatalog.ActionsByCode.TryGetValue(
          conflict.Code,
          out var catalogActions)
          ? catalogActions
          : conflict.Actions;
        var data = new Dictionary<string, string>(conflict.ConflictData, StringComparer.Ordinal);
        data["conflictKey"] = $"{conflict.Code}:{conflict.Path}";
        data["importMode"] = "edit";
        data["actions"] = string.Join(",", actions);
        AddDiagnosticData(data, "carrierType", GetMetadataCarrierType(conflict.Path));
        AddDiagnosticData(data, "metadataPath", conflict.Path);
        AddDiagnosticData(data, "nativePath", GetMetadataCarrierPath(conflict.Path));
        AddDiagnosticData(data, "affectedPayloadPaths", conflict.Path);
        data["conflictKey"] = CreateConflictKey(conflict.Code, conflict.Path, data);
        return new OperationDiagnostic(
          conflict.Code,
          conflict.EventId,
          DiagnosticSeverity.Error,
          conflict.Path,
          conflict.Message,
          data: data);
      }

      if (exception is MetadataIdentityException identity)
      {
        var actions = identity.Code == GltfDiagnosticCodes.DocumentMismatch
          ? new[]
          {
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.RetryWithMetadata,
            GltfMetadataConflictActions.AcceptBranch
          }
          : identity.Code == GltfDiagnosticCodes.AssetLineageMismatch
            ? new[]
            {
              GltfMetadataConflictActions.Abort,
              GltfMetadataConflictActions.AdoptAsNew,
              GltfMetadataConflictActions.DiscardLineage
            }
            : new[]
            {
              GltfMetadataConflictActions.Abort,
              GltfMetadataConflictActions.MapScope,
              GltfMetadataConflictActions.ForkScope,
              GltfMetadataConflictActions.DiscardAffectedState
            };
        return MetadataDiagnostic(
          identity.Code,
          identity.EventId,
          identity.Path ?? path,
          identity.Message,
          actions);
      }

      if (exception is DynamicMetadataIdentityException dynamicIdentity)
      {
        return MetadataDiagnostic(
          dynamicIdentity.IsLineage
            ? GltfDiagnosticCodes.AssetLineageMismatch
            : GltfDiagnosticCodes.DocumentMismatch,
          dynamicIdentity.IsLineage ? 2006 : 2007,
          "scenes[0].extras.earthtool",
          dynamicIdentity.Message,
          dynamicIdentity.IsLineage
            ? GltfMetadataConflictActions.AdoptAsNew
            : GltfMetadataConflictActions.AcceptBranch);
      }

      if (exception is DynamicMetadataGraphException dynamicGraph)
      {
        return MetadataDiagnostic(
          dynamicGraph.Code,
          dynamicGraph.EventId,
          dynamicGraph.Path,
          dynamicGraph.Message);
      }

      if (exception is DynamicPreviewException dynamicPreview)
      {
        return InvalidGeometry(dynamicPreview.Path, dynamicPreview.Message);
      }

      if (exception is StaticLightMetadataException staticLightMetadata)
      {
        return MetadataDiagnostic(
          GltfDiagnosticCodes.MalformedMetadata,
          2003,
          staticLightMetadata.Path,
          staticLightMetadata.Message);
      }

      if (exception is MissingMetadataException)
      {
        return MetadataDiagnostic(
          GltfDiagnosticCodes.MissingManifest,
          2000,
          path,
          exception.Message,
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.RetryWithMetadata,
          GltfMetadataConflictActions.DiscardLineage);
      }

      if (exception is UnsupportedMetadataVersionException)
      {
        return MetadataDiagnostic(
          GltfDiagnosticCodes.UnsupportedMetadataVersion,
          2004,
          path,
          exception.Message,
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.RetryWithMetadata,
          GltfMetadataConflictActions.DiscardLineage);
      }

      if (exception is MalformedMetadataException)
      {
        return MetadataDiagnostic(
          GltfDiagnosticCodes.MalformedMetadata,
          2003,
          path,
          exception.Message,
          GltfMetadataConflictActions.Abort,
          GltfMetadataConflictActions.RetryWithMetadata,
          GltfMetadataConflictActions.DiscardAffectedState,
          GltfMetadataConflictActions.DiscardLineage);
      }

      if (exception is UnsupportedGltfDomainException unsupported)
      {
        return Unsupported(unsupported.Domain, unsupported.Path ?? "$");
      }

      if (exception is RequiredTextureResourceBindingException requiredBinding)
      {
        return new OperationDiagnostic(
          GltfDiagnosticCodes.TextureResourceBindingRequired,
          1121,
          DiagnosticSeverity.Error,
          $"materials[{requiredBinding.MaterialIndex}]",
          "A base-color image is only a decoded texture preview. Supply a typed canonical TEX resource key through textureResourceBindings for this material.",
          data: new Dictionary<string, string>
          {
            ["domain"] = "TexResourceBinding",
            ["materialHandle"] = requiredBinding.MaterialHandle.Value.ToString(
              System.Globalization.CultureInfo.InvariantCulture)
          });
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

    private static string GetMetadataCarrierType(string path)
    {
      if (path.StartsWith("scenes[", StringComparison.Ordinal))
      {
        return "scene";
      }
      if (path.StartsWith("nodes[", StringComparison.Ordinal))
      {
        return "node";
      }
      if (path.StartsWith("meshes[", StringComparison.Ordinal))
      {
        return "mesh";
      }
      if (path.StartsWith("materials[", StringComparison.Ordinal))
      {
        return "material";
      }
      if (path.StartsWith("extensions.KHR_lights_punctual.lights[", StringComparison.Ordinal))
      {
        return "light";
      }
      return "metadata";
    }

    private static void AddDiagnosticData(IDictionary<string, string> data, string key, string value)
    {
      if (!data.ContainsKey(key))
      {
        data.Add(key, value);
      }
    }

    private static MetadataConflictResolutionResult ResolveMetadataConflicts(
      IReadOnlyList<MetadataConflictException> conflicts,
      InterchangeBaseline expectedBaseline,
      GltfEditImportOptions options)
    {
      var diagnostics = conflicts.Select(conflict =>
        BindConflictToBaseline(ToDiagnostic(conflict), expectedBaseline)).ToArray();
      var byKey = diagnostics.ToDictionary(
        diagnostic => diagnostic.Data["conflictKey"],
        StringComparer.Ordinal);
      var unmatched = options.ConflictResolutions.FirstOrDefault(resolution =>
        !byKey.ContainsKey(resolution.ConflictKey));
      if (unmatched is not null)
      {
        return MetadataConflictResolutionResult.Failed(InvalidConflictResolution(
          "The conflict resolution is stale or does not match this input."));
      }

      foreach (var resolution in options.ConflictResolutions)
      {
        var diagnostic = byKey[resolution.ConflictKey];
        var actions = diagnostic.Data["actions"].Split(',');
        if (!actions.Contains(resolution.Action, StringComparer.Ordinal))
        {
          return MetadataConflictResolutionResult.Failed(InvalidConflictResolution(
            "The selected action is not allowed for the matching conflict."));
        }
        if (resolution.Action == GltfMetadataConflictActions.MapScope
          && resolution.TargetNativePath != diagnostic.Data["nativePath"])
        {
          return MetadataConflictResolutionResult.Failed(InvalidConflictResolution(
            "The scope mapping target does not match the conflicted native scope."));
        }
      }

      var applied = new List<GltfMetadataConflictResolution>();
      var unresolved = new List<OperationDiagnostic>();
      foreach (var diagnostic in diagnostics)
      {
        var resolution = options.ConflictResolutions.SingleOrDefault(item =>
          item.ConflictKey == diagnostic.Data["conflictKey"]);
        if (resolution is null || !ResolvesInProcess(resolution.Action))
        {
          unresolved.Add(diagnostic);
        }
        else
        {
          applied.Add(resolution);
        }
      }

      return unresolved.Count == 0
        ? MetadataConflictResolutionResult.Succeeded(applied)
        : MetadataConflictResolutionResult.Failed(unresolved);
    }

    private static bool TryGetWholeLineageResolution(
      Exception exception,
      InterchangeBaseline expectedBaseline,
      GltfEditImportOptions options,
      out GltfMetadataConflictResolution? resolution,
      out OperationDiagnostic? failure)
    {
      resolution = options.ConflictResolutions.SingleOrDefault(item =>
        item.Action == GltfMetadataConflictActions.AdoptAsNew
        || item.Action == GltfMetadataConflictActions.DiscardLineage);
      failure = null;
      if (resolution is null)
      {
        return false;
      }

      var diagnostic = BindConflictToBaseline(ToDiagnostic(exception), expectedBaseline);
      if (options.ConflictResolutions.Count != 1
        || !diagnostic.Data.TryGetValue("conflictKey", out var conflictKey)
        || conflictKey != resolution.ConflictKey
        || !diagnostic.Data.TryGetValue("actions", out var actions)
        || !actions.Split(',').Contains(resolution.Action, StringComparer.Ordinal))
      {
        failure = InvalidConflictResolution(
          "The whole-lineage action is stale or is not allowed for the matching conflict.");
      }
      return true;
    }

    private static bool TryGetParseScopeResolution(
      Exception exception,
      InterchangeBaseline expectedBaseline,
      GltfEditImportOptions options,
      out ParseScopeResolution? result,
      out OperationDiagnostic? failure)
    {
      var resolution = options.ConflictResolutions.FirstOrDefault(item =>
        item.Action == GltfMetadataConflictActions.MapScope
        || item.Action == GltfMetadataConflictActions.ForkScope
        || item.Action == GltfMetadataConflictActions.DiscardAffectedState);
      result = null;
      failure = null;
      if (resolution is null)
      {
        return false;
      }
      var diagnostic = BindConflictToBaseline(ToDiagnostic(exception), expectedBaseline);
      if (!diagnostic.Data.TryGetValue("conflictKey", out var conflictKey)
        || conflictKey != resolution.ConflictKey
        || !diagnostic.Data.TryGetValue("actions", out var actions)
        || !actions.Split(',').Contains(resolution.Action, StringComparer.Ordinal))
      {
        failure = InvalidConflictResolution(
          "The scope action is stale or is not allowed for the matching parse conflict.");
        return true;
      }
      result = new ParseScopeResolution(diagnostic, resolution);
      return true;
    }

    private static OperationDiagnostic BindConflictToBaseline(
      OperationDiagnostic diagnostic,
      InterchangeBaseline expectedBaseline)
    {
      if (!diagnostic.Data.ContainsKey("conflictKey"))
      {
        return diagnostic;
      }
      var data = new Dictionary<string, string>(diagnostic.Data, StringComparer.Ordinal)
      {
        ["expectedLineage"] = expectedBaseline.AssetLineageId.ToString("D"),
        ["expectedDocument"] = expectedBaseline.DocumentId.ToString("D")
      };
      data["conflictKey"] = CreateConflictKey(diagnostic.Code, diagnostic.Path, data);
      return new OperationDiagnostic(
        diagnostic.Code,
        diagnostic.EventId,
        diagnostic.Severity,
        diagnostic.Path,
        diagnostic.Message,
        diagnostic.ByteOffset,
        data);
    }

    private static byte[] RemoveGlbMetadata(byte[] glb)
    {
      var jsonLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12, 4)));
      var json = RemoveJsonMetadata(glb.AsSpan(20, jsonLength).ToArray());
      return ReplaceGlbJson(glb, jsonLength, json);
    }

    private static byte[] RewriteGlbMetadata(
      byte[] glb,
      OperationDiagnostic diagnostic,
      GltfMetadataConflictResolution resolution)
    {
      var jsonLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(glb.AsSpan(12, 4)));
      var json = RewriteJsonScopeMetadata(
        glb.AsSpan(20, jsonLength).ToArray(),
        diagnostic,
        resolution);
      return ReplaceGlbJson(glb, jsonLength, json);
    }

    private static byte[] ReplaceGlbJson(byte[] glb, int oldJsonLength, byte[] json)
    {
      var binaryHeader = checked(20 + oldJsonLength);
      var paddedJsonLength = checked((json.Length + 3) & ~3);
      var binaryChunkLength = glb.Length - binaryHeader;
      var result = new byte[checked(20 + paddedJsonLength + binaryChunkLength)];
      glb.AsSpan(0, 8).CopyTo(result);
      BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), checked((uint)result.Length));
      BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(12), checked((uint)paddedJsonLength));
      BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(16), 0x4E4F534A);
      json.CopyTo(result.AsSpan(20));
      result.AsSpan(20 + json.Length, paddedJsonLength - json.Length).Fill(0x20);
      glb.AsSpan(binaryHeader, binaryChunkLength).CopyTo(result.AsSpan(20 + paddedJsonLength));
      return result;
    }

    private static byte[] RewriteJsonScopeMetadata(
      byte[] json,
      OperationDiagnostic diagnostic,
      GltfMetadataConflictResolution resolution)
    {
      var root = JsonNode.Parse(Encoding.UTF8.GetString(json))?.AsObject()
        ?? throw new InvalidDataException("The glTF JSON document is empty.");
      var source = GetJsonCarrier(root, diagnostic.Data["nativePath"]);
      if (source["extras"] is not JsonObject sourceExtras
        || sourceExtras["earthtool"] is not JsonNode metadata)
      {
        throw new InvalidDataException("The conflicted metadata carrier no longer exists.");
      }
      if (resolution.Action == GltfMetadataConflictActions.MapScope)
      {
        var target = GetJsonCarrier(root, resolution.TargetNativePath!);
        var targetExtras = target["extras"] as JsonObject;
        if (targetExtras?["earthtool"] is not null)
        {
          throw new InvalidDataException("The scope mapping target already owns EarthTool metadata.");
        }
        if (targetExtras is null)
        {
          targetExtras = new JsonObject();
          target["extras"] = targetExtras;
        }
        targetExtras["earthtool"] = metadata.DeepClone();
      }
      sourceExtras.Remove("earthtool");
      if (sourceExtras.Count == 0)
      {
        source.Remove("extras");
      }
      return Encoding.UTF8.GetBytes(root.ToJsonString());
    }

    private static JsonObject GetJsonCarrier(JsonObject root, string nativePath)
    {
      if (TryReadIndex(nativePath, "nodes", out var nodeIndex))
      {
        return root["nodes"]![nodeIndex]!.AsObject();
      }
      if (TryReadIndex(nativePath, "meshes", out var meshIndex))
      {
        return root["meshes"]![meshIndex]!.AsObject();
      }
      if (TryReadIndex(nativePath, "materials", out var materialIndex))
      {
        return root["materials"]![materialIndex]!.AsObject();
      }
      const string lights = "extensions.KHR_lights_punctual.lights";
      if (TryReadIndex(nativePath, lights, out var lightIndex))
      {
        return root["extensions"]!["KHR_lights_punctual"]!["lights"]![lightIndex]!.AsObject();
      }
      if (nativePath == "scenes[0]")
      {
        return root["scenes"]![0]!.AsObject();
      }
      throw new InvalidDataException("The metadata conflict does not identify a supported carrier.");
    }

    private static byte[] RemoveJsonMetadata(byte[] json)
    {
      var root = JsonNode.Parse(Encoding.UTF8.GetString(json))
        ?? throw new InvalidDataException("The glTF JSON document is empty.");
      RemoveMetadata(root);
      return Encoding.UTF8.GetBytes(root.ToJsonString());
    }

    private static void RemoveMetadata(JsonNode node)
    {
      if (node is JsonObject value)
      {
        if (value["extras"] is JsonObject extras)
        {
          extras.Remove("earthtool");
          if (extras.Count == 0)
          {
            value.Remove("extras");
          }
        }
        foreach (var child in value.Select(item => item.Value).Where(child => child is not null).ToArray())
        {
          RemoveMetadata(child!);
        }
      }
      else if (node is JsonArray array)
      {
        foreach (var child in array.Where(child => child is not null))
        {
          RemoveMetadata(child!);
        }
      }
    }

    private static bool ResolvesInProcess(string action)
    {
      return action == GltfMetadataConflictActions.AcceptBranch
        || action == GltfMetadataConflictActions.MapScope
        || action == GltfMetadataConflictActions.AcceptDeletion
        || action == GltfMetadataConflictActions.DiscardAffectedState
        || action == GltfMetadataConflictActions.RegenerateDerivedState;
    }

    private static OperationDiagnostic InvalidConflictResolution(string message)
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.MalformedMetadata,
        2003,
        DiagnosticSeverity.Error,
        "metadata.actions",
        message);
    }

    private static string CreateConflictKey(
      string code,
      string path,
      IReadOnlyDictionary<string, string> data)
    {
      var canonical = new StringBuilder(code).Append('\n').Append(path);
      foreach (var item in data.Where(item => item.Key is not ("conflictKey" or "actions"))
        .OrderBy(item => item.Key, StringComparer.Ordinal))
      {
        canonical.Append('\n').Append(item.Key).Append('=').Append(item.Value);
      }
      using var sha256 = SHA256.Create();
      var digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
      return "v1:" + Convert.ToBase64String(digest).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GetMetadataCarrierPath(string path)
    {
      var payload = path.IndexOf(".payload", StringComparison.Ordinal);
      var guards = path.IndexOf(".guards", StringComparison.Ordinal);
      var separator = payload < 0 ? guards : guards < 0 ? payload : Math.Min(payload, guards);
      return separator < 0 ? path : path.Substring(0, separator);
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

    private static OperationDiagnostic Unsupported(string domain, string path = "$")
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.UnsupportedDomain,
        1102,
        DiagnosticSeverity.Error,
        path,
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
      var attachments = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var diagnostics = new List<OperationDiagnostic>();
      for (var physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        if (BinaryPrimitives.ReadInt16LittleEndian(
          attachments.AsSpan((physicalNumber - 1) * 8, 8)) == short.MinValue)
        {
          continue;
        }
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

    private static IReadOnlyList<OperationDiagnostic> CreateStaticLightDiagnostics(
      StaticMeshAsset asset)
    {
      var attachments = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var spots = asset.CommonBaseHeader.StaticSpotLights.ToArray();
      var omnis = asset.CommonBaseHeader.StaticOmniLights.ToArray();
      var diagnostics = new List<OperationDiagnostic>();
      for (var physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        AddStaticLightDiagnostic(
          diagnostics,
          "spot",
          physicalNumber,
          attachments.AsSpan((physicalNumber + 11) * 8, 8),
          spots.AsSpan((physicalNumber - 1) * 0x30, 0x30));
        AddStaticLightDiagnostic(
          diagnostics,
          "point",
          physicalNumber,
          attachments.AsSpan((physicalNumber + 15) * 8, 8),
          omnis.AsSpan((physicalNumber - 1) * 0x1C, 0x1C));
      }
      return diagnostics.AsReadOnly();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateEmitterHierarchyDiagnostics(
      StaticMeshAsset asset)
    {
      var diagnostics = new List<OperationDiagnostic>();
      for (var number = 1; number <= 4; number++)
      {
        var emitterPhysicalNumber = number + 4;
        var (emitterActive, markerRecordCount) =
          GlbDocument.GetEmitterHierarchyState(asset, number);
        if (!emitterActive && markerRecordCount == 0)
        {
          continue;
        }

        var missing = new List<string>();
        if (!emitterActive)
        {
          missing.Add("emitter");
        }
        else
        {
          if (markerRecordCount == 0)
          {
            missing.Add("markerObject");
          }
          else if (markerRecordCount > 1)
          {
            missing.Add("uniqueMarkerObject");
          }
        }
        if (missing.Count == 0)
        {
          continue;
        }

        diagnostics.Add(new OperationDiagnostic(
          GltfDiagnosticCodes.EmitterHierarchyFallback,
          1120,
          DiagnosticSeverity.Warning,
          $"CommonBaseHeader.AttachmentTable[{emitterPhysicalNumber}]",
          emitterActive
            ? "The emitter helper remains under the root because it has no unique marker-attachment source object."
            : "A marker-attachment source object has no corresponding emitter helper.",
          data: new Dictionary<string, string>
          {
            ["number"] = number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["markerObjectCount"] = markerRecordCount.ToString(
              System.Globalization.CultureInfo.InvariantCulture),
            ["missing"] = string.Join(",", missing)
          }));
      }
      return diagnostics.AsReadOnly();
    }

    private static void AddStaticLightDiagnostic(
      ICollection<OperationDiagnostic> diagnostics,
      string type,
      int physicalNumber,
      ReadOnlySpan<byte> attachment,
      ReadOnlySpan<byte> record)
    {
      if (BinaryPrimitives.ReadInt16LittleEndian(attachment) == short.MinValue)
      {
        return;
      }
      var substituted = new List<string>();
      for (var component = 0; component < 3; component++)
      {
        if (!float.IsFinite(ReadSingle(record, component * 4)))
        {
          substituted.Add($"position[{component}]");
        }
        var color = ReadSingle(record, 0x0C + (component * 4));
        if (!float.IsFinite(color) || color < 0)
        {
          substituted.Add($"color[{component}]");
        }
      }
      var intensity = ReadSingle(record, type == "spot" ? 0x2C : 0x18);
      if (!float.IsFinite(intensity) || intensity < 0)
      {
        substituted.Add("intensity");
      }
      if (type == "spot")
      {
        var distance = ReadSingle(record, 0x18);
        var tangent = ReadSingle(record, 0x20);
        var product = ReadSingle(record, 0x24);
        var slope = ReadSingle(record, 0x28);
        var inner = MathF.Atan(tangent);
        var outer = product / distance;
        if (!float.IsFinite(slope) || !float.IsFinite(slope * slope))
        {
          substituted.Add("direction");
        }
        if (!float.IsFinite(inner)
          || !float.IsFinite(outer)
          || inner < 0
          || outer < inner
          || outer > MathF.PI / 2)
        {
          substituted.Add("cones");
        }
      }
      if (substituted.Count == 0)
      {
        return;
      }
      var collection = type == "spot" ? "StaticSpotLights" : "StaticOmniLights";
      diagnostics.Add(new OperationDiagnostic(
        GltfDiagnosticCodes.StaticLightPreviewSubstituted,
        1117,
        DiagnosticSeverity.Warning,
        $"CommonBaseHeader.{collection}[{physicalNumber}]",
        "Anomalous static-light fields use deterministic finite native preview values.",
        data: new Dictionary<string, string>
        {
          ["physicalNumber"] = physicalNumber.ToString(
            System.Globalization.CultureInfo.InvariantCulture),
          ["type"] = type,
          ["fields"] = string.Join(",", substituted)
        }));
    }

    private static float ReadSingle(ReadOnlySpan<byte> source, int offset)
    {
      return BitConverter.Int32BitsToSingle(
        BinaryPrimitives.ReadInt32LittleEndian(source.Slice(offset, sizeof(float))));
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

    private static OperationDiagnostic? ValidatePlan(
      GltfImportPlan plan,
      GltfImportPlanKind kind,
      GltfPackageKind packageKind,
      InterchangeBaseline? expectedBaseline,
      GltfOperationProfile profile)
    {
      var limit = plan.ValidateProfile(profile);
      if (limit is not null)
      {
        return limit;
      }
      if (plan.Kind != kind)
      {
        return PlanMismatch("mode");
      }
      if (plan.PackageKind != packageKind)
      {
        return PlanMismatch("package");
      }
      if (kind == GltfImportPlanKind.Edit
        && (plan.ExpectedBaseline is null
          || expectedBaseline is null
          || plan.ExpectedBaseline.AssetLineageId != expectedBaseline.AssetLineageId
          || plan.ExpectedBaseline.DocumentId != expectedBaseline.DocumentId))
      {
        return PlanMismatch("expectedBaseline");
      }
      return null;
    }

    private static bool MatchesPlanSource(byte[] source, GltfImportPlan plan)
    {
      return string.Equals(
        GltfImportPlanSerializer.Hash(source),
        plan.SourceSha256,
        StringComparison.Ordinal);
    }

    private static OperationDiagnostic PlanMismatch(string path)
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.ImportPlanMismatch,
        3004,
        DiagnosticSeverity.Error,
        path,
        "The import plan does not match the selected import or source package.",
        data: new Dictionary<string, string> { ["dimension"] = path });
    }

    private static OperationResult<GltfEditImportResult> TranslateStalePlan(
      OperationResult<GltfEditImportResult> result,
      GltfImportPlan plan)
    {
      if (plan.EditOptions!.ConflictResolutions.Count == 0
        || !result.Diagnostics.Any(diagnostic =>
          diagnostic.Code == GltfDiagnosticCodes.MalformedMetadata
          && diagnostic.Path == "metadata.actions"))
      {
        return result;
      }
      return Failed<GltfEditImportResult>(new OperationDiagnostic(
        GltfDiagnosticCodes.StaleImportPlan,
        3003,
        DiagnosticSeverity.Error,
        "conflictActions",
        "A planned conflict action is stale or does not match the current conflict inventory."));
    }

    private static OperationDiagnostic MetadataDiagnostic(
      string code,
      int eventId,
      string path,
      string message,
      params string[] actions)
    {
      if (GltfMetadataConflictCatalog.ActionsByCode.TryGetValue(code, out var catalogActions))
      {
        actions = catalogActions.ToArray();
      }
      var data = new Dictionary<string, string>
      {
        ["importMode"] = "edit",
        ["actions"] = string.Join(",", actions),
        ["carrierType"] = GetMetadataCarrierType(path),
        ["metadataPath"] = path,
        ["nativePath"] = GetMetadataCarrierPath(path),
        ["affectedPayloadPaths"] = path
      };
      data["conflictKey"] = CreateConflictKey(code, path, data);
      return new OperationDiagnostic(
        code,
        eventId,
        DiagnosticSeverity.Error,
        path,
        message,
        data: data);
    }

    private static OperationResult<T> Failed<T>(OperationDiagnostic diagnostic)
      where T : class
    {
      return new OperationResult<T>(OperationStatus.Failed, diagnostics: new[] { diagnostic });
    }

    private static OperationResult<T> Failed<T>(IEnumerable<OperationDiagnostic> diagnostics)
      where T : class
    {
      return new OperationResult<T>(OperationStatus.Failed, diagnostics: diagnostics);
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

    private sealed class MetadataConflictResolutionResult
    {
      internal IReadOnlyList<GltfMetadataConflictResolution> Applied { get; }

      internal IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

      private MetadataConflictResolutionResult(
        IReadOnlyList<GltfMetadataConflictResolution> applied,
        IReadOnlyList<OperationDiagnostic> diagnostics)
      {
        Applied = applied;
        Diagnostics = diagnostics;
      }

      internal static MetadataConflictResolutionResult Succeeded(
        IReadOnlyList<GltfMetadataConflictResolution> applied)
      {
        return new MetadataConflictResolutionResult(applied, Array.Empty<OperationDiagnostic>());
      }

      internal static MetadataConflictResolutionResult Failed(OperationDiagnostic diagnostic)
      {
        return Failed(new[] { diagnostic });
      }

      internal static MetadataConflictResolutionResult Failed(
        IReadOnlyList<OperationDiagnostic> diagnostics)
      {
        return new MetadataConflictResolutionResult(
          Array.Empty<GltfMetadataConflictResolution>(),
          diagnostics);
      }
    }

    private sealed class ParseScopeResolution
    {
      internal OperationDiagnostic Diagnostic { get; }

      internal GltfMetadataConflictResolution Resolution { get; }

      internal ParseScopeResolution(
        OperationDiagnostic diagnostic,
        GltfMetadataConflictResolution resolution)
      {
        Diagnostic = diagnostic;
        Resolution = resolution;
      }
    }
  }

  internal sealed class MetadataIdentityException : Exception
  {
    internal string Code { get; }

    internal int EventId { get; }

    internal string? Path { get; }

    internal MetadataIdentityException(string code, int eventId, string message, string? path = null)
      : base(message)
    {
      Code = code;
      EventId = eventId;
      Path = path;
    }
  }

  internal sealed class StaticLightMetadataException : Exception
  {
    internal string Path { get; }

    internal StaticLightMetadataException(string path, string message)
      : base(message)
    {
      Path = path;
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

  internal sealed class RequiredTextureResourceBindingException : Exception
  {
    internal int MaterialIndex { get; }

    internal GltfMaterialHandle MaterialHandle { get; }

    internal RequiredTextureResourceBindingException(
      int materialIndex,
      GltfMaterialHandle materialHandle)
      : base("A textured new-model material requires a typed canonical TEX resource binding.")
    {
      MaterialIndex = materialIndex;
      MaterialHandle = materialHandle;
    }
  }
}
