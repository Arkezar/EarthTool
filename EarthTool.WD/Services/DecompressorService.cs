using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;

namespace EarthTool.WD.Services
{
  public class DecompressorService : IDecompressor
  {
    private readonly ILogger<DecompressorService> _logger;

    public DecompressorService(ILogger<DecompressorService> logger)
    {
      _logger = logger;
    }

    public byte[] Decompress(ReadOnlySpan<byte> data)
      => Decompress(data.ToArray());

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
        using (var decompressedData = new ZLibStream(stream, CompressionMode.Decompress, true))
        {
          decompressedData.CopyTo(output);
        }

        return output.ToArray();
      }
    }
  }
}