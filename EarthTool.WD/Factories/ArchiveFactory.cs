using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
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
            return new Archive();
        }

        public IArchive OpenArchive(string path)
        {
            var header = GetArchiveHeader(path);
            if (header.ResourceType != ResourceType.WdArchive)
            {
                throw new NotSupportedException("Unsupported archive type");
            }

            var data = File.ReadAllBytes(path);

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
                var decompressedLength = reader.ReadInt32();

                var info = new EarthInfo()
                {
                    Flags = flags,
                    TranslationId = flags.HasFlag(FileFlags.Named) ? reader.ReadString() : null,
                    ResourceType = flags.HasFlag(FileFlags.Resource) ? (ResourceType)(byte)reader.ReadInt32() : null,
                    Guid = flags.HasFlag(FileFlags.Guid) ? new Guid(reader.ReadBytes(16)) : null,
                };
                var itemData = new ReadOnlyMemory<byte>(data, offset, length);
                return new ArchiveItem(filePath, info, itemData, decompressedLength);
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