using System;

namespace EarthTool.PAR.GUI.Models;

/// <summary>
/// Represents an undoable action in the editor.
/// </summary>
public class UndoAction
{
  /// <summary>
  /// Gets or sets the human-readable description of the action.
  /// </summary>
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the callback to execute when undoing this action.
  /// </summary>
  public Action UndoCallback { get; set; } = () => { };

  /// <summary>
  /// Gets or sets the callback to execute when redoing this action.
  /// </summary>
  public Action RedoCallback { get; set; } = () => { };

  /// <summary>
  /// Gets or sets the timestamp when this action was recorded.
  /// </summary>
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
