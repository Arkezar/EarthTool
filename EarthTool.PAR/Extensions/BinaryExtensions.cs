using System.Diagnostics;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Extensions
{
  public static class BinaryExtensions
  {
    /// <summary>
    /// Reference marker value written after entity reference strings in binary format.
    /// </summary>
    public const int ReferenceMarker = -1;

    // Read helper methods
    public static int ReadInteger(this BinaryReader data)
    {
      return data.ReadInt32();
    }

    public static uint ReadUnsignedInteger(this BinaryReader data)
    {
      return data.ReadUInt32();
    }

    public static string ReadParameterString(this BinaryReader data)
    {
      return new string(data.ReadChars(data.ReadInt32()));
    }

    public static string ReadParameterStringRef(this BinaryReader data)
    {
      var text = data.ReadParameterString();
      var marker = data.ReadInt32();
      Debug.Assert(marker == ReferenceMarker, 
        $"Expected reference marker {ReferenceMarker}, but got {marker}. Binary format may be corrupted.");
      return text;
    }

    // Write helper methods
    public static void WriteParameterString(this BinaryWriter writer, string value, Encoding encoding)
    {
      writer.Write(value.Length);
      writer.Write(encoding.GetBytes(value));
    }

    public static void WriteParameterStringRef(this BinaryWriter writer, string value, Encoding encoding)
    {
      writer.WriteParameterString(value, encoding);
      writer.Write(ReferenceMarker);
    }
  }
}
