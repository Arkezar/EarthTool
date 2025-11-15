using System;
using System.Collections.Generic;
using System.Text;
using EarthTool.Common.Interfaces;

namespace EarthTool.WD.Models
{
    public class Archive : SortedSet<IArchiveItem>, IArchive
    {
        public Archive(IEarthInfo header) : this(header, DateTime.Now)
        {
        }

        private Archive(IEarthInfo header, DateTime lastModification) : this(header, lastModification, [])
        {
        }

        public Archive(IEarthInfo header, DateTime lastModification, IEnumerable<IArchiveItem> items) : base(items)
        {
            Header = header;
            LastModification = lastModification;
        }

        public IEarthInfo Header { get; }
        public DateTime LastModification { get; private set; }
        public IReadOnlyCollection<IArchiveItem> Items => this;
        
        public void AddItem(IArchiveItem item)
        {
            Add(item);
            LastModification = DateTime.Now;
        }

        public void RemoveItem(IArchiveItem item)
        {
            Remove(item);
            LastModification = DateTime.Now;
        }

        public byte[] ToByteArray(ICompressor compressor, Encoding encoding)
        {
            throw new NotImplementedException();
        }
    }
}