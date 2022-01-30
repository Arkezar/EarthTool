using EarthTool.Common.Extensions;
using System;
using System.Collections;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class ModelTemplate
  {
    const int ROWS = 4;
    const int COLUMNS = 4;

    public bool[,] Matrix
    {
      get;
    }

    public short Flag
    {
      get;
    }

    public ModelTemplate(Stream stream)
    {
      Matrix = new bool[ROWS, COLUMNS];

      var data = new BitArray(stream.ReadBytes(2));
      FillTemplateMatrix(data);

      Flag = BitConverter.ToInt16(stream.ReadBytes(2));
    }

    public byte[] ToByteArray()
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

    private void FillTemplateMatrix(BitArray data)
    {
      for (var col = COLUMNS - 1; col > -1; col--)
      {
        for (var row = ROWS - 1; row > -1; row--)
        {
          Matrix[row, COLUMNS - 1 - col] = data[col * 4 + row];
        }
      }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      for (var r = 0; r < ROWS; r++)
      {
        for (var c = 0; c < COLUMNS; c++)
        {
          builder.Append(Matrix[r, c] ? '#' : ' ');
        }
        builder.AppendLine();
      }

      return builder.ToString();
    }
  }
}
