using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Parameter : Entity
  {
    public Parameter(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type)
    {
      Values = fieldTypes.Select(s => s ? (object)GetString(data) : GetInteger(data)).ToList();
    }

    public IEnumerable<object> Values { get; }

    private int GetInteger(Stream data)
    {
      return BitConverter.ToInt32(data.ReadBytes(4));
    }

    private string GetString(Stream data)
    {
      return Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
    }
  }
}
