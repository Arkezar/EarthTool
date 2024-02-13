using EarthTool.Common.Interfaces;
using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class Research : ParameterEntry, IBinarySerializable
  {
    public Research()
    {
    }

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
      int requiredResearchCount = GetInteger(data);
      RequiredResearch = Enumerable.Range(0, requiredResearchCount).Select(i => GetInteger(data)).ToList();
    }

    public int Id { get; set; }

    public Faction Faction { get; set; }

    public int CampaignCost { get; set; }

    public int SkirmishCost { get; set; }

    public int CampaignTime { get; set; }

    public int SkirmishTime { get; set; }

    public string Name { get; set; }

    public string Video { get; set; }

    public ResearchType Type { get; set; }

    public string Mesh { get; set; }

    public int MeshParamsIndex { get; set; }

    public IEnumerable<int> RequiredResearch { get; set; }

    public virtual byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
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
          foreach (int research in RequiredResearch)
          {
            bw.Write(research);
          }
        }

        return output.ToArray();
      }
    }

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