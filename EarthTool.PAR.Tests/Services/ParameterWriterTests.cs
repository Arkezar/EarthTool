using EarthTool.PAR.Models;
using EarthTool.PAR.Services;
using EarthTool.PAR.Tests.TestData;
using EarthTool.PAR.Tests.TestDoubles;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.PAR.Tests.Services
{
  public class ParameterWriterTests
  {
    [Fact]
    public void Write_CreatesFileWithExpectedPayload()
    {
      var writer = new ParameterWriter(ParTestData.DefaultEncoding);
      var sample = ParTestData.CreateSampleParFile();
      var outputPath = BuildTempFilePath("writer_exact");

      try
      {
        var resultPath = writer.Write(sample, outputPath);

        resultPath.Should().Be(outputPath);
        var bytes = File.ReadAllBytes(resultPath);
        bytes.Should().Equal(sample.ToByteArray(ParTestData.DefaultEncoding));
      }
      finally
      {
        DeleteIfExists(outputPath);
      }
    }

    [Fact]
    public void Write_And_Read_AreSymmetric()
    {
      var writer = new ParameterWriter(ParTestData.DefaultEncoding);
      var reader = new ParameterReader(new FakeEarthInfoFactory(ParTestData.HeaderBytes), ParTestData.DefaultEncoding);
      var sample = ParTestData.CreateSampleParFile();
      var outputPath = BuildTempFilePath("writer_roundtrip");

      try
      {
        writer.Write(sample, outputPath);

        var roundTrip = reader.Read(outputPath);

        roundTrip.Should().NotBeNull();
        var actual = roundTrip!;
        actual.Groups.Should().HaveCount(sample.Groups.Count());
        actual.Research.Should().HaveCount(sample.Research.Count());

        var originalGroup = sample.Groups.Should().ContainSingle().Subject;
        var actualGroup = actual.Groups.Should().ContainSingle().Subject;
        actualGroup.GroupType.Should().Be(originalGroup.GroupType);

        var originalParameter = originalGroup.Entities.Should().ContainSingle().Which
          .Should().BeOfType<Parameter>().Which;
        var actualParameter = actualGroup.Entities.Should().ContainSingle().Which
          .Should().BeOfType<Parameter>().Which;
        actualParameter.Name.Should().Be(originalParameter.Name);
        actualParameter.RequiredResearch.Should().Equal(originalParameter.RequiredResearch);
        actualParameter.FieldTypes.Should().Equal(originalParameter.FieldTypes);
        actualParameter.Values.Should().Equal(originalParameter.Values);
      }
      finally
      {
        DeleteIfExists(outputPath);
      }
    }

    private static string BuildTempFilePath(string prefix)
    {
      return Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}.par");
    }

    private static void DeleteIfExists(string path)
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }
}
