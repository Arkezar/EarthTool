using EarthTool.Common.Enums;
using System;

namespace EarthTool.Common.Interfaces
{
  public interface IArchiveResource
  {
    byte[] Data { get; }
    int DecompressedLength { get; }
    string Filename { get; }
    FileFlags Flags { get; }
    Guid? Guid { get; }
    int Length { get; }
    int Offset { get; }
    ResourceType? Group { get; }
    string TranslationId { get; }

    IArchiveResource SetData(byte[] data);
  }
}