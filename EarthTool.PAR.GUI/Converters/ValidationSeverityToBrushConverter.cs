using Avalonia.Data.Converters;
using Avalonia.Media;
using EarthTool.PAR.GUI.Models;
using System;
using System.Globalization;

namespace EarthTool.PAR.GUI.Converters;

/// <summary>
/// Converts ValidationSeverity to appropriate Brush color.
/// </summary>
public class ValidationSeverityToBrushConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is ValidationSeverity severity)
    {
      return severity switch
      {
        ValidationSeverity.Error => new SolidColorBrush(Color.Parse("#DC3545")), // Red
        ValidationSeverity.Warning => new SolidColorBrush(Color.Parse("#FF9800")), // Orange
        ValidationSeverity.Info => new SolidColorBrush(Color.Parse("#2196F3")), // Blue
        _ => Brushes.Gray
      };
    }
    return Brushes.Gray;
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
