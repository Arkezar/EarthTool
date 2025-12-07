using EarthTool.Common.GUI.ViewModels;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for the About dialog.
/// </summary>
public class ParAboutViewModel : AboutViewModel
{
  public override string ApplicationName => "EarthTool PAR Editor";

  public override string Description => "A graphical tool for editing Earth 2150 PAR parameter files.";

  public override string Features => "Edit entities, research, and parameters with an intuitive interface.\nSupports undo/redo, validation, and advanced property editors.";
}
