using System.Collections.Generic;
using System.Linq;

namespace EarthTool.PAR.GUI.Models;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
  public ValidationResult()
  {
    Errors = new List<ValidationError>();
  }

  /// <summary>
  /// Gets whether the validation passed (no errors).
  /// </summary>
  public bool IsValid => !Errors.Any();

  /// <summary>
  /// Gets the list of validation errors.
  /// </summary>
  public List<ValidationError> Errors { get; set; }

  /// <summary>
  /// Gets whether there are any errors with Error severity.
  /// </summary>
  public bool HasErrors => Errors.Any(e => e.Severity == ValidationSeverity.Error);

  /// <summary>
  /// Gets whether there are any warnings.
  /// </summary>
  public bool HasWarnings => Errors.Any(e => e.Severity == ValidationSeverity.Warning);
}

/// <summary>
/// Represents a single validation error or warning.
/// </summary>
public class ValidationError
{
  /// <summary>
  /// Gets or sets the name of the property that failed validation.
  /// </summary>
  public string PropertyName { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the error message.
  /// </summary>
  public string ErrorMessage { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the severity of the validation error.
  /// </summary>
  public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}

/// <summary>
/// Severity level for validation errors.
/// </summary>
public enum ValidationSeverity
{
  /// <summary>
  /// Information message (does not prevent saving).
  /// </summary>
  Info,

  /// <summary>
  /// Warning message (does not prevent saving, but user should be aware).
  /// </summary>
  Warning,

  /// <summary>
  /// Error message (prevents saving until fixed).
  /// </summary>
  Error
}
