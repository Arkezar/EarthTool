using System.Threading.Tasks;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Service for displaying file dialogs and user interactions.
/// </summary>
public interface IDialogService
{
  /// <summary>
  /// Shows a folder browser dialog for selecting the game data directory.
  /// </summary>
  /// <returns>The selected folder path, or null if cancelled.</returns>
  Task<string?> ShowFolderBrowserDialogAsync();

  /// <summary>
  /// Shows an open file dialog.
  /// </summary>
  /// <param name="title">Dialog title.</param>
  /// <param name="filters">File type filters (DisplayName, Pattern).</param>
  /// <returns>The selected file path, or null if cancelled.</returns>
  Task<string?> ShowOpenFileDialogAsync(string title = "Open File", params (string DisplayName, string Pattern)[] filters);

  /// <summary>
  /// Shows a save file dialog for extracting a file.
  /// </summary>
  /// <param name="defaultFileName">Default file name to suggest.</param>
  /// <returns>The selected file path, or null if cancelled.</returns>
  Task<string?> ShowSaveFileDialogAsync(string? defaultFileName = null);

  /// <summary>
  /// Shows a message box with the specified message and title.
  /// </summary>
  /// <param name="message">The message to display.</param>
  /// <param name="title">The title of the message box.</param>
  /// <param name="messageBoxType">Type of message box (OK, YesNo, etc.).</param>
  /// <returns>The result of the message box interaction.</returns>
  Task<MessageBoxResult> ShowMessageBoxAsync(string message, string title, MessageBoxType messageBoxType = MessageBoxType.Ok);
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
