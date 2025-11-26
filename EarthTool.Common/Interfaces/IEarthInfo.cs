using EarthTool.Common.Enums;
using System;

namespace EarthTool.Common.Interfaces
{
  public interface IEarthInfo : IBinarySerializable, ICloneable
  {
    FileFlags Flags { get; }
    Guid? Guid { get; }
    ResourceType? ResourceType { get; }
    string TranslationId { get; }

    void SetFlag(FileFlags flag);
    void RemoveFlag(FileFlags flag);
  }
}
