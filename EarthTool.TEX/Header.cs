namespace EarthTool.TEX
{
  public class Header
  {
    public int Type
    {
      get;
    }

    public int Subtype
    {
      get;
    }

    public int NumberOfMaps
    {
      get;
    }

    public Header(int type, int subtype, int numberOfMaps = 0)
    {
      Type = type;
      Subtype = subtype;
      NumberOfMaps = numberOfMaps;
    }
  }
}
