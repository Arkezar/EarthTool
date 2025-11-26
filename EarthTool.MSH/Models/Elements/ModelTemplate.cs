using EarthTool.MSH.Interfaces;
using System.Collections;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class ModelTemplate : IModelTemplate
  {
    public const int ROWS = 4;
    public const int COLUMNS = 4;

    public bool[,] Matrix { get; }

    public short Flag { get; set; }

    public ModelTemplate()
    {
      Matrix = new bool[ROWS, COLUMNS];
    }

    public byte[] ToByteArray(Encoding encoding)
    {
      var bits = new BitArray(ROWS * COLUMNS);
      for (var col = COLUMNS - 1; col > -1; col--)
      {
        for (var row = ROWS - 1; row > -1; row--)
        {
          bits[col * 4 + row] = Matrix[row, COLUMNS - 1 - col];
        }
      }

      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          var output = new byte[(bits.Length - 1) / 8 + 1];
          bits.CopyTo(output, 0);
          writer.Write(output);
          writer.Write(Flag);
        }
        return stream.ToArray();
      }
    }
  }
}