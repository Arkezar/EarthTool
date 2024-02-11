using EarthTool.Common.Extensions;
using EarthTool.PAR.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class BuildingTransporter : VerticalTransporter
  {
    public BuildingTransporter(string name, IEnumerable<int> requiredResearch, EntityClassType type, BinaryReader data) : base(name, requiredResearch, type, data)
    {
      BuilderLineId = GetString(data);
      data.ReadBytes(4);
    }

    public string BuilderLineId { get; }
  }
}
