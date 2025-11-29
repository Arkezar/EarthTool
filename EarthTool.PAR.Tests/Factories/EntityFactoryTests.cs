using EarthTool.PAR.Enums;
using EarthTool.PAR.Factories;
using EarthTool.PAR.Models;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Tests.Factories
{
  public class EntityFactoryTests
  {
    [Fact]
    public void CreateEntity_ReturnsParameterEntityForParameterGroup()
    {
      var factory = new EntityFactory();
      using var stream = new MemoryStream(BuildParameterBytes());
      using var reader = new BinaryReader(stream, Encoding.UTF8);

      var entity = factory.CreateEntity(reader, EntityGroupType.Parameter);

      entity.Should().BeOfType<Parameter>();
      var parameter = (Parameter)entity;
      parameter.Name.Should().Be("PARAM_SPEED");
      parameter.RequiredResearch.Should().Equal(new[] { 1, 2 });
      parameter.FieldTypes.Should().Equal(new[] { false, true });
      parameter.Values.Should().Equal(new[] { "150", "fast" });
    }

    private static byte[] BuildParameterBytes()
    {
      var parameter = new Parameter
      {
        Name = "PARAM_SPEED",
        RequiredResearch = new[] { 1, 2 },
        FieldTypes = new[] { false, true },
        Values = new[] { "150", "fast" }
      };

      return parameter.ToByteArray(Encoding.UTF8);
    }
  }
}
