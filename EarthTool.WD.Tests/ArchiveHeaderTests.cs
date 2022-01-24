using AutoFixture;
using FluentAssertions;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace EarthTool.WD.Tests
{
  public class ArchiveHeaderTests : ArchiveTestsBase
  {
    private ArchiveHeader _testHeader;
    private byte[] _invalidHeader;

    public ArchiveHeaderTests()
    {
      _testHeader = new ArchiveHeader(Fixture.Create<Guid>());
      _invalidHeader = Fixture.CreateMany<byte>(24).ToArray();
    }

    [Fact]
    public void SerializedAndDeserializedHeaderShouldBeEquivalent()
    {
      // Given
      var serialized = _testHeader.ToByteArray();

      // When
      using var input = new MemoryStream(serialized);
      var archiveHeader = new ArchiveHeader(input);

      // Then
      archiveHeader.ToByteArray().Should().BeEquivalentTo(serialized);
      archiveHeader.Identifier.Should().Be(_testHeader.Identifier);
    }

    [Fact]
    public void CorrectHeaderShouldBeValid()
    {
      // When
      var validity = _testHeader.IsValid();

      // Then
      validity.Should().BeTrue();
    }

    [Fact]
    public void IncorrectHeaderShouldBeInvalid()
    {
      // Given
      using var input = new MemoryStream(_invalidHeader);
      var archiveHeader = new ArchiveHeader(input);

      // When
      var validity = archiveHeader.IsValid();

      // Then
      validity.Should().BeFalse();
    }
  }
}