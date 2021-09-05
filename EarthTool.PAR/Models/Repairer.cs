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
    public Repairer(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data)
    {
      if (fieldTypes.Count() > 44)
      {
        RepairerFlags = BitConverter.ToInt32(data.ReadBytes(4));
        RepairHPPerTick = BitConverter.ToInt32(data.ReadBytes(4));
        RepairElectronicsPerTick = BitConverter.ToInt32(data.ReadBytes(4));
        TicksPerRepair = BitConverter.ToInt32(data.ReadBytes(4));
      }
      ConvertTankTime = BitConverter.ToInt32(data.ReadBytes(4));
      ConvertBuildingTime = BitConverter.ToInt32(data.ReadBytes(4));
      ConvertHealthyTankTime = BitConverter.ToInt32(data.ReadBytes(4));
      ConvertHealthyBuildingTime = BitConverter.ToInt32(data.ReadBytes(4));
      RepaintTankTime = BitConverter.ToInt32(data.ReadBytes(4));
      RepaintBuildingTime = BitConverter.ToInt32(data.ReadBytes(4));
      UpgradeTankTime = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepairStartStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepairStartEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepairWorkStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepairWorkEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepairEndStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepairEndEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimConvertStartStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimConvertStartEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimConvertWorkStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimConvertWorkEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimConvertEndStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimConvertEndEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepaintStartStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepaintStartEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepaintWorkStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepaintWorkEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepaintEndStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimRepaintEndEnd = BitConverter.ToInt32(data.ReadBytes(4));
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
  }
}
