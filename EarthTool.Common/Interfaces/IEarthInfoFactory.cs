using EarthTool.Common.Enums;
using System;
using System.IO;

namespace EarthTool.Common.Interfaces
{
  public interface IEarthInfoFactory
  {
    IEarthInfo Get(byte[] data);
    
    IEarthInfo Get(Stream stream);

    IEarthInfo Get(
      FileFlags flags = FileFlags.None,
      Guid? guid = null,
      ResourceType? resourceType = null,
      string translationId = null);
  }
}