using System;

namespace EarthTool.PAR.Enums
{
  [Flags]
  public enum ResourceInputOutputFlags
  {
    OutputContainer       = 0x01,
    OutputSluice          = 0x02,
    OutputTransportSluice = 0x04,
    InputContainer        = 0x10,
    InputSluice           = 0x20,
  }
}
