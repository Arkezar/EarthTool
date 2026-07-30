using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace EarthTool.PAR.GUI.Converters;

/// <summary>
/// Converts group name (class type) to appropriate icon.
/// </summary>
public class GroupNameToIconConverter : IValueConverter
{
  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is string groupName)
    {
      return groupName switch
      {
        // Base classes
        "Parameter Entry" => "📋",
        "Entity" => "🎯",
        "Typed" or "Typed Entity" => "🏷️",

        // Core entity types
        "Interactable" => "🎮",
        "Destructible" => "💥",
        "Equipable" => "🔧",
        "Passive" or "Passive Entity" => "⚪",

        // Specific entity types
        "Vehicle" => "🚗",
        "Building" => "🏭",
        "Unit" => "👤",
        "Equipment" => "⚙️",
        "Missile" => "🚀",
        "Explosion" => "💣",
        "Artifact" => "💎",
        "Mine" => "⛏️",
        "Research" => "🔬",

        // Default
        _ => "📦"
      };
    }
    return "📦";
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
