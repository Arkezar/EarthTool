#nullable enable

using System;
using System.IO;

namespace EarthTool.GLTF.Internal
{
  internal interface ITransactionalFileSystem
  {
    string GetTemporaryPath(string destinationPath);

    Stream CreateTemporary(string temporaryPath);

    void Commit(string temporaryPath, string destinationPath);

    bool TryDelete(string temporaryPath);
  }

  internal sealed class TransactionalFileSystem : ITransactionalFileSystem
  {
    public string GetTemporaryPath(string destinationPath)
    {
      var directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath)) ?? Directory.GetCurrentDirectory();
      var name = Path.GetFileName(destinationPath);
      return Path.Combine(directory, $".{name}.{Guid.NewGuid():N}.tmp");
    }

    public Stream CreateTemporary(string temporaryPath)
    {
      return new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    }

    public void Commit(string temporaryPath, string destinationPath)
    {
      if (File.Exists(destinationPath))
      {
        File.Replace(temporaryPath, destinationPath, null);
      }
      else
      {
        File.Move(temporaryPath, destinationPath);
      }
    }

    public bool TryDelete(string temporaryPath)
    {
      try
      {
        if (File.Exists(temporaryPath))
        {
          File.Delete(temporaryPath);
        }

        return true;
      }
      catch (IOException)
      {
        return false;
      }
      catch (UnauthorizedAccessException)
      {
        return false;
      }
    }
  }
}
