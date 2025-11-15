using EarthTool.WD.Interfaces;
using System;

namespace EarthTool.WD.Models;

public class InMemoryArchiveDataSource(byte[] data) : IArchiveDataSource
{
  public ReadOnlyMemory<byte> Data { get; } = data;
}