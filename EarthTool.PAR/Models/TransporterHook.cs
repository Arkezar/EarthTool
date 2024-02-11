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
    public TransporterHook(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
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
  }
}
