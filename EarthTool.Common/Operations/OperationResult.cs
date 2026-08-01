#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.Common.Operations
{
  /// <summary>Describes the terminal state of one bounded operation.</summary>
  public enum OperationStatus
  {
    /// <summary>The operation completed and produced its complete result.</summary>
    Succeeded,

    /// <summary>The operation failed and produced no partial value.</summary>
    Failed,

    /// <summary>The operation observed cancellation and produced no partial value.</summary>
    Cancelled
  }

  /// <summary>Describes the significance of an operation diagnostic.</summary>
  public enum DiagnosticSeverity
  {
    /// <summary>The diagnostic provides informational context.</summary>
    Information,

    /// <summary>The diagnostic identifies a non-fatal anomaly.</summary>
    Warning,

    /// <summary>The diagnostic identifies an operation failure.</summary>
    Error
  }

  /// <summary>Provides stable machine-readable details for one operation finding.</summary>
  public sealed class OperationDiagnostic
  {
    /// <summary>Gets the stable symbolic diagnostic code.</summary>
    public string Code { get; }

    /// <summary>Gets the stable numeric event identifier.</summary>
    public int EventId { get; }

    /// <summary>Gets the diagnostic severity.</summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>Gets the canonical path to the affected representation.</summary>
    public string Path { get; }

    /// <summary>Gets the source byte offset when one is known.</summary>
    public long? ByteOffset { get; }

    /// <summary>Gets bounded structured diagnostic data.</summary>
    public IReadOnlyDictionary<string, string> Data { get; }

    /// <summary>Gets non-contractual explanatory prose.</summary>
    public string Message { get; }

    /// <summary>Initializes an operation diagnostic.</summary>
    public OperationDiagnostic(
      string code,
      int eventId,
      DiagnosticSeverity severity,
      string path,
      string message,
      long? byteOffset = null,
      IReadOnlyDictionary<string, string>? data = null)
    {
      Code = code ?? throw new ArgumentNullException(nameof(code));
      EventId = eventId;
      Severity = severity;
      Path = path ?? throw new ArgumentNullException(nameof(path));
      Message = message ?? throw new ArgumentNullException(nameof(message));
      ByteOffset = byteOffset;
      Data = new ReadOnlyDictionary<string, string>(
        data?.ToDictionary(pair => pair.Key, pair => pair.Value)
        ?? new Dictionary<string, string>());
    }
  }

  /// <summary>Provides the status and diagnostics produced by an operation.</summary>
  public class OperationResult
  {
    /// <summary>Gets the terminal operation status.</summary>
    public OperationStatus Status { get; }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool Succeeded => Status == OperationStatus.Succeeded;

    /// <summary>Gets the immutable operation diagnostics.</summary>
    public IReadOnlyList<OperationDiagnostic> Diagnostics { get; }

    /// <summary>Initializes an operation result.</summary>
    public OperationResult(OperationStatus status, IEnumerable<OperationDiagnostic>? diagnostics = null)
    {
      Status = status;
      Diagnostics = Array.AsReadOnly(diagnostics?.ToArray() ?? Array.Empty<OperationDiagnostic>());
    }
  }

  /// <summary>Provides an all-or-nothing result containing a successfully materialized value.</summary>
  /// <typeparam name="T">The materialized value type.</typeparam>
  public sealed class OperationResult<T> : OperationResult
    where T : class
  {
    /// <summary>Gets the value on success, or <see langword="null"/> otherwise.</summary>
    public T? Value { get; }

    /// <summary>Initializes a typed operation result.</summary>
    public OperationResult(
      OperationStatus status,
      T? value = null,
      IEnumerable<OperationDiagnostic>? diagnostics = null)
      : base(status, diagnostics)
    {
      if (status == OperationStatus.Succeeded && value is null)
      {
        throw new ArgumentException("A successful operation requires a value.", nameof(value));
      }

      if (status != OperationStatus.Succeeded && value is not null)
      {
        throw new ArgumentException("A failed or cancelled operation cannot expose a partial value.", nameof(value));
      }

      Value = value;
    }
  }
}
