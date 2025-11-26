using Microsoft.Extensions.Logging;
using System;

namespace EarthTool.WD.GUI.Services;

/// <summary>
/// Implementation of INotificationService that logs messages and raises events.
/// </summary>
public class NotificationService : INotificationService
{
  private readonly ILogger<NotificationService> _logger;

  public event EventHandler<NotificationEventArgs>? NotificationRaised;

  public NotificationService(ILogger<NotificationService> logger)
  {
    _logger = logger;
  }

  public void ShowError(string message, Exception? exception = null)
  {
    _logger.LogError(exception, message);
    OnNotificationRaised(new NotificationEventArgs(NotificationType.Error, message, exception));
  }

  public void ShowWarning(string message)
  {
    _logger.LogWarning(message);
    OnNotificationRaised(new NotificationEventArgs(NotificationType.Warning, message));
  }

  public void ShowSuccess(string message)
  {
    _logger.LogInformation("Success: {Message}", message);
    OnNotificationRaised(new NotificationEventArgs(NotificationType.Success, message));
  }

  public void ShowInfo(string message)
  {
    _logger.LogInformation(message);
    OnNotificationRaised(new NotificationEventArgs(NotificationType.Info, message));
  }

  protected virtual void OnNotificationRaised(NotificationEventArgs e)
  {
    NotificationRaised?.Invoke(this, e);
  }
}