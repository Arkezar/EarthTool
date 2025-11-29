using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models.Abstracts
{
  public abstract class ParameterEntry
  {
    /// <summary>
    /// Reference marker value written after entity reference strings in binary format.
    /// </summary>
    protected const int ReferenceMarker = -1;

    public string Name { get; set; }

    // Read helper methods
    protected static int ReadInteger(BinaryReader data)
    {
      return data.ReadInt32();
    }

    protected static uint ReadUnsignedInteger(BinaryReader data)
    {
      return data.ReadUInt32();
    }

    protected static string ReadString(BinaryReader data)
    {
      return new string(data.ReadChars(data.ReadInt32()));
    }

    protected static string ReadStringRef(BinaryReader data)
    {
      var text = ReadString(data);
      var marker = data.ReadInt32();
      Debug.Assert(marker == ReferenceMarker, 
        $"Expected reference marker {ReferenceMarker}, but got {marker}. Binary format may be corrupted.");
      return text;
    }

    // Write helper methods
    protected static void WriteString(BinaryWriter writer, string value, Encoding encoding)
    {
      writer.Write(value.Length);
      writer.Write(encoding.GetBytes(value));
    }

    protected static void WriteStringRef(BinaryWriter writer, string value, Encoding encoding)
    {
      WriteString(writer, value, encoding);
      writer.Write(ReferenceMarker);
    }
  }
}
