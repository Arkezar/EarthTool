using EarthTool.MSH.Interfaces;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Services
{
  public class EarthMeshWriter : IMeshWriter
  {
    private readonly Encoding _encoding;

    public EarthMeshWriter(Encoding encoding)
    {
      _encoding = encoding;
    }

    public void Write(string fileName, IMesh mesh)
    {
      CheckOrCreateOutputPath(fileName);
      using (var stream = File.Create(fileName))
      {
        Write(stream, mesh);
      }
    }

    private void Write(FileStream stream, IMesh mesh)
    {
      using (var writer = new BinaryWriter(stream, _encoding))
      {
        writer.Write(mesh.ToByteArray(_encoding));
      }
    }

    private void CheckOrCreateOutputPath(string filePath)
    {
      var outputPath = Path.GetDirectoryName(filePath);
      if (!Directory.Exists(outputPath))
      {
        Directory.CreateDirectory(outputPath);
      }
    }
  }
}
