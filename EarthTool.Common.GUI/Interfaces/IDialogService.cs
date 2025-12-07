using EarthTool.Common.GUI.Enums;
using EarthTool.Common.GUI.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EarthTool.Common.GUI.Interfaces;

/// <summary>
/// Service for displaying file dialogs and user interactions.
/// </summary>
public interface IDialogService
{
  /// <summary>
  /// Shows a folder browser dialog for selecting the game data directory.
  /// </summary>
  /// <returns>The selected folder path, or null if cancelled.</returns>
  Task<string?> ShowFolderBrowserDialogAsync(string title);

  /// <summary>
  /// Shows an open file dialog.
  /// </summary>
  /// <param name="title">Dialog title.</param>
  /// <param name="allowMultiple"></param>
  /// <param name="filters">File type filters (DisplayName, Pattern).</param>
  /// <returns>The selected file path, or null if cancelled.</returns>
  Task<IEnumerable<string?>>  ShowOpenFilesDialogAsync(string title = "Open File", bool allowMultiple = false, params (string DisplayName, string Pattern)[] filters);

  /// <summary>
  /// Shows a save file dialog for extracting a file.
  /// </summary>
  /// <param name="defaultFileName">Default file name to suggest.</param>
  /// <returns>The selected file path, or null if cancelled.</returns>
  Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, params (string DisplayName, string Pattern)[] fileTypes);

  /// <summary>
  /// Shows a message box with the specified message and title.
  /// </summary>
  /// <param name="message">The message to display.</param>
  /// <param name="title">The title of the message box.</param>
  /// <param name="messageBoxType">Type of message box (OK, YesNo, etc.).</param>
  /// <returns>The result of the message box interaction.</returns>
  Task<MessageBoxResult> ShowMessageBoxAsync(string message, string title, MessageBoxType messageBoxType = MessageBoxType.Ok);

  /// <summary>
  /// Shows a custom dialog with the specified content.
  /// </summary>
  /// <param name="content">The content control to display in the dialog.</param>
  /// <param name="title">The title of the dialog.</param>
  /// <param name="width">The width of the dialog.</param>
  /// <param name="height">The height of the dialog.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  Task ShowCustomDialogAsync(object content, string title, double width = 500, double height = 450);

  Task ShowAboutAsync<TViewModel>(TViewModel viewModel)
    where TViewModel : AboutViewModel;

  Task<string?> ShowInputDialogAsync(string message, string title, string? defaultValue = null);
}
