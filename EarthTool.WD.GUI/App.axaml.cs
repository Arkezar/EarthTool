using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EarthTool.WD.GUI.ViewModels;
using EarthTool.WD.GUI.Views;

namespace EarthTool.WD.GUI;

public partial class App : Application
{
  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel(), };
    }

    base.OnFrameworkInitializationCompleted();
  }
}