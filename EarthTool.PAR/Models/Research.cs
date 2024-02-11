using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.PAR.Models
{
  public class Research : ParameterEntry
  {
    public Research(BinaryReader data)
    {
      Id = GetInteger(data);
      Faction = (Faction)GetInteger(data);
      CampaignCost = GetInteger(data);
      SkirmishCost = GetInteger(data);
      CampaignTime = GetInteger(data);
      SkirmishTime = GetInteger(data);
      Name = GetString(data);
      Video = GetString(data);
      Type = (ResearchType)GetInteger(data);
      Mesh = GetString(data);
      MeshParamsIndex = GetInteger(data);
      var requiredResearchCount = GetInteger(data);
      RequiredResearch = Enumerable.Range(0, requiredResearchCount).Select(i => GetInteger(data)).ToList();
    }
    
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
    
    public IEnumerable<int> RequiredResearch { get; }
    
    protected int GetInteger(BinaryReader data)
    {
      return data.ReadInt32();
    }

    protected string GetString(BinaryReader data)
    {
      return new string(data.ReadChars(data.ReadInt32()));
    }
  }
}
