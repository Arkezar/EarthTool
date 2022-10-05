using EarthTool.Common.Enums;
using System;
using System.IO;

namespace EarthTool.Common.Interfaces
{
  public interface IArchiveFileHeader
  {
    int DecompressedLength { get; }
    string FileName { get; }
    FileFlags Flags { get; }
    Guid? Guid { get; }
    int Length { get; }
    int Offset { get; }
    ResourceType? ResourceType { get; }
    string TranslationId { get; }

    void SetOffset(int offset);
    ReadOnlySpan<byte> GetData(Stream stream);
    byte[] ToByteArray();

    IEarthInfo ToEarthInfo();
  }
}