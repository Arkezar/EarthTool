using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.WD.GUI.Services;

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

  public async Task<string?> ShowOpenFileDialogAsync()
  {
    var window = GetMainWindow();
    if (window == null) return null;

    var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
    {
      Title = "Open WD Archive",
      AllowMultiple = false,
      FileTypeFilter = new[]
      {
        new FilePickerFileType("WD Archive Files")
        {
          Patterns = new[] { "*.WD", "*.wd" }
        },
        new FilePickerFileType("All Files")
        {
          Patterns = new[] { "*.*" }
        }
      }
    });

    return files.Count > 0 ? files[0].Path.LocalPath : null;
  }

  public async Task<string?> ShowSaveFileDialogAsync(string? defaultFileName = null)
  {
    var window = GetMainWindow();
    if (window == null) return null;

    var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
    {
      Title = "Save WD Archive",
      SuggestedFileName = defaultFileName ?? "archive.WD",
      FileTypeChoices = new[]
      {
        new FilePickerFileType("WD Archive Files")
        {
          Patterns = new[] { "*.WD", "*.wd" }
        },
        new FilePickerFileType("All Files")
        {
          Patterns = new[] { "*.*" }
        }
      }
    });

    return file?.Path.LocalPath;
  }

  public async Task<string?> ShowFolderBrowserDialogAsync()
  {
    var window = GetMainWindow();
    if (window == null) return null;

    var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
    {
      Title = "Select Extraction Folder",
      AllowMultiple = false
    });

    return folders.Count > 0 ? folders[0].Path.LocalPath : null;
  }

  public async Task<IReadOnlyList<string>> ShowOpenFilesDialogAsync()
  {
    var window = GetMainWindow();
    if (window == null) return Array.Empty<string>();

    var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
    {
      Title = "Select Files to Add",
      AllowMultiple = true,
      FileTypeFilter = new[]
      {
        new FilePickerFileType("All Files")
        {
          Patterns = new[] { "*.*" }
        }
      }
    });

    return files.Select(f => f.Path.LocalPath).ToList();
  }

  public async Task<MessageBoxResult> ShowMessageBoxAsync(
    string message,
    string title,
    MessageBoxType messageBoxType = MessageBoxType.Ok)
  {
    var window = GetMainWindow();
    if (window == null) return MessageBoxResult.Cancel;

    // Create a modern dialog window
    var dialog = new Window
    {
      Title = title,
      Width = 450,
      MinWidth = 500,
      MinHeight = 220,
      SizeToContent = SizeToContent.WidthAndHeight,
      MaxHeight = 600,
      CanResize = true,
      WindowStartupLocation = WindowStartupLocation.CenterOwner,
      SystemDecorations = SystemDecorations.BorderOnly
    };

    // Main border with rounded corners
    var mainBorder = new Border
    {
      CornerRadius = new CornerRadius(8),
      ClipToBounds = true
    };

    // Main grid with header, content, and footer sections
    var mainGrid = new Grid();
    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

    // Set background for main grid
    if (Application.Current?.Resources.TryGetResource("SystemControlBackgroundAltHighBrush", null, out var bgBrush) == true)
    {
      mainGrid.Background = bgBrush as Avalonia.Media.IBrush;
    }

    // Header section with accent color
    var headerBorder = new Border
    {
      Padding = new Thickness(30, 20),
      CornerRadius = new CornerRadius(8, 8, 0, 0)
    };
    Grid.SetRow(headerBorder, 0);

    if (Application.Current?.Resources.TryGetResource("SystemAccentColor", null, out var accentColor) == true)
    {
      headerBorder.Background = accentColor as Avalonia.Media.IBrush;
    }

    var headerText = new TextBlock
    {
      Text = title,
      FontSize = 18,
      FontWeight = Avalonia.Media.FontWeight.SemiBold,
      Foreground = Avalonia.Media.Brushes.White,
      TextAlignment = Avalonia.Media.TextAlignment.Left
    };
    headerBorder.Child = headerText;
    mainGrid.Children.Add(headerBorder);

    // Content section with message
    var contentScrollViewer = new ScrollViewer
    {
      HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
      VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
      Padding = new Thickness(30, 25)
    };
    Grid.SetRow(contentScrollViewer, 1);

    var messageText = new TextBlock
    {
      Text = message,
      FontSize = 13,
      TextWrapping = Avalonia.Media.TextWrapping.Wrap,
      LineHeight = 20
    };

    if (Application.Current?.Resources.TryGetResource("SystemControlForegroundBaseHighBrush", null, out var fgBrush) == true)
    {
      messageText.Foreground = fgBrush as Avalonia.Media.IBrush;
    }

    contentScrollViewer.Content = messageText;
    mainGrid.Children.Add(contentScrollViewer);

    // Footer section with buttons
    var footerBorder = new Border
    {
      Padding = new Thickness(30, 20),
      CornerRadius = new CornerRadius(0, 0, 8, 8),
      BorderThickness = new Thickness(0, 1, 0, 0)
    };
    Grid.SetRow(footerBorder, 2);

    if (Application.Current?.Resources.TryGetResource("SystemControlBackgroundChromeMediumBrush", null, out var footerBg) == true)
    {
      footerBorder.Background = footerBg as Avalonia.Media.IBrush;
    }

    if (Application.Current?.Resources.TryGetResource("SystemControlForegroundBaseLowBrush", null, out var borderBrush) == true)
    {
      footerBorder.BorderBrush = borderBrush as Avalonia.Media.IBrush;
    }

    var buttonPanel = CreateButtonPanel(messageBoxType, dialog);
    buttonPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
    footerBorder.Child = buttonPanel;
    mainGrid.Children.Add(footerBorder);

    mainBorder.Child = mainGrid;
    dialog.Content = mainBorder;

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
      MinWidth = 100,
      Height = 36,
      IsDefault = isDefault,
      Padding = new Thickness(20, 8),
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

  public async Task<string?> ShowInputDialogAsync(string message, string title, string? defaultValue = null)
  {
    var window = GetMainWindow();
    if (window == null)
      return null;

    // Create a modern dialog window
    var dialog = new Window
    {
      Title = title,
      Width = 450,
      MinWidth = 450,
      MinHeight = 240,
      SizeToContent = SizeToContent.Height,
      MaxHeight = 500,
      CanResize = true,
      WindowStartupLocation = WindowStartupLocation.CenterOwner,
      SystemDecorations = SystemDecorations.BorderOnly
    };

    // Main border with rounded corners
    var mainBorder = new Border
    {
      CornerRadius = new CornerRadius(8),
      ClipToBounds = true
    };

    // Main grid with header, content, and footer sections
    var mainGrid = new Grid();
    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

    // Set background for main grid
    if (Application.Current?.Resources.TryGetResource("SystemControlBackgroundAltHighBrush", null, out var bgBrush) == true)
    {
      mainGrid.Background = bgBrush as Avalonia.Media.IBrush;
    }

    // Header section with accent color
    var headerBorder = new Border
    {
      Padding = new Thickness(30, 20),
      CornerRadius = new CornerRadius(8, 8, 0, 0)
    };
    Grid.SetRow(headerBorder, 0);

    if (Application.Current?.Resources.TryGetResource("SystemAccentColor", null, out var accentColor) == true)
    {
      headerBorder.Background = accentColor as Avalonia.Media.IBrush;
    }

    var headerText = new TextBlock
    {
      Text = title,
      FontSize = 18,
      FontWeight = Avalonia.Media.FontWeight.SemiBold,
      Foreground = Avalonia.Media.Brushes.White,
      TextAlignment = Avalonia.Media.TextAlignment.Left
    };
    headerBorder.Child = headerText;
    mainGrid.Children.Add(headerBorder);

    // Content section with message and input
    var contentPanel = new StackPanel
    {
      Spacing = 15,
      Margin = new Thickness(30, 25)
    };
    Grid.SetRow(contentPanel, 1);

    var messageText = new TextBlock
    {
      Text = message,
      FontSize = 13,
      TextWrapping = Avalonia.Media.TextWrapping.Wrap,
      LineHeight = 20
    };

    if (Application.Current?.Resources.TryGetResource("SystemControlForegroundBaseHighBrush", null, out var fgBrush) == true)
    {
      messageText.Foreground = fgBrush as Avalonia.Media.IBrush;
    }

    var textBox = new TextBox
    {
      Text = defaultValue ?? "",
      Watermark = "Enter value...",
      MinHeight = 32,
      Padding = new Thickness(10, 6)
    };

    contentPanel.Children.Add(messageText);
    contentPanel.Children.Add(textBox);
    mainGrid.Children.Add(contentPanel);

    // Footer section with buttons
    var footerBorder = new Border
    {
      Padding = new Thickness(30, 20),
      CornerRadius = new CornerRadius(0, 0, 8, 8),
      BorderThickness = new Thickness(0, 1, 0, 0)
    };
    Grid.SetRow(footerBorder, 2);

    if (Application.Current?.Resources.TryGetResource("SystemControlBackgroundChromeMediumBrush", null, out var footerBg) == true)
    {
      footerBorder.Background = footerBg as Avalonia.Media.IBrush;
    }

    if (Application.Current?.Resources.TryGetResource("SystemControlForegroundBaseLowBrush", null, out var borderBrush) == true)
    {
      footerBorder.BorderBrush = borderBrush as Avalonia.Media.IBrush;
    }

    // Create buttons
    var okButton = new Button
    {
      Content = "OK",
      MinWidth = 100,
      Height = 36,
      IsDefault = true,
      Padding = new Thickness(20, 8),
      HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
      VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
    };
    okButton.Click += (_, _) =>
    {
      dialog.Tag = textBox.Text;
      dialog.Close();
    };

    var cancelButton = new Button
    {
      Content = "Cancel",
      MinWidth = 100,
      Height = 36,
      IsCancel = true,
      Padding = new Thickness(20, 8),
      HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
      VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
    };
    cancelButton.Click += (_, _) =>
    {
      dialog.Tag = null;
      dialog.Close();
    };

    var buttonPanel = new StackPanel
    {
      Orientation = Avalonia.Layout.Orientation.Horizontal,
      HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
      Spacing = 10
    };
    buttonPanel.Children.Add(okButton);
    buttonPanel.Children.Add(cancelButton);

    footerBorder.Child = buttonPanel;
    mainGrid.Children.Add(footerBorder);

    mainBorder.Child = mainGrid;
    dialog.Content = mainBorder;

    // Focus the textbox when dialog opens and select all text
    dialog.Opened += (_, _) =>
    {
      textBox.Focus();
      textBox.SelectAll();
    };

    await dialog.ShowDialog(window);
    return dialog.Tag as string;
  }

  public async Task ShowCustomDialogAsync(object content, string title, double width = 500, double height = 450)
  {
    var window = GetMainWindow();
    if (window == null)
      return;

    var dialog = new Window
    {
      Title = title,
      Width = width,
      Height = height,
      CanResize = false,
      WindowStartupLocation = WindowStartupLocation.CenterOwner,
      Content = content
    };

    await dialog.ShowDialog(window);
  }
}
