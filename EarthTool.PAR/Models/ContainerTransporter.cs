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
    public ContainerTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      AnimContainerDownStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimContainerDownEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimContainerUpStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimContainerUpEnd = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int AnimContainerDownStart { get; }

    public int AnimContainerDownEnd { get; }

    public int AnimContainerUpStart { get; }

    public int AnimContainerUpEnd { get; }
  }
}
