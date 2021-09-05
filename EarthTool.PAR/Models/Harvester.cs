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
    public Harvester(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      ContainerCount = BitConverter.ToInt32(data.ReadBytes(4));
      TicksPerContainer = BitConverter.ToInt32(data.ReadBytes(4));
      PutResourceAngle = BitConverter.ToInt32(data.ReadBytes(4));
      AnimHarvestStartStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimHarvestStartEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimHarvestWorkStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimHarvestWorkEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimHarvestEndStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimHarvestEndEnd = BitConverter.ToInt32(data.ReadBytes(4));
      HarvestSomkeId = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
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
  }
}
