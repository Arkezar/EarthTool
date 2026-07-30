using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface IMeshHorizontalExtents : IBinarySerializable
  {
    /// <summary>Gets the nonnegative positive-Y magnitude.</summary>
    ushort PositiveY { get; }

    /// <summary>Gets the nonnegative negative-Y magnitude.</summary>
    ushort NegativeY { get; }

    /// <summary>Gets the nonnegative positive-X magnitude.</summary>
    ushort PositiveX { get; }

    /// <summary>Gets the nonnegative negative-X magnitude.</summary>
    ushort NegativeX { get; }
  }
}
