using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;

namespace EarthTool.WD.Services
{
    public class ArchiverService : IArchiver
    {
        private readonly ILogger<ArchiverService> _logger;
        private readonly IArchiveFactory _archiveFactory;
        private readonly IDecompressor _decompressor;
        private readonly ICompressor _compressor;
        private readonly Encoding _encoding;
        private IArchive _archive;

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
            return _archive = _archiveFactory.OpenArchive(filePath);
        }

        public void Extract(IArchiveItem resource, string outputFilePath)
        {
            outputFilePath = outputFilePath.Replace('\\', Path.DirectorySeparatorChar);

            if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            }

            var data = resource.Extract(_decompressor, _encoding);
            File.WriteAllBytes(outputFilePath, data);
        }

        public void ExtractAll(string outputPath)
        {
            foreach (var resource in _archive.Items)
            {
                var outputFilePath = Path.Combine(outputPath, resource.FileName)
                    .Replace('\\', Path.DirectorySeparatorChar);

                Extract(resource, outputFilePath);

                _logger.LogInformation("Extracted file {FileName}", resource.FileName);
            }
        }
    }
}