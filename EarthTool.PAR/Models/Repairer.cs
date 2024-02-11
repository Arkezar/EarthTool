using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Repairer : Equipment
  {
    public Repairer(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
    {
      if (fieldTypes.Count() > 44)
      {
        RepairerFlags = GetInteger(data);
        RepairHPPerTick = GetInteger(data);
        RepairElectronicsPerTick = GetInteger(data);
        TicksPerRepair = GetInteger(data);
      }
      ConvertTankTime = GetInteger(data);
      ConvertBuildingTime = GetInteger(data);
      ConvertHealthyTankTime = GetInteger(data);
      ConvertHealthyBuildingTime = GetInteger(data);
      RepaintTankTime = GetInteger(data);
      RepaintBuildingTime = GetInteger(data);
      UpgradeTankTime = GetInteger(data);
      AnimRepairStartStart = GetInteger(data);
      AnimRepairStartEnd = GetInteger(data);
      AnimRepairWorkStart = GetInteger(data);
      AnimRepairWorkEnd = GetInteger(data);
      AnimRepairEndStart = GetInteger(data);
      AnimRepairEndEnd = GetInteger(data);
      AnimConvertStartStart = GetInteger(data);
      AnimConvertStartEnd = GetInteger(data);
      AnimConvertWorkStart = GetInteger(data);
      AnimConvertWorkEnd = GetInteger(data);
      AnimConvertEndStart = GetInteger(data);
      AnimConvertEndEnd = GetInteger(data);
      AnimRepaintStartStart = GetInteger(data);
      AnimRepaintStartEnd = GetInteger(data);
      AnimRepaintWorkStart = GetInteger(data);
      AnimRepaintWorkEnd = GetInteger(data);
      AnimRepaintEndStart = GetInteger(data);
      AnimRepaintEndEnd = GetInteger(data);
    }

    public int RepairerFlags { get; }

    public int RepairHPPerTick { get; }

    public int RepairElectronicsPerTick { get; }

    public int TicksPerRepair { get; }

    public int ConvertTankTime { get; }

    public int ConvertBuildingTime { get; }

    public int ConvertHealthyTankTime { get; }

    public int ConvertHealthyBuildingTime { get; }

    public int RepaintTankTime { get; }

    public int RepaintBuildingTime { get; }

    public int UpgradeTankTime { get; }

    public int AnimRepairStartStart { get; }

    public int AnimRepairStartEnd { get; }

    public int AnimRepairWorkStart { get; }

    public int AnimRepairWorkEnd { get; }

    public int AnimRepairEndStart { get; }

    public int AnimRepairEndEnd { get; }

    public int AnimConvertStartStart { get; }

    public int AnimConvertStartEnd { get; }

    public int AnimConvertWorkStart { get; }

    public int AnimConvertWorkEnd { get; }

    public int AnimConvertEndStart { get; }

    public int AnimConvertEndEnd { get; }

    public int AnimRepaintStartStart { get; }

    public int AnimRepaintStartEnd { get; }

    public int AnimRepaintWorkStart { get; }

    public int AnimRepaintWorkEnd { get; }

    public int AnimRepaintEndStart { get; }

    public int AnimRepaintEndEnd { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          if (FieldTypes.Count() > 44)
          {
            bw.Write(RepairerFlags);
            bw.Write(RepairHPPerTick);
            bw.Write(RepairElectronicsPerTick);
            bw.Write(TicksPerRepair);
          }
          bw.Write(ConvertTankTime);
          bw.Write(ConvertBuildingTime);
          bw.Write(ConvertHealthyTankTime);
          bw.Write(ConvertHealthyBuildingTime);
          bw.Write(RepaintTankTime);
          bw.Write(RepaintBuildingTime);
          bw.Write(UpgradeTankTime);
          bw.Write(AnimRepairStartStart);
          bw.Write(AnimRepairStartEnd);
          bw.Write(AnimRepairWorkStart);
          bw.Write(AnimRepairWorkEnd);
          bw.Write(AnimRepairEndStart);
          bw.Write(AnimRepairEndEnd);
          bw.Write(AnimConvertStartStart);
          bw.Write(AnimConvertStartEnd);
          bw.Write(AnimConvertWorkStart);
          bw.Write(AnimConvertWorkEnd);
          bw.Write(AnimConvertEndStart);
          bw.Write(AnimConvertEndEnd);
          bw.Write(AnimRepaintStartStart);
          bw.Write(AnimRepaintStartEnd);
          bw.Write(AnimRepaintWorkStart);
          bw.Write(AnimRepaintWorkEnd);
          bw.Write(AnimRepaintEndStart);
          bw.Write(AnimRepaintEndEnd);
        }
        return output.ToArray();
      }
    }
  }
}
