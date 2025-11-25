using Avalonia.Data.Converters;
using EarthTool.Common.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EarthTool.WD.GUI.Converters;

/// <summary>
/// Converts FileFlags enum to a human-readable string.
/// </summary>
public class FileFlagsToStringConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not FileFlags flags)
      return string.Empty;

    if (flags == FileFlags.None)
      return "None";

    var flagList = new List<string>();

    if (flags.HasFlag(FileFlags.Compressed))
      flagList.Add("Compressed");
    if (flags.HasFlag(FileFlags.Archive))
      flagList.Add("Archive");
    if (flags.HasFlag(FileFlags.Text))
      flagList.Add("Text");
    if (flags.HasFlag(FileFlags.Named))
      flagList.Add("Named");
    if (flags.HasFlag(FileFlags.Resource))
      flagList.Add("Resource");
    if (flags.HasFlag(FileFlags.Guid))
      flagList.Add("Guid");

    return string.Join(", ", flagList);
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not string str || string.IsNullOrWhiteSpace(str))
      return FileFlags.None;

    if (str.Equals("None", StringComparison.OrdinalIgnoreCase))
      return FileFlags.None;

    var flags = FileFlags.None;
    var parts = str.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .ToList();

    foreach (var part in parts)
    {
      if (part.Equals("Compressed", StringComparison.OrdinalIgnoreCase))
        flags |= FileFlags.Compressed;
      else if (part.Equals("Archive", StringComparison.OrdinalIgnoreCase))
        flags |= FileFlags.Archive;
      else if (part.Equals("Text", StringComparison.OrdinalIgnoreCase))
        flags |= FileFlags.Text;
      else if (part.Equals("Named", StringComparison.OrdinalIgnoreCase))
        flags |= FileFlags.Named;
      else if (part.Equals("Resource", StringComparison.OrdinalIgnoreCase))
        flags |= FileFlags.Resource;
      else if (part.Equals("Guid", StringComparison.OrdinalIgnoreCase))
        flags |= FileFlags.Guid;
    }

    return flags;
  }
}
