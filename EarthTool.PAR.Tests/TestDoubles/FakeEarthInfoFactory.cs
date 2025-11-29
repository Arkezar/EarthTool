using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Tests.TestDoubles
{
  internal sealed class FakeEarthInfoFactory : IEarthInfoFactory
  {
    private readonly byte[] _expectedHeader;

    public FakeEarthInfoFactory(byte[] expectedHeader)
    {
      _expectedHeader = expectedHeader.ToArray();
    }

    public IEarthInfo Get(byte[] data)
    {
      return new FakeEarthInfo(data);
    }

    public IEarthInfo Get(Stream stream)
    {
      var buffer = new byte[_expectedHeader.Length];
      var totalRead = 0;
      while (totalRead < buffer.Length)
      {
        var read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
        if (read == 0)
        {
          throw new InvalidDataException("Unexpected end of stream while reading header.");
        }

        totalRead += read;
      }

      if (!_expectedHeader.SequenceEqual(buffer))
      {
        throw new InvalidDataException("Unexpected EarthInfo header payload.");
      }

      return new FakeEarthInfo(buffer);
    }

    public IEarthInfo Get(
      FileFlags flags = FileFlags.None,
      Guid? guid = null,
      ResourceType? resourceType = null,
      string? translationId = null)
    {
      return new FakeEarthInfo(_expectedHeader)
      {
        Flags = flags,
        ResourceType = resourceType,
        TranslationId = translationId ?? "TEST",
        Guid = guid
      };
    }
  }

  internal sealed class FakeEarthInfo : IEarthInfo
  {
    private readonly byte[] _payload;

    public FakeEarthInfo(byte[] payload)
    {
      _payload = payload.ToArray();
    }

    public FileFlags Flags { get; internal set; }

    public Guid? Guid { get; internal set; }

    public ResourceType? ResourceType { get; internal set; }

    public string TranslationId { get; internal set; } = "TEST";

    public void SetFlag(FileFlags flag)
    {
      Flags |= flag;
    }

    public void RemoveFlag(FileFlags flag)
    {
      Flags &= ~flag;
    }

    public byte[] ToByteArray(Encoding encoding)
    {
      return _payload.ToArray();
    }

    public object Clone()
    {
      return new FakeEarthInfo(_payload.ToArray())
      {
        Flags = Flags,
        Guid = Guid,
        ResourceType = ResourceType,
        TranslationId = TranslationId
      };
    }
  }
}
