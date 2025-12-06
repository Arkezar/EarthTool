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
