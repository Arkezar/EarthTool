namespace EarthTool.WD.GUI.ViewModels;

/// <summary>
/// ViewModel for the About dialog.
/// </summary>
public class AboutViewModel : ViewModelBase
{
  public string ApplicationName => "EarthTool WD Archive Manager";
  public string Version => "1.0.0";
  public string Description => "A graphical tool for managing Earth 2150 WD archive files.";
  public string Copyright => "© 2025 EarthTool Project";
  public string Author => "Arkezar";

  public string FullAboutText =>
    $"{ApplicationName}\n" +
    $"Version {Version}\n\n" +
    $"{Description}\n\n" +
    $"{Copyright}\n" +
    $"By {Author}";
}