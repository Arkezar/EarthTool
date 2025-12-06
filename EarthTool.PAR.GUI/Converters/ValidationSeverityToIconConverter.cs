using Avalonia.Data.Converters;
using EarthTool.PAR.GUI.Models;
using System;
using System.Globalization;

namespace EarthTool.PAR.GUI.Converters;

/// <summary>
/// Converts ValidationSeverity to appropriate icon character.
/// </summary>
public class ValidationSeverityToIconConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is ValidationSeverity severity)
    {
      return severity switch
      {
        ValidationSeverity.Error => "✕",
        ValidationSeverity.Warning => "⚠",
        ValidationSeverity.Info => "ℹ",
        _ => "?"
      };
    }
    return "?";
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
