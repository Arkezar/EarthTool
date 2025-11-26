using EarthTool.Common.Interfaces;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class Entity : PolymorphicEntity, IBinarySerializable
  {
    public Entity()
    {
    }

    public Entity(string name, IEnumerable<int> requiredResearch, EntityClassType type) : this()
    {
      Name = name;
      RequiredResearch = requiredResearch;
      ClassId = type;
    }

    public string Name { get; set; }

    public IEnumerable<int> RequiredResearch { get; set; }

    [JsonIgnore]
    public abstract IEnumerable<bool> FieldTypes { get; set; }

    public EntityClassType ClassId { get; set; }

    public virtual byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(Name.Length);
          bw.Write(encoding.GetBytes(Name));
          bw.Write(RequiredResearch.Count());
          foreach (int research in RequiredResearch)
          {
            bw.Write(research);
          }

          bw.Write(FieldTypes.Count());
          foreach (bool fieldType in FieldTypes)
          {
            bw.Write(fieldType);
          }
        }

        return output.ToArray();
      }
    }

    protected IEnumerable<bool> IsStringMember(params Func<object>[] fieldGetter)
    {
      return fieldGetter.Select(f => f.Invoke() is string);
    }
  }
}
