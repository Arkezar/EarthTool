using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;

namespace EarthTool.WD.Legacy.Models
{
  public class ArchiveHeader : IArchiveHeader
  {
    private static readonly byte[] WdFileIdentifier = new byte[] { (byte)'W', (byte)'D', 0x01, 0x00 };

    private ReadOnlyMemory<byte> _fileIdentifier { get; }

    public Guid Identifier { get; }

    public ArchiveHeader(Stream stream)
    {
      _fileIdentifier = stream.ReadBytes(4);
      Identifier = Guid.Empty;
    }

    public bool IsValid()
    {
      return _fileIdentifier.Span.SequenceEqual(WdFileIdentifier);
    }

    public byte[] ToByteArray()
    {
      throw new NotSupportedException();
    }
  }
}
