using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Compression;

namespace EarthTool.WD.Services
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
      using var inputStream = new MemoryStream(data);
      return Compress(inputStream);
    }

    public byte[] Compress(Stream stream)
    {
      using var output = new MemoryStream();
      using (var compressionStream = OpenCompressionStream(output, true))
      {
        stream.CopyTo(compressionStream);
        // CRITICAL: Flush and close compression stream before reading output!
      }
      return output.ToArray();
    }

    public Stream OpenCompressionStream(Stream stream, bool leaveOpen = false)
      => new ZLibStream(stream, CompressionMode.Compress, leaveOpen);
  }
}
