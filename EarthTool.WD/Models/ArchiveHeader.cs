using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using System;
using System.IO;
using System.Linq;

namespace EarthTool.WD
{
  public class ArchiveHeader : IArchiveHeader
  {
    private static readonly byte[] WdFileIdentifier = new byte[] { 0xff, 0xa1, 0xd0, (byte)'1', (byte)'W', (byte)'D', 0x00, 0x02 };

    private ReadOnlyMemory<byte> _fileIdentifier { get; }

    public Guid Identifier { get; }

    public ArchiveHeader(Stream stream)
    {
      _fileIdentifier = stream.ReadBytes(8);
      Identifier = new Guid(stream.ReadBytes(16));
    }

    public ArchiveHeader(Guid guid)
    {
      _fileIdentifier = WdFileIdentifier;
      Identifier = guid;
    }

    public bool IsValid()
    {
      return _fileIdentifier.Span.SequenceEqual(WdFileIdentifier);
    }

    public byte[] ToByteArray()
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output))
        {
          bw.Write(_fileIdentifier.ToArray());
          bw.Write(Identifier.ToByteArray());
        }
        return output.ToArray();
      }
    }
  }
}
