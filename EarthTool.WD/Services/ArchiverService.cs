using EarthTool.Common.Interfaces;
using EarthTool.Common.Validation;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Text;
using EarthTool.Common.Enums;
using EarthTool.WD.Models;
using System;

namespace EarthTool.WD.Services
{
  public class ArchiverService : IArchiver
  {
    private readonly ILogger<ArchiverService> _logger;
    private readonly IEarthInfoFactory        _earthInfoFactory;
    private readonly IArchiveFactory          _archiveFactory;
    private readonly IDecompressor            _decompressor;
    private readonly ICompressor              _compressor;
    private readonly Encoding                 _encoding;

    public ArchiverService(
      ILogger<ArchiverService> logger,
      IEarthInfoFactory earthInfoFactory,
      IArchiveFactory archiveFactory,
      IDecompressor decompressor,
      ICompressor compressor,
      Encoding encoding)
    {
      _logger = logger;
      _earthInfoFactory = earthInfoFactory;
      _archiveFactory = archiveFactory;
      _decompressor = decompressor;
      _compressor = compressor;
      _encoding = encoding;
    }

    public IArchive OpenArchive(string filePath)
    {
      return _archiveFactory.OpenArchive(filePath);
    }

    public IArchive CreateArchive()
    {
      return _archiveFactory.NewArchive();
    }

    public IArchive CreateArchive(DateTime lastModification)
    {
      return _archiveFactory.NewArchive(lastModification);
    }

    public IArchive CreateArchive(DateTime lastModification, Guid guid)
    {
      return _archiveFactory.NewArchive(lastModification, guid);
    }

    public void AddFile(IArchive archive, string filePath, string baseDirectory = null, bool compress = true)
    {
      ArgumentNullException.ThrowIfNull(archive);

      var validatedPath = PathValidator.ValidateFileExists(filePath);

      // If baseDirectory not provided, use parent directory of the file
      var baseDir = baseDirectory ?? Path.GetDirectoryName(validatedPath);

      var item = CreateArchiveItemFromFile(
        validatedPath,
        baseDir,
        _earthInfoFactory,
        _compressor,
        compress);

      archive.AddItem(item);
      _logger.LogInformation("Added file {FileName} to archive (compressed: {Compressed})",
        item.FileName, compress);
    }

    public void SaveArchive(IArchive archive, string outputFilePath)
    {
      ArgumentNullException.ThrowIfNull(archive);

      var directory = Path.GetDirectoryName(outputFilePath);
      if (!string.IsNullOrEmpty(directory))
      {
        PathValidator.EnsureDirectoryExists(directory);
      }

      var archiveData = archive.ToByteArray(_compressor, _encoding);
      File.WriteAllBytes(outputFilePath, archiveData);

      _logger.LogInformation("Saved archive to {OutputPath} ({Size} bytes, {ItemCount} items)",
        outputFilePath, archiveData.Length, archive.Items.Count);
    }

    public void Extract(IArchiveItem resource, string outputPath)
    {
      ArgumentNullException.ThrowIfNull(resource);

      var outputFilePath = PathValidator.GetSafeOutputPath(outputPath, resource.FileName);

      var data = Extract(resource);
      File.WriteAllBytes(outputFilePath, data);
      _logger.LogInformation("Extracted file {FileName} to {OutputPath}", resource.FileName, outputFilePath);
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
      ArgumentNullException.ThrowIfNull(archive);

      PathValidator.EnsureDirectoryExists(outputPath);

      foreach (var resource in archive.Items)
      {
        Extract(resource, outputPath);
      }
    }

    /// <summary>
    /// Creates an ArchiveItem from a file on disk
    /// </summary>
    private ArchiveItem CreateArchiveItemFromFile(
      string filePath,
      string baseDirectory,
      IEarthInfoFactory earthInfoFactory,
      ICompressor compressor,
      bool compress = true)
    {
      var fileData = File.ReadAllBytes(filePath);
      
      // Calculate relative path from baseDirectory and normalize to backslashes
      string fileName;
      if (!string.IsNullOrEmpty(baseDirectory))
      {
        fileName = Path.GetRelativePath(baseDirectory, filePath);
        // ALWAYS use backslashes in archive (game requirement!)
        fileName = fileName.Replace('/', '\\');
      }
      else
      {
        // Fallback - use only file name
        fileName = Path.GetFileName(filePath);
      }

      // Read existing header from file if it has EarthInfo
      using var ms = new MemoryStream(fileData);
      var header = earthInfoFactory.Get(ms) ?? earthInfoFactory.Get();
      var headerSize = (int)ms.Position;
      var contentData = new byte[fileData.Length - headerSize];
      Array.Copy(fileData, headerSize, contentData, 0, contentData.Length);

      var decompressedSize = contentData.Length;
      var archiveData = compress && !header.Flags.HasFlag(FileFlags.Compressed) ? compressor.Compress(contentData) : contentData;
      var compressedSize = archiveData.Length;
      var archiveHeader = (IEarthInfo)header.Clone();
      if (compress) archiveHeader.SetFlag(FileFlags.Compressed);

      return new ArchiveItem(fileName, archiveHeader, new InMemoryArchiveDataSource(archiveData), compressedSize, decompressedSize);
    }
  }
}