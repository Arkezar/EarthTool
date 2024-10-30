namespace EarthTool.Common.Interfaces
{
  public interface IArchiveFactory
  {
    IArchive OpenArchive(string path);
    IArchive OpenArchive(byte[] data);
    IArchive NewArchive();
  }
}