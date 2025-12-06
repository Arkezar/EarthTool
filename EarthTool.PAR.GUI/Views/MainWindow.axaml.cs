using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EarthTool.PAR.GUI.ViewModels;
using System.Threading.Tasks;

namespace EarthTool.PAR.GUI.Views;

public partial class MainWindow : Window
{
  public MainWindow()
  {
    InitializeComponent();
    
    // Handle copy command
    AddHandler(Button.ClickEvent, OnButtonClick, RoutingStrategies.Bubble);
  }

  private async void OnButtonClick(object? sender, RoutedEventArgs e)
  {
    if (e.Source is Button button && button.DataContext is PropertyEditorViewModel viewModel)
    {
      if (button.Command == viewModel.CopyValueCommand)
      {
        var valueText = viewModel.Value?.ToString() ?? string.Empty;
        await CopyToClipboardAsync(valueText);
      }
      else if (button.Command == viewModel.NavigateToReferenceCommand)
      {
        var referenceValue = viewModel.Value?.ToString() ?? string.Empty;
        NavigateToReference(referenceValue);
      }
    }
  }

  private void NavigateToReference(string referenceName)
  {
    if (string.IsNullOrEmpty(referenceName))
      return;

    // Get MainWindowViewModel from DataContext
    if (DataContext is MainWindowViewModel mainViewModel)
    {
      // Find entity by name in the tree
      // For now, just log - proper implementation would search through RootNodes
      System.Diagnostics.Debug.WriteLine($"Navigate to reference: {referenceName}");
      
      // TODO: Implement actual navigation by searching tree and selecting the node
      // This would require exposing a search method in MainWindowViewModel
    }
  }

  private async Task CopyToClipboardAsync(string text)
  {
    var clipboard = Clipboard;
    if (clipboard != null)
    {
      await clipboard.SetTextAsync(text);
    }
  }
}
