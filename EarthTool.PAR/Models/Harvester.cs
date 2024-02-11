using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Harvester : Vehicle
  {
    public Harvester(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
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

    public int ContainerCount { get; }

    public int TicksPerContainer { get; }

    public int PutResourceAngle { get; }

    public int AnimHarvestStartStart { get; }
    
    public int AnimHarvestStartEnd { get; }

    public int AnimHarvestWorkStart { get; }

    public int AnimHarvestWorkEnd { get; }

    public int AnimHarvestEndStart { get; }

    public int AnimHarvestEndEnd { get; }

    public string HarvestSomkeId { get; }
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
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
