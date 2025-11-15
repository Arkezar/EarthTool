using System.Collections.Generic;
using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text;
using EarthTool.Common.Enums;

namespace EarthTool.WD.Services
{
    public class ArchiverService : IArchiver
    {
        private readonly ILogger<ArchiverService> _logger;
        private readonly IArchiveFactory _archiveFactory;
        private readonly IDecompressor _decompressor;
        private readonly ICompressor _compressor;
        private readonly Encoding _encoding;

        public ArchiverService(ILogger<ArchiverService> logger, IArchiveFactory archiveFactory,
            IDecompressor decompressor, ICompressor compressor, Encoding encoding)
        {
            _logger = logger;
            _archiveFactory = archiveFactory;
            _decompressor = decompressor;
            _compressor = compressor;
            _encoding = encoding;
        }

        public IArchive OpenArchive(string filePath)
        {
            return _archiveFactory.OpenArchive(filePath);
        }

        public void Extract(IArchiveItem resource, string outputPath)
        {
            var outputFilePath = Path.Combine(outputPath, resource.FileName)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            }

            var data = Extract(resource);
            File.WriteAllBytes(outputFilePath, data);
            _logger.LogInformation("Extracted file {FileName}", resource.FileName);
        }

        private byte[] Extract(IArchiveItem item)
        {
            if (!item.IsCompressed)
            {
                var header = item.Header.ToByteArray(_encoding);
                return header.Concat(item.Data.ToArray()).ToArray();
            }
            else
            {
                var extractHeader = (IEarthInfo)item.Header.Clone();
                extractHeader.RemoveFlag(FileFlags.Compressed);
                var header = extractHeader.ToByteArray(_encoding);
                return header.Concat(_decompressor.Decompress(item.Data.ToArray())).ToArray();
            }
        }

        public void ExtractAll(IArchive archive, string outputPath)
        {
            foreach (var resource in archive.Items)
            {
                Extract(resource, outputPath);
            }
        }
    }
}