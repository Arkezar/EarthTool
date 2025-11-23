using System;

namespace EarthTool.Common.Interfaces
{
    public interface IArchiveItem : IComparable<IArchiveItem>, IDisposable
    {
        string FileName { get; }
        IEarthInfo Header { get; }
        int CompressedSize { get; }
        int DecompressedSize { get; }
        bool IsCompressed { get; }
        ReadOnlyMemory<byte> Data { get; }
    }
}