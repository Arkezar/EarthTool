using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.IO;

namespace EarthTool.TEX.GUI.Converters;

public class FileNameConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is string filePath)
    {
      return Path.GetFileName(filePath);
    }

    return value;
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
