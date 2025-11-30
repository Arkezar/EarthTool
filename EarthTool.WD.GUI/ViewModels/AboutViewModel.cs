using System.Reflection;

namespace EarthTool.WD.GUI.ViewModels;

/// <summary>
/// ViewModel for the About dialog.
/// </summary>
public class AboutViewModel : ViewModelBase
{
  public string ApplicationName => "EarthTool WD Archive Manager";

  public string Version
  {
    get
    {
      var assembly = Assembly.GetExecutingAssembly();
      var version = assembly.GetName().Version;
      return version?.ToString(3) ?? "1.0.0";
    }
  }

  public string Description => "A graphical tool for managing Earth 2150 WD archive files.";
  public string Copyright => "© 2025 EarthTool Project";
  public string Author => "Arkezar";
  public string ProjectUrl => "https://github.com/Arkezar/EarthTool";

  public string FullAboutText =>
    $"{ApplicationName}\n" +
    $"Version {Version}\n\n" +
    $"{Description}\n\n" +
    $"{Copyright}\n" +
    $"By {Author}\n\n" +
    $"{ProjectUrl}";
}
