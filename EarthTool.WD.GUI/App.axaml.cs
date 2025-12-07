using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EarthTool.Common;
using EarthTool.Common.GUI;
using EarthTool.WD;
using EarthTool.WD.GUI.Services;
using EarthTool.WD.GUI.ViewModels;
using EarthTool.WD.GUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace EarthTool.WD.GUI;

public partial class App : Application
{
  private IServiceProvider? _serviceProvider;

  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    // Configure dependency injection
    var services = new ServiceCollection();
    ConfigureServices(services);
    _serviceProvider = services.BuildServiceProvider();

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
      desktop.MainWindow = new MainWindow { DataContext = mainViewModel };
    }

    base.OnFrameworkInitializationCompleted();
  }

  private void ConfigureServices(IServiceCollection services)
  {
    // Logging
    services.AddLogging(builder =>
    {
      builder.AddConsole();
      builder.AddDebug();
      builder.SetMinimumLevel(LogLevel.Debug);
    });

    // Common services (Encoding, EarthInfoFactory)
    services.AddCommonServices();

    // WD services (ArchiverService, ArchiveFactory, Compressor, Decompressor)
    services.AddWdServices();

    // GUI services
    services.AddCommonGuiServices();
    services.AddSingleton<ITextFlagService, TextFlagService>();

    // ViewModels
    services.AddTransient<MainWindowViewModel>();
  }

  /// <summary>
  /// Gets a service from the DI container.
  /// </summary>
  public static T? GetService<T>() where T : class
  {
    if (Current is App app)
    {
      return app._serviceProvider?.GetService<T>();
    }
    return null;
  }
}
