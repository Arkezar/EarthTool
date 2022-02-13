using EarthTool.Common.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class TemplateDetails
  {
    const int Rows = 4;
    const int Columns = 4;

    public short[,] SectionHeights { get; }

    public byte[,] SectionFlags { get; }

    public IEnumerable<ModelTemplate> SectionRotations { get; }

    public IEnumerable<BitArray> SectionFlagRotations { get; }

    public TemplateDetails(Stream stream)
    {
      SectionHeights = GetSectionHeights(stream);
      SectionFlags = GetSectionFlags(stream);
      SectionRotations = GetSectionRotations(stream);

      stream.ReadBytes(32);
    }

    public byte[] ToByteArray()
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(SectionHeights.Cast<short>().SelectMany(s => BitConverter.GetBytes(s)).ToArray());
          writer.Write(SectionFlags.Cast<byte>().ToArray());
          writer.Write(SectionRotations.SelectMany(s => s.ToByteArray()).ToArray());
          writer.Write(new byte[32]);
        }
        return stream.ToArray();
      }
    }

    private short[,] GetSectionHeights(Stream stream)
    {
      var sectionHeights = new short[Rows, Columns];

      for (var row = 0; row < Rows; row++)
      {
        for (var col = 0; col < Columns; col++)
        {
          sectionHeights[row, col] = BitConverter.ToInt16(stream.ReadBytes(2));
        }
      }

      return sectionHeights;
    }

    private byte[,] GetSectionFlags(Stream stream)
    {
      var sectionFlags = new byte[Rows, Columns];

      for (var row = 0; row < Rows; row++)
      {
        for (var col = 0; col < Columns; col++)
        {
          sectionFlags[row, col] = (byte)stream.ReadByte();
        }
      }

      return sectionFlags;
    }

    private IEnumerable<ModelTemplate> GetSectionRotations(Stream stream)
    {
      return Enumerable.Range(0, 4).Select(_ => new ModelTemplate(stream)).ToList();
    }
  }
}
