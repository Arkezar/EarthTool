using EarthTool.PAR.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(Id);
          bw.Write((int)Faction);
          bw.Write(CampaignCost);
          bw.Write(SkirmishCost);
          bw.Write(CampaignTime);
          bw.Write(SkirmishTime);
          bw.Write(Name.Length);
          bw.Write(encoding.GetBytes(Name));
          bw.Write(Video.Length);
          bw.Write(encoding.GetBytes(Video));
          bw.Write((int)Type);
          bw.Write(Mesh.Length);
          bw.Write(encoding.GetBytes(Mesh));
          bw.Write(MeshParamsIndex);
          bw.Write(RequiredResearch.Count());
          foreach (var research in RequiredResearch)
          {
            bw.Write(research);
          }
        }
        return output.ToArray();
      }
    }
  }
}
