using EarthTool.Common.Interfaces;
using Ionic.Zlib;
using Microsoft.Extensions.Logging;
using System.IO;

namespace EarthTool.WD.Services
{
  public class EncryptionService : IEncryption
  {
    private readonly ILogger<EncryptionService> _logger;

    public EncryptionService(ILogger<EncryptionService> logger)
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
        using (var decompressedData = new ZlibStream(stream, CompressionMode.Compress, true))
        {
          decompressedData.CopyTo(output);
          return output.ToArray();
        }
      }
    }

    public byte[] Decompress(byte[] data)
    {
      using (var compressedStream = new MemoryStream(data))
      {
        return Decompress(compressedStream);
      }
    }

    public byte[] Decompress(Stream stream)
    {
      using (var output = new MemoryStream())
      {
        using (var decompressedData = new ZlibStream(stream, CompressionMode.Decompress, true))
        {
          decompressedData.CopyTo(output);
          return output.ToArray();
        }
      }
    }
  }
}
