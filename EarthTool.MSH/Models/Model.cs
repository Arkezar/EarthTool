using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class Model
  {
    public string FilePath
    {
      get;
    }

    public int Type
    {
      get; private set;
    }

    public short UnknownVal1
    {
      get; private set;
    }

    public short UnknownVal2
    {
      get; private set;
    }

    public short UnknownVal3
    {
      get; private set;
    }

    public short UnknownVal4
    {
      get; private set;
    }

    public int UnknownVal5
    {
      get; private set;
    }

    public IList<ModelPart> Parts
    {
      get;
    }

    public Model(string path)
    {
      Parts = new List<ModelPart>();
      FilePath = path;

      using (var file = new FileStream(path, FileMode.Open))
      {
        CheckHeader(file);
        LoadInfo(file);
        if(Type != 0)
        {
          throw new NotSupportedException("Not supported mesh format");
        }
        LoadParts(file);
      }
    }

    private void CheckHeader(Stream stream)
    {
      var type = stream.ReadBytes(8).AsSpan();
      if (!type.SequenceEqual(new byte[] { 0x4d, 0x45, 0x53, 0x48, 0x01, 0x00, 0x00, 0x00 }))
      {
        throw new NotSupportedException("Unhandled file format");
      }
    }

    private void LoadInfo(Stream stream)
    {
      Type = BitConverter.ToInt32(stream.ReadBytes(4));
      stream.ReadBytes(108);
      stream.ReadBytes(256);
      stream.ReadBytes(488);
      UnknownVal1 = BitConverter.ToInt16(stream.ReadBytes(2));
      UnknownVal2 = BitConverter.ToInt16(stream.ReadBytes(2));
      UnknownVal3 = BitConverter.ToInt16(stream.ReadBytes(2));
      UnknownVal4 = BitConverter.ToInt16(stream.ReadBytes(2));
      UnknownVal5 = BitConverter.ToInt16(stream.ReadBytes(4));
    }

    private void LoadParts(Stream stream)
    {
      while (stream.Position < stream.Length)
      {
        Parts.Add(new ModelPart(stream));
      }
    }
  }
}
