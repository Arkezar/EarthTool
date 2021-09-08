using EarthTool.Common.Enums;
using System;
using System.IO;

namespace EarthTool.Common.Interfaces
{
  public interface IArchiveResource
  {
    int DecompressedLength { get; }
    string Filename { get; }
    FileFlags Flags { get; }
    Guid? Guid { get; }
    int Length { get; }
    int Offset { get; }
    ResourceType? ResourceType { get; }
    string TranslationId { get; }

    byte[] GetData(Stream stream);
  }
}