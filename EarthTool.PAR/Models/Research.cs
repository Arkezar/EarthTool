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
      Id = ReadInteger(data);
      Faction = (Faction)ReadInteger(data);
      CampaignCost = ReadInteger(data);
      SkirmishCost = ReadInteger(data);
      CampaignTime = ReadInteger(data);
      SkirmishTime = ReadInteger(data);
      Name = ReadString(data);
      Video = ReadString(data);
      Type = (ResearchType)ReadInteger(data);
      Mesh = ReadString(data);
      MeshParamsIndex = ReadInteger(data);
      int requiredResearchCount = ReadInteger(data);
      RequiredResearch = Enumerable.Range(0, requiredResearchCount).Select(i => ReadInteger(data)).ToList();
    }

    public int Id { get; set; }

    public Faction Faction { get; set; }

    public int CampaignCost { get; set; }

    public int SkirmishCost { get; set; }

    public int CampaignTime { get; set; }

    public int SkirmishTime { get; set; }

    public string Video { get; set; }

    public ResearchType Type { get; set; }

    public string Mesh { get; set; }

    public int MeshParamsIndex { get; set; }

    public IEnumerable<int> RequiredResearch { get; set; }

    public virtual byte[] ToByteArray(Encoding encoding)
    {
      using var output = new MemoryStream();

      using var bw = new BinaryWriter(output, encoding);
      bw.Write(Id);
      bw.Write((int)Faction);
      bw.Write(CampaignCost);
      bw.Write(SkirmishCost);
      bw.Write(CampaignTime);
      bw.Write(SkirmishTime);
      WriteString(bw, Name, encoding);
      WriteString(bw, Video, encoding);
      bw.Write((int)Type);
      WriteString(bw, Mesh, encoding);
      bw.Write(MeshParamsIndex);
      bw.Write(RequiredResearch.Count());
      foreach (int research in RequiredResearch)
      {
        bw.Write(research);
      }

      return output.ToArray();
    }
  }
}
