using EarthTool.Common.Enums;
using MvvmCross.Converters;
using System;
using System.Globalization;
using System.Text;

namespace EarthTool.GUI.Core.Converters
{
  public class FileFlagsValueConverter : MvxValueConverter<FileFlags, string>
  {
    protected override string Convert(FileFlags value, Type targetType, object parameter, CultureInfo culture)
    {
      return new StringBuilder("xx").Append(GetValueForFlag(value, FileFlags.Guid, "G"))
                                    .Append(GetValueForFlag(value, FileFlags.Resource, "R"))
                                    .Append(GetValueForFlag(value, FileFlags.Named, "N"))
                                    .Append(GetValueForFlag(value, FileFlags.Text, "T"))
                                    .Append(GetValueForFlag(value, FileFlags.Archive, "A"))
                                    .Append(GetValueForFlag(value, FileFlags.Compressed, "C"))
                                    .ToString();
    }

    private string GetValueForFlag(FileFlags value, FileFlags flag, string flagValue)
    {
      return value.HasFlag(flag) ? flagValue : "x";
    }
  }
}
