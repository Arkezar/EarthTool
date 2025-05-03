using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EarthTool.PAR.GUI.Services;
using EarthTool.PAR.GUI.ViewModels;
using EarthTool.PAR.GUI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace EarthTool.PAR.GUI;

public partial class App : Application
{
  public override void Initialize()
  {
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    // Register all the services needed for the application to run
    var collection = ConfigureServices();

    // Creates a ServiceProvider containing services from the provided IServiceCollection
    var services = collection.BuildServiceProvider();

    var vm = services.GetRequiredService<MainWindowViewModel>();

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      desktop.MainWindow = new MainWindow { DataContext = vm };
    }

    base.OnFrameworkInitializationCompleted();
  }

  private static IServiceCollection ConfigureServices()
  {
    var services = new ServiceCollection();
    services.AddParServices();
    services.AddTransient<MainWindowViewModel>();
    services.AddTransient<ParameterTreeBuilder>();
    return services;
  }
}