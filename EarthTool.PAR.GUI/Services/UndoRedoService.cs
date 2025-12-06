using EarthTool.PAR.GUI.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Implementation of undo/redo service using command pattern.
/// </summary>
public class UndoRedoService : IUndoRedoService
{
  private readonly ILogger<UndoRedoService> _logger;
  private readonly Stack<UndoAction> _undoStack;
  private readonly Stack<UndoAction> _redoStack;
  private int _maxHistorySize = 100;

  public UndoRedoService(ILogger<UndoRedoService> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _undoStack = new Stack<UndoAction>();
    _redoStack = new Stack<UndoAction>();
  }

  /// <inheritdoc/>
  public bool CanUndo => _undoStack.Count > 0;

  /// <inheritdoc/>
  public bool CanRedo => _redoStack.Count > 0;

  /// <inheritdoc/>
  public int MaxHistorySize
  {
    get => _maxHistorySize;
    set
    {
      if (value < 1)
        throw new ArgumentException("Max history size must be at least 1", nameof(value));
      _maxHistorySize = value;
      TrimHistory();
    }
  }

  /// <inheritdoc/>
  public void RecordAction(string description, Action undoCallback, Action redoCallback)
  {
    if (undoCallback == null) throw new ArgumentNullException(nameof(undoCallback));
    if (redoCallback == null) throw new ArgumentNullException(nameof(redoCallback));

    var action = new UndoAction
    {
      Description = description ?? string.Empty,
      UndoCallback = undoCallback,
      RedoCallback = redoCallback,
      Timestamp = DateTime.UtcNow
    };

    _undoStack.Push(action);
    _redoStack.Clear(); // Clear redo stack on new action

    TrimHistory();

    _logger.LogDebug("Recorded action: {Description}", description);
  }

  /// <inheritdoc/>
  public void Undo()
  {
    if (!CanUndo)
    {
      _logger.LogWarning("Cannot undo: no actions in undo stack");
      return;
    }

    var action = _undoStack.Pop();

    try
    {
      action.UndoCallback();
      _redoStack.Push(action);
      _logger.LogInformation("Undone action: {Description}", action.Description);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during undo of action: {Description}", action.Description);
      // Put the action back on the undo stack
      _undoStack.Push(action);
      throw;
    }
  }

  /// <inheritdoc/>
  public void Redo()
  {
    if (!CanRedo)
    {
      _logger.LogWarning("Cannot redo: no actions in redo stack");
      return;
    }

    var action = _redoStack.Pop();

    try
    {
      action.RedoCallback();
      _undoStack.Push(action);
      _logger.LogInformation("Redone action: {Description}", action.Description);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during redo of action: {Description}", action.Description);
      // Put the action back on the redo stack
      _redoStack.Push(action);
      throw;
    }
  }

  /// <inheritdoc/>
  public void Clear()
  {
    _undoStack.Clear();
    _redoStack.Clear();
    _logger.LogDebug("Cleared all undo/redo history");
  }

  /// <inheritdoc/>
  public IEnumerable<UndoAction> GetUndoHistory()
  {
    return _undoStack.ToArray();
  }

  /// <inheritdoc/>
  public IEnumerable<UndoAction> GetRedoHistory()
  {
    return _redoStack.ToArray();
  }

  /// <summary>
  /// Trims the undo history to the maximum size.
  /// </summary>
  private void TrimHistory()
  {
    if (_undoStack.Count <= _maxHistorySize) return;

    var itemsToKeep = _undoStack.Take(_maxHistorySize).ToList();
    _undoStack.Clear();
    itemsToKeep.Reverse(); // Reverse to maintain stack order

    foreach (var item in itemsToKeep)
    {
      _undoStack.Push(item);
    }

    _logger.LogDebug("Trimmed undo history to {MaxSize} items", _maxHistorySize);
  }
}
