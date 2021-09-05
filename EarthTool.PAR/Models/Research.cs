using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Research
  {
    public Research(Stream data)
    {
      var requiredResearchCount = BitConverter.ToInt32(data.ReadBytes(4));
      RequiredResearch = Enumerable.Range(0, requiredResearchCount).Select(i => BitConverter.ToInt32(data.ReadBytes(4))).ToList();
      Id = BitConverter.ToInt32(data.ReadBytes(4));
      Faction = (Faction)BitConverter.ToInt32(data.ReadBytes(4));
      CampaignCost = BitConverter.ToInt32(data.ReadBytes(4));
      SkirmishCost = BitConverter.ToInt32(data.ReadBytes(4));
      CampaignTime = BitConverter.ToInt32(data.ReadBytes(4));
      SkirmishTime = BitConverter.ToInt32(data.ReadBytes(4));
      Name = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      Video = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      Type = (ResearchType)BitConverter.ToInt32(data.ReadBytes(4));
      Mesh = Encoding.ASCII.GetString(data.ReadBytes(BitConverter.ToInt32(data.ReadBytes(4))));
      MeshParamsIndex = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public IEnumerable<int> RequiredResearch { get; }

    public int Id { get; }

    public Faction Faction { get; }

    public int CampaignCost { get; }

    public int SkirmishCost { get; }

    public int CampaignTime { get; }

    public int SkirmishTime { get; }

    public string Name { get; }

    public string Video { get; }

    public ResearchType Type { get; }

    public string Mesh { get; }

    public int MeshParamsIndex { get; }
  }
}
