using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class UnitTransporter : VerticalTransporter
  {
    public UnitTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, Stream data) : base(name, requiredResearch, type, data)
    {
      UnitsCount = BitConverter.ToInt32(data.ReadBytes(4));
      DockingHeight = BitConverter.ToInt32(data.ReadBytes(4));
      AnimLoadingStartStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimLoadingStartEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimLoadingEndStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimLoadingEndEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimUnloadingStartStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimUnloadingStartEnd = BitConverter.ToInt32(data.ReadBytes(4));
      AnimUnloadingEndStart = BitConverter.ToInt32(data.ReadBytes(4));
      AnimUnloadingEndEnd = BitConverter.ToInt32(data.ReadBytes(4));
    }

    public int UnitsCount { get; }

    public int DockingHeight { get; }

    public int AnimLoadingStartStart { get; }

    public int AnimLoadingStartEnd { get; }

    public int AnimLoadingEndStart { get; }

    public int AnimLoadingEndEnd { get; }

    public int AnimUnloadingStartStart { get; }

    public int AnimUnloadingStartEnd { get; }

    public int AnimUnloadingEndStart { get; }

    public int AnimUnloadingEndEnd { get; }
  }
}
