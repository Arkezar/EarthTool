using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class Harvester : Vehicle
  {
    public Harvester()
    {
    }

    public Harvester(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(
      name, requiredResearch, type, data)
    {
      ContainerCount = GetInteger(data);
      TicksPerContainer = GetInteger(data);
      PutResourceAngle = GetInteger(data);
      AnimHarvestStartStart = GetInteger(data);
      AnimHarvestStartEnd = GetInteger(data);
      AnimHarvestWorkStart = GetInteger(data);
      AnimHarvestWorkEnd = GetInteger(data);
      AnimHarvestEndStart = GetInteger(data);
      AnimHarvestEndEnd = GetInteger(data);
      HarvestSomkeId = GetString(data);
      data.ReadBytes(4);
    }

    public int ContainerCount { get; set; }

    public int TicksPerContainer { get; set; }

    public int PutResourceAngle { get; set; }

    public int AnimHarvestStartStart { get; set; }

    public int AnimHarvestStartEnd { get; set; }

    public int AnimHarvestWorkStart { get; set; }

    public int AnimHarvestWorkEnd { get; set; }

    public int AnimHarvestEndStart { get; set; }

    public int AnimHarvestEndEnd { get; set; }

    public string HarvestSomkeId { get; set; }

    [JsonIgnore]
    public override IEnumerable<bool> FieldTypes
    {
      get => base.FieldTypes.Concat(IsStringMember(
        () => ContainerCount,
        () => TicksPerContainer,
        () => PutResourceAngle,
        () => AnimHarvestStartStart,
        () => AnimHarvestStartEnd,
        () => AnimHarvestWorkStart,
        () => AnimHarvestWorkEnd,
        () => AnimHarvestEndStart,
        () => AnimHarvestEndEnd,
        () => HarvestSomkeId,
        () => 1
      ));
      set => base.FieldTypes = value;
    }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(ContainerCount);
          bw.Write(TicksPerContainer);
          bw.Write(PutResourceAngle);
          bw.Write(AnimHarvestStartStart);
          bw.Write(AnimHarvestStartEnd);
          bw.Write(AnimHarvestWorkStart);
          bw.Write(AnimHarvestWorkEnd);
          bw.Write(AnimHarvestEndStart);
          bw.Write(AnimHarvestEndEnd);
          bw.Write(HarvestSomkeId.Length);
          bw.Write(encoding.GetBytes(HarvestSomkeId));
          bw.Write(-1);
        }

        return output.ToArray();
      }
    }
  }
}
