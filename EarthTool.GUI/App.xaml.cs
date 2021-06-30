using Autofac;
using Autofac.Extensions.DependencyInjection;
using EarthTool.MSH;
using EarthTool.MSH.Converters.Collada;
using EarthTool.MSH.Converters.Wavefront;
using EarthTool.TEX;
using EarthTool.WD;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;
using System.Windows;

namespace EarthTool.GUI
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private readonly IHost _host;

    public App()
    {
      _host = Initialize().Build();
    }

    private static IHostBuilder Initialize()
    {
      var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();

      return Host.CreateDefaultBuilder()
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .ConfigureLogging(config =>
        {
          config.AddConfiguration(configuration.GetSection("Logging"));
          config.AddDebug();
        })
        .ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
          containerBuilder.RegisterModule<WDModule>();
          containerBuilder.RegisterModule<TEXModule>();
          containerBuilder.RegisterModule<MSHModule>();
          containerBuilder.RegisterModule<MSHColladaModule>();
          containerBuilder.RegisterModule<MSHWavefrontModule>();
        })
        .ConfigureServices((ctx, services) =>
        {
          services.AddSingleton<MainWindow>();
        });
    }

    protected override void OnStartup(StartupEventArgs e)
    {
      var window = _host.Services.GetRequiredService<MainWindow>();
      window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
      using (_host)
      {
        await _host.StopAsync(TimeSpan.FromSeconds(5));
      }

      base.OnExit(e);
    }
  }
}
