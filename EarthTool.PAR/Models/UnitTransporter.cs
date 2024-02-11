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
    public UnitTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      UnitsCount = GetInteger(data);
      DockingHeight = GetInteger(data);
      AnimLoadingStartStart = GetInteger(data);
      AnimLoadingStartEnd = GetInteger(data);
      AnimLoadingEndStart = GetInteger(data);
      AnimLoadingEndEnd = GetInteger(data);
      AnimUnloadingStartStart = GetInteger(data);
      AnimUnloadingStartEnd = GetInteger(data);
      AnimUnloadingEndStart = GetInteger(data);
      AnimUnloadingEndEnd = GetInteger(data);
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
