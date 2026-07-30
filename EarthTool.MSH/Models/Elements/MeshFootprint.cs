using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class MeshFootprint : IMeshFootprint
  {
    public const int BoxCount = 16;
    public const int CoverageCount = 4;

    public MeshFootprint()
    {
      BoxHeights = new ushort[BoxCount];
      BoxFlags = new byte[BoxCount];
      CoverageDescriptors = new uint[CoverageCount];
      CoverageBitmaps = new ulong[CoverageCount];
    }

    public ushort[] BoxHeights { get; set; }

    public byte[] BoxFlags { get; set; }

    public uint[] CoverageDescriptors { get; set; }

    public ulong[] CoverageBitmaps { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream, encoding))
        {
          for (var i = BoxCount - 1; i >= 0; i--)
          {
            writer.Write(BoxHeights[i]);
          }

          for (var i = BoxCount - 1; i >= 0; i--)
          {
            writer.Write(BoxFlags[i]);
          }

          foreach (var descriptor in CoverageDescriptors)
          {
            writer.Write(descriptor);
          }

          foreach (var bitmap in CoverageBitmaps)
          {
            writer.Write(bitmap);
          }
        }

        return stream.ToArray();
      }
    }
  }
}
