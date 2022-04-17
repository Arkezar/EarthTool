using EarthTool.Common.Interfaces;
using System;
using System.IO;
using System.Numerics;

namespace EarthTool.MSH.Interfaces
{
  public interface IVector : IEquatable<IVector>, IBinarySerializable
  {
    Vector3 Value { get; }
    float X { get; }
    float Y { get; }
    float Z { get; }
  }
}