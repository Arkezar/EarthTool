using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EarthTool.Common;
using EarthTool.Common.GUI;
using EarthTool.TEX.GUI.ViewModels;
using EarthTool.TEX.GUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EarthTool.TEX.GUI;

public partial class App : Application
{
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
    var serviceProvider = services.BuildServiceProvider();

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      var mainViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
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

    // TEX services
    services.AddTexServices();
    
    // // PAR Editor services
    // services.AddSingleton<IParFileService, ParFileService>();
    // services.AddSingleton<IUndoRedoService, UndoRedoService>();
    // services.AddSingleton<IEntityValidationService, EntityValidationService>();
    // services.AddSingleton<IPropertyEditorFactory, PropertyEditorFactory>();

    // GUI services
    services.AddCommonGuiServices();

    // ViewModels
    // services.AddTransient<EntityDetailsViewModel>();
    services.AddTransient<MainWindowViewModel>();
  }
}
