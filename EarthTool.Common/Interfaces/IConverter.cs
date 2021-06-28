using EarthTool.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EarthTool.Common.Interfaces
{
  public interface IConverter
  {
    IConverter WithOptions(IReadOnlyCollection<Option> options);
    Task Convert(string filePath, string outputPath = null);
  }
}
