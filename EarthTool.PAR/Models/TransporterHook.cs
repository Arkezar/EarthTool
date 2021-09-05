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
    public TransporterHook(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      AnimTransporterDownStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimTransporterDownEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimTransporterUpStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimTransporterUpEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AngleToGetPut = BitConverter.ToInt32(data.ReadBytes(4));
      AngleOfGetUnitByLandTransporter = BitConverter.ToInt32(data.ReadBytes(4));
      TakeHeight = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int AnimTransporterDownStart { get; }

    public int AnimTransporterDownEnd { get; }

    public int AnimTransporterUpStart { get; }

    public int AnimTransporterUpEnd { get; }

    public int AngleToGetPut { get; }

    public int AngleOfGetUnitByLandTransporter { get; }

    public int TakeHeight { get; }
  }
}
