using EarthTool.PAR.Enums;
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
      RepairerCapabilities = (RepairerCapabilityFlags)ReadInteger(data);
      RepairHPPerTick = ReadInteger(data);
      RepairElectronicsPerTick = ReadInteger(data);
      TicksPerRepair = ReadInteger(data);
      ConvertTankTime = ReadInteger(data);
      ConvertBuildingTime = ReadInteger(data);
      ConvertHealthyTankTime = ReadInteger(data);
      ConvertHealthyBuildingTime = ReadInteger(data);
      RepaintTankTime = ReadInteger(data);
      RepaintBuildingTime = ReadInteger(data);
      UpgradeTankTime = ReadInteger(data);
      AnimRepairStartStart = ReadInteger(data);
      AnimRepairStartEnd = ReadInteger(data);
      AnimRepairWorkStart = ReadInteger(data);
      AnimRepairWorkEnd = ReadInteger(data);
      AnimRepairEndStart = ReadInteger(data);
      AnimRepairEndEnd = ReadInteger(data);
      AnimConvertStartStart = ReadInteger(data);
      AnimConvertStartEnd = ReadInteger(data);
      AnimConvertWorkStart = ReadInteger(data);
      AnimConvertWorkEnd = ReadInteger(data);
      AnimConvertEndStart = ReadInteger(data);
      AnimConvertEndEnd = ReadInteger(data);
      AnimRepaintStartStart = ReadInteger(data);
      AnimRepaintStartEnd = ReadInteger(data);
      AnimRepaintWorkStart = ReadInteger(data);
      AnimRepaintWorkEnd = ReadInteger(data);
      AnimRepaintEndStart = ReadInteger(data);
      AnimRepaintEndEnd = ReadInteger(data);
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
