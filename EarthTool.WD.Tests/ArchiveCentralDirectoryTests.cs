using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using System.Linq;
using EarthTool.WD.Models;
using EarthTool.WD.Resources;
using Xunit;
using System.IO;

namespace EarthTool.WD.Tests
{
  public class ArchiveCentralDirectoryTests : ArchiveTestsBase
  {
    public ArchiveCentralDirectoryTests()
    {
      Fixture.Customize<ArchiveFileHeader>(c => c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));
    }

    [Fact]
    public void SerializedAndDeserializedCentralDirectoryShouldBeEquivalent()
    {
      // Given
      var centralDirectory = Fixture.Create<ArchiveCentralDirectory>();
      var header1 = Fixture.Create<ArchiveFileHeader>();
      centralDirectory.Add(header1);

      // When
      var serialized = centralDirectory.ToByteArray();

      using var input = new MemoryStream(serialized);
      var deserialized = new ArchiveCentralDirectory(input, Encoding);
      
      // Then
      deserialized.LastModified.Should().Be(centralDirectory.LastModified);
      deserialized.FileHeaders.Should().HaveCount(centralDirectory.FileHeaders.Count());      
    }

    [Fact]
    public void EmptyCentralDirectoryShouldHaveOneFileHeaderAfterAddition()
    {
      // Given
      var centralDirectory = Fixture.Create<ArchiveCentralDirectory>();

      // When
      var header1 = Fixture.Create<ArchiveFileHeader>();
      centralDirectory.Add(header1);

      // Then
      centralDirectory.FileHeaders.Should().HaveCount(1);
    }
  }
}
