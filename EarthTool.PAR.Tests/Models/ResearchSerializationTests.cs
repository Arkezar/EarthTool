using EarthTool.PAR.Enums;
using EarthTool.PAR.Models;
using EarthTool.PAR.Services;
using EarthTool.PAR.Tests.TestDoubles;
using System.Text;

namespace EarthTool.PAR.Tests.Models
{
  public class ResearchSerializationTests
  {
    [Fact]
    public void Research_Roundtrip_Success()
    {
      // Arrange
      var original = new Research
      {
        Id = 100,
        Faction = Faction.UCS,
        CampaignCost = 5000,
        SkirmishCost = 3000,
        CampaignTime = 180,
        SkirmishTime = 120,
        Name = "Advanced Weapon Research",
        Video = "research_weapon.bik",
        Type = ResearchType.Special,
        Mesh = "research.msh",
        MeshParamsIndex = 1,
        RequiredResearch = new[] { 90, 91, 92 }
      };

      // Act
      var bytes = original.ToByteArray(Encoding.UTF8);
      using var stream = new MemoryStream(bytes);
      using var reader = new BinaryReader(stream, Encoding.UTF8);
      var restored = new Research(reader);

      // Assert
      restored.Id.Should().Be(original.Id);
      restored.Faction.Should().Be(original.Faction);
      restored.CampaignCost.Should().Be(original.CampaignCost);
      restored.SkirmishCost.Should().Be(original.SkirmishCost);
      restored.Name.Should().Be(original.Name);
      restored.Video.Should().Be(original.Video);
      restored.Type.Should().Be(original.Type);
      restored.Mesh.Should().Be(original.Mesh);
      restored.RequiredResearch.Should().Equal(original.RequiredResearch);
    }

    [Fact]
    public void Research_WithNoRequiredResearch_Roundtrip()
    {
      // Arrange
      var research = new Research
      {
        Id = 1,
        Faction = Faction.ED,
        CampaignCost = 0,
        SkirmishCost = 0,
        CampaignTime = 0,
        SkirmishTime = 0,
        Name = "Basic Research",
        Video = "",
        Type = ResearchType.Special,
        Mesh = "",
        MeshParamsIndex = 0,
        RequiredResearch = new int[0]
      };

      // Act
      var bytes = research.ToByteArray(Encoding.UTF8);
      using var stream = new MemoryStream(bytes);
      using var reader = new BinaryReader(stream, Encoding.UTF8);
      var restored = new Research(reader);

      // Assert
      restored.Id.Should().Be(research.Id);
      restored.Name.Should().Be(research.Name);
      restored.RequiredResearch.Should().BeEmpty();
    }
  }
}
