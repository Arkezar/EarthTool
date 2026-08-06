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
using System.Threading;
using System.Threading.Tasks;
using CanonicalAttachmentRecord = EarthTool.MSH.Internal.CanonicalAttachmentRecord;
using CanonicalBaseHeaderEncoder = EarthTool.MSH.Internal.CanonicalBaseHeaderEncoder;
using CanonicalCannonRenderPosition = EarthTool.MSH.Internal.CanonicalCannonRenderPosition;
using CanonicalOmniLight = EarthTool.MSH.Internal.CanonicalOmniLight;
using CanonicalSpotLight = EarthTool.MSH.Internal.CanonicalSpotLight;
using CanonicalStaticBaseHeaderInput = EarthTool.MSH.Internal.CanonicalStaticBaseHeaderInput;
using CanonicalStaticMeshAssembler = EarthTool.MSH.Internal.CanonicalStaticMeshAssembler;
using CanonicalStaticMeshAssemblyInput = EarthTool.MSH.Internal.CanonicalStaticMeshAssemblyInput;
using CanonicalStaticRenderObjectSequenceEncoder = EarthTool.MSH.Internal.CanonicalStaticRenderObjectSequenceEncoder;
using DynamicEffectBehavior = EarthTool.MSH.Internal.DynamicEffectBehavior;
using DynamicObjectPlacement = EarthTool.MSH.Internal.DynamicObjectPlacement;

namespace EarthTool.GLTF
{
  /// <summary>Provides the sealed MSH and glTF interchange facade.</summary>
  public sealed class GltfInterchange
  {
    private readonly ITransactionalFileSystem _fileSystem;

    /// <summary>Initializes the facade using the platform filesystem.</summary>
    public GltfInterchange()
      : this(new TransactionalFileSystem()) { }

    internal GltfInterchange(ITransactionalFileSystem fileSystem)
    {
      _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <summary>Exports one supported static asset as a strictly validated GLB.</summary>
    public async Task<OperationResult> ExportGlbAsync(
      StaticMeshAsset asset,
      Stream destination,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
    {
      return WithoutValue(
        await ExportGlbWithReceiptAsync(asset, destination, options, profile, cancellationToken)
          .ConfigureAwait(false)
      );
    }

    internal async Task<OperationResult<GltfExportReceipt>> ExportGlbWithReceiptAsync(
      StaticMeshAsset asset,
      Stream destination,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
          options.DocumentId ?? Guid.NewGuid()
        );
        var identityMap = options.StaticIdentityMap
          ?? GltfStaticIdentityMap.CreateSequential(asset);
        var animationDiagnostics = CreateAnimationDiagnostics(asset, baseline, identityMap);
        var projectionDiagnostics = animationDiagnostics
          .Concat(CreateCannonRenderPositionDiagnostics(asset))
          .Concat(CreateStaticLightDiagnostics(asset))
          .Concat(CreateEmitterHierarchyDiagnostics(asset))
          .Concat(CreateSourceLossDiagnostics(asset))
          .ToArray();
        var metadataLength = GlbDocument.GetMaximumMetadataByteCount(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds,
          identityMap
        );
        if (metadataLength > profile.MaxMetadataBytes)
        {
          return Failed<GltfExportReceipt>(
            Limit("scenes[0].extras.earthtool", metadataLength, profile.MaxMetadataBytes)
          );
        }

        var minimumOutputLength = GlbDocument.GetMinimumOutputByteCount(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds,
          identityMap,
          true
        );
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
          identityMap,
          new Dictionary<StaticRenderObject, TexPreview>(),
          options.SourceBaseName,
          out var fingerprint
        );
        if (withoutPreviews.Length > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(
            Limit("$", withoutPreviews.Length, profile.MaxOutputBytes)
          );
        }
        var previewResult = TexPreviewLoader.Load(
          asset,
          options,
          profile,
          profile.MaxOutputBytes - withoutPreviews.Length,
          cancellationToken
        );
        if (previewResult.HasErrors)
        {
          return Failed<GltfExportReceipt>(
            previewResult.Diagnostics.First(diagnostic =>
              diagnostic.Severity == DiagnosticSeverity.Error
            )
          );
        }

        var glb =
          previewResult.Previews.Count == 0
            ? withoutPreviews
            : GlbDocument.Create(
              asset,
              baseline,
              options.PreservedUnknownMetadata,
              options.MetadataNextIds,
              options.ArtistObjectLocalIds,
              identityMap,
              previewResult.Previews,
              options.SourceBaseName,
              out fingerprint
            );
        var exportDiagnostics = previewResult.Diagnostics.Concat(projectionDiagnostics).ToArray();
        if (glb.Length > profile.MaxOutputBytes)
        {
          glb = withoutPreviews;
          exportDiagnostics = WithoutEmittedPreviewDiagnostics(previewResult.Diagnostics)
            .Concat(projectionDiagnostics)
            .ToArray();
        }

        GlbDocument.Validate(glb, profile);
        cancellationToken.ThrowIfCancellationRequested();
        await destination.WriteAsync(glb, 0, glb.Length, cancellationToken).ConfigureAwait(false);
        return new OperationResult<GltfExportReceipt>(
          OperationStatus.Succeeded,
          new GltfExportReceipt(baseline, fingerprint),
          exportDiagnostics
        );
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
    public async Task<OperationResult> ExportGlbFileAsync(
      StaticMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
    {
      return WithoutValue(
        await ExportGlbFileWithReceiptAsync(asset, destinationPath, options, profile, cancellationToken)
          .ConfigureAwait(false)
      );
    }

    internal async Task<OperationResult<GltfExportReceipt>> ExportGlbFileWithReceiptAsync(
      StaticMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
          result = await ExportGlbWithReceiptAsync(asset, temporary, options, profile, cancellationToken)
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

    /// <summary>Transactionally exports one supported static asset as separate glTF and buffer files.</summary>
    public async Task<OperationResult> ExportGltfFileAsync(
      StaticMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
    {
      return WithoutValue(
        await ExportGltfFileWithReceiptAsync(asset, destinationPath, options, profile, cancellationToken)
          .ConfigureAwait(false)
      );
    }

    internal async Task<OperationResult<GltfExportReceipt>> ExportGltfFileWithReceiptAsync(
      StaticMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
          options.DocumentId ?? Guid.NewGuid()
        );
        var identityMap = options.StaticIdentityMap
          ?? GltfStaticIdentityMap.CreateSequential(asset);
        var animationDiagnostics = CreateAnimationDiagnostics(asset, baseline, identityMap);
        var projectionDiagnostics = animationDiagnostics
          .Concat(CreateCannonRenderPositionDiagnostics(asset))
          .Concat(CreateStaticLightDiagnostics(asset))
          .Concat(CreateEmitterHierarchyDiagnostics(asset))
          .Concat(CreateSourceLossDiagnostics(asset))
          .ToArray();
        var metadataLength = GlbDocument.GetMaximumMetadataByteCount(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds,
          identityMap
        );
        if (metadataLength > profile.MaxMetadataBytes)
        {
          return Failed<GltfExportReceipt>(
            Limit("scenes[0].extras.earthtool", metadataLength, profile.MaxMetadataBytes)
          );
        }

        var minimumOutputLength = GlbDocument.GetMinimumOutputByteCount(
          asset,
          baseline,
          options.PreservedUnknownMetadata,
          options.MetadataNextIds,
          options.ArtistObjectLocalIds,
          identityMap,
          false
        );
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
          identityMap,
          new Dictionary<StaticRenderObject, TexPreview>(),
          options.SourceBaseName,
          out var fingerprint
        );
        var withoutPreviewLength = checked(
          withoutPreviews.Json.Length + withoutPreviews.Binary.Length
        );
        if (withoutPreviewLength > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(
            Limit("$", withoutPreviewLength, profile.MaxOutputBytes)
          );
        }
        var previewResult = TexPreviewLoader.Load(
          asset,
          options,
          profile,
          profile.MaxOutputBytes - withoutPreviewLength,
          cancellationToken
        );
        if (previewResult.HasErrors)
        {
          return Failed<GltfExportReceipt>(
            previewResult.Diagnostics.First(diagnostic =>
              diagnostic.Severity == DiagnosticSeverity.Error
            )
          );
        }

        var package =
          previewResult.Previews.Count == 0
            ? withoutPreviews
            : GlbDocument.CreateSeparate(
              asset,
              baseline,
              options.PreservedUnknownMetadata,
              options.MetadataNextIds,
              options.ArtistObjectLocalIds,
              identityMap,
              previewResult.Previews,
              options.SourceBaseName,
              out fingerprint
            );
        var outputLength = checked(
          package.Json.Length
          + package.Binary.Length
          + package.ImageSidecars.Values.Sum(bytes => bytes.Length)
        );
        var exportDiagnostics = previewResult.Diagnostics.Concat(projectionDiagnostics).ToArray();
        if (outputLength > profile.MaxOutputBytes)
        {
          package = withoutPreviews;
          exportDiagnostics = WithoutEmittedPreviewDiagnostics(previewResult.Diagnostics)
            .Concat(projectionDiagnostics)
            .ToArray();
        }

        ValidateGeometryProfile(
          GlbDocument.ParseSeparate(package.Json, package.Binary, profile),
          profile
        );
        GlbDocument.ValidateSeparate(
          package.Json,
          package.Binary,
          package.BufferFileName,
          package.ImageSidecars
        );
        var directory =
          Path.GetDirectoryName(Path.GetFullPath(destinationPath))
          ?? Directory.GetCurrentDirectory();
        var sidecars = new Dictionary<string, byte[]>(package.ImageSidecars, StringComparer.Ordinal)
        {
          [package.BufferFileName] = package.Binary,
        };
        var manifestFullPath = Path.GetFullPath(destinationPath);
        if (Directory.Exists(manifestFullPath))
        {
          throw new IOException("The glTF manifest path collides with a directory.");
        }
        var sidecarPaths = sidecars.ToDictionary(
          sidecar => sidecar.Key,
          sidecar => Path.Combine(directory, sidecar.Key),
          StringComparer.Ordinal
        );

        foreach (var sidecar in sidecars.OrderBy(sidecar => sidecar.Key, StringComparer.Ordinal))
        {
          var sidecarPath = sidecarPaths[sidecar.Key];
          if (
            string.Equals(
              manifestFullPath,
              Path.GetFullPath(sidecarPath),
              StringComparison.OrdinalIgnoreCase
            )
          )
          {
            throw new IOException(
              "The glTF manifest path collides with a content-addressed sidecar."
            );
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
            await temporary
              .WriteAsync(sidecar.Value, 0, sidecar.Value.Length, cancellationToken)
              .ConfigureAwait(false);
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
          if (
            !File.Exists(sidecarPaths[sidecar.Key])
            || !HasSameContent(sidecarPaths[sidecar.Key], sidecar.Value)
          )
          {
            throw new IOException("A committed glTF sidecar is incomplete or invalid.");
          }
        }

        cancellationToken.ThrowIfCancellationRequested();
        using (var temporary = _fileSystem.CreateTemporary(manifestTemporaryPath))
        {
          await temporary
            .WriteAsync(package.Json, 0, package.Json.Length, cancellationToken)
            .ConfigureAwait(false);
          await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        _fileSystem.Commit(manifestTemporaryPath, destinationPath);
        return new OperationResult<GltfExportReceipt>(
          OperationStatus.Succeeded,
          new GltfExportReceipt(baseline, fingerprint),
          exportDiagnostics
        );
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
    public async Task<OperationResult> ExportGlbAsync(
      DynamicMeshAsset asset,
      Stream destination,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
    {
      return WithoutValue(
        await ExportGlbWithReceiptAsync(asset, destination, options, profile, cancellationToken)
          .ConfigureAwait(false)
      );
    }

    internal async Task<OperationResult<GltfExportReceipt>> ExportGlbWithReceiptAsync(
      DynamicMeshAsset asset,
      Stream destination,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
          options.DocumentId ?? Guid.NewGuid()
        );
        var previewResult = TexPreviewLoader.Load(
          asset,
          options,
          profile,
          profile.MaxOutputBytes,
          cancellationToken
        );
        if (previewResult.HasErrors)
        {
          return Failed<GltfExportReceipt>(
            previewResult.Diagnostics.First(diagnostic =>
              diagnostic.Severity == DiagnosticSeverity.Error
            )
          );
        }
        var meshPreviewResult = MshPreviewLoader.Load(asset, options, profile, cancellationToken);
        var glb = DynamicGltfDocument.Create(
          asset,
          baseline,
          profile,
          previewResult.Previews,
          meshPreviewResult.Previews,
          options.DynamicObjectIds,
          options.SourceBaseName,
          out var fingerprint
        );
        DynamicGltfDocument.ValidateGlb(glb, profile);
        cancellationToken.ThrowIfCancellationRequested();
        await destination.WriteAsync(glb, 0, glb.Length, cancellationToken).ConfigureAwait(false);
        return new OperationResult<GltfExportReceipt>(
          OperationStatus.Succeeded,
          new GltfExportReceipt(baseline, fingerprint),
          previewResult.Diagnostics
            .Concat(meshPreviewResult.Diagnostics)
            .Concat(CreateSourceLossDiagnostics(asset))
        );
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
    public async Task<OperationResult> ExportGlbFileAsync(
      DynamicMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
    {
      return WithoutValue(
        await ExportGlbFileWithReceiptAsync(asset, destinationPath, options, profile, cancellationToken)
          .ConfigureAwait(false)
      );
    }

    internal async Task<OperationResult<GltfExportReceipt>> ExportGlbFileWithReceiptAsync(
      DynamicMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
          result = await ExportGlbWithReceiptAsync(asset, temporary, options, profile, cancellationToken)
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
    public async Task<OperationResult> ExportGltfFileAsync(
      DynamicMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
    {
      return WithoutValue(
        await ExportGltfFileWithReceiptAsync(asset, destinationPath, options, profile, cancellationToken)
          .ConfigureAwait(false)
      );
    }

    internal async Task<OperationResult<GltfExportReceipt>> ExportGltfFileWithReceiptAsync(
      DynamicMeshAsset asset,
      string destinationPath,
      GltfExportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
          options.DocumentId ?? Guid.NewGuid()
        );
        var previewResult = TexPreviewLoader.Load(
          asset,
          options,
          profile,
          profile.MaxOutputBytes,
          cancellationToken
        );
        if (previewResult.HasErrors)
        {
          return Failed<GltfExportReceipt>(
            previewResult.Diagnostics.First(diagnostic =>
              diagnostic.Severity == DiagnosticSeverity.Error
            )
          );
        }
        var meshPreviewResult = MshPreviewLoader.Load(asset, options, profile, cancellationToken);
        var package = DynamicGltfDocument.CreateSeparate(
          asset,
          baseline,
          profile,
          previewResult.Previews,
          meshPreviewResult.Previews,
          options.DynamicObjectIds,
          options.SourceBaseName,
          out var fingerprint
        );
        GlbDocument.ValidateSeparate(
          package.Json,
          package.Binary,
          package.BufferFileName,
          package.ImageSidecars
        );
        var directory =
          Path.GetDirectoryName(Path.GetFullPath(destinationPath))
          ?? Directory.GetCurrentDirectory();
        var sidecarPath = Path.Combine(directory, package.BufferFileName);
        if (
          Directory.Exists(sidecarPath)
          || File.Exists(sidecarPath) && !HasSameContent(sidecarPath, package.Binary)
        )
        {
          throw new IOException(
            "A content-addressed dynamic glTF sidecar has conflicting content."
          );
        }
        if (!File.Exists(sidecarPath))
        {
          sidecarTemporaryPath = _fileSystem.GetTemporaryPath(sidecarPath);
          using (var temporary = _fileSystem.CreateTemporary(sidecarTemporaryPath))
          {
            await temporary
              .WriteAsync(package.Binary, 0, package.Binary.Length, cancellationToken)
              .ConfigureAwait(false);
            await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
          }
          cancellationToken.ThrowIfCancellationRequested();
          _fileSystem.Commit(sidecarTemporaryPath, sidecarPath);
          committedSidecarPath = sidecarPath;
          sidecarTemporaryPath = null;
        }
        using (var temporary = _fileSystem.CreateTemporary(manifestTemporaryPath))
        {
          await temporary
            .WriteAsync(package.Json, 0, package.Json.Length, cancellationToken)
            .ConfigureAwait(false);
          await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        _fileSystem.Commit(manifestTemporaryPath, destinationPath);
        manifestCommitted = true;
        return new OperationResult<GltfExportReceipt>(
          OperationStatus.Succeeded,
          new GltfExportReceipt(baseline, fingerprint),
          previewResult.Diagnostics
            .Concat(meshPreviewResult.Diagnostics)
            .Concat(CreateSourceLossDiagnostics(asset))
        );
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

    /// <summary>Creates one immutable static or dynamic mesh asset from a GLB stream.</summary>
    public async Task<OperationResult<MeshAsset>> CreateMeshAsync(
      Stream source,
      GltfNewModelImportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }

      profile ??= GltfOperationProfile.Default;
      options ??= new GltfNewModelImportOptions();
      try
      {
        var glb = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken)
          .ConfigureAwait(false);
        return CreateMeshCore(
          glb,
          separatePackage: null,
          options,
          profile,
          cancellationToken
        );
      }
      catch (OperationCanceledException)
      {
        return Cancelled<MeshAsset>();
      }
      catch (Exception ex)
      {
        return Failed<MeshAsset>(ToDiagnostic(ex));
      }
    }

    /// <summary>Creates one immutable mesh asset from a GLB stream bound to a typed import plan.</summary>
    public async Task<OperationResult<MeshAsset>> CreateMeshWithPlanAsync(
      Stream source,
      GltfImportPlan plan,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
      try
      {
        var mismatch = ValidatePlan(
          plan,
          GltfPackageKind.Glb,
          profile
        );
        if (mismatch is not null)
        {
          return Failed<MeshAsset>(mismatch);
        }
        var glb = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken)
          .ConfigureAwait(false);
        if (!MatchesPlanSource(glb, plan))
        {
          return Failed<MeshAsset>(PlanMismatch("sourceSha256"));
        }
        return CreateMeshCore(
          glb,
          separatePackage: null,
          plan.NewModelOptions!,
          profile,
          cancellationToken
        );
      }
      catch (OperationCanceledException)
      {
        return Cancelled<MeshAsset>();
      }
      catch (Exception ex)
      {
        return Failed<MeshAsset>(ToDiagnostic(ex));
      }
    }

    /// <summary>Creates one immutable static or dynamic mesh asset from a separate-glTF file.</summary>
    public async Task<OperationResult<MeshAsset>> CreateMeshFileAsync(
      string sourcePath,
      GltfNewModelImportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
        return CreateMeshCore(package.Json, package, options, profile, cancellationToken);
      }
      catch (OperationCanceledException)
      {
        return Cancelled<MeshAsset>();
      }
      catch (Exception ex)
      {
        return Failed<MeshAsset>(ToDiagnostic(ex, sourcePath));
      }
    }

    /// <summary>Creates one immutable mesh asset from a separate-glTF package bound to a typed import plan.</summary>
    public async Task<OperationResult<MeshAsset>> CreateMeshFileWithPlanAsync(
      string sourcePath,
      GltfImportPlan plan,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
      try
      {
        var mismatch = ValidatePlan(
          plan,
          GltfPackageKind.Gltf,
          profile
        );
        if (mismatch is not null)
        {
          return Failed<MeshAsset>(mismatch);
        }
        var package = await ReadSeparatePackageAsync(sourcePath, profile, cancellationToken)
          .ConfigureAwait(false);
        if (!GltfImportPlanSerializer.MatchesSeparateSource(package, plan.SourceSha256))
        {
          return Failed<MeshAsset>(PlanMismatch("sourceSha256"));
        }
        return CreateMeshCore(
          package.Json,
          package,
          plan.NewModelOptions!,
          profile,
          cancellationToken
        );
      }
      catch (OperationCanceledException)
      {
        return Cancelled<MeshAsset>();
      }
      catch (Exception ex)
      {
        return Failed<MeshAsset>(ToDiagnostic(ex, sourcePath));
      }
    }

    private static OperationResult<MeshAsset> CreateMeshCore(
      byte[] source,
      SeparateGltfPackage? separatePackage,
      GltfNewModelImportOptions options,
      GltfOperationProfile profile,
      CancellationToken cancellationToken
    )
    {
      ReadOnlyMemory<byte> json;
      if (separatePackage is null)
      {
        var jsonLength =
          source.Length >= 20
            ? checked((int)BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(12, sizeof(uint))))
            : 0;
        json =
          jsonLength > 0 && 20 + jsonLength <= source.Length
            ? source.AsMemory(20, jsonLength)
            : ReadOnlyMemory<byte>.Empty;
      }
      else
      {
        GlbDocument.ValidateSeparate(
          separatePackage.Json,
          separatePackage.Binary,
          separatePackage.BufferUri,
          separatePackage.Images
        );
        json = separatePackage.Json;
      }

      if (!json.IsEmpty && CanonicalDynamicGltfImporter.HasClaim(json, profile.MaxJsonDepth))
      {
        var imported = separatePackage is null
          ? CanonicalDynamicGltfImporter.ImportGlb(source, options, profile, cancellationToken)
          : CanonicalDynamicGltfImporter.ImportSeparate(
            separatePackage.Json,
            separatePackage.Binary,
            options,
            profile,
            cancellationToken
          );
        return ToMeshAsset(imported);
      }

      var staticOptions = new CanonicalStaticGltfCreationOptions(
        Guid.NewGuid(),
        options
      );
      var staticImport = separatePackage is null
        ? ImportCanonicalStaticGlb(source, staticOptions, profile, cancellationToken)
        : ImportCanonicalStaticSeparate(
          separatePackage.Json,
          separatePackage.Binary,
          staticOptions,
          profile,
          cancellationToken
        );
      return ToMeshAsset(staticImport);
    }

    internal static OperationResult<StaticMeshAsset> ImportCanonicalStaticGlb(
      byte[] source,
      CanonicalStaticGltfCreationOptions options,
      GltfOperationProfile profile,
      CancellationToken cancellationToken
    )
    {
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = GlbDocument.ParseCanonicalStatic(source, profile);
        ValidateGeometryProfile(parsed, profile);
        return ImportCanonicalStaticParsed(
          parsed,
          options,
          profile,
          cancellationToken
        );
      }
      catch (OperationCanceledException)
      {
        return Cancelled<StaticMeshAsset>();
      }
      catch (Exception ex)
      {
        return Failed<StaticMeshAsset>(ToDiagnostic(ex));
      }
    }

    internal static OperationResult<StaticMeshAsset> ImportCanonicalStaticSeparate(
      byte[] json,
      byte[] binary,
      CanonicalStaticGltfCreationOptions options,
      GltfOperationProfile profile,
      CancellationToken cancellationToken
    )
    {
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = GlbDocument.ParseSeparateCanonicalStatic(json, binary, profile);
        ValidateGeometryProfile(parsed, profile);
        return ImportCanonicalStaticParsed(
          parsed,
          options,
          profile,
          cancellationToken
        );
      }
      catch (OperationCanceledException)
      {
        return Cancelled<StaticMeshAsset>();
      }
      catch (Exception ex)
      {
        return Failed<StaticMeshAsset>(ToDiagnostic(ex));
      }
    }

    private static OperationResult<StaticMeshAsset> ImportCanonicalStaticParsed(
      ParsedGlb parsed,
      CanonicalStaticGltfCreationOptions options,
      GltfOperationProfile profile,
      CancellationToken cancellationToken
    )
    {
      var metadata = CanonicalAuthoringMetadata.Read(
        parsed.Nodes.Select((node, index) =>
          new AuthoringMetadataCarrier(
            $"nodes[{index}]",
            node.Name ?? string.Empty,
            node.Metadata
          )
        ),
        profile
      );
      if (!metadata.Succeeded)
      {
        return new OperationResult<StaticMeshAsset>(
          metadata.Status,
          diagnostics: metadata.Diagnostics
        );
      }

      var semanticOptions = CreateCanonicalStaticSemanticOptions(
        parsed,
        options,
        metadata.Value!
      );
      if (!semanticOptions.Succeeded)
      {
        return new OperationResult<StaticMeshAsset>(
          semanticOptions.Status,
          diagnostics: metadata.Diagnostics.Concat(semanticOptions.Diagnostics)
        );
      }

      var imported = ImportNewModelParsed(
        parsed,
        profile,
        cancellationToken,
        semanticOptions.Value!.ImportOptions,
        options.CreationGuid,
        allowAuthoringMetadata: true,
        cannonValues: semanticOptions.Value.CannonValues
      );
      if (!imported.Succeeded)
      {
        return new OperationResult<StaticMeshAsset>(
          imported.Status,
          diagnostics: metadata.Diagnostics.Concat(imported.Diagnostics)
        );
      }
      return new OperationResult<StaticMeshAsset>(
        OperationStatus.Succeeded,
        imported.Value!.Asset,
        metadata.Diagnostics.Concat(imported.Diagnostics)
      );
    }

    private static OperationResult<CanonicalStaticGltfSemanticOptions> CreateCanonicalStaticSemanticOptions(
      ParsedGlb parsed,
      CanonicalStaticGltfCreationOptions options,
      CanonicalAuthoringMetadataDocument metadata
    )
    {
      var sourceNodes = GetNodeOrder(parsed)
        .Where(index => parsed.Nodes[index].MeshIndex.HasValue)
        .Select(index => (node: parsed.Nodes[index], index))
        .ToArray();
      var unsupportedOwner = parsed.Nodes
        .Select((node, index) =>
          CanonicalAuthoringOwner.TryParse(node.Name, out var owner)
            ? (Owner: (CanonicalAuthoringOwner?)owner, Node: node, Index: index)
            : (Owner: null, Node: node, Index: index)
        )
        .FirstOrDefault(item =>
          item.Owner.HasValue
          && (
            item.Owner.Value.Kind is CanonicalAuthoringOwnerKind.DynamicObject
              or CanonicalAuthoringOwnerKind.Animation
            || item.Owner.Value.Kind == CanonicalAuthoringOwnerKind.StaticSource
              && !item.Node.MeshIndex.HasValue
          )
        );
      if (unsupportedOwner.Owner.HasValue)
      {
        return Failed<CanonicalStaticGltfSemanticOptions>(
          new OperationDiagnostic(
            GltfAuthoringMetadataDiagnosticCodes.RequiredValueMissing,
            4002,
            DiagnosticSeverity.Error,
            $"nodes[{unsupportedOwner.Index}]",
            "The canonical owner declares a required semantic unsupported by static creation."
          )
        );
      }
      var roles = options.ImportOptions.ObjectRoles.ToDictionary(item => item.Key, item => item.Value);
      var cannonValues = new Dictionary<int, CannonAuthoringValues>();
      var staticLightOptions = new Dictionary<
        GltfLightHandle,
        GltfNewModelStaticLightOptions
      >(options.ImportOptions.StaticLightOptions);
      StaticSourceAuthoringValues? rootValues = null;
      foreach (var source in sourceNodes)
      {
        if (
          !CanonicalAuthoringOwner.TryParse(source.node.Name, out var owner)
          || owner.Kind != CanonicalAuthoringOwnerKind.StaticSource
        )
        {
          return Failed<CanonicalStaticGltfSemanticOptions>(
            new OperationDiagnostic(
              GltfAuthoringMetadataDiagnosticCodes.RequiredValueMissing,
              4002,
              DiagnosticSeverity.Error,
              $"nodes[{source.index}]",
              "A static source object requires an exact canonical ET_Static_{n} name."
            )
          );
        }

        var values = metadata.Get<StaticSourceAuthoringValues>(owner);
        rootValues ??= values;
        if (values.Roles != GltfStaticObjectRoles.None || values.BarrelMaximumAngle != 0)
        {
          roles.TryAdd(
            GetNodeHandle(parsed, source.index),
            new GltfNewModelObjectRole(values.Roles, values.BarrelMaximumAngle)
          );
        }
      }

      foreach (var light in parsed.Nodes.Where(node => node.LightIndex.HasValue))
      {
        if (
          !CanonicalAuthoringOwner.TryParse(light.Name, out var owner)
          || owner.Kind != CanonicalAuthoringOwnerKind.StaticLight
        )
        {
          continue;
        }
        var lightIndex = light.LightIndex!.Value;
        if (lightIndex < 0 || lightIndex >= parsed.Lights.Count)
        {
          continue;
        }
        var values = metadata.Get<StaticLightAuthoringValues>(owner);
        var handle = GetLightHandle(parsed, lightIndex);
        staticLightOptions.TryAdd(
          handle,
          new GltfNewModelStaticLightOptions(
            values.TargetDistance,
            values.TerrainLightAmplitude
          )
        );
      }

      foreach (var node in parsed.Nodes)
      {
        if (CanonicalAuthoringOwner.TryParse(node.Name, out var owner)
          && owner.Kind == CanonicalAuthoringOwnerKind.Cannon)
        {
          cannonValues.Add(owner.Number, metadata.Get<CannonAuthoringValues>(owner));
        }
      }

      var footprint = options.ImportOptions.Footprint ?? (rootValues?.Footprint is null
        ? null
        : new GltfNewModelFootprint(
          rootValues.Footprint.PresenceMask,
          rootValues.Footprint.TopElevations,
          rootValues.Footprint.CornerPassageFlags
        ));
      var extents = options.ImportOptions.HorizontalExtents ?? (rootValues?.HorizontalExtents is null
        ? null
        : new GltfNewModelHorizontalExtents(
          rootValues.HorizontalExtents.PositiveY,
          rootValues.HorizontalExtents.NegativeY,
          rootValues.HorizontalExtents.PositiveX,
          rootValues.HorizontalExtents.NegativeX
        ));
      return new OperationResult<CanonicalStaticGltfSemanticOptions>(
        OperationStatus.Succeeded,
        new CanonicalStaticGltfSemanticOptions(
          new GltfNewModelImportOptions(
            options.ImportOptions.TextureResourceBindings,
            footprint,
            extents,
            roles,
            staticLightOptions
          ),
          cannonValues
        )
      );
    }

    /// <summary>Imports a dynamic GLB into an expected lineage and document baseline.</summary>
    internal async Task<OperationResult<GltfDynamicEditImportResult>> ImportEditDynamicGlbAsync(
      Stream source,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
          cancellationToken
        );
        var nextBaseline = new InterchangeBaseline(expectedBaseline.AssetLineageId, Guid.NewGuid());
        return new OperationResult<GltfDynamicEditImportResult>(
          OperationStatus.Succeeded,
          new GltfDynamicEditImportResult(
            imported.Asset,
            nextBaseline,
            imported.Fingerprint,
            imported.Preservation,
            new[] { "RootDynamicObject" },
            imported.ObjectIds
          ),
          CreateDynamicPlacementDiagnostics(imported)
        );
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
    internal async Task<OperationResult<GltfDynamicEditImportResult>> ImportEditDynamicGltfFileAsync(
      string sourcePath,
      InterchangeBaseline expectedBaseline,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
          cancellationToken
        );
        var nextBaseline = new InterchangeBaseline(expectedBaseline.AssetLineageId, Guid.NewGuid());
        return new OperationResult<GltfDynamicEditImportResult>(
          OperationStatus.Succeeded,
          new GltfDynamicEditImportResult(
            imported.Asset,
            nextBaseline,
            imported.Fingerprint,
            imported.Preservation,
            new[] { "RootDynamicObject" },
            imported.ObjectIds
          ),
          CreateDynamicPlacementDiagnostics(imported)
        );
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

    /// <summary>Imports a metadata-free GLB as a canonical authored static mesh representation.</summary>
    internal async Task<OperationResult<GltfNewModelImportResult>> ImportNewModelGlbAsync(
      Stream source,
      GltfNewModelImportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
    internal async Task<OperationResult<GltfNewModelImportResult>> ImportNewModelGlbWithPlanAsync(
      Stream source,
      GltfImportPlan plan,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
        GltfPackageKind.Glb,
        profile
      );
      if (mismatch is not null)
      {
        return Failed<GltfNewModelImportResult>(mismatch);
      }
      try
      {
        var bytes = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken)
          .ConfigureAwait(false);
        if (!MatchesPlanSource(bytes, plan))
        {
          return Failed<GltfNewModelImportResult>(PlanMismatch("sourceSha256"));
        }
        using var captured = new MemoryStream(bytes, false);
        return await ImportNewModelGlbAsync(
            captured,
            plan.NewModelOptions,
            profile,
            cancellationToken
          )
          .ConfigureAwait(false);
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
    internal async Task<OperationResult<GltfNewModelImportResult>> ImportNewModelGltfFileAsync(
      string sourcePath,
      GltfNewModelImportOptions? options = null,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
    internal async Task<
      OperationResult<GltfNewModelImportResult>
    > ImportNewModelGltfFileWithPlanAsync(
      string sourcePath,
      GltfImportPlan plan,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
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
        GltfPackageKind.Gltf,
        profile
      );
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
          plan.NewModelOptions!
        );
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
      GltfNewModelImportOptions options
    )
    {
      var parsed = GlbDocument.ParseSeparateNewModel(package.Json, package.Binary, profile);
      GlbDocument.ValidateSeparate(package.Json, package.Binary, package.BufferUri, package.Images);
      ValidateGeometryProfile(parsed, profile);
      return ImportNewModelParsed(parsed, profile, cancellationToken, options);
    }

    /// <summary>Strictly validates one supported GLB without materializing MSH output.</summary>
    public async Task<OperationResult> ValidateGlbAsync(
      Stream source,
      GltfOperationProfile? profile = null,
      CancellationToken cancellationToken = default
    )
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }

      profile ??= GltfOperationProfile.Default;
      try
      {
        var bytes = await ReadBoundedAsync(source, profile.MaxInputBytes, cancellationToken)
          .ConfigureAwait(false);
        var jsonLength =
          bytes.Length >= 20
            ? checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12, sizeof(uint))))
            : 0;
        if (
          jsonLength > 0
          && 20 + jsonLength <= bytes.Length
          && DynamicGltfDocument.HasDynamicManifest(
            bytes.AsMemory(20, jsonLength),
            profile.MaxJsonDepth
          )
        )
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
            metadataConflicts.Select(conflict => ToDiagnostic(conflict))
          );
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
      CancellationToken cancellationToken = default
    )
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
            profile
          );
          return new OperationResult(OperationStatus.Succeeded);
        }
        var parsed = GlbDocument.ParseSeparate(package.Json, package.Binary, profile);
        ValidateGeometryProfile(parsed, profile);
        GlbDocument.ValidateSeparate(
          package.Json,
          package.Binary,
          package.BufferUri,
          package.Images
        );
        var metadataConflicts = parsed.MetadataConflicts.Build();
        if (metadataConflicts.Count != 0)
        {
          return new OperationResult(
            OperationStatus.Failed,
            metadataConflicts.Select(conflict => ToDiagnostic(conflict, sourcePath))
          );
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
      CancellationToken cancellationToken
    )
    {
      await using var jsonStream = new FileStream(
        sourcePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        81920,
        true
      );
      var json = await ReadBoundedAsync(jsonStream, profile.MaxInputBytes, cancellationToken)
        .ConfigureAwait(false);
      var bufferUri = GlbDocument.GetSeparateBufferUri(json, profile);
      var directory =
        Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Directory.GetCurrentDirectory();
      var bufferPath = ResolveContainedSidecar(directory, bufferUri);
      EnsureRegularSidecar(bufferPath);
      await using var binaryStream = new FileStream(
        bufferPath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        81920,
        true
      );
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
        if (
          !imageUri.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
          && !imageUri.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
          && !imageUri.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
        )
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
          true
        );
        EnsureRegularSidecar(imagePath);
        var image = await ReadBoundedAsync(imageStream, remaining, cancellationToken)
          .ConfigureAwait(false);
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
        IReadOnlyDictionary<string, byte[]> images
      )
      {
        Json = json;
        Binary = binary;
        BufferUri = bufferUri;
        Images = images;
      }
    }

    private static string ResolveContainedSidecar(string directory, string relativeUri)
    {
      if (
        string.IsNullOrWhiteSpace(relativeUri)
        || relativeUri.IndexOfAny(new[] { '\\', '?', '#' }) >= 0
        || Uri.TryCreate(relativeUri, UriKind.Absolute, out _)
      )
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
        if (
          Directory.Exists(current)
          && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0
        )
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
      GltfNewModelImportOptions options,
      Guid? creationGuid = null,
      bool allowAuthoringMetadata = false,
      IReadOnlyDictionary<int, CannonAuthoringValues>? cannonValues = null
    )
    {
      cancellationToken.ThrowIfCancellationRequested();
      var sceneLightDiagnostics = CreateIgnoredSceneLightDiagnostics(parsed);
      var lightIntensityDiagnostics = CreateIgnoredNewModelLightIntensityDiagnostics(
        parsed,
        options
      );
      var animationDiagnostics = CreateIgnoredNewModelAnimationDiagnostics(parsed);
      var inertDiagnostics = CreateIgnoredInertDataDiagnostics(parsed)
        .Concat(CreateIgnoredSceneNodeDiagnostics(parsed))
        .ToArray();
      var texBindingDiagnostics = CreateNewModelTexBindingDiagnostics(parsed, options);
      if (parsed.HasReservedMetadata && !allowAuthoringMetadata)
      {
        return Failed<GltfNewModelImportResult>(
          Diagnostic(
            GltfDiagnosticCodes.OrphanEnvelope,
            2011,
            "$",
            "New-model import requires input without reserved EarthTool metadata."
          )
        );
      }

      ValidateNewModelMaterialBindings(parsed, options);
      long serializedLength;
      try
      {
        serializedLength =
          EarthTool.MSH.Internal.MshCanonicalSerializer.GetCanonicalStaticSerializedLength(
            parsed
              .Nodes.Where(node => node.MeshIndex.HasValue)
              .SelectMany(node => parsed.Meshes[node.MeshIndex!.Value].Primitives)
              .Select(primitive => (primitive.Vertices.Count, primitive.Triangles.Count))
          );
        serializedLength = checked(
          serializedLength
          + parsed
            .Nodes.Where(node => node.MeshIndex.HasValue)
            .SelectMany(node => parsed.Meshes[node.MeshIndex!.Value].Primitives)
            .Sum(primitive =>
              primitive.MaterialIndex.HasValue
              && options.TextureResourceBindings.TryGetValue(
                GetMaterialHandle(parsed, primitive.MaterialIndex.Value),
                out var binding
              )
                ? binding?.Length ?? 0
                : 0
            )
        );
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
        emitterOwnership
      );
      var effectivePositions = CreateEffectivePositions(
        draft,
        source =>
          source.Source.RenderObjects.SelectMany(renderObject =>
            renderObject.RenderVertices.Select(vertex => vertex.Position)
          ),
        source => source.Pivot,
        source => source.Children
      );
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
          return Failed<GltfNewModelImportResult>(
            InvalidGeometry(
              "CommonBaseHeader.Footprint",
              $"The derived occupied top elevation {maximumZ:R} is outside the representable range 0..{ushort.MaxValue / 256f:R}."
            )
          );
        }
        var elevations = new float[16];
        elevations[15] = maximumZ;
        footprint = new CanonicalStaticFootprint(0x8000, elevations, new byte[16]);
      }
      var horizontalExtents = options.HorizontalExtents?.ToCanonical();
      if (
        horizontalExtents is null
        && !TryCreateHorizontalExtents(
          effectivePositions,
          out horizontalExtents,
          out var rangeFailure
        )
      )
      {
        return Failed<GltfNewModelImportResult>(
          InvalidGeometry("CommonBaseHeader.HorizontalExtents", rangeFailure!)
        );
      }
      var lineage = Guid.NewGuid();
      var pivots = new Dictionary<CanonicalStaticRenderObject, System.Numerics.Vector3>();
      AddNewModelPivots(draft, pivots);
      var animationInputs = new Dictionary<
        CanonicalStaticRenderObject,
        StaticAnimationReplacement
      >();
      AddNewModelAnimations(animations, draft, animationInputs);
      var attachmentRecords = new Dictionary<int, CanonicalAttachmentRecord>();
      var cannonRenderPositions = new Dictionary<int, CanonicalCannonRenderPosition>();
      var staticSpotLights = new Dictionary<int, CanonicalSpotLight>();
      var staticOmniLights = new Dictionary<int, CanonicalOmniLight>();
      AddNewModelBaseHeaderArtistObjects(
        parsed,
        options,
        emitterOwnership,
        cannonValues,
        attachmentRecords,
        cannonRenderPositions,
        staticSpotLights,
        staticOmniLights
      );
      var resourceBindings = new Dictionary<CanonicalStaticRenderObject, string?>();
      AddNewModelResourceBindings(draft, resourceBindings);
      var renderObjectOrdinals = CanonicalStaticRenderObjectSequenceEncoder
        .GetCanonicalSequence(draft.Source)
        .Select((renderObject, ordinal) => (renderObject, ordinal))
        .ToDictionary(item => item.renderObject, item => item.ordinal);
      var committed = CanonicalStaticMeshAssembler.Assemble(
        new CanonicalStaticMeshAssemblyInput(
          creationGuid ?? Guid.NewGuid(),
          new CanonicalStaticBaseHeaderInput(
            animations.Lengths,
            resourceBindings.Keys.SelectMany(record => record.RenderVertices),
            footprint,
            horizontalExtents,
            attachmentRecords: attachmentRecords,
            cannonRenderPositions: cannonRenderPositions,
            staticSpotLights: staticSpotLights,
            staticOmniLights: staticOmniLights
          ),
          draft.Source,
          pivots.ToDictionary(item => renderObjectOrdinals[item.Key], item => item.Value),
          animationInputs.ToDictionary(item => renderObjectOrdinals[item.Key], item => item.Value),
          resourceBindings.ToDictionary(
            item => renderObjectOrdinals[item.Key],
            item => item.Value
          )
        ),
        new MshOperationProfile(
          maxOutputBytes: profile.MaxOutputBytes,
          maxStaticVerticesPerObject: profile.MaxActiveRenderVertices,
          maxStaticHierarchyDepth: profile.MaxHierarchyDepth
        )
      );
      if (!committed.TryGetValue(out var authored))
      {
        return new OperationResult<GltfNewModelImportResult>(
          OperationStatus.Failed,
          diagnostics: committed.Diagnostics.Select(ToGltfAuthoringDiagnostic)
        );
      }

      var baseline = new InterchangeBaseline(lineage, Guid.NewGuid());
      return new OperationResult<GltfNewModelImportResult>(
        OperationStatus.Succeeded,
        new GltfNewModelImportResult(
          authored,
          baseline,
          CreateNewModelPreservationReport(authored)
        ),
        sceneLightDiagnostics
          .Concat(lightIntensityDiagnostics)
          .Concat(animationDiagnostics)
          .Concat(inertDiagnostics)
          .Concat(texBindingDiagnostics)
          .Concat(committed.Diagnostics)
      );
    }

    private static void ValidateNewModelMaterialBindings(
      ParsedGlb parsed,
      GltfNewModelImportOptions options
    )
    {
      var usedMaterialIndices = parsed
        .Meshes.SelectMany(mesh => mesh.Primitives)
        .Where(primitive => primitive.MaterialIndex.HasValue)
        .Select(primitive => primitive.MaterialIndex!.Value)
        .Distinct()
        .ToArray();
      foreach (
        var materialIndex in usedMaterialIndices.Where(index =>
          parsed.Materials[index].HasBaseColorTexture
        )
      )
      {
        var materialHandle = GetMaterialHandle(parsed, materialIndex);
        if (
          !options.TextureResourceBindings.TryGetValue(materialHandle, out var binding)
          || binding is null
        )
        {
          throw new RequiredTextureResourceBindingException(materialIndex, materialHandle);
        }
      }
      if (
        parsed
          .Meshes.SelectMany(mesh => mesh.Primitives)
          .Any(primitive =>
            primitive.MaterialIndex.HasValue
            && parsed.Materials[primitive.MaterialIndex.Value].HasBaseColorTexture
            && !primitive.HasTextureCoordinate
          )
      )
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
      GltfNewModelImportOptions options
    )
    {
      for (var nodeIndex = 0; nodeIndex < parsed.Nodes.Count; nodeIndex++)
      {
        if (
          GlbDocument.TryParseStaticLightHelperName(parsed.Nodes[nodeIndex].Name, out _, out _)
          && (
            !parsed.Nodes[nodeIndex].LightIndex.HasValue
            || parsed.Nodes[nodeIndex].MeshIndex.HasValue
            || parsed.Nodes[nodeIndex].CameraIndex.HasValue
            || parsed.Nodes[nodeIndex].Children.Count != 0
          )
        )
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
        if (
          !lightIndex.HasValue
          || !parsed
            .Nodes.Select((node, index) => (node, index))
            .Any(item =>
              item.node.LightIndex == lightIndex.Value
              && GlbDocument.TryParseStaticLightHelperName(item.node.Name, out _, out _)
            )
        )
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
        if (
          GlbDocument.TryParseAttachmentHelperName(
            parsed.Nodes[nodeIndex].Name,
            out var physicalNumber
          ) && physicalNumber is >= 5 and <= 8
        )
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
          $"nodes[{duplicate.Value[0]}]"
        );
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
            $"nodes[{emitterNode}]"
          );
        }
        emitterNodes.Add(emitterNode);
        var current = parentIndices[emitterNode];
        while (current >= 0 && !parsed.Nodes[current].MeshIndex.HasValue)
        {
          if (parsed.Nodes[current].LightIndex.HasValue
            || parsed.Nodes[current].CameraIndex.HasValue)
          {
            throw new UnsupportedGltfDomainException(
              "EmitterMarkerHierarchy",
              $"nodes[{emitterNode}]"
            );
          }
          scaffoldingNodes.Add(current);
          current = parentIndices[current];
        }
        if (current < 0)
        {
          throw new UnsupportedGltfDomainException(
            "EmitterMarkerHierarchy",
            $"nodes[{emitterNode}]"
          );
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
      EmitterOwnershipPlan emitterOwnership
    )
    {
      var roots = CreateNewModelSources(
        parsed.RootNodeIndex,
        System.Numerics.Matrix4x4.Identity,
        parsed,
        options,
        animatedSourceNodes,
        emitterOwnership
      );
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
      EmitterOwnershipPlan emitterOwnership
    )
    {
      var node = parsed.Nodes[nodeIndex];
      var effectiveTransform = node.LocalTransform * inheritedLinearTransform;
      if (!node.MeshIndex.HasValue)
      {
        var claimedArtistObject =
          GlbDocument.TryParseAttachmentHelperName(node.Name, out _)
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
        var collapsed = node
          .Children.SelectMany(child =>
            CreateNewModelSources(
              child,
              effectiveTransform,
              parsed,
              options,
              animatedSourceNodes,
              emitterOwnership
            )
          )
          .ToArray();
        if (collapsed.Length == 0 && !emitterOwnership.ScaffoldingNodeIndices.Contains(nodeIndex))
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
      var renderObjects = new List<CanonicalStaticRenderObject>();
      var resourceBindings = new Dictionary<CanonicalStaticRenderObject, string?>();
      foreach (var primitive in mesh.Primitives)
      {
        var renderObject = new CanonicalStaticRenderObject(
          primitive.Vertices.Select(vertex =>
            TransformNewModelVertex(vertex, authoredLinearTransform, normalTransform)
          ),
          primitive.Triangles.Select(triangle =>
            reverseWinding
              ? new CanonicalTriangle(triangle.Vertex0, triangle.Vertex2, triangle.Vertex1)
              : new CanonicalTriangle(triangle.Vertex0, triangle.Vertex1, triangle.Vertex2)
          )
        );
        renderObjects.Add(renderObject);
        resourceBindings.Add(
          renderObject,
          primitive.MaterialIndex.HasValue
          && options.TextureResourceBindings.TryGetValue(
            GetMaterialHandle(parsed, primitive.MaterialIndex.Value),
            out var textureResourceKey
          )
            ? textureResourceKey
            : null
        );
      }
      var children = node
        .Children.SelectMany(child =>
          CreateNewModelSources(
            child,
            animatedSourceNodes.Contains(nodeIndex)
              ? System.Numerics.Matrix4x4.Identity
              : linearTransform,
            parsed,
            options,
            animatedSourceNodes,
            emitterOwnership
          )
        )
        .ToArray();
      var pivot = new System.Numerics.Vector3(translation.X, -translation.Z, translation.Y);
      var typedRole = options.ObjectRoles.TryGetValue(
        GetNodeHandle(parsed, nodeIndex),
        out var role
      )
        ? role.ToCanonical()
        : null;
      var inferredMarkerFlags = emitterOwnership.MarkerFlagsBySourceNode.TryGetValue(
        nodeIndex,
        out var markers
      )
        ? markers
        : StaticRenderObjectFlags.None;
      var canonicalRole =
        typedRole is null && inferredMarkerFlags == StaticRenderObjectFlags.None
          ? null
          : new CanonicalStaticObjectRole(
            (typedRole?.Flags ?? StaticRenderObjectFlags.None) | inferredMarkerFlags,
            typedRole?.BarrelMaximumAngle ?? 0
          );
      return new[]
      {
        new NewModelSourceDraft(
          nodeIndex,
          new CanonicalStaticSourceObject(
            renderObjects,
            children.Select(child => child.Source),
            canonicalRole
          ),
          pivot,
          resourceBindings,
          children
        ),
      };
    }

    private static void AddNewModelBaseHeaderArtistObjects(
      ParsedGlb parsed,
      GltfNewModelImportOptions options,
      EmitterOwnershipPlan emitterOwnership,
      IReadOnlyDictionary<int, CannonAuthoringValues>? cannonValues,
      IDictionary<int, CanonicalAttachmentRecord> attachmentRecords,
      IDictionary<int, CanonicalCannonRenderPosition> cannonRenderPositions,
      IDictionary<int, CanonicalSpotLight> staticSpotLights,
      IDictionary<int, CanonicalOmniLight> staticOmniLights
    )
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
          nodes
        );
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
            throw ArtistObjectConflict(
              "A cannon render-position target is occupied more than once."
            );
          }
        }
        else if (
          node.LightIndex.HasValue
          && GlbDocument.TryParseStaticLightHelperName(node.Name, out var type, out physicalNumber)
        )
        {
          if (!lights.TryAdd((type, physicalNumber), index))
          {
            throw ArtistObjectConflict(
              "A static-light target is occupied more than once.",
              $"nodes[{index}]"
            );
          }
        }
      }
      foreach (var attachment in attachments)
      {
        attachmentRecords[attachment.Key] = CreateCanonicalAttachmentRecord(
          transforms[attachment.Value],
          0x80
        );
      }
      foreach (var cannon in cannons)
      {
        var yawHalfRange = cannonValues is not null
          && cannonValues.TryGetValue(cannon.Key, out var cannonAuthoringValues)
            ? cannonAuthoringValues.YawHalfRange
            : (byte)0x80;
        attachmentRecords[cannon.Key] = CreateCanonicalAttachmentRecord(
          transforms[cannon.Value],
          yawHalfRange
        );
        cannonRenderPositions.Add(
          cannon.Key,
          CreateCanonicalCannonRenderPosition(transforms[cannon.Value].Translation)
        );
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
        if (
          node.LightIndex is null
          || node.LightIndex.Value < 0
          || node.LightIndex.Value >= parsed.Lights.Count
          || parsed.Lights[node.LightIndex.Value].Type != item.Key.Type
        )
        {
          throw new UnsupportedGltfDomainException(
            "StaticLights",
            $"nodes[{item.Value}].extensions.KHR_lights_punctual"
          );
        }
        if (definitionReferenceCounts[node.LightIndex.Value] != 1)
        {
          throw ArtistObjectConflict(
            "A static-light artist object must own an unshared punctual-light definition.",
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}]"
          );
        }
        var lightOptions = options.StaticLightOptions.TryGetValue(
          GetLightHandle(parsed, node.LightIndex.Value),
          out var explicitLightOptions
        )
          ? explicitLightOptions
          : null;
        var range = parsed.Lights[node.LightIndex.Value].Range;
        if (
          item.Key.Type == "spot"
          && range.HasValue
          && lightOptions?.TargetDistance.HasValue == true
        )
        {
          throw new UnsupportedGltfDomainException(
            "StaticLights",
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}].range"
          );
        }
        var targetDistance = range ?? lightOptions?.TargetDistance;
        if (
          item.Key.Type == "spot"
          && (targetDistance is not > 0 || !float.IsFinite(targetDistance.Value))
        )
        {
          throw new UnsupportedGltfDomainException(
            "StaticLights",
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}].range"
          );
        }
        if (!usedLightDefinitions.Add(node.LightIndex.Value))
        {
          throw ArtistObjectConflict(
            "A new-model static-light definition cannot be shared.",
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}]"
          );
        }
        var hasCanonicalNodeName = GlbDocument.TryParseStaticLightHelperName(
          node.Name,
          out _,
          out _
        );
        var hasCanonicalDefinitionName = GlbDocument.TryParseStaticLightHelperName(
          parsed.Lights[node.LightIndex.Value].Name,
          out var definitionType,
          out var definitionNumber
        );
        if (
          hasCanonicalNodeName && !hasCanonicalDefinitionName
          || hasCanonicalDefinitionName
            && (definitionType != item.Key.Type || definitionNumber != item.Key.Number)
        )
        {
          throw new StaticLightMetadataException(
            $"extensions.KHR_lights_punctual.lights[{node.LightIndex.Value}].name",
            "The canonical static-light instance and definition names contradict each other."
          );
        }
        var attachmentNumber =
          item.Key.Type == "spot" ? item.Key.Number + 12 : item.Key.Number + 16;
        attachmentRecords[attachmentNumber] = CreateCanonicalStaticLightAttachmentRecord(
          transforms[item.Value].Translation,
          $"nodes[{item.Value}].translation"
        );
        if (item.Key.Type == "spot")
        {
          staticSpotLights.Add(
            item.Key.Number,
            CreateCanonicalSpotLight(
              parsed.Lights[node.LightIndex.Value],
              transforms[item.Value],
              $"nodes[{item.Value}]",
              lightOptions
            )
          );
        }
        else
        {
          staticOmniLights.Add(
            item.Key.Number,
            CreateCanonicalOmniLight(
              parsed.Lights[node.LightIndex.Value],
              transforms[item.Value],
              $"nodes[{item.Value}]",
              lightOptions
            )
          );
        }
      }
    }

    private static CanonicalStaticVertex TransformNewModelVertex(
      RenderVertex vertex,
      System.Numerics.Matrix4x4 linearTransform,
      System.Numerics.Matrix4x4 normalTransform
    )
    {
      var position = System.Numerics.Vector3.Transform(vertex.Position, linearTransform);
      var normal = System.Numerics.Vector3.TransformNormal(vertex.Normal, normalTransform);
      var normalLengthSquared = normal.LengthSquared();
      if (
        !IsFinite(position)
        || !IsFinite(normal)
        || !float.IsFinite(normalLengthSquared)
        || normalLengthSquared == 0
      )
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
        vertex.TextureCoordinate
      );
    }

    private static void AddNewModelPivots(
      NewModelSourceDraft draft,
      IDictionary<CanonicalStaticRenderObject, System.Numerics.Vector3> pivots
    )
    {
      if (draft.Pivot != System.Numerics.Vector3.Zero)
      {
        pivots.Add(draft.Source.RenderObjects[0], draft.Pivot);
      }
      foreach (var child in draft.Children)
      {
        AddNewModelPivots(child, pivots);
      }
    }

    private static NewModelAnimationSet CreateNewModelAnimations(
      ParsedGlb parsed,
      long serializedLength,
      int maximumOutputLength
    )
    {
      var authoredAnimations = parsed
        .Animations.Select((animation, index) => (animation, index))
        .Where(item => TryGetCanonicalAnimationClass(item.animation.Name, out _))
        .ToArray();
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
        if (
          animation.Objects.Count == 0
          || !TryGetCanonicalAnimationFrameCount(animation, out var frameCount)
        )
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
          paths
        );
        if (
          !consumedTargets
            .OrderBy(value => value)
            .SequenceEqual(animatedObjects.Keys.OrderBy(value => value))
        )
        {
          throw new UnsupportedGltfDomainException("animations");
        }
        serializedLength = checked(
          serializedLength + ((long)paths.Count * frameCount * (12 + 12 + 64))
        );
        if (serializedLength > maximumOutputLength)
        {
          throw new ResourceLimitException(serializedLength, maximumOutputLength);
        }
        var sampledByNode = animatedObjects.ToDictionary(
          item => item.Key,
          item => item.Value.SampleFrames(frameCount)
        );
        foreach (var path in paths)
        {
          tracks.Add(
            new NewModelAnimationTrack(
              path.NodeIndex,
              classIndex,
              ComposeNewModelAnimationFrames(path.Path, parsed, sampledByNode, frameCount)
            )
          );
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
        tracks.AsReadOnly()
      );
    }

    private static void CollectNewModelAnimationPaths(
      int nodeIndex,
      IReadOnlyList<int> collapsedPath,
      ParsedGlb parsed,
      ISet<int> animatedNodes,
      ISet<int> consumedTargets,
      ICollection<(int NodeIndex, IReadOnlyList<int> Path)> paths
    )
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
            paths
          );
        }
        return;
      }

      foreach (var child in node.Children)
      {
        CollectNewModelAnimationPaths(child, path, parsed, animatedNodes, consumedTargets, paths);
      }
    }

    private static IReadOnlyList<ProjectedAnimationFrame> ComposeNewModelAnimationFrames(
      IReadOnlyList<int> path,
      ParsedGlb parsed,
      IReadOnlyDictionary<int, IReadOnlyList<ProjectedAnimationFrame>> sampledByNode,
      int frameCount
    )
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

    private static void AddNewModelAnimations(
      NewModelAnimationSet animations,
      NewModelSourceDraft draft,
      IDictionary<CanonicalStaticRenderObject, StaticAnimationReplacement> animationInputs
    )
    {
      if (animations.Tracks.Count == 0)
      {
        return;
      }

      var sources = new Dictionary<int, CanonicalStaticSourceObject>();
      AddNewModelSources(draft, sources);
      foreach (var animation in animations.Tracks)
      {
        var tracks = StaticAnimationProjection.CreateCanonicalTracks(animation.Frames);
        animationInputs.Add(
          sources[animation.NodeIndex].RenderObjects[0],
          new StaticAnimationReplacement(tracks, checked((uint)animation.ClassIndex))
        );
      }
    }

    private static void AddNewModelSources(
      NewModelSourceDraft draft,
      IDictionary<int, CanonicalStaticSourceObject> result
    )
    {
      result.Add(draft.NodeIndex, draft.Source);
      foreach (var child in draft.Children)
      {
        AddNewModelSources(child, result);
      }
    }

    private static void AddNewModelResourceBindings(
      NewModelSourceDraft draft,
      IDictionary<CanonicalStaticRenderObject, string?> resourceBindings
    )
    {
      foreach (var binding in draft.TextureResourceBindings)
      {
        resourceBindings.Add(binding.Key, binding.Value);
      }
      foreach (var child in draft.Children)
      {
        AddNewModelResourceBindings(child, resourceBindings);
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
        Canonicalized("RootTrailingBytes"),
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
        diagnostic.Data
      );
    }

    private sealed class NewModelAnimationSet
    {
      internal AnimationClassBytes Lengths { get; }

      internal IReadOnlyList<NewModelAnimationTrack> Tracks { get; }

      internal ISet<int> AnimatedSourceNodes { get; }

      internal NewModelAnimationSet(
        AnimationClassBytes lengths,
        IReadOnlyList<NewModelAnimationTrack> tracks
      )
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
        IReadOnlyList<ProjectedAnimationFrame> frames
      )
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

      internal IReadOnlyDictionary<
        CanonicalStaticRenderObject,
        string?
      > TextureResourceBindings
      { get; }

      internal IReadOnlyList<NewModelSourceDraft> Children { get; }

      internal NewModelSourceDraft(
        int nodeIndex,
        CanonicalStaticSourceObject source,
        System.Numerics.Vector3 pivot,
        IReadOnlyDictionary<CanonicalStaticRenderObject, string?> textureResourceBindings,
        IReadOnlyList<NewModelSourceDraft> children
      )
      {
        NodeIndex = nodeIndex;
        Source = source;
        Pivot = pivot;
        TextureResourceBindings = textureResourceBindings;
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
        ISet<int> scaffoldingNodeIndices
      )
      {
        MarkerFlagsBySourceNode = markerFlagsBySourceNode;
        EmitterNodeIndices = emitterNodeIndices;
        ScaffoldingNodeIndices = scaffoldingNodeIndices;
      }
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredSceneLightDiagnostics(
      ParsedGlb parsed
    )
    {
      return parsed
        .Nodes.Select((node, index) => (node, index))
        .Where(item =>
          item.node.LightIndex.HasValue
          && item.node.Metadata is null
          && !GlbDocument.TryParseStaticLightHelperName(item.node.Name, out _, out _)
        )
        .Select(item => new OperationDiagnostic(
          GltfDiagnosticCodes.SceneLightIgnored,
          1118,
          DiagnosticSeverity.Warning,
          $"nodes[{item.index}]",
          "An untagged noncanonical punctual light remains scene-only artist lighting."
        ))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredNewModelLightIntensityDiagnostics(
      ParsedGlb parsed,
      GltfNewModelImportOptions options
    )
    {
      return parsed
        .Nodes.Select(node => node.LightIndex)
        .OfType<int>()
        .Where(index => index >= 0 && index < parsed.Lights.Count)
        .Where(index =>
          parsed
            .Nodes.Select((node, nodeIndex) => (node, nodeIndex))
            .Any(item =>
              item.node.LightIndex == index
              && GlbDocument.TryParseStaticLightHelperName(item.node.Name, out _, out _)
            )
        )
        .Distinct()
        .Where(index => parsed.Lights[index].Intensity != 1)
        .Select(index => new OperationDiagnostic(
          GltfDiagnosticCodes.NewModelPhotometricIntensityIgnored,
          1120,
          DiagnosticSeverity.Warning,
          $"extensions.KHR_lights_punctual.lights[{index}].intensity",
          "New-model photometric intensity was not used as terrain-light amplitude."
        ))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredNewModelAnimationDiagnostics(
      ParsedGlb parsed
    )
    {
      return parsed
        .Animations.Select((animation, index) => (animation, index))
        .Where(item => !TryGetCanonicalAnimationClass(item.animation.Name, out _))
        .Select(item => new OperationDiagnostic(
          GltfDiagnosticCodes.InertDataIgnored,
          1119,
          DiagnosticSeverity.Warning,
          $"animations[{item.index}]",
          "A noncanonical metadata-free animation remains scene-only and was ignored."
        ))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredInertDataDiagnostics(
      ParsedGlb parsed
    )
    {
      return parsed
        .IgnoredInertPaths.Select(path => new OperationDiagnostic(
          GltfDiagnosticCodes.InertDataIgnored,
          1119,
          DiagnosticSeverity.Warning,
          path,
          "Inert native glTF data was excluded from canonical MSH state."
        ))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateIgnoredSceneNodeDiagnostics(
      ParsedGlb parsed
    )
    {
      return GetNodeOrder(parsed)
        .Where(nodeIndex =>
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
          "An unknown metadata-free empty leaf node remains scene-only and was ignored."
        ))
        .ToArray();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateDynamicPlacementDiagnostics(
      DynamicGltfImport imported
    )
    {
      return imported.PlacementDataIgnored
        ? new[]
        {
          new OperationDiagnostic(
            GltfDiagnosticCodes.InertDataIgnored,
            1119,
            DiagnosticSeverity.Warning,
            $"nodes[{imported.PlacementRootIndex}]",
            "Placement-root transforms and animation remain scene-only and were excluded from canonical MSH state."
          ),
        }
        : Array.Empty<OperationDiagnostic>();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateNewModelTexBindingDiagnostics(
      ParsedGlb parsed,
      GltfNewModelImportOptions options
    )
    {
      return options
        .TextureResourceBindings.Where(binding => binding.Value is not null)
        .Select(binding => (binding, MaterialIndex: GetMaterialIndex(parsed, binding.Key)))
        .Where(item =>
          item.MaterialIndex.HasValue
          && !parsed.Materials[item.MaterialIndex.Value].HasBaseColorTexture
        )
        .Select(item => new OperationDiagnostic(
          GltfDiagnosticCodes.TextureResourceMissing,
          1107,
          DiagnosticSeverity.Warning,
          $"materials[{item.MaterialIndex!.Value}]",
          "The explicit TEX resource binding has no decoded native preview and remains reference-only."
        ))
        .ToArray();
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
      GltfOperationProfile profile
    )
    {
      if (asset.StaticRenderObjectSequence.Count == 0)
      {
        return Unsupported("StaticRenderObjectSequence");
      }

      for (
        var renderObjectIndex = 0;
        renderObjectIndex < asset.StaticRenderObjectSequence.Count;
        renderObjectIndex++
      )
      {
        var renderObject = asset.StaticRenderObjectSequence[renderObjectIndex];
        if (
          renderObject.RenderVertices.Count == 0
          || renderObject.RenderVertices.Count > profile.MaxActiveRenderVertices
          || renderObject.Triangles.Count == 0
        )
        {
          return renderObject.RenderVertices.Count > profile.MaxActiveRenderVertices
            ? Limit(
              $"StaticRenderObjectSequence[{renderObjectIndex}].RenderVertices",
              renderObject.RenderVertices.Count,
              profile.MaxActiveRenderVertices
            )
            : InvalidGeometry(
              $"StaticRenderObjectSequence[{renderObjectIndex}]",
              "Static geometry must contain vertices and triangles."
            );
        }

        if (
          renderObject.RenderVertices.Any(vertex =>
            !IsFinite(vertex.Position) || !IsFinite(vertex.Normal)
          )
        )
        {
          return InvalidGeometry(
            $"StaticRenderObjectSequence[{renderObjectIndex}].RenderVertices",
            "Static geometry positions and normals must be finite."
          );
        }

        if (
          renderObject.Triangles.Any(triangle =>
            triangle.Vertex0 >= renderObject.RenderVertices.Count
            || triangle.Vertex1 >= renderObject.RenderVertices.Count
            || triangle.Vertex2 >= renderObject.RenderVertices.Count
          )
        )
        {
          return InvalidGeometry(
            $"StaticRenderObjectSequence[{renderObjectIndex}].Triangles",
            "Triangle index is outside the active render-vertex range."
          );
        }

        if (
          renderObject.RenderVertices.Any(vertex =>
            !IsFinite(vertex.TextureCoordinate.X) || !IsFinite(vertex.TextureCoordinate.Y)
          )
        )
        {
          return InvalidGeometry(
            $"StaticRenderObjectSequence[{renderObjectIndex}].RenderVertices",
            "Static geometry texture coordinates must be finite."
          );
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
            profile.MaxActiveRenderVertices
          );
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

    private static bool TryGetCanonicalAnimationFrameCount(
      ParsedGltfAnimation animation,
      out int frameCount
    )
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
        _ => -1,
      };
      return classIndex >= 0;
    }

    private static IReadOnlyList<System.Numerics.Vector3> CreateEffectivePositions<TSource>(
      TSource root,
      Func<TSource, IEnumerable<System.Numerics.Vector3>> getPositions,
      Func<TSource, System.Numerics.Vector3> getPivot,
      Func<TSource, IEnumerable<TSource>> getChildren
    )
    {
      var positions = new List<System.Numerics.Vector3>();
      AddEffectivePositions(
        root,
        System.Numerics.Vector3.Zero,
        true,
        getPositions,
        getPivot,
        getChildren,
        positions
      );
      return positions;
    }

    private static void AddEffectivePositions<TSource>(
      TSource source,
      System.Numerics.Vector3 parentOffset,
      bool root,
      Func<TSource, IEnumerable<System.Numerics.Vector3>> getPositions,
      Func<TSource, System.Numerics.Vector3> getPivot,
      Func<TSource, IEnumerable<TSource>> getChildren,
      ICollection<System.Numerics.Vector3> positions
    )
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
      out string? rangeFailure
    )
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
        (Axis: "-X", Value: negativeX),
      };
      foreach (var value in values)
      {
        if (!float.IsFinite(value.Value) || value.Value * 256d > ushort.MaxValue)
        {
          horizontalExtents = null;
          rangeFailure =
            $"The derived {value.Axis} horizontal extent {value.Value:R} exceeds the representable maximum {ushort.MaxValue / 256f:R}.";
          return false;
        }
      }
      horizontalExtents = new CanonicalHorizontalExtents(
        positiveY,
        negativeY,
        positiveX,
        negativeX
      );
      rangeFailure = null;
      return true;
    }

    private static float ReadSingle(byte[] source, int offset)
    {
      return BitConverter.Int32BitsToSingle(
        BinaryPrimitives.ReadInt32LittleEndian(source.AsSpan(offset))
      );
    }

    private static (byte Heading, float VerticalTargetSlope) ReadStaticLightDirection(
      Matrix4x4 transform,
      string path
    )
    {
      if (!Matrix4x4.Decompose(transform, out _, out var rotation, out _) || !IsFinite(rotation))
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
      return (
        unchecked((byte)((int)MathF.Floor((heading * 256 / (MathF.PI * 2)) + 0.5f) & 0xFF)),
        direction.Y / horizontalLength
      );
    }

    private static CanonicalAttachmentRecord CreateCanonicalStaticLightAttachmentRecord(
      Vector3 translation,
      string path
    )
    {
      if (!IsFinite(translation))
      {
        throw new UnsupportedGltfDomainException("StaticLightPose", path);
      }
      var record = new CanonicalAttachmentRecord(
        new Vector3(translation.X, translation.Z, translation.Y),
        0,
        0
      );
      try
      {
        CanonicalBaseHeaderEncoder.EncodeAttachmentRecord(record);
      }
      catch (OverflowException)
      {
        throw new UnsupportedGltfDomainException("StaticLightPose", path);
      }
      return record;
    }

    private static CanonicalSpotLight CreateCanonicalSpotLight(
      ParsedGltfLight light,
      Matrix4x4 transform,
      string path,
      GltfNewModelStaticLightOptions? options
    )
    {
      ValidateStaticLight(light, transform, path);
      if (
        light.InnerConeAngle < 0
        || light.OuterConeAngle < light.InnerConeAngle
        || light.OuterConeAngle > MathF.PI / 2
      )
      {
        throw new UnsupportedGltfDomainException("StaticLightTypeConversion", path);
      }
      var distance =
        light.Range is > 0 && float.IsFinite(light.Range.Value)
          ? light.Range.Value
          : options?.TargetDistance ?? 1;
      var (heading, verticalTargetSlope) = ReadStaticLightDirection(
        transform,
        path + ".rotation"
      );
      return new CanonicalSpotLight(
        new Vector3(transform.Translation.X, transform.Translation.Z, transform.Translation.Y),
        light.Color,
        distance,
        heading,
        MathF.Tan(light.InnerConeAngle),
        light.OuterConeAngle * distance,
        verticalTargetSlope,
        options?.TerrainLightAmplitude ?? 1
      );
    }

    private static CanonicalOmniLight CreateCanonicalOmniLight(
      ParsedGltfLight light,
      Matrix4x4 transform,
      string path,
      GltfNewModelStaticLightOptions? options
    )
    {
      ValidateStaticLight(light, transform, path);
      return new CanonicalOmniLight(
        new Vector3(transform.Translation.X, transform.Translation.Z, transform.Translation.Y),
        light.Color,
        options?.TerrainLightAmplitude ?? 1
      );
    }

    private static void ValidateStaticLight(
      ParsedGltfLight light,
      Matrix4x4 transform,
      string path
    )
    {
      if (
        !IsFinite(light.Color)
        || light.Color.X < 0
        || light.Color.Y < 0
        || light.Color.Z < 0
        || !float.IsFinite(light.Intensity)
        || light.Intensity < 0
        || !IsFinite(transform.Translation)
      )
      {
        throw new UnsupportedGltfDomainException("StaticLightTypeConversion", path);
      }
    }

    private static void AddArtistCandidate(
      IDictionary<int, List<int>> candidates,
      int physicalNumber,
      int nodeIndex
    )
    {
      if (!candidates.TryGetValue(physicalNumber, out var nodes))
      {
        nodes = new List<int>();
        candidates.Add(physicalNumber, nodes);
      }
      nodes.Add(nodeIndex);
    }

    private static MetadataIdentityException ArtistObjectConflict(
      string message,
      string? path = null
    )
    {
      return new MetadataIdentityException(
        GltfDiagnosticCodes.AmbiguousPartitionCorrespondence,
        2012,
        message,
        path
      );
    }

    private static IReadOnlyDictionary<int, Matrix4x4> CreateArtistObjectTransforms(
      int rootNodeIndex,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      ISet<int>? explicitArtistObjects = null
    )
    {
      var result = new Dictionary<int, Matrix4x4>();
      AddArtistObjectTransforms(
        rootNodeIndex,
        Matrix4x4.Identity,
        nodes,
        result,
        explicitArtistObjects ?? new HashSet<int>()
      );
      return result;
    }

    private static void AddArtistObjectTransforms(
      int nodeIndex,
      Matrix4x4 inheritedTransform,
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes,
      IDictionary<int, Matrix4x4> result,
      ISet<int> explicitArtistObjects
    )
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
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes
    )
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
      IReadOnlyList<(ParsedGltfNode Parsed, MetadataEnvelope? Metadata)> nodes
    )
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
      ISet<int> explicitArtistObjects
    )
    {
      return explicitArtistObjects.Contains(nodeIndex)
        || node.Metadata?.AttachmentRecord is not null
        || node.Metadata?.CannonRenderPositionRecord is not null
        || node.Metadata?.StaticLightAttachmentRecord is not null
        || node.Metadata is null
          && (
            GlbDocument.TryParseAttachmentHelperName(node.Parsed.Name, out _)
            || GlbDocument.TryParseCannonHelperName(node.Parsed.Name, out _)
            || node.Parsed.LightIndex.HasValue
              && GlbDocument.TryParseStaticLightHelperName(node.Parsed.Name, out _, out _)
          );
    }

    private static CanonicalAttachmentRecord CreateCanonicalAttachmentRecord(
      Matrix4x4 transform,
      byte extra
    )
    {
      var (translation, heading) = ReadAttachmentTransform(transform);
      var record = new CanonicalAttachmentRecord(
        new Vector3(translation.X, translation.Z, translation.Y),
        heading,
        extra
      );
      try
      {
        CanonicalBaseHeaderEncoder.EncodeAttachmentRecord(record);
      }
      catch (OverflowException)
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }
      return record;
    }

    private static (Vector3 Translation, byte Heading) ReadAttachmentTransform(Matrix4x4 transform)
    {
      if (
        !Matrix4x4.Decompose(transform, out var scale, out var rotation, out var translation)
        || !IsFinite(scale)
        || !IsFinite(translation)
        || !IsFinite(rotation)
      )
      {
        throw new UnsupportedGltfDomainException("AttachmentPose");
      }
      var reconstructed =
        Matrix4x4.CreateScale(scale)
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

    private static CanonicalCannonRenderPosition CreateCanonicalCannonRenderPosition(
      Vector3 translation
    )
    {
      if (!IsFinite(translation))
      {
        throw new UnsupportedGltfDomainException("CannonRenderPosition");
      }
      return new CanonicalCannonRenderPosition(
        new Vector3(translation.X, translation.Z, translation.Y)
      );
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

    private static async Task<byte[]> ReadBoundedAsync(
      Stream source,
      int maximum,
      CancellationToken cancellationToken
    )
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
        var read = await source
          .ReadAsync(buffer, 0, buffer.Length, cancellationToken)
          .ConfigureAwait(false);
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
          out var catalogActions
        )
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
          data: data
        );
      }

      if (exception is MetadataIdentityException identity)
      {
        var actions =
          identity.Code == GltfDiagnosticCodes.DocumentMismatch
            ? new[]
            {
              GltfMetadataConflictActions.Abort,
              GltfMetadataConflictActions.RetryWithMetadata,
              GltfMetadataConflictActions.AcceptBranch,
            }
          : identity.Code == GltfDiagnosticCodes.AssetLineageMismatch
            ? new[]
            {
              GltfMetadataConflictActions.Abort,
              GltfMetadataConflictActions.AdoptAsNew,
              GltfMetadataConflictActions.DiscardLineage,
            }
          : new[]
          {
            GltfMetadataConflictActions.Abort,
            GltfMetadataConflictActions.MapScope,
            GltfMetadataConflictActions.ForkScope,
            GltfMetadataConflictActions.DiscardAffectedState,
          };
        return MetadataDiagnostic(
          identity.Code,
          identity.EventId,
          identity.Path ?? path,
          identity.Message,
          actions
        );
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
            : GltfMetadataConflictActions.AcceptBranch
        );
      }

      if (exception is DynamicMetadataGraphException dynamicGraph)
      {
        return MetadataDiagnostic(
          dynamicGraph.Code,
          dynamicGraph.EventId,
          dynamicGraph.Path,
          dynamicGraph.Message
        );
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
          staticLightMetadata.Message
        );
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
          GltfMetadataConflictActions.DiscardLineage
        );
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
          GltfMetadataConflictActions.DiscardLineage
        );
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
          GltfMetadataConflictActions.DiscardLineage
        );
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
              System.Globalization.CultureInfo.InvariantCulture
            ),
          }
        );
      }

      if (exception is ResourceLimitException limit)
      {
        return Limit(path, limit.Actual, limit.Maximum);
      }

      if (exception is ModelException)
      {
        return Diagnostic(
          GltfDiagnosticCodes.StrictValidationFailed,
          1103,
          path,
          exception.Message
        );
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

    private static void AddDiagnosticData(
      IDictionary<string, string> data,
      string key,
      string value
    )
    {
      if (!data.ContainsKey(key))
      {
        data.Add(key, value);
      }
    }

    private static string CreateConflictKey(
      string code,
      string path,
      IReadOnlyDictionary<string, string> data
    )
    {
      var canonical = new StringBuilder(code).Append('\n').Append(path);
      foreach (
        var item in data.Where(item => item.Key is not ("conflictKey" or "actions"))
          .OrderBy(item => item.Key, StringComparer.Ordinal)
      )
      {
        canonical.Append('\n').Append(item.Key).Append('=').Append(item.Value);
      }
      using var sha256 = SHA256.Create();
      var digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
      return "v1:"
        + Convert.ToBase64String(digest).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GetMetadataCarrierPath(string path)
    {
      var payload = path.IndexOf(".payload", StringComparison.Ordinal);
      var guards = path.IndexOf(".guards", StringComparison.Ordinal);
      var separator =
        payload < 0 ? guards
        : guards < 0 ? payload
        : Math.Min(payload, guards);
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
          ["maximum"] = maximum.ToString(System.Globalization.CultureInfo.InvariantCulture),
        }
      );
    }

    private static OperationDiagnostic Unsupported(string domain, string path = "$")
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.UnsupportedDomain,
        1102,
        DiagnosticSeverity.Error,
        path,
        $"The {domain} domain is outside the one-triangle walking-skeleton profile.",
        data: new Dictionary<string, string> { ["domain"] = domain }
      );
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
        "Decoded TEX previews were omitted to keep the package within the output limit."
      );
    }

    private static IReadOnlyList<OperationDiagnostic> CreateAnimationDiagnostics(
      StaticMeshAsset asset,
      InterchangeBaseline baseline,
      GltfStaticIdentityMap identityMap
    )
    {
      var sources = StaticSourceObjectTraversal
        .Flatten(asset.RootSourceObject)
        .ToDictionary(identityMap.GetSourceObjectId);
      var indices = asset
        .StaticRenderObjectSequence.Select((record, index) => new { record, Index = index })
        .ToDictionary(item => item.record, item => item.Index);
      var diagnostics = new List<OperationDiagnostic>();
      foreach (
        var item in StaticAnimationProjection
          .Create(asset, baseline, identityMap)
          .Objects.OrderBy(item => item.ClassIndex)
          .ThenBy(item => item.SourceObjectLocalId)
      )
      {
        var recordIndex = indices[sources[item.SourceObjectLocalId].StaticRenderObjects[0]];
        var commonData = new Dictionary<string, string>
        {
          ["sourceObject"] = item.SourceObjectLocalId.ToString(
            System.Globalization.CultureInfo.InvariantCulture
          ),
          ["animationClassValue"] = item.AnimationClassValue.ToString(
            System.Globalization.CultureInfo.InvariantCulture
          ),
          ["class"] = ((char)('A' + item.ClassIndex)).ToString(),
        };
        if (item.AnimationClassValue > 3)
        {
          diagnostics.Add(
            new OperationDiagnostic(
              GltfDiagnosticCodes.AnimationClassUnrecognized,
              1115,
              DiagnosticSeverity.Warning,
              $"StaticRenderObjectSequence[{recordIndex}].AnimationClassValue",
              "An unrecognized animation-class value uses its modulo-four class for native projection.",
              data: commonData
            )
          );
        }
        if (item.FailureFrame.HasValue)
        {
          var metadataOnlyData = new Dictionary<string, string>(commonData)
          {
            ["frame"] = item.FailureFrame!.Value.ToString(
              System.Globalization.CultureInfo.InvariantCulture
            ),
          };
          diagnostics.Add(
            new OperationDiagnostic(
              GltfDiagnosticCodes.AnimationMetadataOnly,
              1114,
              DiagnosticSeverity.Warning,
              $"StaticRenderObjectSequence[{recordIndex}].AnimationTracks",
              "The source animation cannot be represented exactly as native glTF TRS and remains metadata-only.",
              data: metadataOnlyData
            )
          );
        }
      }
      return diagnostics.AsReadOnly();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateCannonRenderPositionDiagnostics(
      StaticMeshAsset asset
    )
    {
      var records = asset.CommonBaseHeader.CannonRenderPositions.ToArray();
      var attachments = asset.CommonBaseHeader.AttachmentTable.ToArray();
      var diagnostics = new List<OperationDiagnostic>();
      for (var physicalNumber = 1; physicalNumber <= 4; physicalNumber++)
      {
        if (
          BinaryPrimitives.ReadInt16LittleEndian(attachments.AsSpan((physicalNumber - 1) * 8, 8))
          == short.MinValue
        )
        {
          continue;
        }
        var record = records.AsSpan((physicalNumber - 1) * 12, 12);
        var substituted = new List<int>();
        for (var component = 0; component < 3; component++)
        {
          var value = BitConverter.Int32BitsToSingle(
            BinaryPrimitives.ReadInt32LittleEndian(record.Slice(component * 4, 4))
          );
          if (!float.IsFinite(value))
          {
            substituted.Add(component);
          }
        }
        if (substituted.Count == 0)
        {
          continue;
        }
        diagnostics.Add(
          new OperationDiagnostic(
            GltfDiagnosticCodes.CannonRenderPositionPreviewSubstituted,
            1116,
            DiagnosticSeverity.Warning,
            $"CommonBaseHeader.CannonRenderPositions[{physicalNumber}]",
            "Non-finite cannon render-position components use zero in the native preview.",
            data: new Dictionary<string, string>
            {
              ["physicalNumber"] = physicalNumber.ToString(
                System.Globalization.CultureInfo.InvariantCulture
              ),
              ["components"] = string.Join(",", substituted),
            }
          )
        );
      }
      return diagnostics.AsReadOnly();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateStaticLightDiagnostics(
      StaticMeshAsset asset
    )
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
          spots.AsSpan((physicalNumber - 1) * 0x30, 0x30)
        );
        AddStaticLightDiagnostic(
          diagnostics,
          "point",
          physicalNumber,
          attachments.AsSpan((physicalNumber + 15) * 8, 8),
          omnis.AsSpan((physicalNumber - 1) * 0x1C, 0x1C)
        );
      }
      return diagnostics.AsReadOnly();
    }

    private static IReadOnlyList<OperationDiagnostic> CreateEmitterHierarchyDiagnostics(
      StaticMeshAsset asset
    )
    {
      var diagnostics = new List<OperationDiagnostic>();
      for (var number = 1; number <= 4; number++)
      {
        var emitterPhysicalNumber = number + 4;
        var (emitterActive, markerRecordCount) = GlbDocument.GetEmitterHierarchyState(
          asset,
          number
        );
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

        diagnostics.Add(
          new OperationDiagnostic(
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
                System.Globalization.CultureInfo.InvariantCulture
              ),
              ["missing"] = string.Join(",", missing),
            }
          )
        );
      }
      return diagnostics.AsReadOnly();
    }

    private static void AddStaticLightDiagnostic(
      ICollection<OperationDiagnostic> diagnostics,
      string type,
      int physicalNumber,
      ReadOnlySpan<byte> attachment,
      ReadOnlySpan<byte> record
    )
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
        if (
          !float.IsFinite(inner)
          || !float.IsFinite(outer)
          || inner < 0
          || outer < inner
          || outer > MathF.PI / 2
        )
        {
          substituted.Add("cones");
        }
      }
      if (substituted.Count == 0)
      {
        return;
      }
      var collection = type == "spot" ? "StaticSpotLights" : "StaticOmniLights";
      diagnostics.Add(
        new OperationDiagnostic(
          GltfDiagnosticCodes.StaticLightPreviewSubstituted,
          1117,
          DiagnosticSeverity.Warning,
          $"CommonBaseHeader.{collection}[{physicalNumber}]",
          "Anomalous static-light fields use deterministic finite native preview values.",
          data: new Dictionary<string, string>
          {
            ["physicalNumber"] = physicalNumber.ToString(
              System.Globalization.CultureInfo.InvariantCulture
            ),
            ["type"] = type,
            ["fields"] = string.Join(",", substituted),
          }
        )
      );
    }

    private static float ReadSingle(ReadOnlySpan<byte> source, int offset)
    {
      return BitConverter.Int32BitsToSingle(
        BinaryPrimitives.ReadInt32LittleEndian(source.Slice(offset, sizeof(float)))
      );
    }

    private static IReadOnlyList<OperationDiagnostic> WithoutEmittedPreviewDiagnostics(
      IEnumerable<OperationDiagnostic> diagnostics
    )
    {
      return diagnostics
        .Where(diagnostic =>
          diagnostic.Code != GltfDiagnosticCodes.TextureDefaultPreviewUsed
          && diagnostic.Code != GltfDiagnosticCodes.TextureDiagnosticPreviewUsed
          && diagnostic.Code != GltfDiagnosticCodes.TextureVariantsNotRepresented
        )
        .Concat(new[] { PreviewOutputLimitWarning() })
        .ToArray();
    }

    private static OperationDiagnostic Diagnostic(
      string code,
      int eventId,
      string path,
      string message
    )
    {
      return new OperationDiagnostic(code, eventId, DiagnosticSeverity.Error, path, message);
    }

    private static IReadOnlyList<OperationDiagnostic> CreateSourceLossDiagnostics(MeshAsset asset)
    {
      if (asset.Origin == MeshAssetOrigin.Canonical)
      {
        return Array.Empty<OperationDiagnostic>();
      }

      var paths = new List<string>();
      var canonicalDeclaration = asset.Kind == MeshAssetKind.Static ? 0x20D0A1FFu : 0x30D0A1FFu;
      var canonicalArchiveType = asset.Kind == MeshAssetKind.Static ? null : (uint?)1;
      if (asset.ArchiveFraming.Declaration != canonicalDeclaration)
      {
        paths.Add("ArchiveFraming.Declaration");
      }
      if (asset.ArchiveFraming.ArchiveType != canonicalArchiveType)
      {
        paths.Add("ArchiveFraming.ArchiveType");
      }
      if (asset.ArchiveFraming.CreationGuid.HasValue)
      {
        paths.Add("ArchiveFraming.CreationGuid");
      }
      if (asset.RootTrailingBytes.Count != 0)
      {
        paths.Add("RootTrailingBytes");
      }

      switch (asset)
      {
        case StaticMeshAsset staticAsset:
          AddStaticSourceLossPaths(staticAsset, paths);
          break;
        case DynamicMeshAsset dynamicAsset:
          AddDynamicSourceLossPaths(
            dynamicAsset.RootDynamicObject,
            "RootDynamicObject",
            paths,
            new HashSet<string>(StringComparer.Ordinal)
          );
          break;
      }

      return paths
        .Distinct(StringComparer.Ordinal)
        .Select(path => new OperationDiagnostic(
          GltfDiagnosticCodes.SourceRepresentationNotPreserved,
          1130,
          DiagnosticSeverity.Warning,
          path,
          "This accepted serialized representation is not carried as an authoring input. Later glTF asset creation regenerates canonical MSH state and does not restore its source bytes.",
          data: new Dictionary<string, string>
          {
            ["origin"] = asset.Origin.ToString(),
            ["creationMode"] = "canonical",
          }
        ))
        .ToArray();
    }

    private static void AddStaticSourceLossPaths(
      StaticMeshAsset asset,
      ICollection<string> paths
    )
    {
      var header = asset.CommonBaseHeader;
      if ((header.BoxPresenceMask & 0xFFFF0000u) != 0)
      {
        paths.Add("CommonBaseHeader.BoxPresenceMask");
      }
      if (!header.AnimationFrameIndices.Equals(default(AnimationClassBytes)))
      {
        paths.Add("CommonBaseHeader.AnimationFrameIndices");
      }
      if (header.BoxCornerPassageFlags.Any(value => (value & 0xF0) != 0))
      {
        paths.Add("CommonBaseHeader.BoxCornerPassageFlags");
      }
      var rotatedFootprint = CanonicalBaseHeaderEncoder.GetCanonicalRotatedFootprintMatches(header);
      if (!rotatedFootprint.OccupancyDescriptors)
      {
        paths.Add("CommonBaseHeader.RotatedOccupancyDescriptors");
      }
      if (!rotatedFootprint.CornerPassageMaps)
      {
        paths.Add("CommonBaseHeader.RotatedCornerPassageMaps");
      }

      for (var lightIndex = 0; lightIndex < 4; lightIndex++)
      {
        var reservedOffset = lightIndex * 0x30 + 0x1D;
        if (header.StaticSpotLights.Skip(reservedOffset).Take(3).Any(value => value != 0))
        {
          paths.Add($"CommonBaseHeader.StaticSpotLights[{lightIndex + 1}].ReservedBytes");
        }
      }

      for (var attachmentIndex = 0; attachmentIndex < 49; attachmentIndex++)
      {
        var record = header.AttachmentTable.Skip(attachmentIndex * 8).Take(8).ToArray();
        var inactive = BinaryPrimitives.ReadInt16LittleEndian(record) == short.MinValue;
        var physicalNumber = attachmentIndex + 1;
        if (
          inactive
          && !record.SequenceEqual(new byte[] { 0, 0x80, 0, 0x80, 0, 0x80, 0, 0 })
        )
        {
          paths.Add($"CommonBaseHeader.AttachmentTable[{physicalNumber}]");
        }
        else if (!inactive && physicalNumber > 4)
        {
          var canonicalExtra = physicalNumber is >= 13 and <= 20 ? (byte)0 : (byte)0x80;
          if (record[7] != canonicalExtra)
          {
            paths.Add($"CommonBaseHeader.AttachmentTable[{physicalNumber}].Extra");
          }
        }
      }

      for (var cannonIndex = 0; cannonIndex < 4; cannonIndex++)
      {
        var attachment = header.AttachmentTable.Skip(cannonIndex * 8).Take(8).ToArray();
        var renderPosition = header.CannonRenderPositions
          .Skip(cannonIndex * 12)
          .Take(12)
          .ToArray();
        if (
          IsInactiveAttachment(header, cannonIndex)
          && renderPosition.Any(value => value != 0)
        )
        {
          paths.Add($"CommonBaseHeader.CannonRenderPositions[{cannonIndex + 1}]");
        }
        else if (
          !IsInactiveAttachment(header, cannonIndex)
          && !MatchesCanonicalAttachment(attachment, renderPosition, attachment[6], attachment[7])
        )
        {
          paths.Add($"CommonBaseHeader.AttachmentTable[{cannonIndex + 1}]");
        }
      }
      for (var lightIndex = 0; lightIndex < 4; lightIndex++)
      {
        var spotAttachmentIndex = lightIndex + 12;
        var spotAttachment = header.AttachmentTable.Skip(spotAttachmentIndex * 8).Take(8).ToArray();
        var spotRecord = header.StaticSpotLights.Skip(lightIndex * 0x30).Take(0x30).ToArray();
        if (
          IsInactiveAttachment(header, spotAttachmentIndex)
          && spotRecord.Any(value => value != 0)
        )
        {
          paths.Add($"CommonBaseHeader.StaticSpotLights[{lightIndex + 1}]");
        }
        else if (
          !IsInactiveAttachment(header, spotAttachmentIndex)
          && !MatchesCanonicalAttachment(spotAttachment, spotRecord, 0, 0)
        )
        {
          paths.Add($"CommonBaseHeader.AttachmentTable[{spotAttachmentIndex + 1}]");
        }

        var omniAttachmentIndex = lightIndex + 16;
        var omniAttachment = header.AttachmentTable.Skip(omniAttachmentIndex * 8).Take(8).ToArray();
        var omniRecord = header.StaticOmniLights.Skip(lightIndex * 0x1C).Take(0x1C).ToArray();
        if (
          IsInactiveAttachment(header, omniAttachmentIndex)
          && omniRecord.Any(value => value != 0)
        )
        {
          paths.Add($"CommonBaseHeader.StaticOmniLights[{lightIndex + 1}]");
        }
        else if (
          !IsInactiveAttachment(header, omniAttachmentIndex)
          && !MatchesCanonicalAttachment(omniAttachment, omniRecord, 0, 0)
        )
        {
          paths.Add($"CommonBaseHeader.AttachmentTable[{omniAttachmentIndex + 1}]");
        }
      }

      var vertexBlockCountFound = false;
      var vertexBlockPaddingFound = false;
      var unclassifiedFlagsFound = false;
      var texturePathFound = false;
      var animationClassFound = false;
      var animationTracksFound = false;
      var nextMarkerFound = false;
      var reservedTextureComponentFound = false;
      var normalSharingFound = false;
      var positionSharingFound = false;
      var triangleFlagsFound = false;
      for (var recordIndex = 0; recordIndex < asset.StaticRenderObjectSequence.Count; recordIndex++)
      {
        var record = asset.StaticRenderObjectSequence[recordIndex];
        var path = $"StaticRenderObjectSequence[{recordIndex}]";
        var minimumBlockCount = (record.RenderVertices.Count + 3) / 4;
        if (!vertexBlockCountFound && record.VertexBlockCount != minimumBlockCount)
        {
          paths.Add(path + ".VertexBlockCount");
          vertexBlockCountFound = true;
        }
        if (!vertexBlockPaddingFound && record.VertexBlockPadding.Any(value => value != 0))
        {
          paths.Add(path + ".VertexBlockPadding");
          vertexBlockPaddingFound = true;
        }
        if (!unclassifiedFlagsFound && record.UnclassifiedObjectFlagsHighWord != 0)
        {
          paths.Add(path + ".UnclassifiedObjectFlagsHighWord");
          unclassifiedFlagsFound = true;
        }
        if (!texturePathFound && record.TexturePathBytes.Count != 0)
        {
          paths.Add(path + ".TexturePathBytes");
          texturePathFound = true;
        }
        if (!animationClassFound && !record.KnownAnimationClass.HasValue)
        {
          paths.Add(path + ".AnimationClassValue");
          animationClassFound = true;
        }
        else if (!animationTracksFound && record.KnownAnimationClass.HasValue)
        {
          var declaredLength = GetAnimationClassValue(
            header.AnimationLengths,
            record.KnownAnimationClass.Value
          );
          if (
            record.AnimationTracks.ScaleFrames.Count > declaredLength
            || record.AnimationTracks.TranslationFrames.Count > declaredLength
            || record.AnimationTracks.Matrices.Count > declaredLength
          )
          {
            paths.Add(path + ".AnimationTracks");
            animationTracksFound = true;
          }
        }
        var canonicalNextMarker = recordIndex + 1 < asset.StaticRenderObjectSequence.Count ? 1u : 0u;
        if (!nextMarkerFound && record.NextRecordMarker != canonicalNextMarker)
        {
          paths.Add(path + ".NextRecordMarker");
          nextMarkerFound = true;
        }

        for (var vertexIndex = 0; vertexIndex < record.RenderVertices.Count; vertexIndex++)
        {
          var vertex = record.RenderVertices[vertexIndex];
          if (
            !reservedTextureComponentFound
            && BitConverter.SingleToInt32Bits(vertex.ReservedTextureComponent) != 0
          )
          {
            paths.Add($"{path}.RenderVertices[{vertexIndex}].ReservedTextureComponent");
            reservedTextureComponentFound = true;
          }
          if (!normalSharingFound && vertex.NormalSharingIndex != ushort.MaxValue)
          {
            paths.Add($"{path}.RenderVertices[{vertexIndex}].NormalSharingIndex");
            normalSharingFound = true;
          }
          if (!positionSharingFound && vertex.PositionSharingIndex != ushort.MaxValue)
          {
            paths.Add($"{path}.RenderVertices[{vertexIndex}].PositionSharingIndex");
            positionSharingFound = true;
          }
        }

        if (!triangleFlagsFound && record.Triangles.Count != 0)
        {
          for (var triangleIndex = 0; triangleIndex < record.Triangles.Count; triangleIndex++)
          {
            var triangle = record.Triangles[triangleIndex];
            var canonicalFlags = CanonicalStaticRenderObjectSequenceEncoder
              .CalculateTriangleRenderPassFlags(record.RenderVertices, triangle);
            if (triangle.TriangleRenderPassFlags != canonicalFlags)
            {
              paths.Add($"{path}.Triangles[{triangleIndex}].TriangleRenderPassFlags");
              triangleFlagsFound = true;
              break;
            }
          }
        }
      }

      if (asset.StoredTrailingHierarchyUnwindCount != asset.ExpectedTrailingHierarchyUnwindCount)
      {
        paths.Add("StoredTrailingHierarchyUnwindCount");
      }
    }

    private static bool MatchesCanonicalAttachment(
      IReadOnlyList<byte> attachment,
      IReadOnlyList<byte> positionSource,
      byte heading,
      byte extra
    )
    {
      var bytes = positionSource.ToArray();
      var position = new Vector3(ReadSingle(bytes, 0), ReadSingle(bytes, 4), ReadSingle(bytes, 8));
      if (!IsFinite(position))
      {
        return false;
      }
      try
      {
        var canonical = CanonicalBaseHeaderEncoder.EncodeAttachmentRecord(
          new CanonicalAttachmentRecord(position, heading, extra)
        );
        return attachment.SequenceEqual(canonical);
      }
      catch (OverflowException)
      {
        return false;
      }
    }

    private static bool IsInactiveAttachment(CommonMeshBaseHeader header, int zeroBasedIndex)
    {
      return BinaryPrimitives.ReadInt16LittleEndian(
          header.AttachmentTable.Skip(zeroBasedIndex * 8).Take(2).ToArray()
        )
        == short.MinValue;
    }

    private static byte GetAnimationClassValue(
      AnimationClassBytes values,
      StaticAnimationClass animationClass
    )
    {
      return animationClass switch
      {
        StaticAnimationClass.A => values.A,
        StaticAnimationClass.B => values.B,
        StaticAnimationClass.C => values.C,
        _ => values.D,
      };
    }

    private static void AddDynamicSourceLossPaths(
      DynamicObject value,
      string path,
      ICollection<string> paths,
      ISet<string> categories
    )
    {
      if (
        !value.CommonBaseHeader.IsCanonicalDynamic
        && categories.Add("CommonBaseHeader")
      )
      {
        paths.Add(path + ".CommonBaseHeader");
      }

      var extension = value.Extension;
      if (!extension.KnownEffectType.HasValue && categories.Add("EffectType"))
      {
        paths.Add(path + ".Extension.EffectType");
      }
      if (!extension.KnownLightType.HasValue && categories.Add("LightType"))
      {
        paths.Add(path + ".Extension.LightType");
      }
      if (extension.ReservedWord != 0 && categories.Add("ReservedWord"))
      {
        paths.Add(path + ".Extension.ReservedWord");
      }
      if (extension.MeshNameBytes.Count != 0 && categories.Add("MeshNameBytes"))
      {
        paths.Add(path + ".Extension.MeshNameBytes");
      }
      if (extension.TexturePathBytes.Count != 0 && categories.Add("TexturePathBytes"))
      {
        paths.Add(path + ".Extension.TexturePathBytes");
      }
      foreach (
        var finding in DynamicEffectBehavior.Diagnose(
          extension,
          path == "RootDynamicObject" ? DynamicObjectPlacement.Root : DynamicObjectPlacement.Child
        )
      )
      {
        if (categories.Add(finding.PathSuffix))
        {
          paths.Add(path + finding.PathSuffix);
        }
      }

      for (var childIndex = 0; childIndex < value.Children.Count; childIndex++)
      {
        AddDynamicSourceLossPaths(
          value.Children[childIndex],
          $"{path}.Children[{childIndex}]",
          paths,
          categories
        );
      }
    }

    private static OperationDiagnostic? ValidatePlan(
      GltfImportPlan plan,
      GltfPackageKind packageKind,
      GltfOperationProfile profile
    )
    {
      var limit = plan.ValidateProfile(profile);
      if (limit is not null)
      {
        return limit;
      }
      if (plan.PackageKind != packageKind)
      {
        return PlanMismatch("package");
      }
      return null;
    }

    private static bool MatchesPlanSource(byte[] source, GltfImportPlan plan)
    {
      return string.Equals(
        GltfImportPlanSerializer.Hash(source),
        plan.SourceSha256,
        StringComparison.Ordinal
      );
    }

    private static OperationDiagnostic PlanMismatch(string path)
    {
      return new OperationDiagnostic(
        GltfDiagnosticCodes.ImportPlanMismatch,
        3004,
        DiagnosticSeverity.Error,
        path,
        "The import plan does not match the selected import or source package.",
        data: new Dictionary<string, string> { ["dimension"] = path }
      );
    }

    private static OperationDiagnostic MetadataDiagnostic(
      string code,
      int eventId,
      string path,
      string message,
      params string[] actions
    )
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
        ["affectedPayloadPaths"] = path,
      };
      data["conflictKey"] = CreateConflictKey(code, path, data);
      return new OperationDiagnostic(
        code,
        eventId,
        DiagnosticSeverity.Error,
        path,
        message,
        data: data
      );
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

    private static OperationResult WithoutValue<T>(OperationResult<T> result)
      where T : class
    {
      return new OperationResult(result.Status, result.Diagnostics);
    }

    private static OperationResult<MeshAsset> ToMeshAsset<T>(OperationResult<T> result)
      where T : MeshAsset
    {
      return result.Succeeded
        ? new OperationResult<MeshAsset>(result.Status, result.Value, result.Diagnostics)
        : new OperationResult<MeshAsset>(result.Status, diagnostics: result.Diagnostics);
    }

    private static OperationResult<T> Cancelled<T>()
      where T : class
    {
      return new OperationResult<T>(
        OperationStatus.Cancelled,
        diagnostics: new[] { CancelledDiagnostic() }
      );
    }

    private static OperationResult Cancelled()
    {
      return new OperationResult(OperationStatus.Cancelled, new[] { CancelledDiagnostic() });
    }

    private static OperationDiagnostic CancelledDiagnostic()
    {
      return Diagnostic(
        GltfDiagnosticCodes.Cancelled,
        1105,
        "$",
        "The glTF operation was cancelled."
      );
    }
  }

  internal sealed class MetadataIdentityException : Exception
  {
    internal string Code { get; }

    internal int EventId { get; }

    internal string? Path { get; }

    internal MetadataIdentityException(
      string code,
      int eventId,
      string message,
      string? path = null
    )
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
      : base(message) { }
  }

  internal sealed class AmbiguousPartitionCorrespondenceException : Exception
  {
    internal AmbiguousPartitionCorrespondenceException(string message)
      : base(message) { }
  }

  internal sealed class RequiredTextureResourceBindingException : Exception
  {
    internal int MaterialIndex { get; }

    internal GltfMaterialHandle MaterialHandle { get; }

    internal RequiredTextureResourceBindingException(
      int materialIndex,
      GltfMaterialHandle materialHandle
    )
      : base("A textured new-model material requires a typed canonical TEX resource binding.")
    {
      MaterialIndex = materialIndex;
      MaterialHandle = materialHandle;
    }
  }
}
