using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using EarthTool.WD.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthTool.WD.Models
{
  public class ArchiveCentralDirectory : IArchiveCentralDirectory
  {
    public DateTime LastModified { get; }

    private LinkedList<IArchiveFileHeader> _fileHeaders;
    public IEnumerable<IArchiveFileHeader> FileHeaders
      => _fileHeaders;

    public ArchiveCentralDirectory(Stream stream, Encoding encoding)
    {
      LastModified = DateTime.FromFileTimeUtc(BitConverter.ToInt64(stream.ReadBytes(8)));
      _fileHeaders = GetFileHeaders(stream, encoding);
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

    private LinkedList<IArchiveFileHeader> GetFileHeaders(Stream stream, Encoding encoding)
    {
      var fileCount = BitConverter.ToInt16(stream.ReadBytes(2));
      return new LinkedList<IArchiveFileHeader>(Enumerable.Range(0, fileCount).Select(i => GetFileHeader(stream, encoding)));
    }

    private IArchiveFileHeader GetFileHeader(Stream stream, Encoding encoding)
      => new ArchiveFileHeader(stream, encoding);
  }
}
