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
    public ContainerTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data, IEnumerable<bool> fieldTypes) : base(name, requiredResearch, type, data, fieldTypes)
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
    
    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(base.ToByteArray(encoding));
          bw.Write(AnimContainerDownStart);
          bw.Write(AnimContainerDownEnd);
          bw.Write(AnimContainerUpStart);
          bw.Write(AnimContainerUpEnd);
        }
        return output.ToArray();
      }
    }
  }
}
