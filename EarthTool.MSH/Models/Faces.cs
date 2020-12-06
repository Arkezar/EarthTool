using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.MSH.Models
{
  public class Faces : List<Face>
  {
    const int FACES_BLOCK_LENGTH = 8;

    public Faces(Stream stream)
    {
      var faces = BitConverter.ToInt32(stream.ReadBytes(4));

      AddRange(Enumerable.Range(0, faces).Select(_ => GetFace(stream.ReadBytes(FACES_BLOCK_LENGTH))).Take(faces));
    }

    private Face GetFace(byte[] faceData)
    {

      return new Face()
      {
        V1 = BitConverter.ToInt16(faceData, 0),
        V2 = BitConverter.ToInt16(faceData, 2),
        V3 = BitConverter.ToInt16(faceData, 4),
        UNKNOWN = BitConverter.ToInt16(faceData, 6)
      };
    }
  }
}
