using System;
using System.Collections.Generic;
using System.Text;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;

namespace EarthTool.WD.Models
{
    public class Archive : SortedSet<IArchiveItem>, IArchive
    {
        public Archive() : this(EarthInfo.WdArchiveHeader)
        {
        }

        private Archive(IEarthInfo header) : this(header, DateTime.Now)
        {
        }

        private Archive(IEarthInfo header, DateTime lastModified) : this(header, lastModified, [])
        {
        }

        public Archive(IEarthInfo header, DateTime lastModified, IEnumerable<IArchiveItem> items) : base(items)
        {
            Header = header;
            LastModified = lastModified;
        }

        public IEarthInfo Header { get; }
        public DateTime LastModified { get; private set; }
        public IReadOnlyCollection<IArchiveItem> Items => this;

        public void AddItem(IArchiveItem item)
        {
            Add(item);
            LastModified = DateTime.Now;
        }

        public void RemoveItem(IArchiveItem item)
        {
            Remove(item);
            LastModified = DateTime.Now;
        }

        public byte[] ToByteArray(ICompressor compressor, Encoding encoding)
        {
            throw new NotImplementedException();
        }
    }
}