using EarthTool.PAR.Enums;
using EarthTool.PAR.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Repairer : Equipment
  {
    public Repairer()
    {
    }

    public Repairer(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data)
      : base(name, requiredResearch, type, data)
    {
      RepairerCapabilities = (RepairerCapabilityFlags)data.ReadInteger();
      RepairHPPerTick = data.ReadInteger();
      RepairElectronicsPerTick = data.ReadInteger();
      TicksPerRepair = data.ReadInteger();
      ConvertTankTime = data.ReadInteger();
      ConvertBuildingTime = data.ReadInteger();
      ConvertHealthyTankTime = data.ReadInteger();
      ConvertHealthyBuildingTime = data.ReadInteger();
      RepaintTankTime = data.ReadInteger();
      RepaintBuildingTime = data.ReadInteger();
      UpgradeTankTime = data.ReadInteger();
      AnimRepairStartStart = data.ReadInteger();
      AnimRepairStartEnd = data.ReadInteger();
      AnimRepairWorkStart = data.ReadInteger();
      AnimRepairWorkEnd = data.ReadInteger();
      AnimRepairEndStart = data.ReadInteger();
      AnimRepairEndEnd = data.ReadInteger();
      AnimConvertStartStart = data.ReadInteger();
      AnimConvertStartEnd = data.ReadInteger();
      AnimConvertWorkStart = data.ReadInteger();
      AnimConvertWorkEnd = data.ReadInteger();
      AnimConvertEndStart = data.ReadInteger();
      AnimConvertEndEnd = data.ReadInteger();
      AnimRepaintStartStart = data.ReadInteger();
      AnimRepaintStartEnd = data.ReadInteger();
      AnimRepaintWorkStart = data.ReadInteger();
      AnimRepaintWorkEnd = data.ReadInteger();
      AnimRepaintEndStart = data.ReadInteger();
      AnimRepaintEndEnd = data.ReadInteger();
    }

    public RepairerCapabilityFlags RepairerCapabilities { get; set; }

    public int RepairHPPerTick { get; set; }

    public int RepairElectronicsPerTick { get; set; }

    public int TicksPerRepair { get; set; }

    public int ConvertTankTime { get; set; }

    public int ConvertBuildingTime { get; set; }

    public int ConvertHealthyTankTime { get; set; }

    public int ConvertHealthyBuildingTime { get; set; }

    public int RepaintTankTime { get; set; }

    public int RepaintBuildingTime { get; set; }

    public int UpgradeTankTime { get; set; }

    public int AnimRepairStartStart { get; set; }

    public int AnimRepairStartEnd { get; set; }

    public int AnimRepairWorkStart { get; set; }

    public int AnimRepairWorkEnd { get; set; }

    public int AnimRepairEndStart { get; set; }

    public int AnimRepairEndEnd { get; set; }

    public int AnimConvertStartStart { get; set; }

    public int AnimConvertStartEnd { get; set; }

    public int AnimConvertWorkStart { get; set; }

    public int AnimConvertWorkEnd { get; set; }

    public int AnimConvertEndStart { get; set; }

    public int AnimConvertEndEnd { get; set; }

    public int AnimRepaintStartStart { get; set; }

    public int AnimRepaintStartEnd { get; set; }

    public int AnimRepaintWorkStart { get; set; }

    public int AnimRepaintWorkEnd { get; set; }

    public int AnimRepaintEndStart { get; set; }

    public int AnimRepaintEndEnd { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get
        => base.FieldTypes.Concat(IsStringMember(
          () => RepairerCapabilities,
          () => RepairHPPerTick,
          () => RepairElectronicsPerTick,
          () => TicksPerRepair,
          () => ConvertTankTime,
          () => ConvertBuildingTime,
          () => ConvertHealthyTankTime,
          () => ConvertHealthyBuildingTime,
          () => RepaintTankTime,
          () => RepaintBuildingTime,
          () => UpgradeTankTime,
          () => AnimRepairStartStart,
          () => AnimRepairStartEnd,
          () => AnimRepairWorkStart,
          () => AnimRepairWorkEnd,
          () => AnimRepairEndStart,
          () => AnimRepairEndEnd,
          () => AnimConvertStartStart,
          () => AnimConvertStartEnd,
          () => AnimConvertWorkStart,
          () => AnimConvertWorkEnd,
          () => AnimConvertEndStart,
          () => AnimConvertEndEnd,
          () => AnimRepaintStartStart,
          () => AnimRepaintStartEnd,
          () => AnimRepaintWorkStart,
          () => AnimRepaintWorkEnd,
          () => AnimRepaintEndStart,
          () => AnimRepaintEndEnd
        ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(base.ToByteArray(encoding));
      bw.Write((int)RepairerCapabilities);
      bw.Write(RepairHPPerTick);
      bw.Write(RepairElectronicsPerTick);
      bw.Write(TicksPerRepair);
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

      return output.ToArray();
    }
  }
}
