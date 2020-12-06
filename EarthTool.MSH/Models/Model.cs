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
        CheckAndSkipHeader(file);
        LoadParts(file);
      }
    }

    private void CheckAndSkipHeader(Stream stream)
    {
      var type = Encoding.ASCII.GetString(stream.ReadBytes(4));
      if (type != "MESH")
      {
        throw new NotSupportedException("Unhandled file format");
      }
      else
      {
        //skipping
        stream.ReadBytes(872);
      }
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
