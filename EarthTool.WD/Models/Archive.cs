using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.WD.Resources
{
  public class Archive : IArchive
  {
    public DateTime LastModified { get; }

    public IEnumerable<IArchiveResource> Resources { get; }

    public Archive(byte[] directoryData)
    {
      using (var stream = new MemoryStream(directoryData))
      {
        LastModified = DateTime.FromFileTimeUtc(BitConverter.ToInt64(stream.ReadBytes(8)));
        var fileCount = BitConverter.ToInt16(stream.ReadBytes(2));
        Resources = Enumerable.Range(0, fileCount).Select(i => new ArchiveResource(stream)).ToList();
      }
    }
  }
}
