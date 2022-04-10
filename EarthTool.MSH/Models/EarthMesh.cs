using EarthTool.Common;
using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using EarthTool.MSH.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class EarthMesh : IMesh
  {
    public IEarthInfo FileHeader { get; set; }

    public IMeshDescriptor Descriptor { get; set; }

    public IEnumerable<IModelPart> Geometries { get; set; }

    public PartNode PartsTree { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(FileHeader.ToByteArray(encoding));
          bw.Write(Identifiers.Mesh);
          bw.Write(Descriptor.ToByteArray(encoding));
          bw.Write(Geometries.SelectMany(p => p.ToByteArray(encoding)).ToArray());
        }
        return output.ToArray().ToArray();
      }
    }
  }
}
