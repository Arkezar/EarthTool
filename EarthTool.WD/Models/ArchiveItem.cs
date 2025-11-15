using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System;

namespace EarthTool.WD.Models;

public class ArchiveItem(string fileName, IEarthInfo header, ReadOnlyMemory<byte> data, int decompressedSize)
    : IArchiveItem
{
    public string FileName { get; } = fileName;
    public IEarthInfo Header { get; } = header;
    public int CompressedSize => data.Length;
    public int DecompressedSize { get; } = decompressedSize;
    public bool IsCompressed => Header.Flags.HasFlag(FileFlags.Compressed);
    public ReadOnlyMemory<byte> Data => data;

    public int CompareTo(IArchiveItem other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return string.Compare(FileName, other.FileName, StringComparison.Ordinal);
    }
}