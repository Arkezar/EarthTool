using MESHConverter.Extensions;
using System;
using System.IO;
using System.Text;

namespace MESHConverter
{
  public class TextureInfo
  {
    public string FileName
    {
      get;
    }

    public TextureInfo(Stream stream)
    {
      var filnameLength = BitConverter.ToInt32(stream.ReadBytes(4));
      FileName = Encoding.ASCII.GetString(stream.ReadBytes(filnameLength));
    }
  }
}
