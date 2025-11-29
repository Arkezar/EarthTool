using EarthTool.Common;
using EarthTool.PAR.Enums;
using EarthTool.PAR.Models;
using EarthTool.PAR.Services;
using EarthTool.PAR.Tests.TestData;
using EarthTool.PAR.Tests.TestDoubles;
using System;
using System.IO;

namespace EarthTool.PAR.Tests.Services
{
  public class ParameterReaderTests
  {
    [Fact]
    public void Read_ReturnsNull_WhenFileDoesNotExist()
    {
      var reader = CreateReader();

      var result = reader.Read(Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.par"));

      result.Should().BeNull();
    }

    [Fact]
    public void Read_Throws_WhenSignatureIsInvalid()
    {
      var reader = CreateReader();
      var filePath = CreateFileWithInvalidSignature();

      try
      {
        Action act = () => reader.Read(filePath);

        act.Should().Throw<NotSupportedException>();
      }
      finally
      {
        File.Delete(filePath);
      }
    }

    [Fact]
    public void Read_ParsesGroupsAndResearch()
    {
      var reader = CreateReader();
      var samplePar = ParTestData.CreateSampleParFile();
      var filePath = ParTestData.CreateTemporaryParFile(samplePar);

      try
      {
        var result = reader.Read(filePath);

        result.Should().NotBeNull();
        var parFile = result!;
        parFile.FileHeader.ToByteArray(ParTestData.DefaultEncoding)
          .Should().Equal(ParTestData.HeaderBytes);

        var group = parFile.Groups.Should().ContainSingle().Subject;
        group.Faction.Should().Be(Faction.UCS);
        group.GroupType.Should().Be(EntityGroupType.Parameter);

        var parameter = group.Entities.Should().ContainSingle().Which;
        var typedParameter = parameter.Should().BeOfType<Parameter>().Which;
        typedParameter.Name.Should().Be("PARAM_SPEED");
        typedParameter.RequiredResearch.Should().Equal(new[] { 1, 2 });
        typedParameter.FieldTypes.Should().Equal(new[] { false, true });
        typedParameter.Values.Should().Equal(new[] { "150", "fast" });

        var research = parFile.Research.Should().ContainSingle().Which;
        research.Id.Should().Be(5);
        research.Faction.Should().Be(Faction.ED);
        research.Name.Should().Be("Speed Research");
        research.RequiredResearch.Should().Equal(new[] { 3, 4 });
      }
      finally
      {
        File.Delete(filePath);
      }
    }

    private static ParameterReader CreateReader()
    {
      return new ParameterReader(new FakeEarthInfoFactory(ParTestData.HeaderBytes), ParTestData.DefaultEncoding);
    }

    private static string CreateFileWithInvalidSignature()
    {
      var path = Path.Combine(Path.GetTempPath(), $"invalid_{Guid.NewGuid():N}.par");

      using var stream = File.Create(path);
      stream.Write(ParTestData.HeaderBytes, 0, ParTestData.HeaderBytes.Length);
      stream.Write(new byte[Identifiers.Paramters.Length], 0, Identifiers.Paramters.Length);

      return path;
    }
  }
}
