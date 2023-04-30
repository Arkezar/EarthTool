using EarthTool.Common.Interfaces;
using EarthTool.WD.Legacy.Models;

namespace EarthTool.WD.Legacy.Factories
{
  public class ArchiveFactory : IArchiveFactory
  {
    private readonly IDecompressor _decompressor;
    private readonly ICompressor _compressor;

    public ArchiveFactory(IDecompressor decompressor, ICompressor compressor)
    {
      _decompressor = decompressor;
      _compressor = compressor;
    }

    public IArchive NewArchive()
    {
      throw new NotImplementedException();
    }

    public IArchive OpenArchive(string path)
    {
      using (var stream = new FileStream(path, FileMode.Open))
      {
        return new Archive(path, stream, _decompressor, _compressor);
      }
    }

    public IArchive OpenArchive(byte[] data)
    {
      using (var stream = new MemoryStream(data))
      {
        return new Archive(null, stream, _decompressor, _compressor);
      }
    }
  }
}
