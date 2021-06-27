namespace EarthTool.WD.Resources
{
  public class Terrain : TranslatableResource
  {
    public Terrain(string filename, (uint, uint, uint) fileInfo, string transaltionId, byte[] data) : base(filename, fileInfo, transaltionId, data)
    {
    }
  }
}
