using EarthTool.Common.GUI.ViewModels;

namespace EarthTool.WD.GUI.ViewModels;

/// <summary>
/// ViewModel for the About dialog.
/// </summary>
public class WdAboutViewModel : AboutViewModel
{
  public override string ApplicationName => "EarthTool WD Archive Manager";

  public override string Description => "A graphical tool for managing Earth 2150 WD archive files.";

  public override string Features => "Extract, create, and modify WD archives with an intuitive interface.\nSupports compression, text flags, and folder structures.";
}
