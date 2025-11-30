using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;

namespace EarthTool.WD.GUI.ViewModels;

/// <summary>
/// ViewModel for the About dialog.
/// </summary>
public class AboutViewModel : ViewModelBase
{
  public AboutViewModel()
  {
    OpenUrlCommand = ReactiveCommand.Create<string>(OpenUrl);
  }

  public string ApplicationName => "EarthTool WD Archive Manager";

  public string Version
  {
    get
    {
      var assembly = Assembly.GetExecutingAssembly();
      
      // Try to get InformationalVersion first (full SemVer from GitVersion)
      var infoVersion = assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion;
      
      if (!string.IsNullOrEmpty(infoVersion))
      {
        return infoVersion;
      }
      
      // Fallback to standard version
      var version = assembly.GetName().Version;
      return version?.ToString(3) ?? "0.0.1-dev";
    }
  }

  public string FullVersion => $"Version {Version}";

  public string Description => "A graphical tool for managing Earth 2150 WD archive files.";

  public string Features => "Extract, create, and modify WD archives with an intuitive interface.\nSupports compression, text flags, and folder structures.";

  public string Copyright => "© 2025 EarthTool Project";

  public string Author => "Arkezar";

  public string ProjectUrl => "https://github.com/Arkezar/EarthTool";

  public string LicenseInfo => "Licensed under MIT License";

  public ReactiveCommand<string, Unit> OpenUrlCommand { get; }

  private void OpenUrl(string url)
  {
    try
    {
      // Cross-platform URL opening
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        Process.Start("xdg-open", url);
      }
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        Process.Start("open", url);
      }
    }
    catch (Exception)
    {
      // Silently fail if we can't open the URL
    }
  }
}
