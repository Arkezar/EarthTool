#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.GLTF
{
  /// <summary>Describes how one serialized MSH field was handled during mesh creation.</summary>
  public enum PreservationDisposition
  {
    /// <summary>The source representation was retained exactly.</summary>
    Retained = 0,

    /// <summary>The representation was regenerated from artist-visible input.</summary>
    Regenerated = 1,

    /// <summary>The source representation was deliberately removed.</summary>
    Invalidated = 2,

    /// <summary>The representation was replaced with its canonical authored form.</summary>
    Canonicalized = 3,
  }

  /// <summary>Describes one MSH preservation effect reported by glTF mesh creation.</summary>
  public sealed class PreservationChange
  {
    /// <summary>Gets the affected MSH field path.</summary>
    public string FieldPath { get; }

    /// <summary>Gets the preservation disposition.</summary>
    public PreservationDisposition Disposition { get; }

    /// <summary>Gets the stable reason category.</summary>
    public string Reason { get; }

    /// <summary>Initializes one preservation effect.</summary>
    public PreservationChange(
      string fieldPath,
      PreservationDisposition disposition,
      string reason
    )
    {
      FieldPath = fieldPath ?? throw new ArgumentNullException(nameof(fieldPath));
      Disposition = disposition;
      Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }
  }

  /// <summary>Reports ordered MSH preservation effects for one glTF mesh creation.</summary>
  public sealed class PreservationReport
  {
    /// <summary>Gets the ordered preservation effects.</summary>
    public IReadOnlyList<PreservationChange> Changes { get; }

    internal PreservationReport(IEnumerable<PreservationChange> changes)
    {
      Changes = Array.AsReadOnly(
        (changes ?? throw new ArgumentNullException(nameof(changes))).ToArray()
      );
    }
  }
}
