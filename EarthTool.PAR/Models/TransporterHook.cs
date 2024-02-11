using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class TransporterHook : Equipment
  {
    public TransporterHook(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      AnimTransporterDownStart = GetInteger(data);
      AnimTransporterDownEnd = GetInteger(data);
      AnimTransporterUpStart = GetInteger(data);
      AnimTransporterUpEnd = GetInteger(data);
      AngleToGetPut = GetInteger(data);
      AngleOfGetUnitByLandTransporter = GetInteger(data);
      TakeHeight = GetInteger(data);
    }

    public int AnimTransporterDownStart { get; }

    public int AnimTransporterDownEnd { get; }

    public int AnimTransporterUpStart { get; }

    public int AnimTransporterUpEnd { get; }

    public int AngleToGetPut { get; }

    public int AngleOfGetUnitByLandTransporter { get; }

    public int TakeHeight { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(AnimTransporterDownStart);
          bw.Write(AnimTransporterDownEnd);
          bw.Write(AnimTransporterUpStart);
          bw.Write(AnimTransporterUpEnd);
          bw.Write(AngleToGetPut);
          bw.Write(AngleOfGetUnitByLandTransporter);
          bw.Write(TakeHeight);
        }
        return output.ToArray();
      }
    }
  }
}
