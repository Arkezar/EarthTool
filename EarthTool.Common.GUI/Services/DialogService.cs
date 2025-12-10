using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using EarthTool.Common.GUI.Enums;
using EarthTool.Common.GUI.Interfaces;
using EarthTool.Common.GUI.ViewModels;
using EarthTool.Common.GUI.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.Common.GUI.Services;

/// <summary>
/// Implementation of IDialogService using Avalonia's StorageProvider API.
/// </summary>
public class DialogService : IDialogService
{
  private readonly ILogger<DialogService> _logger;
  private readonly INotificationService   _notificationService;

  public DialogService(ILogger<DialogService> logger, INotificationService notificationService)
  {
    _logger = logger;
    _notificationService = notificationService;
  }
  
  private Window? GetMainWindow()
  {
    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      return desktop.MainWindow;
    }
    return null;
  }

  public async Task<IEnumerable<string?>> ShowOpenFilesDialogAsync(string title = "Open File", bool allowMultiple = false, params (string DisplayName, string Pattern)[] filters)
  {
    var window = GetMainWindow();
    if (window == null) return null;

    var fileTypeChoices = filters.Length > 0
      ? filters.Select(f => new FilePickerFileType(f.DisplayName) { Patterns = new[] { f.Pattern, f.Pattern.ToUpper() } }).ToList()
      : new[] { new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } } }.ToList();

    var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
    {
      Title = title,
      AllowMultiple = allowMultiple,
      FileTypeFilter = fileTypeChoices,
    });

    return files.Select(f=> f.Path.LocalPath);
  }

  public async Task<string?> ShowFolderBrowserDialogAsync(string title)
  {
    var window = GetMainWindow();
    if (window == null) return null;

    var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
    {
      Title = title,
      AllowMultiple = false
    });

    return folders.Count > 0 ? folders[0].Path.LocalPath : null;
  }

  public async Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, params (string DisplayName, string Pattern)[] fileTypes)
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

  public async Task ShowAboutAsync<TViewModel>(TViewModel viewModel) where TViewModel : AboutViewModel
  {
    try
    {
      var aboutView = new AboutView()
      {
        DataContext = viewModel
      };

      await ShowCustomDialogAsync(
        aboutView,
        "About EarthTool PAR Editor",
        width: 550,
        height: 550);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to show About dialog");
      _notificationService.ShowError("Failed to show About dialog", ex);
    }
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
}
