using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EarthTool.WD.GUI.Converters;

/// <summary>
/// Converts byte count to human-readable format (KB, MB, GB).
/// </summary>
public class BytesToHumanReadableConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not int bytes && value is not long)
      return value?.ToString();

    var byteCount = System.Convert.ToInt64(value);

    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
    double len = byteCount;
    int order = 0;
    while (len >= 1024 && order < sizes.Length - 1)
    {
      order++;
      len = len / 1024;
    }

    return $"{len:0.##} {sizes[order]}";
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
