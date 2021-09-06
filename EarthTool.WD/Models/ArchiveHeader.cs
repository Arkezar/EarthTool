using EarthTool.Common.Interfaces;
using System;
using System.Linq;

namespace EarthTool.WD
{
  public class ArchiveHeader : IArchiveHeader
  {
    public ArchiveHeader(byte[] data)
    {
      FileIdentifier = data.AsSpan(0, 8).ToArray();
      ArchiveIdentifier = new Guid(data.AsSpan(8, 16));
    }

    public byte[] FileIdentifier { get; }

    public Guid ArchiveIdentifier { get; }

    public bool IsValid()
    {
      var expected = new byte[] { 0xff, 0xa1, 0xd0, (byte)'1', (byte)'W', (byte)'D', 0x00, 0x02 };
      return FileIdentifier.SequenceEqual(expected);
    }
  }
}
