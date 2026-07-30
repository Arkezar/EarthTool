using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface IMeshFootprint : IBinarySerializable
  {
    /// <summary>Gets box heights by logical box index 0 through 15.</summary>
    ushort[] BoxHeights { get; }

    /// <summary>Gets box flags by the same logical indices as <see cref="BoxHeights"/>.</summary>
    byte[] BoxFlags { get; }

    /// <summary>Gets the four raw 32-bit coverage descriptors in serialized order.</summary>
    uint[] CoverageDescriptors { get; }

    /// <summary>Gets the four raw 64-bit coverage bitmaps in serialized order.</summary>
    ulong[] CoverageBitmaps { get; }
  }
}
