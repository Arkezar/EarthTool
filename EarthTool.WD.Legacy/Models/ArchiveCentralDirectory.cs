using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;

namespace EarthTool.WD.Legacy.Models
{
  public class ArchiveCentralDirectory : IArchiveCentralDirectory
  {
    public DateTime LastModified { get; }

    private LinkedList<IArchiveFileHeader> _fileHeaders;
    public IEnumerable<IArchiveFileHeader> FileHeaders
      => _fileHeaders;

    public ArchiveCentralDirectory(Stream stream)
    {
      LastModified = DateTime.FromFileTimeUtc(BitConverter.ToInt32(stream.ReadBytes(4)));
      _fileHeaders = GetFileHeaders(stream);
    }

    public ArchiveCentralDirectory()
    {
      LastModified = DateTime.UtcNow;
      _fileHeaders = new LinkedList<IArchiveFileHeader>();
    }

    public byte[] ToByteArray()
    {
      using (var output = new MemoryStream())
      {
        using (var writer = new BinaryWriter(output))
        {
          writer.Write(LastModified.ToFileTimeUtc());
          writer.Write((short)FileHeaders.Count());
          foreach (var fileHeader in FileHeaders)
          {
            writer.Write(fileHeader.ToByteArray());
          }
        }
        return output.ToArray();
      }
    }

    public void Add(IArchiveFileHeader fileHeader)
    {
      _fileHeaders.AddLast(fileHeader);
    }

    public void Remove(IArchiveFileHeader fileHeader)
    {
      var element = _fileHeaders.Find(fileHeader);
      while((element = element.Next) != null)
      {
        element.Value.SetOffset(element.Value.Offset - fileHeader.Length);
      }
      _fileHeaders.Remove(fileHeader);
    }

    private LinkedList<IArchiveFileHeader> GetFileHeaders(Stream stream)
    {
      var fileCount = BitConverter.ToInt16(stream.ReadBytes(2));
      return new LinkedList<IArchiveFileHeader>(Enumerable.Range(0, fileCount).Select(i => GetFileHeader(stream)));
    }

    private IArchiveFileHeader GetFileHeader(Stream stream)
      => new ArchiveFileHeader(stream);
  }
}
