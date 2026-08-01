#nullable enable

using EarthTool.Common.Operations;
using EarthTool.MSH.Assets;
using EarthTool.MSH.Internal;
using EarthTool.MSH.Operations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly ILogger<MshReader> _logger;

    /// <summary>Initializes a reader without operation logging.</summary>
    public MshReader()
      : this(NullLogger<MshReader>.Instance)
    {
    }

    /// <summary>Initializes a reader that logs successful compatibility warnings.</summary>
    public MshReader(ILogger<MshReader> logger)
    {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
        var buffer = new byte[(int)Math.Min(81920L, (long)profile.MaxInputBytes + 1)];
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

        var decoded = MshV1Decoder.Decode(owned.ToArray(), profile, cancellationToken);
        LogWarnings(_logger, decoded.Diagnostics);

        return new OperationResult<MeshAsset>(
          OperationStatus.Succeeded,
          decoded.Asset,
          decoded.Diagnostics);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return Cancelled<MeshAsset>();
      }
      catch (MshContentException ex)
      {
        return Failed<MeshAsset>(ex.Diagnostic);
      }
      catch (Exception ex) when (IsIoFailure(ex))
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
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return Cancelled<MeshAsset>();
      }
      catch (Exception ex) when (IsIoFailure(ex))
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
        Bound(exception.Message),
        data: new Dictionary<string, string>
        {
          ["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name
        });
    }

    internal static bool IsIoFailure(Exception exception)
    {
      return exception is IOException
        || exception is UnauthorizedAccessException
        || exception is NotSupportedException
        || exception is ObjectDisposedException
        || exception is OperationCanceledException;
    }

    internal static void LogWarnings(ILogger logger, IEnumerable<OperationDiagnostic> diagnostics)
    {
      foreach (var diagnostic in diagnostics)
      {
        if (diagnostic.Severity == DiagnosticSeverity.Warning)
        {
          logger.LogWarning(
            new EventId(diagnostic.EventId, diagnostic.Code),
            "{Code} at {Path}: {Message}",
            diagnostic.Code,
            diagnostic.Path,
            diagnostic.Message);
        }
      }
    }

    private static string Bound(string message)
    {
      const int maximumLength = 512;
      return message.Length <= maximumLength ? message : message.Substring(0, maximumLength);
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
    private readonly ILogger<MshValidator> _logger;

    /// <summary>Initializes a validator without operation logging.</summary>
    public MshValidator()
      : this(NullLogger<MshValidator>.Instance)
    {
    }

    /// <summary>Initializes a validator that logs successful compatibility warnings.</summary>
    public MshValidator(ILogger<MshValidator> logger)
    {
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
        if (asset.SerializedLength > profile.MaxOutputBytes)
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

        var bytes = asset.GetSerializedRepresentation();
        var decoded = MshV1Decoder.Decode(bytes, profile, cancellationToken);
        MshReader.LogWarnings(_logger, decoded.Diagnostics);
        return Task.FromResult<OperationResult>(new OperationResult(
          OperationStatus.Succeeded,
          decoded.Diagnostics));
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
    private readonly ILogger<MshWriter> _logger;

    /// <summary>Initializes a writer using the platform filesystem.</summary>
    public MshWriter()
      : this(new TransactionalFileSystem(), NullLogger<MshWriter>.Instance)
    {
    }

    /// <summary>Initializes a writer using the platform filesystem and operation logging.</summary>
    public MshWriter(ILogger<MshWriter> logger)
      : this(new TransactionalFileSystem(), logger)
    {
    }

    internal MshWriter(ITransactionalFileSystem fileSystem)
      : this(fileSystem, NullLogger<MshWriter>.Instance)
    {
    }

    internal MshWriter(ITransactionalFileSystem fileSystem, ILogger<MshWriter> logger)
    {
      _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<OperationResult> WriteAsync(
      MeshAsset asset,
      Stream destination,
      MshOperationProfile? profile = null,
      CancellationToken cancellationToken = default)
    {
      var result = await WriteCoreAsync(asset, destination, profile, cancellationToken).ConfigureAwait(false);
      if (result.Succeeded)
      {
        MshReader.LogWarnings(_logger, result.Diagnostics);
      }

      return result;
    }

    private static async Task<OperationResult> WriteCoreAsync(
      MeshAsset asset,
      Stream destination,
      MshOperationProfile? profile,
      CancellationToken cancellationToken)
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
        if (asset.SerializedLength > profile.MaxOutputBytes)
        {
          return Limit();
        }

        var bytes = asset.GetSerializedRepresentation();
        var decoded = MshV1Decoder.Decode(bytes, profile, cancellationToken);
        await destination.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
        return new OperationResult(OperationStatus.Succeeded, decoded.Diagnostics);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return MshValidator.Cancelled();
      }
      catch (MshContentException ex)
      {
        return new OperationResult(OperationStatus.Failed, new[] { ex.Diagnostic });
      }
      catch (Exception ex) when (MshReader.IsIoFailure(ex))
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

      if (asset is null)
      {
        throw new ArgumentNullException(nameof(asset));
      }

      profile ??= MshOperationProfile.Default;
      var validation = await new MshValidator()
        .ValidateAsync(asset, profile, cancellationToken)
        .ConfigureAwait(false);
      if (!validation.Succeeded)
      {
        return validation;
      }

      string? temporaryPath = null;
      OperationResult? writeResult = null;
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        temporaryPath = _fileSystem.GetTemporaryPath(destinationPath);
        using (var temporary = _fileSystem.CreateTemporary(temporaryPath))
        {
          writeResult = await WriteCoreAsync(asset, temporary, profile, cancellationToken).ConfigureAwait(false);
          if (!writeResult.Succeeded)
          {
            return writeResult;
          }

          await temporary.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        using (var staged = _fileSystem.OpenTemporaryRead(temporaryPath))
        {
          var stagedProfile = new MshOperationProfile(
            maxInputBytes: profile.MaxOutputBytes,
            maxOutputBytes: profile.MaxOutputBytes,
            maxDiagnostics: profile.MaxDiagnostics,
            maxRootTrailingBytes: profile.MaxRootTrailingBytes);
          var stagedValidation = await new MshReader()
            .ReadAsync(staged, stagedProfile, cancellationToken)
            .ConfigureAwait(false);
          if (!stagedValidation.Succeeded)
          {
            return new OperationResult(stagedValidation.Status, stagedValidation.Diagnostics);
          }
        }

        cancellationToken.ThrowIfCancellationRequested();
        _fileSystem.Commit(temporaryPath, destinationPath);
        MshReader.LogWarnings(_logger, writeResult!.Diagnostics);
        return writeResult;
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return MshValidator.Cancelled();
      }
      catch (Exception ex) when (MshReader.IsIoFailure(ex))
      {
        return new OperationResult(OperationStatus.Failed, new[] { MshReader.IoFailure(destinationPath, ex) });
      }
      finally
      {
        if (temporaryPath is not null)
        {
          _fileSystem.TryDelete(temporaryPath);
        }
      }
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
