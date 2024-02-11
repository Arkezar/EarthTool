using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models
{
  public abstract class Entity : ParameterEntry
  {
    public Entity(string name, IEnumerable<int> requiredResearch, EntityClassType type, IEnumerable<bool> fieldTypes)
    {
      Name = name;
      RequiredResearch = requiredResearch;
      ClassId = type;
      FieldTypes = fieldTypes;
    }

    public string Name { get; }

    public IEnumerable<int> RequiredResearch { get; }
    
    public IEnumerable<bool> FieldTypes { get; }

    public EntityClassType ClassId { get; }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(Name.Length);
          bw.Write(encoding.GetBytes(Name));
          bw.Write(RequiredResearch.Count());
          foreach (var research in RequiredResearch)
          {
            bw.Write(research);
          }
          bw.Write(FieldTypes.Count());
          foreach (var fieldType in FieldTypes)
          {
            bw.Write(fieldType);
          }
          if(ClassId != EntityClassType.None)
          {
            bw.Write((int)ClassId);
          }
        }
        return output.ToArray();
      }
    }
  }
}
