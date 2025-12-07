using EarthTool.Common.GUI.Enums;
using System;

namespace EarthTool.Common.GUI.Interfaces;

/// <summary>
/// Service for displaying notifications and status messages to the user.
/// </summary>
public interface INotificationService
{
  /// <summary>
  /// Shows an error message to the user.
  /// </summary>
  /// <param name="message">The error message.</param>
  /// <param name="exception">Optional exception that caused the error.</param>
  void ShowError(string message, Exception? exception = null);

  /// <summary>
  /// Shows a warning message to the user.
  /// </summary>
  /// <param name="message">The warning message.</param>
  void ShowWarning(string message);

  /// <summary>
  /// Shows a success message to the user.
  /// </summary>
  /// <param name="message">The success message.</param>
  void ShowSuccess(string message);

  /// <summary>
  /// Shows an informational message to the user.
  /// </summary>
  /// <param name="message">The informational message.</param>
  void ShowInfo(string message);

  /// <summary>
  /// Event raised when a notification is displayed.
  /// </summary>
  event EventHandler<NotificationEventArgs>? NotificationRaised;
}

/// <summary>
/// Event arguments for notification events.
/// </summary>
public class NotificationEventArgs : EventArgs
{
  public NotificationType Type { get; }
  public string Message { get; }
  public Exception? Exception { get; }

  public NotificationEventArgs(NotificationType type, string message, Exception? exception = null)
  {
    Type = type;
    Message = message;
    Exception = exception;
  }
}
