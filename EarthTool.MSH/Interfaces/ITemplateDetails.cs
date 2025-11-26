using EarthTool.Common.Interfaces;
using EarthTool.MSH.Models.Elements;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface ITemplateDetails : IBinarySerializable
  {
    IEnumerable<byte[,]> SectionFlagRotations { get; }
    byte[,] SectionFlags { get; }
    short[,] SectionHeights { get; }
    IEnumerable<ModelTemplate> SectionRotations { get; }
  }
}
