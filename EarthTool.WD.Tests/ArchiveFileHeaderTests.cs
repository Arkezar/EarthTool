using AutoFixture;
using AutoFixture.Kernel;
using EarthTool.WD.Resources;
using FluentAssertions;
using System.IO;
using Xunit;

namespace EarthTool.WD.Tests
{
  public class ArchiveFileHeaderTests : ArchiveTestsBase
  {
    ArchiveFileHeader _testHeader;

    public ArchiveFileHeaderTests()
    {
      Fixture.Customize<ArchiveFileHeader>(c => c.FromFactory(new MethodInvoker(new GreedyConstructorQuery())));

      _testHeader = Fixture.Create<ArchiveFileHeader>();
    }

    [Fact]
    public void SerializedAndDeserializedFileHeaderShouldBeEquivalent()
    {
      // Given
      var serialized = _testHeader.ToByteArray();

      // When
      using var input = new MemoryStream(serialized);
      var deserialized = new ArchiveFileHeader(input, Encoding);

      // Then
      deserialized.FileName.Should().Be(_testHeader.FileName);
      deserialized.Offset.Should().Be(_testHeader.Offset);
      deserialized.Length.Should().Be(_testHeader.Length);
      deserialized.DecompressedLength.Should().Be(_testHeader.DecompressedLength);
      deserialized.Flags.Should().Be(_testHeader.Flags);
      deserialized.TranslationId.Should().Be(_testHeader.TranslationId);
      deserialized.ResourceType.Should().Be(_testHeader.ResourceType);
      deserialized.Guid.Should().Be(_testHeader.Guid);
    }
  }
}
