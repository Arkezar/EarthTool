using EarthTool.PAR.GUI.Models;
using System;
using System.Collections.Generic;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Service for managing undo/redo operations.
/// </summary>
public interface IUndoRedoService
{
  /// <summary>
  /// Gets whether an undo operation is available.
  /// </summary>
  bool CanUndo { get; }

  /// <summary>
  /// Gets whether a redo operation is available.
  /// </summary>
  bool CanRedo { get; }

  /// <summary>
  /// Gets or sets the maximum number of actions to keep in history.
  /// </summary>
  int MaxHistorySize { get; set; }

  /// <summary>
  /// Records a new undoable action.
  /// </summary>
  /// <param name="description">Human-readable description of the action.</param>
  /// <param name="undoCallback">Callback to execute when undoing.</param>
  /// <param name="redoCallback">Callback to execute when redoing.</param>
  void RecordAction(string description, Action undoCallback, Action redoCallback);

  /// <summary>
  /// Undoes the last action.
  /// </summary>
  void Undo();

  /// <summary>
  /// Redoes the last undone action.
  /// </summary>
  void Redo();

  /// <summary>
  /// Clears all undo/redo history.
  /// </summary>
  void Clear();

  /// <summary>
  /// Gets the undo history.
  /// </summary>
  IEnumerable<UndoAction> GetUndoHistory();

  /// <summary>
  /// Gets the redo history.
  /// </summary>
  IEnumerable<UndoAction> GetRedoHistory();
}
