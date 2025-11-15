using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System;

namespace EarthTool.WD.Models;

public class ArchiveItem : IArchiveItem
{
    private readonly ReadOnlyMemory<byte> _data;

    public ArchiveItem(string fileName, IEarthInfo header, ReadOnlyMemory<byte> data, int decompressedSize)
    {
        FileName = fileName;
        Header = header;
        _data = data;
        DecompressedSize = decompressedSize;
    }
    
    public string FileName { get; }
    public IEarthInfo Header { get; }
    public int CompressedSize => _data.Length;
    public int DecompressedSize { get; }
    public bool IsCompressed => Header.Flags.HasFlag(FileFlags.Compressed);
    public ReadOnlyMemory<byte> Data => _data;

    public int CompareTo(IArchiveItem other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return string.Compare(FileName, other.FileName, StringComparison.Ordinal);
    }
}