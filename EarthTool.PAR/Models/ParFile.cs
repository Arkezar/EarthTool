using EarthTool.Common.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Models
{
  public class ParFile
  {
    public ParFile(Stream stream)
    {
      ValidateHeader(stream);

      var groupCount = BitConverter.ToInt32(stream.ReadBytes(4));
      stream.ReadBytes(4);
      Groups = Enumerable.Range(0, groupCount).Select(i => new EntityGroup(stream)).ToList();

      var researchCount = BitConverter.ToInt32(stream.ReadBytes(4));
      Research = Enumerable.Range(0, researchCount).Select(i => new Research(stream)).ToList();
    }

    public IEnumerable<EntityGroup> Groups { get; }

    public IEnumerable<Research> Research { get; }

    private void ValidateHeader(Stream stream)
    {
      var identifer = Encoding.ASCII.GetString(stream.ReadBytes(8));

      if (identifer != "PAR\0?\0\0\0")
      {
        throw new Exception("Unsupported file format.");
      }
    }
  }
}
