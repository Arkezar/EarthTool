using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class ShieldGenerator : Entity
  {
    public ShieldGenerator(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) :
      base(name, requiredResearch, type, fieldTypes)
    {
      ShieldCost = GetInteger(data);
      ShieldValue = GetInteger(data);
      ReloadTime = GetInteger(data);
      ShieldMeshName = GetString(data);
      ShieldMeshViewIndex = GetInteger(data);
    }

    public int ShieldCost { get; }

    public int ShieldValue { get; }

    public int ReloadTime { get; }

    public string ShieldMeshName { get; }

    public int ShieldMeshViewIndex { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(ShieldCost);
          bw.Write(ShieldValue);
          bw.Write(ReloadTime);
          bw.Write(ShieldMeshName.Length);
          bw.Write(encoding.GetBytes(ShieldMeshName));
          bw.Write(ShieldMeshViewIndex);
        }
        return output.ToArray();
      }
    }
  }
}