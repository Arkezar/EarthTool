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

    // Create a simple dialog window
    var dialog = new Window
    {
      Title = title,
      Width = 400,
      Height = 200,
      CanResize = false,
      WindowStartupLocation = WindowStartupLocation.CenterOwner
    };

    var textBlock = new TextBlock
    {
      Text = message,
      TextWrapping = Avalonia.Media.TextWrapping.Wrap,
      VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
    };

    var buttonPanel = CreateButtonPanel(messageBoxType, dialog);

    var stackPanel = new StackPanel
    {
      Margin = new Thickness(20),
      Spacing = 15
    };
    stackPanel.Children.Add(textBlock);
    stackPanel.Children.Add(buttonPanel);

    dialog.Content = stackPanel;

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
      Width = 80,
      IsDefault = isDefault
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

    var dialog = new Window
    {
      Title = title,
      Width = 400,
      Height = 180,
      CanResize = false,
      WindowStartupLocation = WindowStartupLocation.CenterOwner
    };

    var textBlock = new TextBlock
    {
      Text = message,
      TextWrapping = Avalonia.Media.TextWrapping.Wrap,
      Margin = new Avalonia.Thickness(0, 0, 0, 10)
    };

    var textBox = new TextBox
    {
      Text = defaultValue ?? "",
      Watermark = "Enter folder name...",
      Margin = new Avalonia.Thickness(0, 0, 0, 15)
    };

    var okButton = new Button
    {
      Content = "OK",
      Width = 80,
      IsDefault = true,
      Margin = new Avalonia.Thickness(0, 0, 10, 0)
    };
    okButton.Click += (_, _) =>
    {
      dialog.Tag = textBox.Text;
      dialog.Close();
    };

    var cancelButton = new Button
    {
      Content = "Cancel",
      Width = 80,
      IsCancel = true
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

    var stackPanel = new StackPanel
    {
      Margin = new Avalonia.Thickness(20),
      Spacing = 10
    };
    stackPanel.Children.Add(textBlock);
    stackPanel.Children.Add(textBox);
    stackPanel.Children.Add(buttonPanel);

    dialog.Content = stackPanel;

    // Focus the textbox when dialog opens
    dialog.Opened += (_, _) => textBox.Focus();

    await dialog.ShowDialog(window);
    return dialog.Tag as string;
  }
}