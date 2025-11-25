using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EarthTool.WD.GUI.ViewModels;

namespace EarthTool.WD.GUI.Views;

public partial class MainWindow : Window
{
  public MainWindow()
  {
    InitializeComponent();
    
    // Add keyboard handler for ESC to clear selection
    this.AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
  }

  private void OnKeyDown(object? sender, KeyEventArgs e)
  {
    if (e.Key == Key.Escape && DataContext is MainWindowViewModel viewModel)
    {
      viewModel.SelectedItem = null;
      e.Handled = true;
    }
  }

  private void TreeView_PointerPressed(object? sender, PointerPressedEventArgs e)
  {
    // Check if clicked on empty space (not on a tree item)
    if (sender is TreeView treeView && DataContext is MainWindowViewModel viewModel)
    {
      var point = e.GetCurrentPoint(treeView);
      var element = treeView.InputHitTest(point.Position);
      
      // Walk up the visual tree to see if we hit a TreeViewItem
      var current = element as Control;
      bool hitTreeViewItem = false;
      
      while (current != null && current != treeView)
      {
        if (current is TreeViewItem)
        {
          hitTreeViewItem = true;
          break;
        }
        current = current.Parent as Control;
      }
      
      // If we didn't hit a TreeViewItem, clear selection
      if (!hitTreeViewItem)
      {
        viewModel.SelectedItem = null;
      }
    }
  }
}