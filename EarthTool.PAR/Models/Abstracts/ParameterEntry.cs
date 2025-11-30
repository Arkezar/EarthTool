using EarthTool.PAR.Extensions;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class ParameterEntry
  {
    protected int ReferenceMarker => BinaryExtensions.ReferenceMarker;
    
    public string Name { get; set; }
  }
}
