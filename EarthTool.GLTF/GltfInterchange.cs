#nullable enable

using EarthTool.Common.Operations;
using EarthTool.GLTF.Internal;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Services;
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
        var unsupported = ValidateAssetProfile(asset);
        if (unsupported is not null)
        {
          return Failed<GltfExportReceipt>(unsupported);
        }

        var baseline = new InterchangeBaseline(
          options.AssetLineageId ?? Guid.NewGuid(),
          options.DocumentId ?? Guid.NewGuid());
        var glb = GlbDocument.Create(asset, baseline, out var fingerprint);
        if (glb.Length > profile.MaxOutputBytes)
        {
          return Failed<GltfExportReceipt>(Limit("$", glb.Length, profile.MaxOutputBytes));
        }

        GlbDocument.Parse(glb, profile.MaxJsonDepth);
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
        var manifest = GlbDocument.ParseMetadata(
          parsed.ManifestMetadata,
          profile.MaxMetadataBytes,
          profile.MaxJsonDepth);
        var mesh = GlbDocument.ParseMetadata(
          parsed.MeshMetadata,
          profile.MaxMetadataBytes,
          profile.MaxJsonDepth);
        ValidateMetadata(manifest, mesh, expectedBaseline);

        var fingerprint = StaticGeometryFingerprint.Create(
          expectedBaseline,
          mesh.LocalId,
          parsed.Vertices,
          parsed.Triangle);
        if (!string.Equals(fingerprint.Sha256, mesh.Fingerprint, StringComparison.Ordinal))
        {
          return Failed<GltfEditImportResult>(Diagnostic(
            GltfDiagnosticCodes.StaleNativeProjection,
            2008,
            "meshes[0]",
            "The native geometry no longer matches its preservation fingerprint."));
        }

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

        var nextBaseline = new InterchangeBaseline(expectedBaseline.AssetLineageId, Guid.NewGuid());
        return new OperationResult<GltfEditImportResult>(
          OperationStatus.Succeeded,
          new GltfEditImportResult(
            asset,
            nextBaseline,
            fingerprint,
            new[]
            {
              "ArchiveFraming",
              "BaseHeader",
              "StaticRenderObjectSequence[0]"
            }));
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
        GlbDocument.Parse(bytes, profile.MaxJsonDepth);
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

    private static OperationDiagnostic? ValidateAssetProfile(StaticMeshAsset asset)
    {
      if (asset.StaticRenderObjectSequence.Count != 1)
      {
        return Unsupported("StaticRenderObjectSequence");
      }

      var renderObject = asset.StaticRenderObjectSequence[0];
      if (renderObject.RenderVertices.Count != 3 || renderObject.Triangles.Count != 1)
      {
        return Unsupported("Geometry");
      }

      return null;
    }

    private static void ValidateMetadata(
      MetadataEnvelope manifest,
      MetadataEnvelope mesh,
      InterchangeBaseline expected)
    {
      if (manifest.ScopeKind != "manifest" || manifest.LocalId != 0 || manifest.SourceMsh is null)
      {
        throw new InvalidDataException("The scene metadata manifest is malformed.");
      }

      if (mesh.ScopeKind != "mesh"
        || mesh.LocalId != 1
        || mesh.Fingerprint is null
        || mesh.FingerprintName != "static-geometry"
        || mesh.FingerprintVersion != 1)
      {
        throw new MalformedMetadataException("The mesh metadata envelope is malformed.");
      }

      if (manifest.AssetLineageId != expected.AssetLineageId
        || mesh.AssetLineageId != expected.AssetLineageId)
      {
        throw new MetadataIdentityException(GltfDiagnosticCodes.AssetLineageMismatch, 2003,
          "The GLB belongs to a different asset lineage.");
      }

      if (manifest.DocumentId != expected.DocumentId || mesh.DocumentId != expected.DocumentId)
      {
        throw new MetadataIdentityException(GltfDiagnosticCodes.DocumentMismatch, 2004,
          "The GLB belongs to a different interchange document.");
      }

      if (manifest.AssetLineageId != mesh.AssetLineageId || manifest.DocumentId != mesh.DocumentId)
      {
        throw new InvalidDataException("The metadata envelopes do not share one baseline.");
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
}
