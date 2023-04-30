using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace EarthTool.WD.Legacy.Services
{
  public class CompressorService : ICompressor
  {
    private readonly ILogger<CompressorService> _logger;

    public CompressorService(ILogger<CompressorService> logger)
    {
      _logger = logger;
    }

    public byte[] Compress(byte[] data)
    {
      using (var compressedStream = new MemoryStream(data))
      {
        return Compress(compressedStream);
      }
    }

    public byte[] Compress(Stream stream)
    {
      using (var output = new MemoryStream())
      {
        using (var compressionStream = new ZLibStream(output, CompressionMode.Compress))
        {
          stream.CopyTo(compressionStream);
        }
        return output.ToArray();
      }
    }
  }
}
