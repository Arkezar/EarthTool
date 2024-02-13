using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Abstracts
{
  public class PolymorphicEntity : ParameterEntry
  {
    public PolymorphicEntity()
    {
      TypeName = GetType().FullName;
    }

    [JsonPropertyName("$type")] 
    public string TypeName { get; set; }
  }
}