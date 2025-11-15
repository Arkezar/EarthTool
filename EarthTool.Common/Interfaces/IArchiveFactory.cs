namespace EarthTool.Common.Interfaces
{
  public interface IArchiveFactory
  {
    IArchive OpenArchive(string path);
    IArchive NewArchive();
  }
}