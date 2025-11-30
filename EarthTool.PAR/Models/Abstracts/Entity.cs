using EarthTool.Common.Interfaces;
using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class Entity : ParameterEntry, IBinarySerializable
  {
    public Entity()
    {
      TypeName = GetType().FullName;
    }

    [JsonPropertyName("$type")]
    public string TypeName { get; set; }

    public Entity(string name, IEnumerable<int> requiredResearch, EntityClassType type) : this()
    {
      Name = name;
      RequiredResearch = requiredResearch;
      ClassId = type;
    }

    public IEnumerable<int> RequiredResearch { get; set; }

    [JsonIgnore]
    public abstract IEnumerable<bool> FieldTypes { get; set; }

    public EntityClassType ClassId { get; set; }

    public virtual byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();
      using var bw = new BinaryWriter(output, encoding);
      bw.WriteParameterString(Name, encoding);
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

      return output.ToArray();
    }

    protected IEnumerable<bool> IsStringMember(params Func<object>[] fieldGetter)
    {
      return fieldGetter.Select(f => f.Invoke() is string);
    }
  }
}
