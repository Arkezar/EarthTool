using System;
using System.IO;
using System.Linq;

namespace EarthTool.Common.Validation
{
  /// <summary>
  /// Provides path validation and sanitization utilities
  /// </summary>
  public static class PathValidator
  {
    /// <summary>
    /// Validates and sanitizes file name to prevent path traversal attacks
    /// </summary>
    /// <param name="fileName">The file name to sanitize</param>
    /// <returns>Sanitized file name safe for file system operations</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty</exception>
    public static string SanitizeFileName(string fileName)
    {
      if (string.IsNullOrWhiteSpace(fileName))
      {
        throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
      }

      // Remove path traversal attempts (../, ..\, etc.)
      var pathSegments = fileName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
      var safeSegments = pathSegments.Where(segment => segment != "." && segment != "..").ToArray();

      if (safeSegments.Length == 0)
      {
        throw new ArgumentException($"Invalid file name: {fileName}", nameof(fileName));
      }

      // Rejoin path segments
      return string.Join(Path.DirectorySeparatorChar.ToString(), safeSegments);
    }

    /// <summary>
    /// Ensures directory exists, creates if necessary
    /// </summary>
    /// <param name="path">The directory path to ensure exists</param>
    /// <returns>Full path to the validated directory</returns>
    /// <exception cref="ArgumentException">Thrown when path is null or empty</exception>
    public static string EnsureDirectoryExists(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        throw new ArgumentException("Path cannot be null or empty", nameof(path));
      }

      var fullPath = Path.GetFullPath(path);

      if (!Directory.Exists(fullPath))
      {
        Directory.CreateDirectory(fullPath);
      }

      return fullPath;
    }

    /// <summary>
    /// Validates file exists and is readable
    /// </summary>
    /// <param name="filePath">The file path to validate</param>
    /// <returns>Full path to the validated file</returns>
    /// <exception cref="ArgumentException">Thrown when filePath is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when file does not exist</exception>
    public static string ValidateFileExists(string filePath)
    {
      if (string.IsNullOrWhiteSpace(filePath))
      {
        throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
      }

      var fullPath = Path.GetFullPath(filePath);

      if (!File.Exists(fullPath))
      {
        throw new FileNotFoundException($"File not found: {fullPath}", fullPath);
      }

      return fullPath;
    }

    /// <summary>
    /// Validates and returns safe output file path
    /// </summary>
    /// <param name="outputDirectory">The output directory path</param>
    /// <param name="fileName">The file name from archive</param>
    /// <returns>Safe, validated output file path</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public static string GetSafeOutputPath(string outputDirectory, string fileName)
    {
      var safeDirectory = EnsureDirectoryExists(outputDirectory);
      var safeFileName = SanitizeFileName(fileName);

      var outputPath = Path.Combine(safeDirectory, safeFileName);

      // Ensure the output directory for nested paths exists
      var outputDir = Path.GetDirectoryName(outputPath);
      if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
      {
        Directory.CreateDirectory(outputDir);
      }

      return outputPath;
    }
  }
}
