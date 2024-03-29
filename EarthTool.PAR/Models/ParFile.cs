﻿using EarthTool.Common;
using EarthTool.Common.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.Models
{
  public class ParFile : IBinarySerializable
  {
    [JsonIgnore] public IEarthInfo FileHeader { get; set; }

    public IEnumerable<EntityGroup> Groups { get; set; }

    public IEnumerable<Research> Research { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (MemoryStream output = new MemoryStream())
      {
        using (BinaryWriter bw = new BinaryWriter(output, encoding))
        {
          bw.Write(FileHeader.ToByteArray(encoding));
          bw.Write(Identifiers.Paramters);
          bw.Write(Groups.Count());
          bw.Write(0);
          foreach (EntityGroup group in Groups)
          {
            bw.Write(group.ToByteArray(encoding));
          }

          bw.Write(Research.Count());
          bw.Write(-1);
          foreach (Research research in Research)
          {
            bw.Write(research.ToByteArray(encoding));
          }
        }

        return output.ToArray();
      }
    }
  }
}