using EarthTool.MSH.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class TemplateDetails : ITemplateDetails
  {
    const int Rows = 4;
    const int Columns = 4;

    public TemplateDetails()
    {
      SectionHeights = new short[Rows, Columns];
      SectionFlags = new byte[Rows, Columns];
      SectionRotations = Enumerable.Repeat(new ModelTemplate(), 4);
      SectionFlagRotations = Enumerable.Repeat(new byte[Rows, Columns], 4);
    }

    public short[,] SectionHeights { get; set; }

    public byte[,] SectionFlags { get; set; }

    public IEnumerable<ModelTemplate> SectionRotations { get; set; }

    public IEnumerable<byte[,]> SectionFlagRotations { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(SectionHeights.Cast<short>().SelectMany(s => BitConverter.GetBytes(s)).ToArray());
          writer.Write(SectionFlags.Cast<byte>().ToArray());
          writer.Write(SectionRotations.SelectMany(s => s.ToByteArray(encoding)).ToArray());
          writer.Write(SectionFlagRotations.SelectMany(s => GetFlagRotationByte(s)).ToArray());
        }
        return stream.ToArray();
      }
    }

    private byte[] GetFlagRotationByte(byte[,] rotation)
    {
      var result = new byte[2 * Columns];
      for (var i = 0; i < Columns; i++)
      {
        for (var j = 0; j < 2; j++)
        {
          var upper = (byte)(rotation[i, j * 2] << 4);
          var lower = rotation[i, j * 2 + 1];
          result[1 + (2 * i) - j] = (byte)(upper + lower);
        }
      }

      return result;
    }
  }
}
