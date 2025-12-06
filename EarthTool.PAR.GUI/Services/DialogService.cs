using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Implementation of IDialogService using Avalonia's StorageProvider API.
/// </summary>
public class DialogService : IDialogService
{
  private Window? GetMainWindow()
  {
    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      return desktop.MainWindow;
    }
    return null;
  }

  public async Task<string?> ShowOpenFileDialogAsync(string title = "Open File", params (string DisplayName, string Pattern)[] filters)
  {
    var window = GetMainWindow();
    if (window == null) return null;

    var fileTypeChoices = filters.Length > 0
      ? filters.Select(f => new FilePickerFileType(f.DisplayName) { Patterns = new[] { f.Pattern } }).ToList()
      : new[] { new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } } }.ToList();

    var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
    {
      Title = title,
      AllowMultiple = false,
      FileTypeFilter = fileTypeChoices
    });

    return files.Count > 0 ? files[0].Path.LocalPath : null;
  }

  public async Task<string?> ShowFolderBrowserDialogAsync()
  {
    var window = GetMainWindow();
    if (window == null) return null;

    var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
    {
      Title = "Select Game Data Folder",
      AllowMultiple = false
    });

    return folders.Count > 0 ? folders[0].Path.LocalPath : null;
  }

  public async Task<string?> ShowSaveFileDialogAsync(string? defaultFileName = null)
  {
    var window = GetMainWindow();
    if (window == null) return null;

    var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
    {
      Title = "Save File",
      SuggestedFileName = defaultFileName ?? "file",
      FileTypeChoices = new[]
      {
        new FilePickerFileType("All Files")
        {
          Patterns = new[] { "*.*" }
        }
      }
    });

    return file?.Path.LocalPath;
  }

  public async Task<MessageBoxResult> ShowMessageBoxAsync(
    string message,
    string title,
    MessageBoxType messageBoxType = MessageBoxType.Ok)
  {
    var window = GetMainWindow();
    if (window == null) return MessageBoxResult.Cancel;

    // Create a simple dialog window
    var dialog = new Window
    {
      Title = title,
      Width = 450,
      MinWidth = 450,
      MinHeight = 180,
      SizeToContent = SizeToContent.Height,
      MaxHeight = 600,
      CanResize = false,
      WindowStartupLocation = WindowStartupLocation.CenterOwner,
      SystemDecorations = SystemDecorations.BorderOnly
    };

    // Main grid with content and buttons
    var mainGrid = new Grid();
    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

    // Message content
    var contentScrollViewer = new ScrollViewer
    {
      HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
      VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
      Padding = new Thickness(20)
    };
    Grid.SetRow(contentScrollViewer, 0);

    var messageText = new TextBlock
    {
      Text = message,
      TextWrapping = Avalonia.Media.TextWrapping.Wrap,
      LineHeight = 20
    };

    contentScrollViewer.Content = messageText;
    mainGrid.Children.Add(contentScrollViewer);

    // Button panel
    var footerBorder = new Border
    {
      Padding = new Thickness(20, 15),
      BorderThickness = new Thickness(0, 1, 0, 0)
    };
    Grid.SetRow(footerBorder, 1);

    var buttonPanel = CreateButtonPanel(messageBoxType, dialog);
    buttonPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
    footerBorder.Child = buttonPanel;
    mainGrid.Children.Add(footerBorder);

    dialog.Content = mainGrid;

    await dialog.ShowDialog(window);
    return dialog.Tag as MessageBoxResult? ?? MessageBoxResult.Cancel;
  }

  private Panel CreateButtonPanel(MessageBoxType type, Window dialog)
  {
    var panel = new StackPanel
    {
      Orientation = Avalonia.Layout.Orientation.Horizontal,
      HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
      Spacing = 10
    };

    switch (type)
    {
      case MessageBoxType.Ok:
        panel.Children.Add(CreateButton("OK", MessageBoxResult.Ok, dialog, true));
        break;
      case MessageBoxType.OkCancel:
        panel.Children.Add(CreateButton("OK", MessageBoxResult.Ok, dialog, true));
        panel.Children.Add(CreateButton("Cancel", MessageBoxResult.Cancel, dialog, false));
        break;
      case MessageBoxType.YesNo:
        panel.Children.Add(CreateButton("Yes", MessageBoxResult.Yes, dialog, true));
        panel.Children.Add(CreateButton("No", MessageBoxResult.No, dialog, false));
        break;
      case MessageBoxType.YesNoCancel:
        panel.Children.Add(CreateButton("Yes", MessageBoxResult.Yes, dialog, true));
        panel.Children.Add(CreateButton("No", MessageBoxResult.No, dialog, false));
        panel.Children.Add(CreateButton("Cancel", MessageBoxResult.Cancel, dialog, false));
        break;
    }

    return panel;
  }

  private Button CreateButton(string content, MessageBoxResult result, Window dialog, bool isDefault)
  {
    var button = new Button
    {
      Content = content,
      MinWidth = 80,
      Height = 32,
      IsDefault = isDefault,
      Padding = new Thickness(15, 6),
      HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
      VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
    };

    button.Click += (_, _) =>
    {
      dialog.Tag = result;
      dialog.Close();
    };

    return button;
  }
}
