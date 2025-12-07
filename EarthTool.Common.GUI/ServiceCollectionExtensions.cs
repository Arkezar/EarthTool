using EarthTool.Common.GUI.Interfaces;
using EarthTool.Common.GUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EarthTool.Common.GUI;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddCommonGuiServices(this IServiceCollection services)
    => services
      .AddScoped<IDialogService, DialogService>()
      .AddScoped<INotificationService, NotificationService>();
}
