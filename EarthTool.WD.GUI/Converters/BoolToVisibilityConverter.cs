using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EarthTool.WD.GUI.Converters;

/// <summary>
/// Converts boolean values to visibility states.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is bool boolValue)
    {
      // Check if invert parameter is specified
      var invert = parameter as string == "Invert";
      var result = invert ? !boolValue : boolValue;
      return result;
    }
    return false;
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
