using EarthTool.Common.Enums;
using EarthTool.GUI.Core.ViewModels;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace EarthTool.GUI.WPF.Views
{
  /// <summary>
  /// Interaction logic for WDView.xaml
  /// </summary>
  [MvxWindowPresentation]
  [MvxViewFor(typeof(WdViewModel))]
  public partial class WdView : MvxWindow<WdViewModel>
  {
    public WdView()
    {
      InitializeComponent();
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
      var filter = GetFileFilter();
      var fileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog
      {
        Multiselect = false,
        Filter = filter.Filter,
        FilterIndex = ++filter.Index
      };
      if (fileDialog.ShowDialog() ?? false)
      {
        ViewModel.FilePath = fileDialog.FileName;
      }
    }

    private void ExtractButton_Click(object sender, RoutedEventArgs e)
    {
      var filter = GetFileFilter(Path.GetExtension(ViewModel.SelectedResource.Filename));
      var folderDialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog
      {
        FileName = Path.GetFileName(ViewModel.SelectedResource.Filename),
        Filter = filter.Filter,
        FilterIndex = ++filter.Index
      };
      if (folderDialog.ShowDialog() ?? false)
      {
        ViewModel.Extract(folderDialog.FileName);
      }
    }

    private void ExtractAllButton_Click(object sender, RoutedEventArgs e)
    {
      var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
      if (folderDialog.ShowDialog() ?? false)
      {
        ViewModel.ExtractAll(folderDialog.SelectedPath);
      }
    }

    private (int Index, string Filter) GetFileFilter(string fileExtension = "wd")
    {
      var types = GetFilterMap();
      var filterTypes = types.Select(t => $"{t.Value} (*.{t.Key})|*.{t.Key}");
      var idx = types.Keys.ToList().IndexOf(fileExtension.Trim('.'));
      return (idx, string.Join('|', filterTypes));
    }

    private IDictionary<string, string> GetFilterMap()
    {
      var types = Enum.GetValues<FileType>().OrderBy(v => v.ToString());
      var typesMap = types.ToDictionary(k => k.ToString().ToLower(), v => typeof(FileType).GetField(v.ToString()).GetCustomAttribute<DescriptionAttribute>().Description);
      typesMap.Add("*", "All files");
      return typesMap;
    }
  }
}