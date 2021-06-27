using System.Threading.Tasks;

namespace EarthTool.Common.Interfaces
{
  public interface IExtractor
  {
    Task Extract(string filePath, string outputPath = null);
  }
}
