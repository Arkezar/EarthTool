using System.Collections.Generic;
using System.Threading.Tasks;

namespace EarthTool.WD.GUI.Services;

/// <summary>
/// Service for displaying file dialogs and user interactions.
/// </summary>
public interface IDialogService
{
  /// <summary>
  /// Shows an open file dialog for selecting a WD archive file.
  /// </summary>
  /// <returns>The selected file path, or null if cancelled.</returns>
  Task<string?> ShowOpenFileDialogAsync();

  /// <summary>
  /// Shows a save file dialog for saving a WD archive.
  /// </summary>
  /// <param name="defaultFileName">Default file name to suggest.</param>
  /// <returns>The selected file path, or null if cancelled.</returns>
  Task<string?> ShowSaveFileDialogAsync(string? defaultFileName = null);

  /// <summary>
  /// Shows a folder browser dialog for selecting an extraction directory.
  /// </summary>
  /// <returns>The selected folder path, or null if cancelled.</returns>
  Task<string?> ShowFolderBrowserDialogAsync();

  /// <summary>
  /// Shows an open file dialog for selecting multiple files to add to archive.
  /// </summary>
  /// <returns>List of selected file paths, or empty list if cancelled.</returns>
  Task<IReadOnlyList<string>> ShowOpenFilesDialogAsync();

  /// <summary>
  /// Shows a message box with the specified message and title.
  /// </summary>
  /// <param name="message">The message to display.</param>
  /// <param name="title">The title of the message box.</param>
  /// <param name="messageBoxType">Type of message box (OK, YesNo, etc.).</param>
  /// <returns>The result of the message box interaction.</returns>
  Task<MessageBoxResult> ShowMessageBoxAsync(string message, string title, MessageBoxType messageBoxType = MessageBoxType.Ok);

  /// <summary>
  /// Shows an input dialog for entering text.
  /// </summary>
  /// <param name="message">The message to display.</param>
  /// <param name="title">The title of the dialog.</param>
  /// <param name="defaultValue">Default value for the input.</param>
  /// <returns>The entered text, or null if cancelled.</returns>
  Task<string?> ShowInputDialogAsync(string message, string title, string? defaultValue = null);
}

/// <summary>
/// Type of message box to display.
/// </summary>
public enum MessageBoxType
{
  Ok,
  OkCancel,
  YesNo,
  YesNoCancel
}

/// <summary>
/// Result of a message box interaction.
/// </summary>
public enum MessageBoxResult
{
  Ok,
  Cancel,
  Yes,
  No
}