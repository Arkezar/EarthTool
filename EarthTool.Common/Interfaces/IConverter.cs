using System.Threading.Tasks;

namespace EarthTool.Common.Interfaces
{
  public interface IConverter
  {
    Task Convert(string filePath, string outputPath = null);
  }
}
