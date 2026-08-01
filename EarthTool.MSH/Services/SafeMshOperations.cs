#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EarthTool.MSH.Services
{
  /// <summary>Provides bounded, fail-closed MSH reading.</summary>
  public sealed class MshReader : IMshReader
  {
    /// <inheritdoc />
    public async Task<OperationResult<MeshAsset>> ReadAsync(
      Stream source,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (source is null)
      {
        throw new ArgumentNullException(nameof(source));
      }

      profile ??= MshOperationProfile.Default;
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (source.CanSeek && source.Length - source.Position > profile.MaxInputBytes)
        {
          return Failed<MeshAsset>(Limit("$", source.Length - source.Position, profile.MaxInputBytes));
        }

        using var owned = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
          var read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
          if (read == 0)
          {
            break;
          }

          if (owned.Length + read > profile.MaxInputBytes)
          {
            return Failed<MeshAsset>(Limit("$", owned.Length + read, profile.MaxInputBytes));
          }

          owned.Write(buffer, 0, read);
        }

        var asset = MshV1Decoder.Decode(owned.ToArray(), cancellationToken);
        return new OperationResult<MeshAsset>(OperationStatus.Succeeded, asset);
      }
      catch (OperationCanceledException)
      {
        return Cancelled<MeshAsset>();
      }
      catch (MshContentException ex)
      {
        return Failed<MeshAsset>(ex.Diagnostic);
      }
      catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
      {
        return Failed<MeshAsset>(IoFailure("$", ex));
      }
    }

    /// <inheritdoc />
    public async Task<OperationResult<MeshAsset>> ReadFileAsync(
      string path,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (path is null)
      {
        throw new ArgumentNullException(nameof(path));
      }

      try
      {
        using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await ReadAsync(source, profile, cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        return Cancelled<MeshAsset>();
      }
      catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
      {
        return Failed<MeshAsset>(IoFailure("$", ex));
      }
    }

    private static OperationDiagnostic Limit(string path, long actual, int maximum)
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.ResourceLimitExceeded,
        1004,
        DiagnosticSeverity.Error,
        path,
        "The MSH input exceeds the configured operation profile.",
        data: new Dictionary<string, string>
        {
          ["actual"] = actual.ToString(System.Globalization.CultureInfo.InvariantCulture),
          ["maximum"] = maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)
        });
    }

    internal static OperationDiagnostic IoFailure(string path, Exception exception)
    {
      return new OperationDiagnostic(
        MshDiagnosticCodes.IoFailure,
        1007,
        DiagnosticSeverity.Error,
        path,
        exception.Message);
    }

    internal static OperationResult<T> Failed<T>(OperationDiagnostic diagnostic)
      where T : class
    {
      return new OperationResult<T>(OperationStatus.Failed, diagnostics: new[] { diagnostic });
    }

    internal static OperationResult<T> Cancelled<T>()
      where T : class
    {
      return new OperationResult<T>(
        OperationStatus.Cancelled,
        diagnostics: new[]
        {
          new OperationDiagnostic(
            MshDiagnosticCodes.Cancelled,
            1008,
            DiagnosticSeverity.Error,
            "$",
            "The MSH operation was cancelled.")
        });
    }
  }

  /// <summary>Validates immutable MSH assets independently of writing.</summary>
  public sealed class MshValidator : IMshValidator
  {
    /// <inheritdoc />
    public Task<OperationResult> ValidateAsync(
      MeshAsset asset,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      if (asset is null)
      {
        throw new ArgumentNullException(nameof(asset));
      }

      profile ??= MshOperationProfile.Default;
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (asset is not StaticMeshAsset staticAsset)
        {
          return Task.FromResult(UnsupportedAsset());
        }

        var bytes = staticAsset.GetSerializedRepresentation();
        if (bytes.Length > profile.MaxOutputBytes)
        {
          return Task.FromResult<OperationResult>(new OperationResult(
            OperationStatus.Failed,
            new[]
            {
              new OperationDiagnostic(
                MshDiagnosticCodes.ResourceLimitExceeded,
                1004,
                DiagnosticSeverity.Error,
                "$",
                "The serialized MSH exceeds the configured operation profile.")
            }));
        }

        MshV1Decoder.Decode(bytes, cancellationToken);
        return Task.FromResult<OperationResult>(new OperationResult(OperationStatus.Succeeded));
      }
      catch (OperationCanceledException)
      {
        return Task.FromResult<OperationResult>(Cancelled());
      }
      catch (MshContentException ex)
      {
        return Task.FromResult<OperationResult>(new OperationResult(
          OperationStatus.Failed,
          new[] { ex.Diagnostic }));
      }
    }

    private static OperationResult UnsupportedAsset()
    {
      return new OperationResult(
        OperationStatus.Failed,
        new[]
        {
          new OperationDiagnostic(
            MshDiagnosticCodes.UnsupportedDomain,
            1005,
            DiagnosticSeverity.Error,
            "$",
            "Only the static one-triangle asset is supported by this slice.",
            data: new Dictionary<string, string> { ["domain"] = "MeshKind" })
        });
    }

    internal static OperationResult Cancelled()
    {
      return new OperationResult(
        OperationStatus.Cancelled,
        new[]
        {
          new OperationDiagnostic(
            MshDiagnosticCodes.Cancelled,
            1008,
            DiagnosticSeverity.Error,
            "$",
            "The MSH operation was cancelled.")
        });
    }
  }

  /// <summary>Provides validated stream writes and transactional file writes.</summary>
  public sealed class MshWriter : IMshWriter
  {
    private readonly ITransactionalFileSystem _fileSystem;

    /// <summary>Initializes a writer using the platform filesystem.</summary>
    public MshWriter()
      : this(new TransactionalFileSystem())
    {
    }

    internal MshWriter(ITransactionalFileSystem fileSystem)
    {
      _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    public async Task<OperationResult> WriteAsync(
      MeshAsset asset,
      Stream destination,
      MshOperationProfile? profile = null,
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

      profile ??= MshOperationProfile.Default;
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (asset is not StaticMeshAsset staticAsset)
        {
          return UnsupportedAsset();
        }

        var bytes = staticAsset.GetSerializedRepresentation();
        if (bytes.Length > profile.MaxOutputBytes)
        {
          return Limit();
        }

        MshV1Decoder.Decode(bytes, cancellationToken);
        await destination.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
        return new OperationResult(OperationStatus.Succeeded);
      }
      catch (OperationCanceledException)
      {
        return MshValidator.Cancelled();
      }
      catch (MshContentException ex)
      {
        return new OperationResult(OperationStatus.Failed, new[] { ex.Diagnostic });
      }
      catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
      {
        return new OperationResult(OperationStatus.Failed, new[] { MshReader.IoFailure("$", ex) });
      }
    }

    /// <inheritdoc />
    public async Task<OperationResult> WriteFileAsync(
      MeshAsset asset,
      string destinationPath,
      MshOperationProfile? profile = null,
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
        using (var temporary = _fileSystem.CreateTemporary(temporaryPath))
        {
          var result = await WriteAsync(asset, temporary, profile, cancellationToken).ConfigureAwait(false);
          if (!result.Succeeded)
          {
            return result;
          }

          await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        _fileSystem.Commit(temporaryPath, destinationPath);
        return new OperationResult(OperationStatus.Succeeded);
      }
      catch (OperationCanceledException)
      {
        return MshValidator.Cancelled();
      }
      catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
      {
        return new OperationResult(OperationStatus.Failed, new[] { MshReader.IoFailure(destinationPath, ex) });
      }
      finally
      {
        _fileSystem.TryDelete(temporaryPath);
      }
    }

    private static OperationResult UnsupportedAsset()
    {
      return new OperationResult(
        OperationStatus.Failed,
        new[]
        {
          new OperationDiagnostic(
            MshDiagnosticCodes.UnsupportedDomain,
            1005,
            DiagnosticSeverity.Error,
            "$",
            "Only the static one-triangle asset is supported by this slice.")
        });
    }

    private static OperationResult Limit()
    {
      return new OperationResult(
        OperationStatus.Failed,
        new[]
        {
          new OperationDiagnostic(
            MshDiagnosticCodes.ResourceLimitExceeded,
            1004,
            DiagnosticSeverity.Error,
            "$",
            "The serialized MSH exceeds the configured operation profile.")
        });
    }
  }
}
