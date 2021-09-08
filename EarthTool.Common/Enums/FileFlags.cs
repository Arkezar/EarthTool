using System;
using System.ComponentModel;

namespace EarthTool.Common.Enums
{
  [Flags]
  public enum FileFlags
  {
    None = 0,
    Compressed = 1,
    Archive = 2,
    Text = 4,
    Named = 8,
    Resource = 16,
    Guid = 32
  }
}
