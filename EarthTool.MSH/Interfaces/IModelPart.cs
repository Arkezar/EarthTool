﻿using EarthTool.Common.Interfaces;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface IModelPart : IBinarySerializable
  {
    IAnimations Animations { get; }
    byte Depth { get; }
    IEnumerable<IFace> Faces { get; }
    IVector Offset { get; }
    ITextureInfo Texture { get; }
    byte[] UnknownBytes { get; }
    byte PartType { get; }
    short UnknownFlag2 { get; }
    int UnknownValue { get; }
    IEnumerable<IVertex> Vertices { get; }
  }
}