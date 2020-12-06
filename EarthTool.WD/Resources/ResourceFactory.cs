using System;
using System.IO;
using System.Text;

namespace EarthTool.WD.Resources
{
  public class ResourceFactory
  {
    public Resource Create(Stream stream)
    {
      var name = GetName(stream);

      var type = stream.ReadByte();
      switch (type)
      {
        case 0:
          return new Resource(name, GetResourceInfo(stream));
        case 1: //generic data
          return new Resource(name, GetResourceInfo(stream));
        case 3: //player
          return new Resource(name, GetResourceInfo(stream));
        case 5: //text
          return new Resource(name, GetResourceInfo(stream));
        case 9: //interface?
          return new Interface(name, GetResourceInfo(stream), GetName(stream));
        case 17: //dialog
          return new Resource(name, GetResourceInfo(stream));
        case 25: //interface?
          return new Resource(name, GetResourceInfo(stream));
        case 33:
          return new Mesh(name, GetResourceInfo(stream), GetBytes(stream, 16));
        case 43: //level?
          return new Level(name, GetResourceInfo(stream), GetName(stream), GetBytes(stream, 16));
        case 49: //mesh
          return new Mesh(name, GetResourceInfo(stream), GetBytes(stream, 20));
        case 57:
          return new Terrain(name, GetResourceInfo(stream), GetName(stream), GetBytes(stream, 20));
        case 59: //level
          return new Level(name, GetResourceInfo(stream), GetName(stream), GetBytes(stream, 20));
        case 255: //group
          return new Group(name, (0, 0, 0), GetBytes(stream, 3));
        default:
          throw new Exception("Unhandled resource type " + type);
      }
    }

    private (uint, uint, uint) GetResourceInfo(Stream stream)
    {
      var buffer = new byte[4];
      stream.Read(buffer, 0, 4);
      var offset = BitConverter.ToUInt32(buffer, 0);
      stream.Read(buffer, 0, 4);
      var length = BitConverter.ToUInt32(buffer, 0);
      stream.Read(buffer, 0, 4);
      var decompressedLength = BitConverter.ToUInt32(buffer, 0);
      return (offset, length, decompressedLength);
    }

    private string GetName(Stream stream)
    {
      var length = GetLength(stream);
      var nameByte = new byte[length];
      stream.Read(nameByte, 0, length);
      var name = Encoding.ASCII.GetString(nameByte);
      return name;
    }

    private int GetLength(Stream stream)
    {
      var length = stream.ReadByte();
      if (length == 73)
      {
        var current = stream.Position;

        byte[] idxBuffer = new byte[3];
        stream.Read(idxBuffer, 0, 3);

        if (!(idxBuffer[0] == 68 && idxBuffer[1] == 0))
        {
          stream.Seek(current, SeekOrigin.Begin);
        }
        else
        {
          length = stream.ReadByte();
        }
      }
      return length;
    }

    private byte[] GetBytes(Stream stream, int length)
    {
      var buffer = new byte[length];
      stream.Read(buffer, 0, length);
      return buffer;
    }
  }
}
