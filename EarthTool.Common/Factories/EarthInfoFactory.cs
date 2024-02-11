using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using System;
using System.IO;
using System.Text;

namespace EarthTool.Common.Factories
{
  public class EarthInfoFactory : IEarthInfoFactory
  {
    private readonly Encoding _encoding;

    public EarthInfoFactory(Encoding encoding)
    {
      _encoding = encoding;
    }

    public IEarthInfo Get(string filePath, FileFlags flags, Guid? guid = null, ResourceType? resourceType = null, string translationId = null)
    {
      if (guid.HasValue)
      {
        flags |= FileFlags.Guid;
      }
      if (resourceType.HasValue)
      {
        flags |= FileFlags.Resource;
      }
      if (!string.IsNullOrEmpty(translationId))
      {
        flags |= FileFlags.Named;
      }
      return new EarthInfo
      {
        FilePath = filePath,
        Flags = flags,
        TranslationId = translationId,
        ResourceType = resourceType,
        Guid = guid
      };
    }

    public IEarthInfo Get(FileStream stream)
    {
      if (HasEarthInfo(stream))
      {
        using (var br = new BinaryReader(stream, _encoding, true))
        {
          var flags = (FileFlags)br.ReadByte();
          return new EarthInfo
          {
            FilePath = stream.Name,
            Flags = flags,
            TranslationId = flags.HasFlag(FileFlags.Named) ? br.ReadString() : null,
            ResourceType = flags.HasFlag(FileFlags.Resource) ? (ResourceType)br.ReadByte() : null,
            Guid = flags.HasFlag(FileFlags.Guid) ? new Guid(br.ReadBytes(16)) : null
          };
        }
      }
      return default;
    }

    private bool HasEarthInfo(Stream stream)
    {
      using (var br = new BinaryReader(stream, _encoding, true))
      {
        var hasInfo = HasEarthInfo(br.ReadBytes(Identifiers.Info.Length));
        if (!hasInfo)
        {
          stream.Seek(0, SeekOrigin.Begin);
        }
        return hasInfo;
      }
    }

    private bool HasEarthInfo(byte[] data)
    {
      return data.AsSpan().StartsWith(Identifiers.Info);
    }
  }
}
