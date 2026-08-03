#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.GLTF.Internal
{
  internal readonly struct SafeResourceMatch
  {
    internal string? Path { get; }
    internal string? Root { get; }
    internal bool Ambiguous { get; }
    internal bool Shadowed { get; }

    internal SafeResourceMatch(string? path, string? root, bool ambiguous, bool shadowed)
    {
      Path = path;
      Root = root;
      Ambiguous = ambiguous;
      Shadowed = shadowed;
    }
  }

  internal static class SafeResourceLookup
  {
    internal static SafeResourceMatch Resolve(
      string relativePath,
      IReadOnlyList<string> roots,
      Func<string, IEnumerable<string>> enumerateFileSystemEntries)
    {
      var segments = relativePath.Split(Path.DirectorySeparatorChar);
      string? selected = null;
      string? selectedRoot = null;
      var shadowed = false;
      foreach (var root in roots)
      {
        if (!Directory.Exists(root) || HasLinkInAncestry(root))
        {
          continue;
        }
        var candidates = new[] { root };
        foreach (var segment in segments)
        {
          candidates = candidates.SelectMany(current =>
              Directory.Exists(current)
                ? enumerateFileSystemEntries(current)
                  .Where(path => AsciiEquals(Path.GetFileName(path), segment))
                : Array.Empty<string>())
            .Where(path => !IsLink(path))
            .ToArray();
          if (candidates.Length == 0)
          {
            break;
          }
        }
        var completeMatches = candidates.Where(File.Exists).ToArray();
        if (completeMatches.Length == 0)
        {
          continue;
        }
        if (completeMatches.Length > 1)
        {
          if (selected is null)
          {
            return new SafeResourceMatch(null, null, true, false);
          }
          shadowed = true;
          continue;
        }
        if (selected is null)
        {
          selected = completeMatches[0];
          selectedRoot = root;
        }
        else
        {
          shadowed = true;
        }
      }
      return new SafeResourceMatch(selected, selectedRoot, false, shadowed);
    }

    internal static bool IsSafeContainedPath(string root, string path)
    {
      return !IsLink(path) && !HasLinkInAncestry(path) && IsContainedBy(root, path);
    }

    internal static bool AsciiEquals(string left, string right)
    {
      if (left.Length != right.Length)
      {
        return false;
      }
      for (var index = 0; index < left.Length; index++)
      {
        var a = left[index] is >= 'A' and <= 'Z' ? (char)(left[index] + 32) : left[index];
        var b = right[index] is >= 'A' and <= 'Z' ? (char)(right[index] + 32) : right[index];
        if (a != b)
        {
          return false;
        }
      }
      return true;
    }

    private static bool IsLink(string path)
    {
      return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    }

    private static bool HasLinkInAncestry(string path)
    {
      for (DirectoryInfo? current = new DirectoryInfo(Path.GetFullPath(path));
        current is not null;
        current = current.Parent)
      {
        if (IsLink(current.FullName))
        {
          return true;
        }
      }
      return false;
    }

    private static bool IsContainedBy(string root, string path)
    {
      var rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)
        + Path.DirectorySeparatorChar;
      var comparison = Path.DirectorySeparatorChar == '\\'
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
      return Path.GetFullPath(path).StartsWith(rootPath, comparison);
    }
  }
}
