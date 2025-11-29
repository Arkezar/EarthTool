using EarthTool.PAR.Enums;
using EarthTool.PAR.Models;
using EarthTool.PAR.Models.Abstracts;
using EarthTool.PAR.Tests.TestDoubles;
using System;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Tests.TestData
{
  internal static class ParTestData
  {
    public static Encoding DefaultEncoding => Encoding.UTF8;

    public static byte[] HeaderBytes => new byte[] { 0xE1, 0x21, 0x05, 0x99, 0x01, 0x00, 0x00, 0x00 };

    public static ParFile CreateSampleParFile()
    {
      return new ParFile
      {
        FileHeader = new FakeEarthInfo(HeaderBytes),
        Groups = new[]
        {
          new EntityGroup
          {
            Faction = Faction.UCS,
            GroupType = EntityGroupType.Parameter,
            Entities = new Entity[]
            {
              new Parameter
              {
                Name = "PARAM_SPEED",
                RequiredResearch = new[] { 1, 2 },
                FieldTypes = new[] { false, true },
                Values = new[] { "150", "fast" }
              }
            }
          }
        },
        Research = new[]
        {
          new Research
          {
            Id = 5,
            Faction = Faction.ED,
            CampaignCost = 100,
            SkirmishCost = 120,
            CampaignTime = 30,
            SkirmishTime = 25,
            Name = "Speed Research",
            Video = "speed.bik",
            Type = ResearchType.Special,
            Mesh = "speed_mesh",
            MeshParamsIndex = 2,
            RequiredResearch = new[] { 3, 4 }
          }
        }
      };
    }

    public static string CreateTemporaryParFile(ParFile parFile)
    {
      var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.par");
      File.WriteAllBytes(path, parFile.ToByteArray(DefaultEncoding));
      return path;
    }
  }
}
