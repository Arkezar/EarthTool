#nullable enable

using System;
using System.IO;

namespace EarthTool.CLI.Commands.MSH;

internal interface ICliReportFileSystem
{
  string GetTemporaryPath(string destinationPath);
  Stream CreateTemporary(string temporaryPath);
  void Commit(string temporaryPath, string destinationPath);
  void TryDelete(string temporaryPath);
}

internal sealed class CliReportFileSystem : ICliReportFileSystem
{
  public string GetTemporaryPath(string destinationPath)
  {
    var directory = Path.GetDirectoryName(destinationPath) ?? Directory.GetCurrentDirectory();
    return Path.Combine(
      directory,
      $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");
  }

  public Stream CreateTemporary(string temporaryPath)
  {
    return new FileStream(
      temporaryPath,
      FileMode.CreateNew,
      FileAccess.Write,
      FileShare.None);
  }

  public void Commit(string temporaryPath, string destinationPath)
  {
    File.Move(temporaryPath, destinationPath, true);
  }

  public void TryDelete(string temporaryPath)
  {
    try
    {
      File.Delete(temporaryPath);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
      return;
    }
  }
}
