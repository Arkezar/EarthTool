namespace EarthTool.WD.Resources
{
  public class Resource
  {
    public string Filename
    {
      get;
    }

    public byte[] UnknownData
    {
      get;
    }

    public bool HasUnknownData
      => UnknownData != null && UnknownData.Length > 0;

    public uint Offset
    {
      get;
    }

    public uint Length
    {
      get;
    }

    public uint DecompressedLength
    {
      get;
    }

    public Resource(string filename, (uint, uint, uint) fileInfo, byte[] data = null)
    {
      Filename = filename;
      Offset = fileInfo.Item1;
      Length = fileInfo.Item2;
      DecompressedLength = fileInfo.Item3;
      UnknownData = data;
    }

    public override string ToString()
    {
      return $"{Filename} Offset: {Offset} Compressed: {Length} Uncompressed: {DecompressedLength}";
    }
  }
}
