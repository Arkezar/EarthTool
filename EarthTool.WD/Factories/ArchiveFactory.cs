using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Validation;
using EarthTool.WD.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.WD.Factories
{
    public class ArchiveFactory(
        IDecompressor decompressor,
        IEarthInfoFactory earthInfoFactory,
        Encoding encoding)
        : IArchiveFactory
    {
        public IArchive NewArchive()
        {
            var header = earthInfoFactory.Get(FileFlags.Compressed | FileFlags.Resource | FileFlags.Guid);
            return new Archive(header);
        }

        public IArchive OpenArchive(string path)
        {
            var validatedPath = PathValidator.ValidateFileExists(path);
            
            var header = GetArchiveHeader(validatedPath);
            if (header.ResourceType != ResourceType.WdArchive)
            {
                throw new NotSupportedException($"Unsupported archive type: {header.ResourceType}");
            }

            var data = File.ReadAllBytes(validatedPath);

            using var reader = GetDescriptorReader(data);
            var lastModified = DateTime.FromFileTimeUtc(reader.ReadInt64());
            var items = GetItems(data, reader);
            return new Archive(header, lastModified, items);
        }

        private BinaryReader GetDescriptorReader(byte[] data)
        {
            var descriptorLength = BitConverter.ToInt32(new ReadOnlySpan<byte>(data, data.Length - 4, 4));
            var centralDirectory =
                decompressor.Decompress(new ReadOnlySpan<byte>(data, data.Length - descriptorLength,
                    descriptorLength - 4));
            return new BinaryReader(new MemoryStream(centralDirectory), encoding);
        }

        private SortedSet<IArchiveItem> GetItems(byte[] data, BinaryReader reader)
        {
            var itemCount = reader.ReadInt16();
            return new SortedSet<IArchiveItem>(Enumerable.Range(0, itemCount).Select(_ =>
            {
                var filePath = reader.ReadString();
                var flags = (FileFlags)reader.ReadByte();
                var offset = reader.ReadInt32();
                var length = reader.ReadInt32();
                var decompressedSize = reader.ReadInt32();
                var translationId = flags.HasFlag(FileFlags.Named) ? reader.ReadString() : null;
                var resourceType = flags.HasFlag(FileFlags.Resource) ? (ResourceType?)(byte)reader.ReadInt32() : null;
                var guid = flags.HasFlag(FileFlags.Guid) ? (Guid?)new Guid(reader.ReadBytes(16)) : null;
                var header = earthInfoFactory.Get(flags, guid, resourceType, translationId);
                var itemData = new ReadOnlyMemory<byte>(data, offset, length);
                return new ArchiveItem(filePath, header, itemData, decompressedSize);
            }));
        }
        
        private IEarthInfo GetArchiveHeader(string path)
        {
            using var stream = File.OpenRead(path);
            using var decompressedStream = new MemoryStream(decompressor.Decompress(stream));
            return earthInfoFactory.Get(decompressedStream);
        }
    }
}