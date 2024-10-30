using EarthTool.Common.Enums;
using System;
using System.IO;

namespace EarthTool.Common.Interfaces
{
  public interface IEarthInfoFactory
  {
    IEarthInfo Get(FileStream stream);

    IEarthInfo Get(
      string filePath,
      FileFlags flags,
      Guid? guid = null,
      ResourceType? resourceType = null,
      string translationId = null);
  }
}