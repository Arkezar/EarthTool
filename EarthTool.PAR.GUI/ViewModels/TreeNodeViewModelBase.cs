using ReactiveUI;
using System;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Base class for all tree node ViewModels in the entity hierarchy.
/// Supports 4-level tree: Faction -> GroupType -> EntityGroup -> Entity
/// </summary>
public abstract class TreeNodeViewModelBase : ViewModelBase
{
  private bool _isExpanded;
  private bool _isVisible = true;

  /// <summary>
  /// Display name shown in the tree (with optional count)
  /// </summary>
  public abstract string DisplayName { get; }

  /// <summary>
  /// Icon emoji for the node type
  /// </summary>
  public abstract string Icon { get; }

  /// <summary>
  /// Child nodes (null for leaf nodes like Entity)
  /// </summary>
  public abstract ObservableCollection<TreeNodeViewModelBase>? Children { get; }

  /// <summary>
  /// Total count of descendants (used in DisplayName)
  /// </summary>
  public abstract int ChildCount { get; }

  /// <summary>
  /// Whether the node is expanded in the tree
  /// </summary>
  public bool IsExpanded
  {
    get => _isExpanded;
    set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
  }

  /// <summary>
  /// Whether the node is visible (used for filtering)
  /// </summary>
  public bool IsVisible
  {
    get => _isVisible;
    set => this.RaiseAndSetIfChanged(ref _isVisible, value);
  }

  /// <summary>
  /// Apply search filter recursively. Returns true if node or descendants match.
  /// Hides non-matching branches, expands matching ones.
  /// </summary>
  public virtual bool ApplyFilter(string searchText)
  {
    if (string.IsNullOrWhiteSpace(searchText))
    {
      IsVisible = true;
      IsExpanded = false; // Collapse all when filter cleared
      if (Children != null)
      {
        foreach (var child in Children)
          child.ApplyFilter(searchText);
      }
      return true;
    }

    bool hasMatchingChild = false;
    if (Children != null)
    {
      foreach (var child in Children)
      {
        if (child.ApplyFilter(searchText))
          hasMatchingChild = true;
      }
    }

    // Check if node's display name matches
    bool nameMatches = DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    IsVisible = nameMatches || hasMatchingChild;

    // Expand nodes with matching descendants
    if (IsVisible && hasMatchingChild)
      IsExpanded = true;

    return IsVisible;
  }
}
