using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class ContainerTransporter : Equipment
  {
    public ContainerTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      AnimContainerDownStart = GetInteger(data);
      AnimContainerDownEnd = GetInteger(data);
      AnimContainerUpStart = GetInteger(data);
      AnimContainerUpEnd = GetInteger(data);
    }

    public int AnimContainerDownStart { get; }

    public int AnimContainerDownEnd { get; }

    public int AnimContainerUpStart { get; }

    public int AnimContainerUpEnd { get; }
  }
}
