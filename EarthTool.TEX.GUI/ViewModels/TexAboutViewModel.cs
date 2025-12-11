using EarthTool.Common.GUI.ViewModels;

namespace EarthTool.TEX.GUI.ViewModels;

public class TexAboutViewModel : AboutViewModel
{
  public override string ApplicationName => "EarthTool TEX Viewer";
  public override string Description => "A viewer and converter for Earth 2150 texture files (.tex). Browse texture collections, inspect metadata, and export images with full mipmap support.";
  public override string Features => "• View texture files with header information\n• Display texture flags and properties\n• Support for multi-sided and animated textures\n• Export to PNG with mipmap levels\n• LOD (Level of Detail) support\n• Cursor definitions and damage states";
}
